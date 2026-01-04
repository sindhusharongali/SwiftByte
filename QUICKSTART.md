# SwiftBite - Quick Start Guide

## 5-Minute Setup

### Step 1: Start Infrastructure (Docker)

```bash
cd d:\SwiftByte
docker-compose up -d
```

**Verify services are running:**
```bash
docker ps
# Should show: rabbitmq, redis, elasticsearch, kibana
```

### Step 2: Restore NuGet Packages

```bash
cd d:\SwiftByte
dotnet restore
```

### Step 3: Start Services (Open 4 terminals)

**Terminal 1 - OrderService:**
```bash
cd d:\SwiftByte\OrderService
dotnet run
# Runs on https://localhost:7001 | http://localhost:5001
```

**Terminal 2 - PaymentService:**
```bash
cd d:\SwiftByte\PaymentService
dotnet run
# Runs on https://localhost:7002 | http://localhost:5002
```

**Terminal 3 - RestaurantService:**
```bash
cd d:\SwiftByte\RestaurantService
dotnet run
# Runs on https://localhost:7003 | http://localhost:5003
```

**Terminal 4 - SearchService:**
```bash
cd d:\SwiftByte\SearchService
dotnet run
# Runs on https://localhost:7004 | http://localhost:5004
```

Wait for all services to print "Application started" message.

---

## Quick Test

### Test 1: Place an Order

```powershell
# PowerShell
$body = @{
    customerId = "550e8400-e29b-41d4-a716-446655440000"
    totalAmount = 45.99
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:7001/api/order/place-order" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body `
  -SkipCertificateCheck

$response
# Shows OrderId
```

**Expected Output:**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
}
```

**What happens behind the scenes:**
- OrderService publishes `OrderPlaced` event
- OrderStateMachine transitions to `WaitingForPayment`
- OrderService sends `ChargePayment` command to PaymentService
- PaymentService processes payment (2-second delay)
- PaymentService publishes `PaymentProcessed` event
- OrderStateMachine transitions to `WaitingForKitchenConfirmation`
- OrderService sends `ConfirmKitchen` command to RestaurantService
- RestaurantService confirms order (1.5-second delay)
- RestaurantService publishes `KitchenConfirmed` event
- OrderStateMachine transitions to `Completed`

**Check service logs** to see the complete flow!

### Test 2: Search Menu Items

```powershell
# First, index a menu item
$menuItem = @{
    id = "550e8400-e29b-41d4-a716-446655440000"
    name = "Margherita Pizza"
    description = "Fresh mozzarella and basil"
    price = 14.99
    category = "main"
    available = $true
    restaurantId = "f47ac10b-58cc-4372-a567-0e02b2c3d479"
    createdAt = Get-Date -AsUTC
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:7004/api/product/index" `
  -Method Post `
  -ContentType "application/json" `
  -Body $menuItem `
  -SkipCertificateCheck

$response
# Shows indexed item ID

# Wait 1 second for Elasticsearch to index

Start-Sleep -Seconds 1

# Search for the menu item
$response = Invoke-RestMethod -Uri "https://localhost:7004/api/product/search?query=pizza" `
  -SkipCertificateCheck

$response | ConvertTo-Json
# Shows search results
```

---

## Monitoring

### RabbitMQ Management UI
- URL: http://localhost:15672
- Username: guest
- Password: guest
- **View:** Queues: payment-service, restaurant-service

### Elasticsearch Health
```powershell
Invoke-RestMethod http://localhost:9200/ | ConvertTo-Json
```

### Kibana Dashboard
- URL: http://localhost:5601
- Explore menu_items index

---

## Troubleshooting

### Services won't start

**Error:** "Unable to connect to RabbitMQ"
```bash
# Check RabbitMQ is running
docker ps | grep rabbitmq

# Check RabbitMQ logs
docker logs swiftbite-rabbitmq

# Restart RabbitMQ
docker restart swiftbite-rabbitmq
```

**Error:** "Port already in use"
```bash
# Stop all containers
docker-compose down

