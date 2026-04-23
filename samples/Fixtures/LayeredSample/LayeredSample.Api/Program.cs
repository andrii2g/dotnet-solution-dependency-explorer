using LayeredSample.Api.Endpoints;
using LayeredSample.Application.Invoices;
using LayeredSample.Infrastructure.Data;

var repository = new SqlOrderRepository();
var service = new InvoiceService(repository);
var controller = new OrdersController(service);

Console.WriteLine(controller.GetOpenInvoiceSummary());
