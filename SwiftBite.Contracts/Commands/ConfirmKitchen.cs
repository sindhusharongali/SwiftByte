namespace SwiftBite.Contracts.Commands;

public interface ConfirmKitchen
{
    Guid OrderId { get; }
    Guid RestaurantId { get; }
}
