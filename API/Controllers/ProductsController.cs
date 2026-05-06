
using System.Security.Claims;
using API.Dtos;
using API.Errors;
using API.Helpers;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Specifications;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    
    public class ProductsController : BaseApiController 
    {

        private readonly IGenericRepository<Product> _productsRepo;
        private readonly IGenericRepository<ProductBrand> _productBrandRepo;
        private readonly IGenericRepository<ProductType> _productTypeRepo;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly StoreContext _context;

       public ProductsController(
            IGenericRepository<Product> productsRepo, 
            IGenericRepository<ProductBrand> productBrandRepo, 
            IGenericRepository<ProductType> productTypeRepo, 
            IMapper mapper,
            UserManager<AppUser> userManager, 
            StoreContext context)
        {
            _mapper = mapper;
            _productTypeRepo = productTypeRepo;
            _productBrandRepo = productBrandRepo;
            _productsRepo = productsRepo;
            
            // --- NOU: Le inițializăm ---
            _userManager = userManager;
            _context = context;
        }
        [Cached(600)]
        [HttpGet]
        public async Task<ActionResult<Pagination<ProductToReturnDto>>> GetProducts([FromQuery]ProductSpecParams productParams)
        {

            var spec = new ProductsWithTypesAndBrandsSpecification(productParams);

            var countSpec = new ProductWithFiltersForCountSpecification(productParams);
            var totalItems = await _productsRepo.CountAsync(countSpec);
            var products = await _productsRepo.ListAsync(spec);
            var data = _mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products);

            return Ok(new Pagination<ProductToReturnDto>(productParams.PageIndex, productParams.PageSize, totalItems, data));
        }

        [Cached(600)]
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
         [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductToReturnDto>> GetProduct(int id)
        {
            var spec = new ProductsWithTypesAndBrandsSpecification(id);
            var product = await _productsRepo.GetEntityWithSpec(spec);

            if(product ==  null) return NotFound(new ApiResponse(404));

            return _mapper.Map<Product, ProductToReturnDto>(product);
        }

        [Cached(600)]
        [HttpGet("brands")]
        public async Task<ActionResult<IReadOnlyList<ProductBrand>>> GetProductBrands()
        {
            return Ok(await _productBrandRepo.ListAllAsync());
        }

        [Cached(600)]
        [HttpGet("types")]
        public async Task<ActionResult<IReadOnlyList<ProductType>>> GetProductTypes()
        {
            return Ok(await _productTypeRepo.ListAllAsync());
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")] // Doar producătorii pot accesa asta
        [HttpPost("add-product")]
        public async Task<ActionResult<ProductToReturnDto>> AddProduct([FromForm] ProductCreateDto productDto)
        {
            // 1. Verificăm dacă producătorul care apelează endpoint-ul a fost validat de Admin
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email); // Necesită injectarea UserManager în constructor dacă nu o ai
            
            if (user == null || !user.IsVerified)
            {
                return BadRequest("Contul tău nu este aprobat de administrator. Nu poți adăuga produse încă.");
            }

            // 2. Gestionăm salvarea imaginii
            var photoUrl = "";
            if (productDto.Picture != null && productDto.Picture.Length > 0)
            {
                // Generăm un nume unic pentru poză ca să nu se suprascrie
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.Picture.FileName);
                
                // Setăm calea către folderul de imagini publice al aplicației Angular (wwwroot)
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                // Salvăm fișierul
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await productDto.Picture.CopyToAsync(fileStream);
                }

                // Asta e calea pe care o salvăm în DB, ca Angular să știe de unde să o citească
                photoUrl = "images/products/" + fileName;
            }

            // 3. Creăm entitatea Product
            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                ProductTypeId = productDto.ProductTypeId,
                ProductBrandId = productDto.ProductBrandId,
                PictureUrl = photoUrl,
                ProducerId = user.Id // Salvăm ID-ul producătorului!
            };

            // 4. Salvăm în baza de date folosind repository-ul (sau contextul)
            _context.Products.Add(product); // Dacă folosești DbContext direct, lasă așa. Dacă folosești un UnitOfWork sau Generic Repository, ajustează linia.
            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Problemă la salvarea produsului.");

            return Ok(new { message = "Produs adăugat cu succes!" });
        }
    }
}