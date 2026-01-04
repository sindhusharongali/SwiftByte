# SwiftBite Microservices - API Documentation

## Service Endpoints

### OrderService
**Base URL:** `https://localhost:7001` or `http://localhost:5001`

#### Place Order
- **Endpoint:** `POST /api/order/place-order`
- **Description:** Create a new order and initiate the order processing saga
- **Request Body:**
  ```json
  {
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "totalAmount": 45.99
  }
  ```
- **Response:** `202 Accepted`
  ```json
  {
    "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
  }
  ```
- **Status Codes:**
  - `202 Accepted` - Order accepted and processing started
  - `400 Bad Request` - Invalid input
  - `500 Internal Server Error` - Server error

---

### PaymentService
**Base URL:** `https://localhost:7002` or `http://localhost:5002`

#### Consumers
- **Queue:** `payment-service`
- **Consumes:** `ChargePayment` command
- **Publishes:** `PaymentProcessed` event

**ChargePayment Command:**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 45.99
}
```

**PaymentProcessed Event:**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "paymentId": "a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6",
  "amount": 45.99,
  "processedAt": "2026-01-03T10:00:05Z"
}
```

---

### RestaurantService
**Base URL:** `https://localhost:7003` or `http://localhost:5003`

#### Consumers
- **Queue:** `restaurant-service`
- **Consumes:** `ConfirmKitchen` command
- **Publishes:** `KitchenConfirmed` event

**ConfirmKitchen Command:**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "restaurantId": "e5f6a7b8-c9d0-41e2-f3g4-h5i6j7k8l9m0"
}
```

**KitchenConfirmed Event:**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "kitchenId": "e5f6a7b8-c9d0-41e2-f3g4-h5i6j7k8l9m0",
  "confirmedAt": "2026-01-03T10:00:08Z",
  "estimatedPrepTime": "00:30:00"
}
```

---

### SearchService
**Base URL:** `https://localhost:7004` or `http://localhost:5004`

#### Search Menu Items
- **Endpoint:** `GET /api/product/search`
- **Description:** Search for menu items by name, description, or category
- **Query Parameters:**
  - `query` (required): Search term (searches in name and description)
  - `category` (optional): Filter by category
- **Response:** `200 OK`
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
- **Examples:**
  ```bash
  # Search for pasta
  GET /api/product/search?query=pasta
  
  # Search for main course pizzas
  GET /api/product/search?query=pizza&category=main
  ```

#### Get Menu Item by ID
- **Endpoint:** `GET /api/product/{id}`
- **Description:** Retrieve a specific menu item by ID
- **Path Parameters:**
  - `id` (required): Menu item GUID
- **Response:** `200 OK`
  ```json
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
  ```
- **Status Codes:**
  - `200 OK` - Item found
  - `404 Not Found` - Item doesn't exist
  - `500 Internal Server Error` - Server error

#### Index Menu Item
- **Endpoint:** `POST /api/product/index`
- **Description:** Add or update a menu item in Elasticsearch
- **Request Body:**
  ```json
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Margherita Pizza",
    "description": "Fresh mozzarella and basil pizza",
    "price": 14.99,
    "category": "main",
    "available": true,
    "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "createdAt": "2026-01-03T10:00:00Z"
  }
  ```
- **Response:** `202 Accepted`
  ```json
  {
    "id": "550e8400-e29b-41d4-a716-446655440000"
  }
  ```
- **Status Codes:**
  - `202 Accepted` - Item accepted for indexing
  - `400 Bad Request` - Invalid input
  - `500 Internal Server Error` - Server error

---

## Event Messages (MassTransit)

### Order Processing Flow

#### 1. OrderPlaced Event
**Published by:** OrderService (OrderController)
**Consumed by:** OrderService (OrderStateMachine)
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "totalAmount": 45.99,
  "placedAt": "2026-01-03T10:00:00Z"
}
```

#### 2. ChargePayment Command
**Sent by:** OrderService (OrderStateMachine)
**Queue:** `payment-service`
**Consumed by:** PaymentService (ChargePaymentConsumer)
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 45.99
}
```

#### 3. PaymentProcessed Event
**Published by:** PaymentService (ChargePaymentConsumer)
**Consumed by:** OrderService (OrderStateMachine)
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "paymentId": "a1b2c3d4-e5f6-47a8-b9c0-d1e2f3a4b5c6",
  "amount": 45.99,
  "processedAt": "2026-01-03T10:00:05Z"
}
```

#### 4. ConfirmKitchen Command
**Sent by:** OrderService (OrderStateMachine)
**Queue:** `restaurant-service`
**Consumed by:** RestaurantService (ConfirmKitchenConsumer)
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "restaurantId": "e5f6a7b8-c9d0-41e2-f3g4-h5i6j7k8l9m0"
}
```

