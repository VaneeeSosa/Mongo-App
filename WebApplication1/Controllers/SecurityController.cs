using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class SecurityController : Controller
    {
        private readonly SecurityService _securityService;

        public SecurityController(SecurityService securityService)
        {
            _securityService = securityService;
        }

        public async Task<IActionResult> Users(string database)
        {
            var users = await _securityService.ListUsersAsync(database);
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string database, string username, string password, string role)
        {
            await _securityService.CreateUserAsync(database, username, password, role);
            return RedirectToAction("Users", new { database });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string database, string username)
        {
            try
            {
                await _securityService.DeleteUserAsync(database, username);
                TempData["SuccessMessage"] = $"El usuario {username} se eliminó exitosamente.";
            }
            catch (Exception ex)
            {
                // Puedes registrar el error con un logging framework o similar
                TempData["ErrorMessage"] = $"Error al eliminar el usuario {username}: {ex.Message}";
            }
            return RedirectToAction("Users", new { database });
        }

    }
}
