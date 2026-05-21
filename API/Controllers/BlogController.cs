using System.Security.Claims;
using System.Text.Json;
using API.Dtos;
using API.Errors;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class BlogController : BaseApiController
    {
        private readonly IGenericRepository<Post> _postRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly StoreContext _context;

        public BlogController(
    IGenericRepository<Post> postRepo,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    UserManager<AppUser> userManager,
    StoreContext context)
{
    _postRepo = postRepo;
    _mapper = mapper;
    _unitOfWork = unitOfWork;
    _userManager = userManager;
    _context = context;
}

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PostToReturnDto>>> GetPosts()
        {
            var spec = new PostsWithAuthorSpecification();
            var posts = await _postRepo.ListAsync(spec);

            return Ok(_mapper.Map<IReadOnlyList<Post>, IReadOnlyList<PostToReturnDto>>(posts));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PostToReturnDto>> GetPost(int id)
        {
            var spec = new PostsWithAuthorSpecification(id);
            var post = await _postRepo.GetEntityWithSpec(spec);

            if (post == null) return NotFound(new ApiResponse(404));

            return _mapper.Map<Post, PostToReturnDto>(post);
        }

        [Authorize(Roles = "Blogger")]
        [HttpPost]
        public async Task<ActionResult<PostToReturnDto>> CreatePost([FromForm] PostCreateDto postToCreate)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return Unauthorized(new ApiResponse(401, "Trebuie să fii logat ca blogger."));

            var sections = new List<PostSectionCreateDto>();

            // NOU: secțiunile vin din Angular ca JSON
            if (!string.IsNullOrWhiteSpace(postToCreate.SectionsJson))
            {
                sections = JsonSerializer.Deserialize<List<PostSectionCreateDto>>(
                    postToCreate.SectionsJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new List<PostSectionCreateDto>();
            }

            var post = new Post
            {
                Title = postToCreate.Title,
                CreatedAt = DateTime.UtcNow,
                AppUserId = user.Id,

                // Content se păstrează pentru articole vechi/search/sugestii.
                // Dacă nu vine Content separat, îl construim din secțiuni.
                Content = !string.IsNullOrWhiteSpace(postToCreate.Content)
                    ? postToCreate.Content
                    : BuildContentFromSections(sections)
            };

            // Imagine copertă
            if (postToCreate.Image != null && postToCreate.Image.Length > 0)
            {
                post.ImageUrl = await SaveBlogImageAsync(postToCreate.Image);
            }

            // NOU: adăugăm secțiuni cu imagini multiple
            for (int i = 0; i < sections.Count; i++)
            {
                var sectionDto = sections[i];

                var section = new PostSection
                {
                    Heading = sectionDto.Heading,
                    Text = sectionDto.Text,
                    Caption = sectionDto.Caption,
                    DisplayOrder = i + 1
                };

                // Angular trimite imaginile ca sectionImages_0, sectionImages_1 etc.
                var sectionImage = Request.Form.Files
                    .FirstOrDefault(f => f.Name == $"sectionImages_{i}");

                if (sectionImage != null && sectionImage.Length > 0)
                {
                    section.ImageUrl = await SaveBlogImageAsync(sectionImage);
                }

                post.Sections.Add(section);
            }

            // Dacă nu ai ales cover image, folosim prima imagine din articol ca imagine de card.
            if (string.IsNullOrEmpty(post.ImageUrl))
            {
                var firstSectionImage = post.Sections
                    .OrderBy(s => s.DisplayOrder)
                    .FirstOrDefault(s => !string.IsNullOrEmpty(s.ImageUrl));

                if (firstSectionImage != null)
                {
                    post.ImageUrl = firstSectionImage.ImageUrl;
                }
            }

            _unitOfWork.Repository<Post>().Add(post);

            var result = await _unitOfWork.Complete();

            if (result <= 0)
                return BadRequest(new ApiResponse(400, "Problem creating post"));

            var spec = new PostsWithAuthorSpecification(post.Id);
            var postToReturn = await _unitOfWork.Repository<Post>().GetEntityWithSpec(spec);

            return Ok(_mapper.Map<Post, PostToReturnDto>(postToReturn));
        }

        [HttpGet("{postId}/comments")]
        public async Task<ActionResult<IReadOnlyList<CommentToReturnDto>>> GetComments(int postId)
        {
            var spec = new CommentsWithUserSpecification(postId);
            var comments = await _unitOfWork.Repository<Comment>().ListAsync(spec);

            return Ok(_mapper.Map<IReadOnlyList<Comment>, IReadOnlyList<CommentToReturnDto>>(comments));
        }

       [Authorize]
[HttpPost("comments")]
public async Task<ActionResult<CommentToReturnDto>> AddComment(CommentCreateDto commentDto)
{
    var email = HttpContext.User.FindFirstValue(ClaimTypes.Email);
    var user = await _userManager.FindByEmailAsync(email);

    if (user == null)
        return Unauthorized(new ApiResponse(401, "Trebuie să fii logat pentru a comenta"));

    // MODIFICAT: trimitem commentDto către AutoMapper
    var comment = _mapper.Map<CommentCreateDto, Comment>(commentDto);

    comment.CreatedAt = DateTime.UtcNow;
    comment.AppUserId = user.Id;

    _unitOfWork.Repository<Comment>().Add(comment);

    var result = await _unitOfWork.Complete();

    if (result <= 0)
        return BadRequest(new ApiResponse(400, "Problem adding comment"));

    var spec = new CommentsWithUserSpecification(comment.PostId);
    var comments = await _unitOfWork.Repository<Comment>().ListAsync(spec);

    var commentToReturn = comments.FirstOrDefault(x => x.Id == comment.Id);

    return Ok(_mapper.Map<Comment, CommentToReturnDto>(commentToReturn));
}

        private string BuildContentFromSections(List<PostSectionCreateDto> sections)
        {
            return string.Join(
                "\n\n",
                sections.Select(s => $"{s.Heading}\n{s.Text}".Trim())
            );
        }

        private async Task<string> SaveBlogImageAsync(IFormFile image)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            var folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "blog"
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "images/blog/" + fileName;
        }
        [Authorize(Roles = "Blogger")]
[HttpGet("my-posts")]
public async Task<ActionResult<IReadOnlyList<PostToReturnDto>>> GetMyPosts()
{
    var email = User.FindFirstValue(ClaimTypes.Email);
    var user = await _userManager.FindByEmailAsync(email);

    if (user == null)
        return Unauthorized(new ApiResponse(401, "Trebuie să fii logat ca blogger."));

    var posts = await _context.Posts
        .Include(p => p.AppUser)
        .Include(p => p.Sections)
        .Where(p => p.AppUserId == user.Id)
        .OrderByDescending(p => p.CreatedAt)
        .ToListAsync();

    return Ok(_mapper.Map<IReadOnlyList<Post>, IReadOnlyList<PostToReturnDto>>(posts));
}


// NOU: editare articol blog
// PUT: api/blog/edit/5
[Authorize(Roles = "Blogger")]
[HttpPut("edit/{id}")]
public async Task<ActionResult<PostToReturnDto>> UpdatePost(int id, [FromForm] PostUpdateDto postToUpdate)
{
    var email = User.FindFirstValue(ClaimTypes.Email);
    var user = await _userManager.FindByEmailAsync(email);

    if (user == null)
        return Unauthorized(new ApiResponse(401, "Trebuie să fii logat ca blogger."));

    var post = await _context.Posts
        .Include(p => p.AppUser)
        .Include(p => p.Sections)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (post == null)
        return NotFound(new ApiResponse(404, "Articolul nu a fost găsit."));

    if (post.AppUserId != user.Id)
        return Forbid();

    post.Title = postToUpdate.Title;
    post.Content = postToUpdate.Content;
    
    // MODIFICAT: dacă userul a selectat o copertă nouă, o înlocuim
    if (postToUpdate.Image != null && postToUpdate.Image.Length > 0)
    {
        post.ImageUrl = await SaveBlogImageAsync(postToUpdate.Image);
    }

    // NOU: ștergere cover image, dacă este bifată din frontend
    if (postToUpdate.RemoveCoverImage)
    {
        post.ImageUrl = null;
    }

    var sectionDtos = new List<PostSectionUpdateDto>();

    if (!string.IsNullOrWhiteSpace(postToUpdate.SectionsJson))
    {
        sectionDtos = JsonSerializer.Deserialize<List<PostSectionUpdateDto>>(
            postToUpdate.SectionsJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? new List<PostSectionUpdateDto>();
    }

    // NOU: ștergem secțiunile care nu mai există în formular
    var incomingExistingIds = sectionDtos
        .Where(s => s.Id.HasValue && s.Id.Value > 0)
        .Select(s => s.Id.Value)
        .ToList();

    var sectionsToRemove = post.Sections
        .Where(s => !incomingExistingIds.Contains(s.Id))
        .ToList();

    foreach (var sectionToRemove in sectionsToRemove)
    {
        _context.PostSections.Remove(sectionToRemove);
    }

    // NOU: actualizăm secțiunile existente și adăugăm secțiuni noi
    for (int i = 0; i < sectionDtos.Count; i++)
    {
        var sectionDto = sectionDtos[i];

        PostSection section;

        if (sectionDto.Id.HasValue && sectionDto.Id.Value > 0)
        {
            section = post.Sections.FirstOrDefault(s => s.Id == sectionDto.Id.Value);

            if (section == null)
            {
                continue;
            }
        }
        else
        {
            section = new PostSection
            {
                PostId = post.Id
            };

            post.Sections.Add(section);
        }

        section.Heading = sectionDto.Heading;
        section.Text = sectionDto.Text;
        section.Caption = sectionDto.Caption;
        section.DisplayOrder = i + 1;

        if (sectionDto.RemoveImage)
        {
            section.ImageUrl = null;
        }

        // Angular trimite imaginile secțiunilor ca sectionImages_0, sectionImages_1 etc.
        var sectionImage = Request.Form.Files
            .FirstOrDefault(f => f.Name == $"sectionImages_{i}");

        if (sectionImage != null && sectionImage.Length > 0)
        {
            section.ImageUrl = await SaveBlogImageAsync(sectionImage);
        }
    }

    // MODIFICAT: dacă nu mai există content separat, îl refacem din secțiuni
    if (string.IsNullOrWhiteSpace(post.Content))
    {
        post.Content = BuildContentFromSections(
            sectionDtos.Select(s => new PostSectionCreateDto
            {
                Heading = s.Heading,
                Text = s.Text,
                Caption = s.Caption,
                DisplayOrder = s.DisplayOrder
            }).ToList()
        );
    }

    await _context.SaveChangesAsync();

    var updatedPost = await _context.Posts
        .Include(p => p.AppUser)
        .Include(p => p.Sections)
        .FirstOrDefaultAsync(p => p.Id == id);

    return Ok(_mapper.Map<Post, PostToReturnDto>(updatedPost));
}


// Opțional, dar util: ștergere articol propriu
// DELETE: api/blog/delete/5
[Authorize(Roles = "Blogger")]
[HttpDelete("delete/{id}")]
public async Task<ActionResult> DeletePost(int id)
{
    var email = User.FindFirstValue(ClaimTypes.Email);
    var user = await _userManager.FindByEmailAsync(email);

    if (user == null)
        return Unauthorized(new ApiResponse(401, "Trebuie să fii logat ca blogger."));

    var post = await _context.Posts
        .Include(p => p.Sections)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (post == null)
        return NotFound(new ApiResponse(404, "Articolul nu a fost găsit."));

    if (post.AppUserId != user.Id)
        return Forbid();

    _context.Posts.Remove(post);

    await _context.SaveChangesAsync();

    return Ok(new { message = "Articolul a fost șters." });
}
    }
}