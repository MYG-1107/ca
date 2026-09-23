namespace Siter.CertificateGenerator.Models;

public sealed class CertificateBatch
{
    public string Id { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string ConferenceName { get; init; } = string.Empty;
    public string DateText { get; init; } = string.Empty;
    public string? TemplateFileName { get; init; }
    public List<Participant> Participants { get; init; } = [];
}
