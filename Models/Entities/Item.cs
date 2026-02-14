using System.ComponentModel.DataAnnotations;
using ConsoleApp4.Models.Enums;

namespace ConsoleApp4.Models.Entities;

public sealed class Item
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public ItemType Type { get; set; }

    public ItemEffectType EffectType { get; set; }

    public int EffectValue { get; set; }

    public int BuyPrice { get; set; }

    public int SellPrice { get; set; }

    public bool IsConsumable { get; set; }
}
