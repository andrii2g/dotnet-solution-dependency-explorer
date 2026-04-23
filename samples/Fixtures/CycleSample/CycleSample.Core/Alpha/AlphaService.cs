using CycleSample.Core.Beta;

namespace CycleSample.Core.Alpha;

public sealed class AlphaService
{
    private readonly BetaPolicy _policy;

    public AlphaService(BetaPolicy policy)
    {
        _policy = policy;
    }

    public string Describe()
    {
        return _policy.Describe();
    }
}
