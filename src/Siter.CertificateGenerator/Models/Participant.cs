namespace Siter.CertificateGenerator.Models;

public sealed class Participant
{
    public int RowNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public string Organization { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string CertificateNumber { get; init; } = string.Empty;
}
