namespace RestaurantService.Consumers;

using MassTransit;
using SwiftBite.Contracts.Commands;
using SwiftBite.Contracts.Events;

public class ConfirmKitchenConsumer : IConsumer<ConfirmKitchen>
{
    private readonly ILogger<ConfirmKitchenConsumer> _logger;

    public ConfirmKitchenConsumer(ILogger<ConfirmKitchenConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConfirmKitchen> context)
    {
        _logger.LogInformation("Kitchen confirming order {OrderId} for restaurant {RestaurantId}", 
            context.Message.OrderId, context.Message.RestaurantId);

        // Simulate kitchen confirmation logic with a delay
        await Task.Delay(1500);

        var estimatedPrepTime = TimeSpan.FromMinutes(Random.Shared.Next(15, 45));

        _logger.LogInformation("Order {OrderId} confirmed. Estimated prep time: {EstimatedTime}", 
            context.Message.OrderId, estimatedPrepTime);

        // Publish KitchenConfirmed event
        await context.Publish<KitchenConfirmed>(new
        {
            OrderId = context.Message.OrderId,
            KitchenId = context.Message.RestaurantId,
            ConfirmedAt = DateTime.UtcNow,
            EstimatedPrepTime = estimatedPrepTime
        });
    }
}
