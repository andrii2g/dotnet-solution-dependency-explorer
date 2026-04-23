using CycleSample.Core.Alpha;

namespace CycleSample.Core.Gamma;

public sealed class GammaGateway
{
    private readonly AlphaService _service;

    public GammaGateway(AlphaService service)
    {
        _service = service;
    }

    public string Describe()
    {
        return _service.GetType().Name;
    }
}
