using LayeredSample.Domain.Orders;

namespace LayeredSample.Application.Abstractions;

public interface IOrderRepository
{
    Order GetCurrentOrder();
}
