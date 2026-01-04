namespace SearchService.Models;

public class MenuItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool Available { get; set; }
    public Guid RestaurantId { get; set; }
    public DateTime CreatedAt { get; set; }
}
