namespace SwiftBite.Contracts.Events;

public interface PaymentProcessed
{
    Guid OrderId { get; }
    Guid PaymentId { get; }
    decimal Amount { get; }
    DateTime ProcessedAt { get; }
}
