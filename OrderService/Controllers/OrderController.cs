namespace OrderService.Controllers;

using Microsoft.AspNetCore.Mvc;
using MassTransit;
using SwiftBite.Contracts.Events;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("place-order")]
    public async Task<IActionResult> PlaceOrder(PlaceOrderRequest request)
    {
        var orderId = Guid.NewGuid();
        
        await _publishEndpoint.Publish<OrderPlaced>(new
        {
            OrderId = orderId,
            CustomerId = request.CustomerId,
            TotalAmount = request.TotalAmount,
            PlacedAt = DateTime.UtcNow
        });

        return Accepted(new { OrderId = orderId });
    }
}

public record PlaceOrderRequest(Guid CustomerId, decimal TotalAmount);
