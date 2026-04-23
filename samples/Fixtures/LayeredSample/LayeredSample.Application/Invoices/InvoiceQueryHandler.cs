namespace LayeredSample.Application.Invoices;

public sealed class InvoiceQueryHandler
{
    private readonly InvoiceService _invoiceService;

    public InvoiceQueryHandler(InvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public string Handle()
    {
        return _invoiceService.BuildInvoiceSummary();
    }
}
