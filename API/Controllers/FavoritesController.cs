using API.Dtos;
using API.Errors;
using API.Extensions;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity; 
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize] 
    public class FavoritesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager; 

        public FavoritesController(IUnitOfWork unitOfWork, IMapper mapper, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager; 
        }

       
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetFavorites()
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            if (string.IsNullOrEmpty(email)) return Unauthorized(new ApiResponse(401));

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound(new ApiResponse(404));

            var spec = new FavoriteProductsWithProductSpecification(user.Id);
            var favorites = await _unitOfWork.Repository<FavoriteProduct>().ListAsync(spec);

            var products = favorites.Select(f => f.Product).ToList();

            return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products));
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> AddFavorite(FavoriteDto favoriteDto)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            if (string.IsNullOrEmpty(email)) return Unauthorized(new ApiResponse(401));

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound(new ApiResponse(404));

            var userId = user.Id;

            var spec = new FavoriteProductsWithProductSpecification(userId);
            var favorites = await _unitOfWork.Repository<FavoriteProduct>().ListAsync(spec);
            
            if (favorites.Any(x => x.ProductId == favoriteDto.ProductId))
                return Ok(new { alreadyExists = true, message = "Product is already in favorites" });

            var favorite = new FavoriteProduct
            {
                ProductId = favoriteDto.ProductId,
                AppUserId = userId 
            };

            _unitOfWork.Repository<FavoriteProduct>().Add(favorite);
            
            if (await _unitOfWork.Complete() <= 0) 
                return BadRequest(new ApiResponse(400, "Problem saving favorite"));

            return Ok(new { alreadyExists = false, message = "Product added to favorites" });
        }


        [HttpDelete]
        public async Task<ActionResult> RemoveFavorite(FavoriteDto favoriteDto)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            if (string.IsNullOrEmpty(email)) return Unauthorized(new ApiResponse(401));

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound(new ApiResponse(404));

            var spec = new BaseSpecification<FavoriteProduct>(x => 
                x.AppUserId == user.Id && x.ProductId == favoriteDto.ProductId);
            
            var favorite = await _unitOfWork.Repository<FavoriteProduct>().GetEntityWithSpec(spec);

            if (favorite == null) return NotFound(new ApiResponse(404));

            _unitOfWork.Repository<FavoriteProduct>().Delete(favorite);
            
            if (await _unitOfWork.Complete() <= 0) 
                return BadRequest(new ApiResponse(400, "Problem removing favorite"));

            return Ok();
        }
    }
}
