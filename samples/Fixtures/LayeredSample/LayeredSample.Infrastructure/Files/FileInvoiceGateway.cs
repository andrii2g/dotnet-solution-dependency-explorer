namespace LayeredSample.Infrastructure.Files;

public sealed class FileInvoiceGateway
{
    public string Export(string invoiceSummary)
    {
        return $"FILE::{invoiceSummary}";
    }
}
