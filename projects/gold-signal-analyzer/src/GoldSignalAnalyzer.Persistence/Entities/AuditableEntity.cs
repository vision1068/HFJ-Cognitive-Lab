namespace GoldSignalAnalyzer.Persistence.Entities;

/// <summary>
/// Company standard audit base: GUID id + created_by / created_on / modified_by / modified_on.
/// </summary>
public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string ModifiedBy { get; set; } = "system";
    public DateTime ModifiedOn { get; set; } = DateTime.UtcNow;
}
