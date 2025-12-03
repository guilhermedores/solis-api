using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models.Reports;

[Table("report_fields")]
public class ReportField
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("report_id")]
    public Guid ReportId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("field_type")]
    public string FieldType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("data_source")]
    public string DataSource { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("format_mask")]
    public string? FormatMask { get; set; }

    [MaxLength(20)]
    [Column("aggregation")]
    public string? Aggregation { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("visible")]
    public bool Visible { get; set; } = true;

    [Column("sortable")]
    public bool Sortable { get; set; } = true;

    [Column("filterable")]
    public bool Filterable { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ReportId))]
    public Report Report { get; set; } = null!;
}
