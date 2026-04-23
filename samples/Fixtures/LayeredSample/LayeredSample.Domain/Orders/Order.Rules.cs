namespace LayeredSample.Domain.Orders;

public partial class Order
{
    public bool QualifiesForDiscount(Policies.DiscountPolicy policy)
    {
        return policy.CanDiscount(TotalAmount);
    }
}
