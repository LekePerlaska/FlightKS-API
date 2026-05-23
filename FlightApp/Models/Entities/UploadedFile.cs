namespace FlightKS.Models.Entities;

public class UploadedFile
{
    public Guid Id { get; set; }

    public Guid UploadedByUserId { get; set; }
    public User UploadedBy { get; set; } = null!;

    public required string FileName { get; set; }
    public required string OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string StoragePath { get; set; }
    public string? RelatedEntityName { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
