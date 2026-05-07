
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
            
            // --- NOU: Le inițializăm ---
            _userManager = userManager;
            _context = context;
        }
        //[Cached(300)]
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

        //[Cached(300)]
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

       // [Cached(300)]
        [HttpGet("brands")]
        public async Task<ActionResult<IReadOnlyList<ProductBrand>>> GetProductBrands()
        {
            return Ok(await _productBrandRepo.ListAllAsync());
        }

       // [Cached(300)]
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
                ProducerId = user.Id, // <-- ATENȚIE: am pus virgulă aici!
                ProducerName = user.DisplayName // <-- ADAUGĂM NUMELE AICI
            };

            // 4. Salvăm în baza de date folosind repository-ul (sau contextul)
            _context.Products.Add(product); // Dacă folosești DbContext direct, lasă așa. Dacă folosești un UnitOfWork sau Generic Repository, ajustează linia.
            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Problemă la salvarea produsului.");

            return Ok(new { message = "Produs adăugat cu succes!" });
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")]
        [HttpGet("my-products")]
        public async Task<ActionResult<IReadOnlyList<ProductToReturnDto>>> GetMyProducts()
        {
            // 1. Găsim cine face cererea
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return Unauthorized();

            // 2. Aducem din baza de date doar produsele lui, inclusiv detaliile de Brand și Tip
            var products = await _context.Products
                .Include(p => p.ProductType)
                .Include(p => p.ProductBrand)
                .Where(p => p.ProducerId == user.Id)
                .ToListAsync();

            // 3. Le transformăm cu AutoMapper și le trimitem spre Angular
            return Ok(_mapper.Map<IReadOnlyList<Product>, IReadOnlyList<ProductToReturnDto>>(products));
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")]
        [HttpDelete("delete-product/{id}")]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            // 1. Găsim utilizatorul
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            // 2. Căutăm produsul în baza de date
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Produsul nu a fost găsit.");

            // 3. SECURITATE: Verificăm dacă produsul îi aparține acestui utilizator
            if (product.ProducerId != user.Id) 
            {
                return Forbid(); // Eroare 403: Nu are voie să se atingă de alt produs
            }

            // 4. Ștergem produsul
            _context.Products.Remove(product);
            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Eroare la ștergerea produsului.");

            return Ok(new { message = "Produs șters cu succes!" });
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")]
        [HttpPut("edit-product/{id}")]
        public async Task<ActionResult> EditProduct(int id, [FromForm] ProductEditDto productDto)
        {
            // 1. Verificăm cine face cererea
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized();

            // 2. Căutăm produsul
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Produsul nu a fost găsit.");

            // 3. Verificăm dacă îi aparține
            if (product.ProducerId != user.Id) return Forbid();

            // 4. Actualizăm datele textuale
            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Price = productDto.Price;
            product.ProductTypeId = productDto.ProductTypeId;
            product.ProductBrandId = productDto.ProductBrandId;
            product.ProducerName = user.DisplayName;;

            // 5. Actualizăm poza DOAR dacă a încărcat una nouă
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
                
                // Actualizăm calea pozei cu cea nouă
                product.PictureUrl = "images/products/" + fileName;
            }

            // 6. Salvăm modificările
            _context.Products.Update(product);
            var result = await _context.SaveChangesAsync() > 0;

            if (!result) return BadRequest("Eroare la salvarea modificărilor.");

            return Ok(new { message = "Produs actualizat cu succes!" });
        }
    }
}