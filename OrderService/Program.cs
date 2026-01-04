using MassTransit;
using StackExchange.Redis;
using Polly;
using Polly.CircuitBreaker;
using OrderService.Saga;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Configure Redis
var redisConnection = ConnectionMultiplexer.Connect("localhost:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// Configure MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .InMemoryRepository();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Configure Polly Circuit Breaker for resilience
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .CircuitBreakerAsync<HttpResponseMessage>(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, timespan) =>
        {
            Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds} seconds");
        },
        onReset: () =>
        {
            Console.WriteLine("Circuit breaker reset");
        });

builder.Services.AddHttpClient("PaymentService")
    .AddPolicyHandler(circuitBreakerPolicy);

builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>(provider =>
    new PaymentServiceClient(
        provider.GetRequiredService<IHttpClientFactory>().CreateClient("PaymentService"),
        provider.GetRequiredService<ILogger<PaymentServiceClient>>())
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

