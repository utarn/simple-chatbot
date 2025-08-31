using System.ComponentModel.DataAnnotations;
using Pgvector;

namespace ChatbotApi.Domain.Entities;

public class TrackFile : BaseAuditableEntity
{
    public new int Id { get; set; }

    [MaxLength(100)]
    public required string FileName { get; set; }

    [MaxLength(500)]
    public required string FilePath { get; set; }

    [MaxLength(100)]
    public required string ContentType { get; set; }

    public required long FileSize { get; set; }

    [MaxLength(50)]
    public required string UserId { get; set; }

    [MaxLength(1000)]
    public required string Description { get; set; }

    public Vector? Embedding { get; set; }

    [MaxLength(64)]
    public string? FileHash { get; set; }

    public int? ChatbotId { get; set; }

    public int? ContactId { get; set; }

    public virtual Chatbot? Chatbot { get; set; }
    public virtual Contact? Contact { get; set; }
}