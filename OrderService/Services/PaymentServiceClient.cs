namespace OrderService.Services;

using Polly.CircuitBreaker;

public interface IPaymentServiceClient
{
    Task<PaymentResponse> ChargePaymentAsync(Guid orderId, decimal amount);
}

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResponse> ChargePaymentAsync(Guid orderId, decimal amount)
    {
        try
        {
            var request = new { OrderId = orderId, Amount = amount };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Sending charge payment request to Payment Service for Order {OrderId}", orderId);
            
            var response = await _httpClient.PostAsync("/api/payment/charge", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Payment charged successfully for Order {OrderId}", orderId);
                return new PaymentResponse { Success = true, OrderId = orderId };
            }
            else
            {
                _logger.LogWarning("Payment service returned status {StatusCode} for Order {OrderId}", 
                    response.StatusCode, orderId);
                return new PaymentResponse { Success = false, OrderId = orderId };
            }
        }
        catch (HttpRequestException ex) when (ex.InnerException is BrokenCircuitException)
        {
            _logger.LogError("Circuit breaker is open. Payment service is unavailable for Order {OrderId}", orderId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Payment Service for Order {OrderId}", orderId);
            throw;
        }
    }
}

public record PaymentResponse
{
    public bool Success { get; set; }
    public Guid OrderId { get; set; }
}
