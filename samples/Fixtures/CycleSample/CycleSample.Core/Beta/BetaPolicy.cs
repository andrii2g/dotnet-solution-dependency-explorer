using CycleSample.Core.Gamma;

namespace CycleSample.Core.Beta;

public sealed class BetaPolicy
{
    private readonly GammaGateway _gateway;

    public BetaPolicy(GammaGateway gateway)
    {
        _gateway = gateway;
    }

    public string Describe()
    {
        return _gateway.Describe();
    }
}
