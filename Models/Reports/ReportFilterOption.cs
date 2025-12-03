using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models.Reports;

[Table("report_filter_options")]
public class ReportFilterOption
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("filter_id")]
    public Guid FilterId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("value")]
    public string Value { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("label")]
    public string Label { get; set; } = string.Empty;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(FilterId))]
    public ReportFilter Filter { get; set; } = null!;
}
