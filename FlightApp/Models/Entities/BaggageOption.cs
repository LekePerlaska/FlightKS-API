namespace FlightKS.Models.Entities;

public class BaggageOption
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public decimal WeightKg { get; set; }
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<BookingBaggage> BookingBaggage { get; set; } = [];
}
