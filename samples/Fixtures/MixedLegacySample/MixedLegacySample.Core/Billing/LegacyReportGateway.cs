namespace MixedLegacySample.Core.Billing;

public sealed class LegacyReportGateway
{
    public string ExportToFile(string invoiceMessage)
    {
        return $"FILE::{invoiceMessage}";
    }
}
