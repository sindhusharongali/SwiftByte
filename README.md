# SwiftBite - Food Delivery Microservices

A comprehensive C# .NET microservices solution for a food delivery application, implementing event-driven architecture with MassTransit, saga pattern orchestration, and distributed resilience patterns.

## Architecture Overview

The solution implements a distributed order management system with the following services:

```
┌─────────────────┐
│   OrderService  │  (Order Orchestration & Saga)
└────────┬────────┘
         │ Publishes: OrderPlaced
         │ Sends: ChargePayment → PaymentService
         │ Sends: ConfirmKitchen → RestaurantService
         │
    ┌────┴────┐
    │          │
    ▼          ▼
┌──────────────┐    ┌─────────────────┐
│PaymentService│    │RestaurantService│
└──────┬───────┘    └────────┬────────┘
       │                     │
  Publishes:            Publishes:
  PaymentProcessed      KitchenConfirmed
       │                     │
       └──────────┬──────────┘
                  ▼
            ┌───────────────┐
            │SearchService  │ (Elasticsearch)
            │ProductSearch  │
            └───────────────┘
```

## Technology Stack

- **.NET 8.0** - Latest LTS framework
- **MassTransit 8.1.2** - Event bus with RabbitMQ transport
- **RabbitMQ 3** - Message broker
- **Redis 7** - Distributed caching and state management
- **Elasticsearch 8.10** - Full-text search engine
- **Polly 8.2** - Resilience and transient fault handling
- **Swagger/OpenAPI** - API documentation

## Project Structure

### SwiftBite.Contracts
Shared event and command contracts used across all services.

**Events:**
- `OrderPlaced` - Triggered when a new order is created
- `PaymentProcessed` - Emitted after successful payment
- `KitchenConfirmed` - Published when kitchen accepts the order
- `OrderRejected` - Published when order processing fails

**Commands:**
- `ChargePayment` - Command to process payment
- `ConfirmKitchen` - Command to confirm order with kitchen

### OrderService
Core orchestration service implementing the saga pattern.

**Key Components:**
- `OrderStateMachine` - MassTransit state machine handling order workflow
- `OrderState` - Saga state persistence
- `OrderController` - API endpoints for order placement
- `PaymentServiceClient` - HTTP client with circuit breaker for resilient payment calls

**Flow:**
1. Receives `OrderPlaced` event
2. Transitions to `WaitingForPayment` state
3. Sends `ChargePayment` command to PaymentService
4. Upon `PaymentProcessed` event, sends `ConfirmKitchen` to RestaurantService
5. Upon `KitchenConfirmed` event, transitions to `Completed` state

**Resilience:**
- Polly Circuit Breaker with 3 failures before breaking
- 30-second break duration
- Applied to PaymentService HTTP calls

### PaymentService
Handles payment processing for orders.

**Consumer:**
- `ChargePaymentConsumer` - Listens for `ChargePayment` commands
- Simulates payment processing (2-second delay)
- Publishes `PaymentProcessed` event

**Endpoint:** `queue:payment-service`

### RestaurantService
Manages kitchen order confirmation.

**Consumer:**
- `ConfirmKitchenConsumer` - Listens for `ConfirmKitchen` commands
- Simulates kitchen confirmation (1.5-second delay)
- Generates random estimated prep time (15-45 minutes)
- Publishes `KitchenConfirmed` event

**Endpoint:** `queue:restaurant-service`

### SearchService
Provides menu item search functionality via Elasticsearch.

**Controller:**
- `ProductController` - RESTful API for search operations

**Endpoints:**
- `GET /api/product/search?query=pizza&category=appetizers` - Search menu items
- `GET /api/product/{id}` - Get specific menu item
- `POST /api/product/index` - Index new menu item to Elasticsearch

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Docker & Docker Compose
- Visual Studio 2022 or VS Code

### Installation

1. **Clone/Open the project:**
```bash
cd d:\SwiftByte
```

2. **Start infrastructure services:**
```bash
docker-compose up -d
```

This starts:
- RabbitMQ (ports 5672, 15672)
- Redis (port 6379)
- Elasticsearch (port 9200)
- Kibana (port 5601)

3. **Restore dependencies:**
```bash
dotnet restore
```

4. **Build solution:**
```bash
dotnet build
```

### Running the Services

Each service runs independently. You can run them in separate terminals:

**Terminal 1 - OrderService:**
```bash
cd OrderService
dotnet run
# Runs on https://localhost:7001 and http://localhost:5001
```

**Terminal 2 - PaymentService:**
```bash
cd PaymentService
dotnet run
# Runs on https://localhost:7002 and http://localhost:5002
```

**Terminal 3 - RestaurantService:**
```bash
cd RestaurantService
dotnet run
# Runs on https://localhost:7003 and http://localhost:5003
```

**Terminal 4 - SearchService:**
```bash
cd SearchService
dotnet run
# Runs on https://localhost:7004 and http://localhost:5004
```

