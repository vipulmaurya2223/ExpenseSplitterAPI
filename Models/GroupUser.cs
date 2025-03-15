using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseSplitterAPI.Models
{
    public class GroupUser
    {
        [Key]
        public int Id { get; set; }

        // ✅ Foreign Key for User
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        // ✅ Foreign Key for Group
        public int GroupId { get; set; }
        [ForeignKey("GroupId")]
        public Group? Group { get; set; }
    }
}
