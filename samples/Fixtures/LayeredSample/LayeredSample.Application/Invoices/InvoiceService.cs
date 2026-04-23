using LayeredSample.Application.Abstractions;
using LayeredSample.Domain.Orders;
using LayeredSample.Domain.Policies;

namespace LayeredSample.Application.Invoices;

public sealed class InvoiceService
{
    private readonly IOrderRepository _repository;
    private readonly DiscountPolicy _discountPolicy = new();

    public InvoiceService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public string BuildInvoiceSummary()
    {
        Order order = _repository.GetCurrentOrder();
        bool discounted = order.QualifiesForDiscount(_discountPolicy);
        return discounted
            ? $"{order.CustomerName} has a discounted invoice."
            : $"{order.CustomerName} has a standard invoice.";
    }
}
