using API.Dtos;
using API.Errors;
using API.Extensions;
using Core.Entities;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ReviewsController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public ReviewsController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        // GET /api/reviews/product/5
        // Returneaza toate review-urile pentru un produs (public, fara autentificare)
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ProductReviewToReturnDto>>> GetProductReviews(int productId)
        {
            var all = await _unitOfWork.Repository<ProductReview>().ListAllAsync();
            var reviews = all
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProductReviewToReturnDto
                {
                    Id = r.Id,
                    BuyerName = r.BuyerName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToList();

            return Ok(reviews);
        }

        // GET /api/reviews/producer?email=cosmetic3@test.com
        // Returneaza toate review-urile pentru un vanzator (public)
        [HttpGet("producer")]
        public async Task<ActionResult<IEnumerable<ProducerReviewToReturnDto>>> GetProducerReviews(
            [FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
                return BadRequest(new ApiResponse(400, "Email required"));

            var all = await _unitOfWork.Repository<Review>().ListAllAsync();
            var reviews = all
                .Where(r => r.ProducerEmail == email)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProducerReviewToReturnDto
                {
                    Id = r.Id,
                    BuyerEmail = r.BuyerEmail,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToList();

            var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            return Ok(new ProducerReviewsResponseDto
            {
                Reviews = reviews,
                AverageRating = Math.Round(avgRating, 1),
                TotalReviews = reviews.Count
            });
        }

        // POST /api/reviews/product
        // Cumparatorul lasa review pentru un produs (necesita autentificare)
        [Authorize]
        [HttpPost("product")]
        public async Task<ActionResult> SubmitProductReview(ProductReviewDto reviewDto)
        {
            var buyerEmail = User.RetrieveEmailFromPrincipal();
            var buyer = await _userManager.FindByEmailAsync(buyerEmail);

            // Verificam sa nu fi lasat deja review pentru acest produs in aceasta comanda
            var all = await _unitOfWork.Repository<ProductReview>().ListAllAsync();
            var alreadyReviewed = all.Any(r =>
                r.ProductId == reviewDto.ProductId &&
                r.OrderId == reviewDto.OrderId &&
                r.BuyerEmail == buyerEmail);

            if (alreadyReviewed)
                return BadRequest(new ApiResponse(400, "Ai lăsat deja un review pentru acest produs."));

            var review = new ProductReview
            {
                ProductId = reviewDto.ProductId,
                OrderId = reviewDto.OrderId,
                BuyerEmail = buyerEmail,
                BuyerName = buyer?.DisplayName ?? buyerEmail,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment
            };

            _unitOfWork.Repository<ProductReview>().Add(review);
            await _unitOfWork.Complete();

            return Ok(new { message = "Review trimis cu succes!" });
        }
    }
}