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
using Microsoft.EntityFrameworkCore;

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
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<Pagination<ProductToReturnDto>>> GetProducts([FromQuery] ProductSpecParams productParams)
        {
            var spec = new ProductsWithTypesAndBrandsSpecification(productParams);
            var countSpec = new ProductWithFiltersForCountSpecification(productParams);
            var totalItems = await _productsRepo.CountAsync(countSpec);
            var products = await _productsRepo.ListAsync(spec);
            var data = _mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products);

            // Populăm ProducerEmail pentru fiecare produs din listă
            foreach (var dto in data)
            {
                var matchedProduct = products.FirstOrDefault(p => p.Id == dto.Id);
                if (matchedProduct?.ProducerId != null)
                {
                    var producer = await _userManager.FindByIdAsync(matchedProduct.ProducerId);
                    if (producer != null) dto.ProducerEmail = producer.Email;
                }
            }

            return Ok(new Pagination<ProductToReturnDto>(productParams.PageIndex, productParams.PageSize, totalItems, data));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductToReturnDto>> GetProduct(int id)
        {
            var spec = new ProductsWithTypesAndBrandsSpecification(id);
            var product = await _productsRepo.GetEntityWithSpec(spec);

            if (product == null) return NotFound(new ApiResponse(404));

            var dto = _mapper.Map<Product, ProductToReturnDto>(product);

            // NOU: Populăm email-ul producătorului pentru butonul de chat
            if (!string.IsNullOrEmpty(product.ProducerId))
            {
                var producer = await _userManager.FindByIdAsync(product.ProducerId);
                if (producer != null) dto.ProducerEmail = producer.Email;
            }

            return dto;
        }

        [HttpGet("brands")]
        public async Task<ActionResult<IReadOnlyList<ProductBrand>>> GetProductBrands()
        {
            return Ok(await _productBrandRepo.ListAllAsync());
        }

        [HttpGet("types")]
        public async Task<ActionResult<IReadOnlyList<ProductType>>> GetProductTypes()
        {
            return Ok(await _productTypeRepo.ListAllAsync());
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")]
        [HttpPost("add-product")]
        public async Task<ActionResult<ProductToReturnDto>> AddProduct([FromForm] ProductCreateDto productDto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !user.IsVerified)
                return BadRequest("Contul tău nu este aprobat de administrator. Nu poți adăuga produse încă.");

            var photoUrl = "";
            if (productDto.Picture != null && productDto.Picture.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.Picture.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await productDto.Picture.CopyToAsync(fileStream);
                }
                photoUrl = "images/products/" + fileName;
            }

            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Price = productDto.Price,
                ProductTypeId = productDto.ProductTypeId,
                ProductBrandId = productDto.ProductBrandId,
                PictureUrl = photoUrl,
                ProducerId = user.Id,
                ProducerName = user.DisplayName
            };

            _context.Products.Add(product);
            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Problemă la salvarea produsului.");

            return Ok(new { message = "Produs adăugat cu succes!" });
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")]
        [HttpGet("my-products")]
        public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetMyProducts()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized();

            var products = await _context.Products
                .Include(p => p.ProductType)
                .Include(p => p.ProductBrand)
                .Where(p => p.ProducerId == user.Id)
                .ToListAsync();

            return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products));
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")]
        [HttpDelete("delete-product/{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Produsul nu a fost găsit.");

            if (product.ProducerId != user.Id) return Forbid();

            _context.Products.Remove(product);
            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Eroare la ștergerea produsului.");

            return Ok(new { message = "Produs șters cu succes!" });
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")]
        [HttpPut("edit-product/{id}")]
        public async Task<ActionResult> EditProduct(int id, [FromForm] ProductEditDto productDto)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Produsul nu a fost găsit.");

            if (product.ProducerId != user.Id) return Forbid();

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.ProductTypeId = productDto.ProductTypeId;
            product.ProductBrandId = productDto.ProductBrandId;
            product.ProducerName = user.DisplayName;

            if (productDto.Picture != null && productDto.Picture.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(productDto.Picture.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await productDto.Picture.CopyToAsync(fileStream);
                }
                product.PictureUrl = "images/products/" + fileName;
            }

            _context.Products.Update(product);
            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Eroare la salvarea modificărilor.");

            return Ok(new { message = "Produs actualizat cu succes!" });
        }
    }
}