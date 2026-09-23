using ClosedXML.Excel;
using Siter.CertificateGenerator.Models;

namespace Siter.CertificateGenerator.Services;

public sealed class ExcelImportService
{
    private static readonly string[] NameHeaders = ["name", "participant name", "full name"];
    private static readonly string[] DesignationHeaders = ["designation", "title", "job title", "role"];
    private static readonly string[] OrganizationHeaders = ["organization", "organisation", "company", "institution"];
    private static readonly string[] EmailHeaders = ["email", "email address"];

    public async Task<List<Participant>> ImportAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        using var workbook = new XLWorkbook(memory);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("The Excel file contains no worksheet.");

        var firstRow = worksheet.FirstRowUsed()
            ?? throw new InvalidDataException("The worksheet is empty.");

        var lastRow = worksheet.LastRowUsed()
            ?? throw new InvalidDataException("The worksheet is empty.");

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in firstRow.CellsUsed())
        {
            var key = Normalize(cell.GetString());
            if (!string.IsNullOrWhiteSpace(key))
                headers[key] = cell.Address.ColumnNumber;
        }

        var nameColumn = Find(headers, NameHeaders);
        if (nameColumn is null)
            throw new InvalidDataException(
                "A Name column was not found. Use 'Name' or 'Participant Name'.");

        var designationColumn = Find(headers, DesignationHeaders);
        var organizationColumn = Find(headers, OrganizationHeaders);
        var emailColumn = Find(headers, EmailHeaders);

        var participants = new List<Participant>();

        for (var row = firstRow.RowNumber() + 1; row <= lastRow.RowNumber(); row++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = Value(worksheet, row, nameColumn.Value);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            participants.Add(new Participant
            {
                RowNumber = row,
                Name = name.Trim(),
                Designation = designationColumn is null ? "" : Value(worksheet, row, designationColumn.Value).Trim(),
                Organization = organizationColumn is null ? "" : Value(worksheet, row, organizationColumn.Value).Trim(),
                Email = emailColumn is null ? "" : Value(worksheet, row, emailColumn.Value).Trim()
            });
        }

        if (participants.Count == 0)
            throw new InvalidDataException("No participant records were found.");

        if (participants.Count > 2000)
            throw new InvalidDataException("The current MVP supports up to 2,000 participants.");

        return participants;
    }

    private static int? Find(Dictionary<string, int> headers, IEnumerable<string> names)
    {
        foreach (var name in names)
            if (headers.TryGetValue(Normalize(name), out var column))
                return column;

        return null;
    }

    private static string Value(IXLWorksheet sheet, int row, int column) =>
        sheet.Cell(row, column).GetFormattedString();

    private static string Normalize(string value) =>
        string.Join(' ', value.Trim().ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
