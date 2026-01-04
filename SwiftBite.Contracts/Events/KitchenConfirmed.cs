namespace SwiftBite.Contracts.Events;

public interface KitchenConfirmed
{
    Guid OrderId { get; }
    Guid KitchenId { get; }
    DateTime ConfirmedAt { get; }
    TimeSpan EstimatedPrepTime { get; }
}
