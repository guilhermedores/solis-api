using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models.Reports;

[Table("reports")]
public class Report
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [MaxLength(100)]
    [Column("category")]
    public string? Category { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("base_table")]
    public string BaseTable { get; set; } = string.Empty;

    [Column("base_query")]
    public string? BaseQuery { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ReportField> Fields { get; set; } = new List<ReportField>();
    public ICollection<ReportFilter> Filters { get; set; } = new List<ReportFilter>();
}
