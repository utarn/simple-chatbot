using System.ComponentModel.DataAnnotations;

namespace ChatbotApi.Domain.Entities;

public class Contact : BaseAuditableEntity
{
    public new int Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public required string UserId { get; set; }

    // Navigation property
    public virtual ICollection<TrackFile> TrackFiles { get; set; } = new List<TrackFile>();
}