using LayeredSample.Application.Invoices;

namespace LayeredSample.Api.Endpoints;

public sealed class OrdersController
{
    private readonly InvoiceQueryHandler _queryHandler;

    public OrdersController(InvoiceService invoiceService)
    {
        _queryHandler = new InvoiceQueryHandler(invoiceService);
    }

    public string GetOpenInvoiceSummary()
    {
        return _queryHandler.Handle();
    }
}
