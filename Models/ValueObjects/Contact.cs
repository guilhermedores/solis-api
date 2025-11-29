using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SolisApi.Models.ValueObjects;

/// <summary>
/// Value Object for Contact
/// </summary>
[Owned]
public class Contact : ValueObject
{
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? Mobile { get; set; }

    [MaxLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    public Contact() { }

    public Contact(string? phone = null, string? mobile = null, string? email = null)
    {
        Phone = phone;
        Mobile = mobile;
        Email = email;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Phone;
        yield return Mobile;
        yield return Email;
    }

    public bool HasContact()
    {
        return !string.IsNullOrEmpty(Phone) ||
               !string.IsNullOrEmpty(Mobile) ||
               !string.IsNullOrEmpty(Email);
    }

    public override string ToString()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(Phone))
            parts.Add($"Phone: {Phone}");
        
        if (!string.IsNullOrEmpty(Mobile))
            parts.Add($"Mobile/WhatsApp: {Mobile}");
        
        if (!string.IsNullOrEmpty(Email))
            parts.Add($"Email: {Email}");
        
        return parts.Count > 0 ? string.Join(" | ", parts) : "No contact";
    }
}