#### 5. KitchenConfirmed Event
**Published by:** RestaurantService (ConfirmKitchenConsumer)
**Consumed by:** OrderService (OrderStateMachine)
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "kitchenId": "e5f6a7b8-c9d0-41e2-f3g4-h5i6j7k8l9m0",
  "confirmedAt": "2026-01-03T10:00:08Z",
  "estimatedPrepTime": "00:30:00"
}
```

#### 6. OrderRejected Event
**Published by:** PaymentService or RestaurantService
**Consumed by:** OrderService (OrderStateMachine)
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "reason": "Payment declined",
  "rejectedAt": "2026-01-03T10:00:06Z"
}
```

---

## HTTP Client Examples

### Using cURL

```bash
# Place an order
curl -X POST https://localhost:7001/api/order/place-order \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "totalAmount": 45.99
  }' \
  -k

# Search for menu items
curl "https://localhost:7004/api/product/search?query=pizza&category=main" \
  -H "Accept: application/json" \
  -k

# Get specific menu item
curl "https://localhost:7004/api/product/550e8400-e29b-41d4-a716-446655440000" \
  -H "Accept: application/json" \
  -k

# Index a menu item
curl -X POST https://localhost:7004/api/product/index \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Margherita Pizza",
    "description": "Fresh mozzarella and basil",
    "price": 14.99,
    "category": "main",
    "available": true,
    "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "createdAt": "2026-01-03T10:00:00Z"
  }' \
  -k
```

### Using PowerShell

```powershell
# Place an order
$body = @{
    customerId = "550e8400-e29b-41d4-a716-446655440000"
    totalAmount = 45.99
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:7001/api/order/place-order" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body `
  -SkipCertificateCheck

$response | ConvertTo-Json

# Search menu items
$response = Invoke-RestMethod -Uri "https://localhost:7004/api/product/search?query=pizza" `
  -Method Get `
  -SkipCertificateCheck

$response | ConvertTo-Json
```

---

## State Machine Diagram

```
┌─────────────────┐
│    Initial      │
└────────┬────────┘
         │
    OrderPlaced Event
         │
         ▼
┌─────────────────────────────┐
│  WaitingForPayment          │
│ (Sends ChargePayment cmd)   │
└────┬────────────────────┬───┘
     │                    │
PaymentProcessed   OrderRejected
     │                    │
     │              ┌─────▼──────┐
     │              │   Failed   │
     │              └────────────┘
     │
     ▼
┌──────────────────────────────────┐
│ WaitingForKitchenConfirmation    │
│ (Sends ConfirmKitchen cmd)       │
└────┬──────────────────────────┬──┘
     │                          │
KitchenConfirmed        OrderRejected
     │                          │
     │                    ┌─────▼──────┐
     │                    │   Failed   │
     │                    └────────────┘
     │
     ▼
  ┌────────┐
  │Completed│
  └────────┘
```

---

## Error Codes

| Code | Message | Description |
|------|---------|-------------|
| 200 | OK | Request successful |
| 202 | Accepted | Request accepted for processing |
| 400 | Bad Request | Invalid request parameters |
| 404 | Not Found | Resource not found |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service temporarily unavailable (circuit breaker open) |

---

## Timing

- **PaymentService:** 2-second simulated processing delay
- **RestaurantService:** 1.5-second simulated confirmation delay
- **Circuit Breaker:** Opens after 3 failures for 30 seconds
- **Message Timeout:** Default MassTransit timeout (check MassTransit docs)

---

## Best Practices

1. **Always use HTTPS** for production deployments
2. **Validate input** before sending to services
3. **Handle async operations** appropriately (202 Accepted responses)
4. **Implement retry logic** for transient failures
5. **Monitor circuit breaker** state in production
6. **Log correlation IDs** for distributed tracing
7. **Use pagination** for large search results

---

## Testing

### Integration Testing
```csharp
// Example: Test place order endpoint
[Fact]
public async Task PlaceOrder_Should_Return_OrderId()
{
    var client = new HttpClient();
    var request = new { customerId = Guid.NewGuid(), totalAmount = 45.99m };
    
    var response = await client.PostAsJsonAsync(
        "https://localhost:7001/api/order/place-order", 
        request);
    
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
}
```

---

## Rate Limiting (Future)

Currently no rate limiting is implemented. Consider implementing:
- Token bucket algorithm
- Sliding window
- Distributed rate limiting with Redis

---

For more details, see the [README.md](README.md) and individual service documentation.
