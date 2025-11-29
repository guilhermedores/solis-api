using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SolisApi.Models.ValueObjects;

/// <summary>
/// Value Object for Address
/// </summary>
[Owned]
public class Address : ValueObject
{
    [Required]
    [MaxLength(8)]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Complement { get; set; }

    [Required]
    [MaxLength(100)]
    public string District { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    public Address() { }

    public Address(string zipCode, string street, string number, string district, string city, string state, string? complement = null)
    {
        ZipCode = zipCode;
        Street = street;
        Number = number;
        District = district;
        City = city;
        State = state;
        Complement = complement;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ZipCode;
        yield return Street;
        yield return Number;
        yield return Complement;
        yield return District;
        yield return City;
        yield return State;
    }

    public override string ToString()
    {
        var complementStr = string.IsNullOrEmpty(Complement) ? "" : $", {Complement}";
        return $"{Street}, {Number}{complementStr} - {District}, {City}/{State} - CEP: {ZipCode}";
    }
}
