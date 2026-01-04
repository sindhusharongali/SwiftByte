namespace PaymentService.Consumers;

using MassTransit;
using SwiftBite.Contracts.Commands;
using SwiftBite.Contracts.Events;

public class ChargePaymentConsumer : IConsumer<ChargePayment>
{
    private readonly ILogger<ChargePaymentConsumer> _logger;

    public ChargePaymentConsumer(ILogger<ChargePaymentConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ChargePayment> context)
    {
        _logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}", 
            context.Message.OrderId, context.Message.Amount);

        // Simulate payment processing with a delay
        await Task.Delay(2000);

        _logger.LogInformation("Payment processed successfully for Order {OrderId}", 
            context.Message.OrderId);

        // Publish PaymentProcessed event
        await context.Publish<PaymentProcessed>(new
        {
            OrderId = context.Message.OrderId,
            PaymentId = Guid.NewGuid(),
            Amount = context.Message.Amount,
            ProcessedAt = DateTime.UtcNow
        });
    }
}
