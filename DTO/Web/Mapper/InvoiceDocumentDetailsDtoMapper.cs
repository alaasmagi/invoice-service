using Base.Contracts.DTO;
using Domain;
using DTO.DataAccess.Web.DTO;

namespace DTO.DataAccess.Web.Mapper;

public class InvoiceDocumentDetailsDtoMapper(TimeProvider timeProvider) : IMapper<InvoiceDocumentDetailsDto, InvoiceDocument>
{
    private readonly InvoiceDocumentRowDtoMapper _rowMapper = new();

    public InvoiceDocumentDetailsDto? Map(InvoiceDocument? entity)
    {
        if (entity == null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return new InvoiceDocumentDetailsDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            SizeBytes = entity.SizeBytes,
            Status = entity.Status,
            IsTimedOut = entity.IsParseTimedOut(now),
            CanRetry = entity.CanRetry(now),
            FailureReason = entity.FailureReason,
            UploadedAt = entity.UploadedAt,
            ParseRequestedAt = entity.ParseRequestedAt,
            ServiceName = entity.ServiceName,
            Rows = entity.Rows.Select(row => _rowMapper.Map(row)!).ToList()
        };
    }

    public IEnumerable<InvoiceDocumentDetailsDto>? Map(IEnumerable<InvoiceDocument>? entities)
    {
        return entities?.Select(Map)!;
    }

    public InvoiceDocument? Map(InvoiceDocumentDetailsDto? entity)
    {
        return entity == null ? null : new InvoiceDocument
        {
            Id = entity.Id,
            FileName = entity.FileName,
            SizeBytes = entity.SizeBytes,
            Status = entity.Status,
            FailureReason = entity.FailureReason,
            UploadedAt = entity.UploadedAt,
            ParseRequestedAt = entity.ParseRequestedAt,
            ServiceName = entity.ServiceName,
            Rows = entity.Rows.Select(row => _rowMapper.Map(row)!).ToList()
        };
    }

    public IEnumerable<InvoiceDocument>? Map(IEnumerable<InvoiceDocumentDetailsDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}

public class InvoiceDocumentRowDtoMapper : IMapper<InvoiceDocumentRowDto, InvoiceDocumentRow>
{
    public InvoiceDocumentRowDto? Map(InvoiceDocumentRow? entity)
    {
        return entity == null ? null : new InvoiceDocumentRowDto
        {
            Id = entity.Id,
            Position = entity.Position,
            AddressText = entity.AddressText,
            Amount = entity.Amount,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            Outcome = entity.Outcome,
            Reason = entity.Reason,
            InvoiceId = entity.InvoiceId
        };
    }

    public IEnumerable<InvoiceDocumentRowDto>? Map(IEnumerable<InvoiceDocumentRow>? entities)
    {
        return entities?.Select(Map)!;
    }

    public InvoiceDocumentRow? Map(InvoiceDocumentRowDto? entity)
    {
        return entity == null ? null : new InvoiceDocumentRow
        {
            Id = entity.Id,
            Position = entity.Position,
            AddressText = entity.AddressText,
            Amount = entity.Amount,
            InvoiceDate = entity.InvoiceDate,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            Outcome = entity.Outcome,
            Reason = entity.Reason,
            InvoiceId = entity.InvoiceId
        };
    }

    public IEnumerable<InvoiceDocumentRow>? Map(IEnumerable<InvoiceDocumentRowDto>? entities)
    {
        return entities?.Select(Map)!;
    }
}
