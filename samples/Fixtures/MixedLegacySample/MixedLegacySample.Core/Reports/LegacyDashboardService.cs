using MixedLegacySample.Core.Billing;

namespace MixedLegacySample.Core.Reports;

public sealed class LegacyDashboardService
{
    private readonly LegacyInvoiceRepositoryService _invoiceRepositoryService;
    private readonly LegacyReportGateway _reportGateway;

    public LegacyDashboardService(
        LegacyInvoiceRepositoryService invoiceRepositoryService,
        LegacyReportGateway reportGateway)
    {
        _invoiceRepositoryService = invoiceRepositoryService;
        _reportGateway = reportGateway;
    }

    public string BuildReport(string customerName)
    {
        string invoiceMessage = _invoiceRepositoryService.SaveInvoice(customerName);
        return _reportGateway.ExportToFile(invoiceMessage);
    }
}
