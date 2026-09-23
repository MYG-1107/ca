using System.Text.Json;
using Siter.CertificateGenerator.Models;

namespace Siter.CertificateGenerator.Services;

public sealed class BatchStorageService
{
    private readonly string root;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public BatchStorageService(IHostEnvironment environment)
    {
        root = Path.Combine(environment.ContentRootPath, "Data", "Batches");
        Directory.CreateDirectory(root);
    }

    public async Task<CertificateBatch> CreateAsync(
        IReadOnlyCollection<Participant> participants,
        string conferenceName,
        string dateText,
        IFormFile? templateFile,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var folder = Path.Combine(root, id);
        Directory.CreateDirectory(folder);

        string? templateName = null;

        if (templateFile is { Length: > 0 })
        {
            var extension = Path.GetExtension(templateFile.FileName).ToLowerInvariant();

            if (extension is not ".png" and not ".jpg" and not ".jpeg")
                throw new InvalidDataException("Certificate template must be PNG or JPEG.");

            templateName = "template" + extension;

            await using var output = File.Create(Path.Combine(folder, templateName));
            await templateFile.CopyToAsync(output, cancellationToken);
        }

        var batch = new CertificateBatch
        {
            Id = id,
            CreatedAtUtc = DateTime.UtcNow,
            ConferenceName = string.IsNullOrWhiteSpace(conferenceName)
                ? "SITER Academy Conference"
                : conferenceName.Trim(),
            DateText = string.IsNullOrWhiteSpace(dateText)
                ? DateTime.Now.ToString("dd MMMM yyyy")
                : dateText.Trim(),
            TemplateFileName = templateName,
            Participants = participants.ToList()
        };

        await using var json = File.Create(Path.Combine(folder, "batch.json"));
        await JsonSerializer.SerializeAsync(json, batch, JsonOptions, cancellationToken);

        return batch;
    }

    public async Task<CertificateBatch?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParseExact(id, "N", out _))
            return null;

        var path = Path.Combine(root, id, "batch.json");
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<CertificateBatch>(
            stream, JsonOptions, cancellationToken);
    }

    public string? TemplatePath(CertificateBatch batch)
    {
        if (string.IsNullOrWhiteSpace(batch.TemplateFileName))
            return null;

        var safeName = Path.GetFileName(batch.TemplateFileName);
        if (safeName != batch.TemplateFileName)
            return null;

        var path = Path.Combine(root, batch.Id, safeName);
        return File.Exists(path) ? path : null;
    }

    public string OutputDirectory(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _))
            throw new ArgumentException("Invalid batch id.", nameof(id));

        var path = Path.Combine(root, id, "generated");
        Directory.CreateDirectory(path);
        return path;
    }
}
