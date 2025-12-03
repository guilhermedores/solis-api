using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models.Reports;

[Table("report_filters")]
public class ReportFilter
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
    [MaxLength(50)]
    [Column("filter_type")]
    public string FilterType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("data_source")]
    public string DataSource { get; set; } = string.Empty;

    [Column("default_value")]
    public string? DefaultValue { get; set; }

    [Column("required")]
    public bool Required { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ReportId))]
    public Report Report { get; set; } = null!;

    public ICollection<ReportFilterOption> Options { get; set; } = new List<ReportFilterOption>();
}
