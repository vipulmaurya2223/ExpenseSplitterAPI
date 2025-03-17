using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseSplitterAPI.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public bool IsPinned { get; set; } = false;

        [Required]
        public int CreatedById { get; set; }  // ✅ Store only UserId
        public User CreatedBy { get; set; }  // ✅ Navigation property

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // ✅ Auto-set timestamp

        public List<GroupUser> Members { get; set; } = new List<GroupUser>();  // ✅ Proper List
    }
}
