namespace DTO.DataAccess.Web.DTO;

/// <summary>What the upload API returns about a document; never the storage key or a link.</summary>
public class InvoiceDocumentDto
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = default!;

    public string Status { get; set; } = default!;
}
