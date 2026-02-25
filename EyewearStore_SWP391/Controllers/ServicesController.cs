// Controllers/ServicesController.cs — file mới
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EyewearStore_SWP391.Controllers
{
    [Route("api/services")]
    [ApiController]
    [Authorize(Roles = "admin,manager")]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceService _svc;
        private readonly IWebHostEnvironment _env;

        public ServicesController(IServiceService svc, IWebHostEnvironment env)
        {
            _svc = svc;
            _env = env;
        }

        // POST /api/services/{id}/toggle
        [HttpPost("{id:int}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var ok = await _svc.ToggleActiveAsync(id);
            return ok ? Ok(new { message = "Status toggled" })
                      : NotFound(new { message = "Service not found" });
        }

        // DELETE /api/services/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteAsync(id);
            return ok ? Ok(new { message = "Deleted successfully" })
                      : NotFound(new { message = "Service not found" });
        }
    }
}
