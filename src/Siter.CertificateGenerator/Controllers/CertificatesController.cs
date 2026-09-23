using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Siter.CertificateGenerator.Services;

namespace Siter.CertificateGenerator.Controllers;

public sealed class CertificatesController : Controller
{
    private readonly BatchStorageService storage;
    private readonly CertificateGeneratorService generator;
    private readonly ILogger<CertificatesController> logger;

    public CertificatesController(
        BatchStorageService storage,
        CertificateGeneratorService generator,
        ILogger<CertificatesController> logger)
    {
        this.storage = storage;
        this.generator = generator;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Batch(string id, CancellationToken cancellationToken)
    {
        var batch = await storage.GetAsync(id, cancellationToken);
        return batch is null ? NotFound() : View(batch);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateAll(
        string id,
        CancellationToken cancellationToken)
    {
        var batch = await storage.GetAsync(id, cancellationToken);
        if (batch is null)
            return NotFound();

        try
        {
            var output = storage.OutputDirectory(batch.Id);
            var template = storage.TemplatePath(batch);

            foreach (var participant in batch.Participants)
            {
                var safeName = Sanitize(participant.Name);
                var path = Path.Combine(output, $"{participant.RowNumber:0000}-{safeName}.pdf");

                await generator.GenerateAsync(
                    participant, batch, path, template, cancellationToken);
            }

            var zip = Path.Combine(
                output, $"{batch.Id}-certificates.zip");

            if (System.IO.File.Exists(zip))
                System.IO.File.Delete(zip);

            ZipFile.CreateFromDirectory(
                output, zip, CompressionLevel.Fastest, false);

            return File(
                await System.IO.File.ReadAllBytesAsync(zip, cancellationToken),
                "application/zip",
                $"SITER-Certificates-{DateTime.Now:yyyyMMdd}.zip");
        }
        catch (OperationCanceledException)
        {
            return BadRequest("Generation cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Certificate generation failed.");
            return StatusCode(500, "Certificate generation failed.");
        }
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string(value.Select(c =>
            invalid.Contains(c) ? '_' : c).ToArray());

        return string.IsNullOrWhiteSpace(result) ? "Participant" : result.Trim();
    }
}
