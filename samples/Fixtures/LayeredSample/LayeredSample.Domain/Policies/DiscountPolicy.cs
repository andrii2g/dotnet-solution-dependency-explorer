namespace LayeredSample.Domain.Policies;

public sealed class DiscountPolicy
{
    public bool CanDiscount(decimal totalAmount)
    {
        return totalAmount >= 100m;
    }
}
