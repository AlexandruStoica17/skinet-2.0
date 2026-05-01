using API.Dtos;
using API.Errors;
using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class FavoritesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FavoritesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // 1. Vezi toate produsele favorite ale utilizatorului
        [HttpGet("{userId}")]
        public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetFavorites(string userId)
        {
            var spec = new FavoriteProductsWithProductSpecification(userId);
            var favorites = await _unitOfWork.Repository<FavoriteProduct>().ListAsync(spec);

            // Returnăm direct produsele din lista de favorite
            var products = favorites.Select(f => f.Product).ToList();

            return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products));
        }

        // 2. Adaugă un produs la favorite (Like)
        [HttpPost]
        public async Task<ActionResult> AddFavorite(FavoriteDto favoriteDto)
        {
            // Verificăm dacă nu cumva îl are deja la favorite (ca să nu dea eroare)
            var spec = new FavoriteProductsWithProductSpecification(favoriteDto.AppUserId);
            var favorites = await _unitOfWork.Repository<FavoriteProduct>().ListAsync(spec);
            
            if (favorites.Any(x => x.ProductId == favoriteDto.ProductId))
                return BadRequest(new ApiResponse(400, "Produsul este deja la favorite"));

            var favorite = new FavoriteProduct
            {
                ProductId = favoriteDto.ProductId,
                AppUserId = favoriteDto.AppUserId
            };

            _unitOfWork.Repository<FavoriteProduct>().Add(favorite);
            
            if (await _unitOfWork.Complete() <= 0) 
                return BadRequest(new ApiResponse(400, "Problemă la salvarea favoritului"));

            return Ok();
        }

        // 3. Șterge de la favorite (Unlike)
        [HttpDelete]
        public async Task<ActionResult> RemoveFavorite(FavoriteDto favoriteDto)
        {
            var spec = new BaseSpecification<FavoriteProduct>(x => 
                x.AppUserId == favoriteDto.AppUserId && x.ProductId == favoriteDto.ProductId);
            
            var favorite = await _unitOfWork.Repository<FavoriteProduct>().GetEntityWithSpec(spec);

            if (favorite == null) return NotFound(new ApiResponse(404));

            _unitOfWork.Repository<FavoriteProduct>().Delete(favorite);
            
            if (await _unitOfWork.Complete() <= 0) 
                return BadRequest(new ApiResponse(400, "Problemă la ștergerea favoritului"));

            return Ok();
        }
    }
}