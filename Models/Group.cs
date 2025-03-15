using System.Collections.Generic;

namespace ExpenseSplitterAPI.Models
{
    public class Group
    {
        public int Id { get; set; }
        public required string Name { get; set; } // ✅ Fix: Add 'required'
        public List<GroupUser> GroupUsers { get; set; } = new List<GroupUser>(); // ✅ Initialize List
    }
}
