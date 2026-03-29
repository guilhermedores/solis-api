namespace SolisApi.Models;

public static class SaleStatuses
{
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Canceled = "canceled";
    public const string Processing = "processing";
}

public static class PaymentStatuses
{
    public const string Unpaid = "unpaid";
    public const string Partial = "partial";
    public const string Paid = "paid";
    public const string Refunded = "refunded";
}

public static class PaymentProcessingStatuses
{
    public const string Pending = "pending";
    public const string Processed = "processed";
    public const string Failed = "failed";
    public const string Reversed = "reversed";
}
