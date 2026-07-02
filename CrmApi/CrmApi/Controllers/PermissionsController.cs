using Crm.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Module, p.Action })
                .ToListAsync();
            return Ok(permissions);
        }
    }
}
