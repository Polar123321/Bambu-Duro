using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Models.Entities;

public sealed class UserItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid ItemId { get; set; }

    public int Quantity { get; set; }
}
