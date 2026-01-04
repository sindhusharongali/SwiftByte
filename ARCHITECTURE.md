# SwiftBite Architecture & Design Patterns

## Overview

SwiftBite is built on **event-driven microservices architecture** with a saga pattern for distributed transaction orchestration. This document outlines the architectural decisions and design patterns used.

## Design Patterns

### 1. **Saga Pattern (Orchestration)**

Used in OrderService to manage the distributed transaction across multiple services.

**Problem:** How to maintain data consistency across multiple services without distributed transactions?

**Solution:** A saga orchestrator (OrderStateMachine) coordinates the workflow:

```
Order Placed
    ↓
[Saga: WaitingForPayment]
    ↓ (Command) ChargePayment
PaymentService processes payment
    ↓ (Event) PaymentProcessed
[Saga: WaitingForKitchenConfirmation]
    ↓ (Command) ConfirmKitchen
RestaurantService confirms order
    ↓ (Event) KitchenConfirmed
[Saga: Completed]
```

**Implementation:**
- MassTransit StateMachine in OrderService
- In-memory repository (can be upgraded to database)
- Correlation by OrderId
- State transitions based on events

**Compensating Transactions:**
- OrderRejected event triggers Failed state
- Future: Add compensating commands to reverse transactions

### 2. **Event-Driven Architecture**

Services communicate through events rather than direct HTTP calls (except for controlled resilience patterns).

**Benefits:**
- Loose coupling
- Scalability
- Resilience to service failures
- Natural event sourcing readiness

**Event Types:**
- **Domain Events:** `OrderPlaced`, `PaymentProcessed`, `KitchenConfirmed`
- **Domain Commands:** `ChargePayment`, `ConfirmKitchen`

**Message Flow:**
```
Publisher → MassTransit → RabbitMQ → Consumer
```

### 3. **Circuit Breaker Pattern (Resilience)**

Implemented using Polly for OrderService → PaymentService communication.

**States:**
```
Closed (Normal)
├─ Requests pass through
├─ Track failures
└─ 3 failures → Open

Open (Failed)
├─ Fail fast
├─ Requests immediately rejected
└─ 30 seconds → Half-Open

Half-Open (Testing)
├─ Allow one test request
├─ Success → Closed
└─ Failure → Open
```

**Configuration:**
```csharp
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .CircuitBreakerAsync<HttpResponseMessage>(
        handledEventsAllowedBeforeBreaking: 3,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, timespan) => { ... },
        onReset: () => { ... });
```

### 4. **Repository Pattern (Future)**

Current implementation uses in-memory state for sagas. Future versions should implement:

```csharp
public interface ISagaRepository<T> where T : SagaStateMachineInstance
{
    Task<T> GetSagaAsync(Guid correlationId);
    Task SaveSagaAsync(T saga);
    Task DeleteSagaAsync(Guid correlationId);
}
```

With SQL Server implementation using Entity Framework Core.

### 5. **Consumer Pattern**

MassTransit consumers implement `IConsumer<T>` interface:

```csharp
public class ChargePaymentConsumer : IConsumer<ChargePayment>
{
    public async Task Consume(ConsumeContext<ChargePayment> context)
    {
        // Process command
        // Publish events
    }
}
```

**Features:**
- Automatic message deserialization
- Built-in retry logic
- Error handling
- Dependency injection

## Service Communication

### Synchronous (Direct HTTP)

**Used:** OrderService → PaymentService (with circuit breaker)

**Reasons:**
- Controlled, monitored relationship
- Circuit breaker prevents cascading failures
- Explicit error handling
- Request/response pattern needed

### Asynchronous (Event-Driven)

**Used:** OrderService ↔ PaymentService ↔ RestaurantService

**Reasons:**
- Decoupling
- Scalability
- Resilience
- Natural workflow modeling

**Message Broker:** RabbitMQ with MassTransit

**Advantages:**
- Fire-and-forget reliability
- Automatic retries
- Dead letter queues
- Message persistence

## Data Consistency

### Saga Pattern Consistency

The saga maintains eventual consistency:

1. **Order Created** (committed immediately)
2. **Payment Pending** (order state changes)
3. **Payment Processed** (payment state changes)
4. **Kitchen Pending** (confirmation state changes)
5. **Kitchen Confirmed** (final order state)

**Consistency Level:** Eventual - temporary inconsistencies resolved through saga compensation.

### Idempotency

Key concept for distributed systems:

- Each service implements idempotent consumers
- Messages can be reprocessed without duplicate side effects
- Correlation IDs ensure proper tracking

## Technology Decisions

### MassTransit (Event Bus)

**Why MassTransit?**
- Abstraction over message broker
- Rich saga support
- Built-in retry logic
- Excellent .NET ecosystem fit

**Alternative:** NServiceBus, Rebus

### RabbitMQ (Message Broker)

**Why RabbitMQ?**
- Open source
- Proven reliability
- Easy deployment (Docker)
- Good .NET support

**Alternative:** Azure Service Bus, AWS SQS, Kafka

### Redis (Caching)

**Current Use:** Distributed caching capability
**Future Use:** 
- Saga state persistence
- Distributed lock management
- Session state

### Elasticsearch (Search)

**Why Elasticsearch?**
- Full-text search
- Complex filtering
- Faceted search
- Scalable indexing

**Alternative:** Solr, Azure Search, Algolia

## Scalability Considerations

### Horizontal Scaling

```
Multiple instances of same service
          ↓
    RabbitMQ queue distributes messages
          ↓
    Each instance processes in parallel
```