# Restart
docker-compose up -d
```

### No response from OrderService

**Check:**
1. Service is running (`Application started` message visible)
2. Correct port (7001 for HTTPS, 5001 for HTTP)
3. RabbitMQ is running (should see connection logs)

### Messages not flowing

**Check MassTransit logs** in each service console:
- "Sending to" messages for commands
- "Consuming" messages for events
- Error messages if anything fails

---

## Next Steps

1. **Read** [API_DOCUMENTATION.md](API_DOCUMENTATION.md) for detailed API reference
2. **Learn** [ARCHITECTURE.md](ARCHITECTURE.md) for design patterns
3. **Explore** source code:
   - [OrderService/Saga/OrderStateMachine.cs](OrderService/Saga/OrderStateMachine.cs)
   - [PaymentService/Consumers/ChargePaymentConsumer.cs](PaymentService/Consumers/ChargePaymentConsumer.cs)
   - [SearchService/Controllers/ProductController.cs](SearchService/Controllers/ProductController.cs)
4. **Modify** and experiment with the code
5. **Add** more services following the same patterns

---

## Development Tips

### Enable Debug Logging

In each service's `Program.cs`:
```csharp
builder.Services.AddLogging(options =>
{
    options.SetMinimumLevel(LogLevel.Debug);
    options.AddConsole();
});
```

### Watch for Messages in RabbitMQ

1. Go to http://localhost:15672
2. Navigate to Queues tab
3. Click on a queue (e.g., "payment-service")
4. Scroll down to see messages

### View Elasticsearch Indices

```powershell
Invoke-RestMethod "http://localhost:9200/_cat/indices" -SkipCertificateCheck | ForEach-Object { Write-Host $_ }
```

### Manually Trigger Messages

Use [RabbitMQ Management UI](http://localhost:15672) to publish test messages to queues.

---

## Project Structure

```
SwiftByte/
├── SwiftBite.Contracts/           # Shared contracts
│   ├── Events/                    # Event interfaces
│   └── Commands/                  # Command interfaces
├── OrderService/                  # Order orchestration
│   ├── Saga/                      # State machine
│   ├── Controllers/               # API endpoints
│   └── Services/                  # Business logic
├── PaymentService/                # Payment processing
│   └── Consumers/                 # Event consumers
├── RestaurantService/             # Kitchen management
│   └── Consumers/                 # Event consumers
├── SearchService/                 # Menu search
│   ├── Controllers/               # API endpoints
│   └── Models/                    # Data models
├── docker-compose.yml             # Infrastructure
├── SwiftBite.sln                  # Solution file
├── README.md                      # Getting started
├── API_DOCUMENTATION.md           # API reference
└── ARCHITECTURE.md                # Design patterns
```

---

## Common Tasks

### Add a new event

1. Create interface in `SwiftBite.Contracts/Events/MyEvent.cs`
2. Publish in publisher service:
   ```csharp
   await context.Publish<MyEvent>(new { ... });
   ```
3. Subscribe in consumer service:
   ```csharp
   public class MyEventConsumer : IConsumer<MyEvent>
   {
       public async Task Consume(ConsumeContext<MyEvent> context) { ... }
   }
   ```

### Add a new API endpoint

1. Create controller in service:
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class MyController : ControllerBase
   {
       [HttpGet("{id}")]
       public async Task<IActionResult> GetItem(Guid id) { ... }
   }
   ```
2. Restart service
3. Access at `https://localhost:port/api/mycontroller/id`

### Add state to saga

1. Add property to `OrderState`:
   ```csharp
   public string MyField { get; set; }
   ```
2. Update state in saga handler:
   ```csharp
   context.Saga.MyField = context.Message.Value;
   ```

---

## Performance Tips

1. **Increase prefetch count** for higher throughput:
   ```csharp
   e.PrefetchCount = 20;
   ```

2. **Batch index operations** in SearchService:
   ```csharp
   await client.BulkAsync(b => b.Index(...));
   ```

3. **Cache frequently accessed data** in Redis:
   ```csharp
   var cached = await redis.GetAsync(key);
   ```

---

## Need Help?

1. Check [README.md](README.md) for comprehensive documentation
2. Review [ARCHITECTURE.md](ARCHITECTURE.md) for design decisions
3. Read [API_DOCUMENTATION.md](API_DOCUMENTATION.md) for endpoint details
4. Examine console logs for error messages
5. Check RabbitMQ Management UI for message flow

---

**Happy coding! 🚀**
