using Microsoft.AspNetCore.Mvc;
using Siter.CertificateGenerator.Services;

namespace Siter.CertificateGenerator.Controllers;

public sealed class HomeController : Controller
{
    private readonly ExcelImportService excel;
    private readonly BatchStorageService storage;
    private readonly ILogger<HomeController> logger;

    public HomeController(
        ExcelImportService excel,
        BatchStorageService storage,
        ILogger<HomeController> logger)
    {
        this.excel = excel;
        this.storage = storage;
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> CreateBatch(
        IFormFile excelFile,
        IFormFile? templateFile,
        string conferenceName,
        string dateText,
        CancellationToken cancellationToken)
    {
        try
        {
            if (excelFile is null || excelFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select an Excel file.");
                return View("Index");
            }

            var extension = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (extension is not ".xlsx" and not ".xlsm")
            {
                ModelState.AddModelError("", "Excel file must be .xlsx or .xlsm.");
                return View("Index");
            }

            await using var stream = excelFile.OpenReadStream();
            var participants = await excel.ImportAsync(stream, cancellationToken);

            var batch = await storage.CreateAsync(
                participants, conferenceName, dateText, templateFile, cancellationToken);

            TempData["Success"] = $"Imported {batch.Participants.Count} participants.";
            return RedirectToAction("Batch", "Certificates", new { id = batch.Id });
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Invalid certificate batch input.");
            ModelState.AddModelError("", ex.Message);
            return View("Index");
        }
    }
}
