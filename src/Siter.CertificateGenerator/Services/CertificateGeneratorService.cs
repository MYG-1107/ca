using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Siter.CertificateGenerator.Models;

namespace Siter.CertificateGenerator.Services;

public sealed class CertificateGeneratorService
{
    public Task GenerateAsync(
        Participant participant,
        CertificateBatch batch,
        string outputPath,
        string? templatePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[]? background = File.Exists(templatePath ?? "")
            ? File.ReadAllBytes(templatePath!)
            : null;

        var number = string.IsNullOrWhiteSpace(participant.CertificateNumber)
            ? $"SIT-{participant.RowNumber:0000}"
            : participant.CertificateNumber;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0);

                if (background is not null)
                    page.Background().Image(background).FitArea();

                page.Content()
                    .Padding(45)
                    .AlignCenter()
                    .AlignMiddle()
                    .Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Text(batch.ConferenceName)
                            .FontSize(24).SemiBold().AlignCenter();

                        column.Item().Text("CERTIFICATE OF PARTICIPATION")
                            .FontSize(30).Bold().AlignCenter();

                        column.Item().Text("This certificate is proudly presented to")
                            .FontSize(14).AlignCenter();

                        column.Item().Text(participant.Name)
                            .FontSize(34).Bold().AlignCenter();

                        if (!string.IsNullOrWhiteSpace(participant.Designation))
                            column.Item().Text(participant.Designation)
                                .FontSize(18).Italic().AlignCenter();

                        if (!string.IsNullOrWhiteSpace(participant.Organization))
                            column.Item().Text(participant.Organization)
                                .FontSize(16).AlignCenter();

                        column.Item().Text($"Issued: {batch.DateText}")
                            .FontSize(12).AlignCenter();

                        column.Item().Text($"Certificate No: {number}")
                            .FontSize(10).FontColor(Colors.Grey.Darken2).AlignCenter();
                    });
            });
        });

        document.GeneratePdf(outputPath);
        return Task.CompletedTask;
    }
}
