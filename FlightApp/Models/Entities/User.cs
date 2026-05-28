namespace FlightKS.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string KeycloakUserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<UploadedFile> UploadedFiles { get; set; } = [];
}
