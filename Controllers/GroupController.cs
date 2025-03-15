using Microsoft.AspNetCore.Mvc;
using ExpenseSplitterAPI.Data;
using ExpenseSplitterAPI.Models;

namespace ExpenseSplitterAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetGroups()
        {
            return Ok(_context.Groups.ToList());
        }

        [HttpGet("{id}")]
        public IActionResult GetGroup(int id)
        {
            var group = _context.Groups.Find(id);
            if (group == null) return NotFound();
            return Ok(group);
        }

        [HttpPost]
        public IActionResult CreateGroup([FromBody] Group group)
        {
            _context.Groups.Add(group);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateGroup(int id, [FromBody] Group updatedGroup)
        {
            var group = _context.Groups.Find(id);
            if (group == null) return NotFound();

            group.Name = updatedGroup.Name;
            _context.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteGroup(int id)
        {
            var group = _context.Groups.Find(id);
            if (group == null) return NotFound();

            _context.Groups.Remove(group);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
