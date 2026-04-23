using CycleSample.Core.Alpha;
using CycleSample.Core.Beta;
using CycleSample.Core.Gamma;

namespace CycleSample.Host;

internal static class Program
{
    private static void Main()
    {
        var alpha = new AlphaService(new BetaPolicy(new GammaGateway(null!)));
        Console.WriteLine(alpha.Describe());
    }
}
