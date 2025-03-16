using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BatchWebApi.Models; // 👈 Fix circular reference

namespace ExpenseSplitterAPI.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("PasswordHash")] // 👈 Maps to DB column
        public string Password { get; set; } = string.Empty;

        public int RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        [JsonIgnore] // 👈 Prevents circular reference
        public Role? Role { get; set; }

        [JsonIgnore] // 👈 Prevents circular reference
        public ICollection<GroupUser>? GroupUsers { get; set; }
    }
}
