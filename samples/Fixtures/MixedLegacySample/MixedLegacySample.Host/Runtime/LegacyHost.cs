using MixedLegacySample.Core.Billing;
using MixedLegacySample.Core.Reports;
using MixedLegacySample.Shared.Time;

namespace MixedLegacySample.Host.Runtime;

public sealed class LegacyHost
{
    private readonly LegacyDashboardService _dashboardService;

    public LegacyHost(ConsoleMessageBus messageBus, SystemClock clock)
    {
        var repositoryService = new LegacyInvoiceRepositoryService(messageBus, clock);
        _dashboardService = new LegacyDashboardService(repositoryService, new LegacyReportGateway());
    }

    public string Run()
    {
        return _dashboardService.BuildReport("Contoso");
    }
}
