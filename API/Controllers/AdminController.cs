using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    // Restricționăm accesul: doar utilizatorii care au rolul de "Admin" pot apela aceste funcții
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;

        public AdminController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            // 1. Aducem toți utilizatorii din baza de date
            var users = await _userManager.Users
                .OrderBy(u => u.DisplayName)
                .Select(u => new
                {
                    u.Id,
                    u.DisplayName,
                    u.Email,
                    u.CompanyName,
                    u.IsVerified,
                    u.DocumentUrl
                })
                .ToListAsync();

            // 2. Pentru fiecare utilizator, îi extragem rolul și formăm lista finală
            var userRoles = new List<object>();
            
            foreach (var user in users)
            {
                var appUser = await _userManager.FindByIdAsync(user.Id);
                var roles = await _userManager.GetRolesAsync(appUser);
                
                userRoles.Add(new
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    CompanyName = user.CompanyName,
                    IsVerified = user.IsVerified,
                    DocumentUrl = user.DocumentUrl,
                    Roles = roles
                });
            }

            return Ok(userRoles);
        }

        [HttpPost("approve-user/{id}")]
        public async Task<ActionResult> ApproveUser(string id)
        {
            // 1. Căutăm producătorul după ID
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound("Utilizatorul nu a fost găsit.");

            // 2. Îi schimbăm statusul în "Verificat"
            user.IsVerified = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded) return BadRequest("Eroare la aprobarea utilizatorului.");

            return Ok(new { message = "Utilizator aprobat cu succes!" });
        }
    }
}