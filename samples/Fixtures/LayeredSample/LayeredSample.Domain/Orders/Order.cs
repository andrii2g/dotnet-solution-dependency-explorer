namespace LayeredSample.Domain.Orders;

public partial class Order
{
    public Order(string customerName, decimal totalAmount)
    {
        CustomerName = customerName;
        TotalAmount = totalAmount;
    }

    public string CustomerName { get; }

    public decimal TotalAmount { get; }
}
