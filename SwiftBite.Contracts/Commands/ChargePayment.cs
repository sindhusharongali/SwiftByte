namespace SwiftBite.Contracts.Commands;

public interface ChargePayment
{
    Guid OrderId { get; }
    Guid CustomerId { get; }
    decimal Amount { get; }
}
