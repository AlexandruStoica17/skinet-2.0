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
        private readonly IUnitOfWork _unitOfWork;


        public ProductsController(
            IGenericRepository<Product> productsRepo,
            IGenericRepository<ProductBrand> productBrandRepo,
            IGenericRepository<ProductType> productTypeRepo,
            IMapper mapper,
            UserManager<AppUser> userManager,
            StoreContext context,
            IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _productTypeRepo = productTypeRepo;
            _productBrandRepo = productBrandRepo;
            _productsRepo = productsRepo;
            _userManager = userManager;
            _context = context;
            _unitOfWork = unitOfWork;
        }

      [HttpGet]
public async Task<ActionResult<Pagination<ProductToReturnDto>>> GetProducts(
    [FromQuery] ProductSpecParams productParams)
{
    var spec = new ProductsWithTypesAndBrandsSpecification(productParams);
    var countSpec = new ProductWithFiltersForCountSpecification(productParams);

    var products = await _productsRepo.ListAsync(spec);

    // MULTISELECT filters applied in-memory (cannot be done in EF expression tree)
    if (!string.IsNullOrEmpty(productParams.SkinTypes))
    {
        var skinList = productParams.SkinTypes.Split(',').Select(s => s.Trim()).ToList();
        products = products.Where(p =>
            !string.IsNullOrEmpty(p.SkinType) &&
            skinList.Any(s => p.SkinType.Contains(s))
        ).ToList();
    }

    if (!string.IsNullOrEmpty(productParams.Usages))
    {
        var usageList = productParams.Usages.Split(',').Select(s => s.Trim()).ToList();
        products = products.Where(p =>
            !string.IsNullOrEmpty(p.Usage) &&
            usageList.Any(s => p.Usage.Contains(s))
        ).ToList();
    }

    if (!string.IsNullOrEmpty(productParams.Benefits))
    {
        var benefitList = productParams.Benefits.Split(',').Select(s => s.Trim()).ToList();
        products = products.Where(p =>
            !string.IsNullOrEmpty(p.Benefits) &&
            benefitList.Any(s => p.Benefits.Contains(s))
        ).ToList();
    }

    if (!string.IsNullOrEmpty(productParams.Formulas))
    {
        var formulaList = productParams.Formulas.Split(',').Select(s => s.Trim()).ToList();
        products = products.Where(p =>
            !string.IsNullOrEmpty(p.Formula) &&
            formulaList.Any(s => p.Formula.Contains(s))
        ).ToList();
    }

    // Rating filter (uses ProductReviews table)
    if (productParams.MinRating > 0)
    {
        var allReviews = await _unitOfWork.Repository<ProductReview>().ListAllAsync();
        products = products.Where(p =>
        {
            var reviews = allReviews.Where(r => r.ProductId == p.Id).ToList();
            if (!reviews.Any()) return false;
            return reviews.Average(r => r.Rating) >= productParams.MinRating;
        }).ToList();
    }

    var totalItems = products.Count;

    // Apply paging manually after in-memory filters
    var pagedProducts = products
        .Skip(productParams.PageSize * (productParams.PageIndex - 1))
        .Take(productParams.PageSize)
        .ToList();

    var data = _mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(pagedProducts);

    return Ok(new Pagination<ProductToReturnDto>(
        productParams.PageIndex, productParams.PageSize, totalItems, data));
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
        return BadRequest("Your account is not approved yet.");

    // AUTO-CREATE BRAND from seller's display name if it doesn't exist
    // Admin can pick any brand; regular sellers get their own brand auto-assigned
    var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
    int brandId;

    if (isAdmin && productDto.ProductBrandId > 0)
    {
        // Admin selected a brand manually
        brandId = productDto.ProductBrandId;
    }
    else
    {
        // Regular seller: find or create their personal brand
        var sellerBrandName = user.DisplayName;
        var existingBrand = _context.ProductBrands
            .FirstOrDefault(b => b.Name == sellerBrandName);

        if (existingBrand != null)
        {
            brandId = existingBrand.Id;
        }
        else
        {
            // Create brand automatically from seller's display name
            var newBrand = new ProductBrand { Name = sellerBrandName };
            _context.ProductBrands.Add(newBrand);
            await _context.SaveChangesAsync();
            brandId = newBrand.Id;
        }
    }

    // Handle image upload
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
        ProductBrandId = brandId,     // uses auto-created or admin-selected brand
        PictureUrl = photoUrl,
        ProducerId = user.Id,
        ProducerName = user.DisplayName,
        // Multiselect fields stored as comma-separated strings
        SkinType  = productDto.SkinType,
        Usage     = productDto.Usage,
        Benefits  = productDto.Benefits,
        Formula   = productDto.Formula
    };

    _context.Products.Add(product);
    var result = await _context.SaveChangesAsync() > 0;

    if (!result) return BadRequest("Problem saving the product.");

    return Ok(new { message = "Product published successfully!" });
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

        // NEW: Recently added products for the "What's New" page
        // GET /api/products/recent?count=12
        [HttpGet("recent")]
        public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetRecentProducts(
            [FromQuery] int count = 12)
        {
            // Fetch all products and order by Id descending (newest first)
            var spec = new ProductsWithTypesAndBrandsSpecification(
                new ProductSpecParams { PageSize = 200, PageIndex = 1 });
            var allProducts = await _productsRepo.ListAsync(spec);

            var recentProducts = allProducts
                .OrderByDescending(p => p.Id)
                .Take(count)
                .ToList();

            return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(recentProducts));
        }

        // NEW: Related product suggestions based on keywords from name/description
        // GET /api/products/suggestions?keywords=lavender,oil&excludeId=5&count=4
        [HttpGet("suggestions")]
        public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetSuggestions(
            [FromQuery] string keywords,
            [FromQuery] int excludeId = 0,
            [FromQuery] int count = 4)
        {
            if (string.IsNullOrWhiteSpace(keywords))
                return Ok(new List<ProductToReturnDto>());

            var keywordList = keywords
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim().ToLower())
                .Where(k => k.Length > 2)
                .ToList();

            if (!keywordList.Any())
                return Ok(new List<ProductToReturnDto>());

            var spec = new ProductsWithTypesAndBrandsSpecification(
                new ProductSpecParams { PageSize = 1000, PageIndex = 1 });
            var allProducts = await _productsRepo.ListAsync(spec);

            // Score each product by how many keywords match name or description
            var suggestions = allProducts
                .Where(p => p.Id != excludeId)
                .Select(p => new
                {
                    Product = p,
                    Score = keywordList.Count(k =>
                        (p.Name?.ToLower().Contains(k) == true) ||
                        (p.Description?.ToLower().Contains(k) == true))
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(count)
                .Select(x => x.Product)
                .ToList();

            return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(suggestions));
        }


    }
}