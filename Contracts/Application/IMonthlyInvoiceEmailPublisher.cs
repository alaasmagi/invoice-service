namespace Contracts.Application;

public interface IMonthlyInvoiceEmailPublisher
{
    Task PublishAsync(MonthlyInvoiceEmailContent content, CancellationToken ct = default);
}