## API Usage

### 1. Place an Order

```bash
curl -X POST https://localhost:7001/api/order/place-order \
  -H "Content-Type: application/json" \
  -d '{"customerId": "550e8400-e29b-41d4-a716-446655440000", "totalAmount": 45.99}'
```

Response:
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
}
```

### 2. Search Menu Items

```bash
curl https://localhost:7004/api/product/search?query=pasta&category=main \
  -H "Accept: application/json"
```

Response:
```json
{
  "total": 5,
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Spaghetti Carbonara",
      "description": "Classic Italian pasta with bacon and cream",
      "price": 12.99,
      "category": "main",
      "available": true,
      "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "createdAt": "2026-01-03T10:00:00Z"
    }
  ],
  "query": "pasta"
}
```

### 3. Index a Menu Item

```bash
curl -X POST https://localhost:7004/api/product/index \
  -H "Content-Type: application/json" \
  -d '{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Margherita Pizza",
    "description": "Fresh mozzarella and basil pizza",
    "price": 14.99,
    "category": "main",
    "available": true,
    "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "createdAt": "2026-01-03T10:00:00Z"
  }'
```

## Service Communication Flow

### Complete Order Workflow

```
1. Client calls POST /api/order/place-order
   ↓
2. OrderService publishes OrderPlaced event
   ↓
3. OrderStateMachine receives OrderPlaced, transitions to WaitingForPayment
   ↓
4. OrderService sends ChargePayment command to payment-service queue
   ↓
5. PaymentService consumes ChargePayment
   ↓
6. PaymentService simulates payment (2 seconds)
   ↓
7. PaymentService publishes PaymentProcessed event
   ↓
8. OrderStateMachine receives PaymentProcessed, transitions to WaitingForKitchenConfirmation
   ↓
9. OrderService sends ConfirmKitchen command to restaurant-service queue
   ↓
10. RestaurantService consumes ConfirmKitchen
    ↓
11. RestaurantService simulates confirmation (1.5 seconds)
    ↓
12. RestaurantService publishes KitchenConfirmed event
    ↓
13. OrderStateMachine receives KitchenConfirmed, transitions to Completed
```

## Resilience Patterns

### Circuit Breaker (Polly)

Applied to OrderService → PaymentService communication:

```
State: Closed (Normal)
├─ 3 consecutive failures → Opens breaker
│
State: Open (Failed)
├─ Requests immediately fail
├─ 30-second duration
│
State: Half-Open (Recovery)
└─ Next request tests service
   ├─ Success → Closes circuit
   └─ Failure → Reopens circuit
```

Events logged:
- "Circuit breaker opened for 30 seconds"
- "Circuit breaker reset"

### Event-Driven Architecture

- Decoupled services through MassTransit
- Automatic retry on transient failures
- No direct HTTP calls between services (except PaymentService with circuit breaker)

## Monitoring & Debugging

### RabbitMQ Management UI
- URL: http://localhost:15672
- Credentials: guest/guest
- View: Queues, exchanges, messages

### Elasticsearch Health
```bash
curl http://localhost:9200/
```

### Kibana Dashboard
- URL: http://localhost:5601
- Monitor indices and run queries

### Service Logs
Check console output or configure structured logging:

```csharp
builder.Services.AddLogging(options =>
{
    options.AddConsole();
    options.AddDebug();
});
```

## Configuration Files

### appsettings.json
Each service has configuration for:
- Logging levels
- Swagger settings
- Database connections (if applicable)

### appsettings.Development.json
Development-specific overrides for:
- Detailed logging
- Swagger UI auto-launch

## Error Handling

Services handle errors gracefully:
- Try-catch blocks in consumers
- Logging of errors and warnings
- Circuit breaker prevents cascading failures
- OrderRejected events for failed orders

## Future Enhancements

1. **Database Integration**
   - SQL Server for state persistence
   - Replace in-memory saga repository

2. **Dead Letter Queues (DLQ)**
   - Capture failed messages
   - Manual retry mechanisms

3. **Distributed Tracing**
   - Jaeger or OpenTelemetry
   - Cross-service request tracking

4. **Additional Services**
   - DeliveryService for tracking
   - NotificationService for updates
   - CustomerService for user management

5. **API Gateway**
   - Centralized entry point
   - Rate limiting and authentication
   - Request routing

## Troubleshooting

### Services won't connect to RabbitMQ
```bash
docker ps # Verify containers are running
docker logs swiftbite-mq # Check RabbitMQ logs
```

### Redis connection errors
```bash
docker exec swiftbite-cache redis-cli ping
# Should respond with PONG
```

### Elasticsearch not responding
```bash
curl http://localhost:9200/_cluster/health
# Check cluster status
```

## License

This project is part of the SwiftBite food delivery platform.

## Support

For issues or questions, refer to the service-specific documentation or contact the development team.
