using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseSplitterAPI.Data;
using ExpenseSplitterAPI.Models;
using System.Security.Claims;

namespace ExpenseSplitterAPI.Controllers
{
    [Route("api/groups")] // ✅ Ensure this matches frontend API calls
    [ApiController]
    [Authorize] // ✅ Ensure user is authenticated
    public class GroupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Fetch All Groups (Includes Members & Creator)
        [HttpGet]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Include(g => g.CreatedBy) // ✅ Ensure Creator details are included
                .Select(g => new
                {
                    id = g.Id,
                    name = g.Name ?? "Unnamed Group",
                    isPinned = g.IsPinned,
                    createdBy = g.CreatedBy != null
                        ? new { id = g.CreatedBy.UserId, name = g.CreatedBy.Name ?? "Unknown Owner" }
                        : null,
                    createdAt = g.CreatedAt,
                    members = g.Members.Select(m => new
                    {
                        id = m.UserId,
                        name = m.User != null ? m.User.Name ?? "Unknown Member" : "Unknown"
                    }).ToList()
                })
                .ToListAsync();

            return Ok(groups);
        }

        // ✅ Fetch Single Group by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroup(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .Include(g => g.CreatedBy)
                .Where(g => g.Id == id)
                .Select(g => new
                {
                    id = g.Id,
                    name = g.Name ?? "Unnamed Group",
                    isPinned = g.IsPinned,
                    createdBy = g.CreatedBy != null
                        ? new { id = g.CreatedBy.UserId, name = g.CreatedBy.Name ?? "Unknown Owner" }
                        : null,
                    createdAt = g.CreatedAt,
                    members = g.Members.Select(m => new
                    {
                        id = m.UserId,
                        name = m.User != null ? m.User.Name ?? "Unknown Member" : "Unknown"
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (group == null) return NotFound(new { message = "Group not found" });

            return Ok(group);
        }

        // ✅ Create New Group
        [HttpPost]
        public async Task<IActionResult> CreateGroup([FromBody] Group group)
        {
            if (string.IsNullOrEmpty(group.Name))
                return BadRequest(new { message = "Group name is required" });

            // ✅ Get logged-in user ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized(new { message = "Unauthorized request" });

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null) return Unauthorized(new { message = "User not found" });

            // ✅ Set creator and add to members
            group.CreatedById = user.UserId;
            group.CreatedAt = DateTime.UtcNow;
            group.Members = new List<GroupUser> { new GroupUser { UserId = user.UserId } };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }

        // ✅ Add Member to Group
        [HttpPost("{groupId}/members")]
        public async Task<IActionResult> AddMember(int groupId, [FromBody] dynamic request)
        {
            if (request == null || request.email == null)
                return BadRequest(new { message = "Invalid request. Provide email in JSON format." });

            string email = request.email.ToString();

            var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound(new { message = "Group not found" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound(new { message = "User not found" });

            if (group.Members.Any(m => m.UserId == user.UserId))
                return Conflict(new { message = "User already in group" });

            group.Members.Add(new GroupUser { UserId = user.UserId });
            await _context.SaveChangesAsync();

            return Ok(new { message = "User added successfully" });
        }

        // ✅ Remove Member from Group
        [HttpDelete("{groupId}/members/{userId}")]
        public async Task<IActionResult> RemoveMember(int groupId, int userId)
        {
            var group = await _context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound(new { message = "Group not found" });

            var member = group.Members.FirstOrDefault(m => m.UserId == userId);
            if (member == null) return NotFound(new { message = "User not found in group" });

            group.Members.Remove(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User removed successfully" });
        }

        // ✅ Pin/Unpin a Group
        [HttpPut("{groupId}/pin")]
        public async Task<IActionResult> PinGroup(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return NotFound(new { message = "Group not found" });

            group.IsPinned = !group.IsPinned;
            await _context.SaveChangesAsync();

            return Ok(new { message = group.IsPinned ? "Group pinned" : "Group unpinned" });
        }

        // ✅ Ensure Only Group Creator Can Delete or Modify Group
        private async Task<bool> IsGroupOwner(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return false;

            return await _context.Groups.AnyAsync(g => g.Id == groupId && g.CreatedById == int.Parse(userId));
        }

        // ✅ Delete Group (Only Owner)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            if (!await IsGroupOwner(id))
                return Forbid(new { message = "Only the group creator can delete the group" });

            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
