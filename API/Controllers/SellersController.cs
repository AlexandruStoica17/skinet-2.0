using API.Dtos;
using API.Errors;
using API.Extensions;
using API.Helpers;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class SellersController : BaseApiController
    {
        private readonly StoreContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public SellersController(StoreContext context, UserManager<AppUser> userManager, IMapper mapper)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<SellerProfileDto>> GetSellerProfile(
            string email,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 8)
        {
            var seller = await _userManager.FindByEmailAsync(email);
            if (seller == null) return NotFound(new ApiResponse(404));

            var query = _context.Products
                .Include(p => p.ProductType)
                .Include(p => p.ProductBrand)
                .Include(p => p.Photos)
                .Where(p => p.ProducerId == seller.Id)
                .OrderBy(p => p.Name);

            var totalItems = await query.CountAsync();
            var products = await query
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToListAsync();

            return Ok(CreateSellerProfileDto(seller, products, pageIndex, pageSize, totalItems));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<SellerProfileUpdateDto>> GetMySellerProfile()
        {
            var user = await _userManager.FindByEmailFromClaimsPrinciple(User);
            if (user == null) return NotFound(new ApiResponse(404));

            return Ok(new SellerProfileUpdateDto
            {
                CompanyName = user.CompanyName,
                Description = user.SellerDescription,
                Story = user.SellerStory,
                History = user.SellerHistory,
                Location = user.SellerLocation,
                MapUrl = user.SellerMapUrl
            });
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<ActionResult> UpdateMySellerProfile(SellerProfileUpdateDto sellerProfile)
        {
            var user = await _userManager.FindByEmailFromClaimsPrinciple(User);
            if (user == null) return NotFound(new ApiResponse(404));

            user.CompanyName = sellerProfile.CompanyName;
            user.SellerDescription = sellerProfile.Description;
            user.SellerStory = sellerProfile.Story;
            user.SellerHistory = sellerProfile.History;
            user.SellerLocation = sellerProfile.Location;
            user.SellerMapUrl = sellerProfile.MapUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(new ApiResponse(400, "Nu s-a putut actualiza pagina vanzatorului."));

            return Ok(new { message = "Pagina vanzatorului a fost actualizata." });
        }

        private SellerProfileDto CreateSellerProfileDto(
            AppUser seller,
            IReadOnlyList<Product> products,
            int pageIndex,
            int pageSize,
            int totalItems)
        {
            var productDtos = _mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products);

            return new SellerProfileDto
            {
                Email = seller.Email,
                DisplayName = seller.DisplayName,
                CompanyName = seller.CompanyName,
                IsVerified = seller.IsVerified,
                DocumentUrl = seller.DocumentUrl,
                Description = seller.SellerDescription,
                Story = seller.SellerStory,
                History = seller.SellerHistory,
                Location = seller.SellerLocation,
                MapUrl = seller.SellerMapUrl,
                SellerType = GuessSellerType(products),
                Products = new Pagination<ProductToReturnDto>(pageIndex, pageSize, totalItems, productDtos)
            };
        }

        private static string GuessSellerType(IReadOnlyList<Product> products)
        {
            return products.Any(p => p.ProductType?.Name == "Ingredients")
                ? "Ingredients"
                : "Cosmetics";
        }
    }
}
