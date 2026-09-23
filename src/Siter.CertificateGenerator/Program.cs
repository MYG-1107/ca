using Microsoft.AspNetCore.Server.IIS;
using QuestPDF.Infrastructure;
using Siter.CertificateGenerator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 25 * 1024 * 1024;
});

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ExcelImportService>();
builder.Services.AddSingleton<BatchStorageService>();
builder.Services.AddSingleton<CertificateGeneratorService>();

QuestPDF.Settings.License = builder.Configuration["QuestPdfLicense"]?.ToLowerInvariant() switch
{
    "community" => LicenseType.Community,
    "professional" => LicenseType.Professional,
    "enterprise" => LicenseType.Enterprise,
    _ => LicenseType.Evaluation
};

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
