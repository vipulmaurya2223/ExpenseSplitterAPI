using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitterAPI.Models
{
    public class GroupUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }
        public Group Group { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; }  // ✅ Properly reference User
    }
}
