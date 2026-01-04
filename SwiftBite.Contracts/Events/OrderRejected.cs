namespace SwiftBite.Contracts.Events;

public interface OrderRejected
{
    Guid OrderId { get; }
    string Reason { get; }
    DateTime RejectedAt { get; }
}
