using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.DataAccess.DTO;

namespace DTO.DataAccess.DataAccess.Mapper;

public class InvoiceDocumentEntityMapper : IMapper<InvoiceDocument, InvoiceDocumentEntity>
{
    private readonly InvoiceDocumentRowEntityMapper _rowMapper = new();

    public InvoiceDocument? Map(InvoiceDocumentEntity? entity)
    {
        return entity == null ? null : new InvoiceDocument
        {
            Id = entity.Id,
            UserId = entity.UserId,
            StorageKey = entity.StorageKey,
            FileName = entity.FileName,
            SizeBytes = entity.SizeBytes,
            Sha256 = entity.Sha256,
            Status = entity.Status,
            FailureReason = entity.FailureReason,
            UploadedAt = entity.UploadedAt,
            ParseRequestedAt = entity.ParseRequestedAt,
            ParseExpiresAt = entity.ParseExpiresAt,
            ServiceName = entity.ServiceName,
            Rows = entity.Rows.Select(row => _rowMapper.Map(row)!).OrderBy(row => row.Position).ToList()
        };
    }

    public IEnumerable<InvoiceDocument>? Map(IEnumerable<InvoiceDocumentEntity>? entities)
    {
        return entities?.Select(Map)!;
    }

    // Rows are written through their own repository, so the document is mapped without them.
    public InvoiceDocumentEntity? Map(InvoiceDocument? entity)
    {
        return entity == null ? null : new InvoiceDocumentEntity
        {
            Id = entity.Id,
            UserId = entity.UserId,
            StorageKey = entity.StorageKey,
            FileName = entity.FileName,
            SizeBytes = entity.SizeBytes,
            Sha256 = entity.Sha256,
            Status = entity.Status,
            FailureReason = entity.FailureReason,
            UploadedAt = entity.UploadedAt,
            ParseRequestedAt = entity.ParseRequestedAt,
            ParseExpiresAt = entity.ParseExpiresAt,
            ServiceName = entity.ServiceName
        };
    }

    public IEnumerable<InvoiceDocumentEntity>? Map(IEnumerable<InvoiceDocument>? entities)
    {
        return entities?.Select(Map)!;
    }
}
