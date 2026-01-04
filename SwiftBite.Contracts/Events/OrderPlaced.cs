namespace SwiftBite.Contracts.Events;

public interface OrderPlaced
{
    Guid OrderId { get; }
    Guid CustomerId { get; }
    decimal TotalAmount { get; }
    DateTime PlacedAt { get; }
}
