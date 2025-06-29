namespace ChatbotApi.Domain.Common;

/// <summary>
/// Base Auditable Entity
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    /// <summary>
    ///    สร้างเมื่อ
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    ///   สร้างโดย
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    ///    แก้ไขล่าสุดเมื่อ
    /// </summary>
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    ///   แก้ไขล่าสุดโดย
    /// </summary>
    public string? LastModifiedBy { get; set; }
}