**Configuration needed:**
- Load balancing
- Service discovery
- Shared state repository (replace in-memory)

### Vertical Scaling

```
Increase CPU/Memory per service instance
          ↓
    Handle more concurrent messages
```

**Limits:**
- Single machine capacity
- Cannot exceed single machine resources

### Message Processing

**Current approach:**
- Sequential processing per queue
- Configurable concurrency

**Optimization:**
```csharp
x.ReceiveEndpoint("order-processing", e =>
{
    e.PrefetchCount = 10;  // Process up to 10 messages
    e.ConcurrentMessageLimit = 10;
});
```

## Security Considerations

### Current Implementation (Development)

- RabbitMQ: default credentials
- Elasticsearch: no security enabled
- HTTP: HTTPS enabled (self-signed in dev)
- Redis: no authentication

### Production Recommendations

1. **RabbitMQ**
   ```csharp
   cfg.Host("rabbitmq.example.com", h =>
   {
       h.Username(config["RabbitMQ:Username"]);
       h.Password(config["RabbitMQ:Password"]);
   });
   ```

2. **API Authentication**
   ```csharp
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options => { ... });
   ```

3. **Authorization**
   ```csharp
   [Authorize(Roles = "Admin")]
   [HttpPost("/admin/order")]
   ```

4. **Redis Password**
   ```csharp
   var options = ConfigurationOptions.Parse("redis.example.com:6379");
   options.Password = config["Redis:Password"];
   ```

5. **Elasticsearch TLS**
   ```csharp
   var settings = new ElasticsearchClientSettings(new Uri("https://..."))
       .ClientCertificate(new X509Certificate2(...))
       .ServerCertificateValidationCallback(...);
   ```

## Monitoring & Observability

### Current Implementation

- Console logging in each service
- RabbitMQ Management UI
- Elasticsearch/Kibana for search

### Recommended Additions

1. **Structured Logging**
   ```csharp
   builder.Services.AddSerilog(new LoggerConfiguration()
       .WriteTo.Console(new RenderedCompactJsonFormatter())
       .WriteTo.Elasticsearch(...));
   ```

2. **Distributed Tracing**
   ```csharp
   builder.Services.AddOpenTelemetry()
       .WithTracing(tracing => 
           tracing.AddMassTransitInstrumentation()
                  .AddElasticsearchInstrumentation());
   ```

3. **Metrics**
   ```csharp
   var meterProvider = new MeterProviderBuilder()
       .AddMeter("OrderService")
       .AddView(...)
       .Build();
   ```

4. **Health Checks**
   ```csharp
   builder.Services.AddHealthChecks()
       .AddRabbitMQ()
       .AddRedis()
       .AddElasticsearch();
   ```

## Testing Strategy

### Unit Testing
- Individual consumer logic
- State machine transitions

### Integration Testing
- Service + MassTransit
- Service + database (future)

### End-to-End Testing
- Full order workflow
- All services running

### Chaos Engineering
- Simulate service failures
- Test circuit breaker
- Verify saga compensation

## Deployment Architecture

### Development

```
docker-compose up
dotnet run (each service in separate terminal)
```

### Staging/Production

```
Docker Swarm or Kubernetes
├── OrderService (3 replicas)
├── PaymentService (2 replicas)
├── RestaurantService (2 replicas)
├── SearchService (2 replicas)
├── RabbitMQ (cluster)
├── Redis (cluster)
└── Elasticsearch (cluster)
```

### CI/CD Pipeline

```
Git Push
    ↓
Build Docker images
    ↓
Run tests
    ↓
Push to registry
    ↓
Deploy to staging
    ↓
Smoke tests
    ↓
Deploy to production
```

## Performance Optimization

### Message Processing

**Current:** Sequential, single message at a time

**Optimize:**
```csharp
e.PrefetchCount = 20;
e.ConcurrentMessageLimit = 20;
```

### Elasticsearch Indexing

**Current:** Synchronous indexing per request

**Optimize:**
- Batch indexing
- Async indexing with background jobs
- Index templates for better mapping

### Caching Strategy

**Current:** Redis available but not used for saga state

**Implement:**
- Cache menu items from Elasticsearch
- Cache user profiles
- Cache recent orders

## Cost Optimization

### Infrastructure

- Use managed services (RabbitMQ Cloud, Elastic Cloud, Redis Cloud)
- Auto-scaling based on message queue depth
- Reserved instances for baseline load

### Development

- Use Docker locally for infrastructure
- Shared development environment
- Feature branch preview environments

## Future Enhancements

1. **Dead Letter Queue Handler**
   - Monitor failed messages
   - Automatic retry with backoff
   - Manual intervention interface

2. **Saga Persistence**
   - Save saga state to database
   - Enable long-running sagas
   - Crash recovery

3. **Event Sourcing**
   - Store all state changes as events
   - Rebuild state from event log
   - Complete audit trail

4. **API Gateway**
   - Central entry point
   - Rate limiting
   - Request routing
   - Authentication/Authorization

5. **Service Mesh**
   - Istio or Linkerd
   - Advanced routing
   - Mutual TLS
   - Advanced observability

6. **Additional Services**
   - DeliveryService
   - NotificationService
   - AnalyticsService
   - BillingService

## References

- [MassTransit Documentation](https://masstransit.io/)
- [Saga Pattern](https://microservices.io/patterns/data/saga.html)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [Polly Resilience](https://github.com/App-vNext/Polly)
- [Elasticsearch .NET Client](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/index.html)

---

**Last Updated:** January 3, 2026
