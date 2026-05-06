using System.Security.Claims;
using API.Dtos;
using API.Errors;
using API.Extensions;
using AutoMapper;
using Core.Entities.Identity;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // NOU: Pentru IFormFile
using System.IO; // NOU: Pentru Directory si Path

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        
        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
        ITokenService tokenService, IMapper mapper)
        {
            _mapper = mapper;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userManager.FindByEmailFromClaimsPrinciple(User);

            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.CreateToken(user),
                DisplayName = user.DisplayName
            };
        }

        [Authorize]
        [HttpGet("address")]
        public async Task<ActionResult<AddressDto>> GetUserAddress()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByUserByClaimsPrincipleWithAddressAsync(User);
            return _mapper.Map<Address, AddressDto>(user.Address);
        }

        [Authorize]
        [HttpPut("address")]
        public async Task<ActionResult<AddressDto>> UpdateUserAddress(AddressDto address)
        {
            var user = await _userManager.FindByUserByClaimsPrincipleWithAddressAsync(HttpContext.User);
            user.Address = _mapper.Map<AddressDto, Address>(address);
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded) return Ok(_mapper.Map<Address, AddressDto>(user.Address));

            return BadRequest("Problem updating the user");
        }

        [HttpGet("emailexists")]
        public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null) return Unauthorized(new ApiResponse(401));

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded) return Unauthorized(new ApiResponse(401));

            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.CreateToken(user),
                DisplayName = user.DisplayName
            };
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (CheckEmailExistsAsync(registerDto.Email).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse { Errors = new[] { "Email address is in use" } });
            }

            var user = new AppUser
            {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = registerDto.Email,
                CompanyName = registerDto.CompanyName,

                IsVerified = (registerDto.Role == "Buyer" || registerDto.Role == "Blogger")
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded) return BadRequest(new ApiResponse(400));

            var validRoles = new[] { "Buyer", "CosmeticsProducer", "IngredientsProducer", "Blogger" };
            if (validRoles.Contains(registerDto.Role))
            {
                await _userManager.AddToRoleAsync(user, registerDto.Role);
            }
            else
            {
                await _userManager.AddToRoleAsync(user, "Buyer");
            }

            return new UserDto
            {
                DisplayName = user.DisplayName,
                Token = await _tokenService.CreateToken(user),
                Email = user.Email
            };
        }

        [Authorize] 
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto passwordDto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized(new ApiResponse(401));

            var result = await _userManager.ChangePasswordAsync(user, passwordDto.OldPassword, passwordDto.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }
                return ValidationProblem();
            }

            return Ok(new { message = "Parola a fost actualizată cu succes!" });
        }

        // --- NOU: ENDPOINT PENTRU UPLOAD DOCUMENTE ---
        [Authorize]
        [HttpPost("upload-document")]
        public async Task<ActionResult> UploadDocument(IFormFile file)
        {
            // Folosim extensia ta existentă pentru a găsi userul logat
            var user = await _userManager.FindByEmailFromClaimsPrinciple(User);
            
            if (file == null || file.Length == 0) return BadRequest("Niciun fișier selectat.");

            // Creăm un folder 'Content/Documents' în API dacă nu există
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Content", "Documents");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            // Adăugăm ID-ul utilizatorului la numele fișierului pentru a-l face unic
            var fileName = user.Id + "_" + Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(path, fileName);

            // Salvăm fizic pe server
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Salvăm ruta în baza de date
            user.DocumentUrl = "Content/Documents/" + fileName;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Document încărcat cu succes!" });
        }

        [HttpGet("make-me-admin")]
        public async Task<ActionResult> MakeMeAdmin(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound("Utilizatorul nu a fost găsit.");

            // Îi dăm rolul de Admin
            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (result.Succeeded) 
                return Ok("Magie! Utilizatorul este acum Admin. Dă Logout și Login în Angular!");
            
            return BadRequest("A apărut o eroare sau are deja rolul.");
        }
    }
}