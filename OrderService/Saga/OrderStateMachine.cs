namespace OrderService.Saga;

using MassTransit;
using SwiftBite.Contracts.Events;
using SwiftBite.Contracts.Commands;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = nameof(OrderStateMachine.WaitingForPayment);
    
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid RestaurantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaymentProcessedAt { get; set; }
    public DateTime? KitchenConfirmedAt { get; set; }
}

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State WaitingForPayment { get; private set; }
    public State WaitingForKitchenConfirmation { get; private set; }
    public State Completed { get; private set; }
    public State Failed { get; private set; }

    public Event<OrderPlaced> OrderPlacedEvent { get; private set; }
    public Event<PaymentProcessed> PaymentProcessedEvent { get; private set; }
    public Event<KitchenConfirmed> KitchenConfirmedEvent { get; private set; }
    public Event<OrderRejected> OrderRejectedEvent { get; private set; }

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderPlacedEvent, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentProcessedEvent, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => KitchenConfirmedEvent, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderRejectedEvent, x => x.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(OrderPlacedEvent)
                .Then(context =>
                {
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.TotalAmount = context.Message.TotalAmount;
                    context.Saga.CreatedAt = context.Message.PlacedAt;
                })
                .Send(new Uri("queue:payment-service"), context =>
                    new
                    {
                        OrderId = context.Saga.OrderId,
                        CustomerId = context.Saga.CustomerId,
                        Amount = context.Saga.TotalAmount
                    } as ChargePayment)
                .TransitionTo(WaitingForPayment));

        During(WaitingForPayment,
            When(PaymentProcessedEvent)
                .Then(context =>
                {
                    context.Saga.PaymentProcessedAt = context.Message.ProcessedAt;
                })
                .Send(new Uri("queue:restaurant-service"), context =>
                    new
                    {
                        OrderId = context.Saga.OrderId,
                        RestaurantId = Guid.NewGuid()
                    } as ConfirmKitchen)
                .TransitionTo(WaitingForKitchenConfirmation),
            When(OrderRejectedEvent)
                .TransitionTo(Failed));

        During(WaitingForKitchenConfirmation,
            When(KitchenConfirmedEvent)
                .Then(context =>
                {
                    context.Saga.KitchenConfirmedAt = context.Message.ConfirmedAt;
                    context.Saga.RestaurantId = context.Message.KitchenId;
                })
                .TransitionTo(Completed),
            When(OrderRejectedEvent)
                .TransitionTo(Failed));
    }
}
