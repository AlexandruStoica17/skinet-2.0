using API.Dtos;
using API.Errors;
using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class BlogController : BaseApiController
    {
        private readonly IGenericRepository<Post> _postRepo;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public BlogController(IGenericRepository<Post> postRepo, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _postRepo = postRepo;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
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

        [HttpPost]
        public async Task<ActionResult<PostToReturnDto>> CreatePost(PostCreateDto postToCreate)
        {
            var post = _mapper.Map<PostCreateDto, Post>(postToCreate);
            post.CreatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Post>().Add(post);

            var result = await _unitOfWork.Complete();

            if (result <= 0) return BadRequest(new ApiResponse(400, "Problem creating post"));

            var spec = new PostsWithAuthorSpecification(post.Id);
            var postToReturn = await _unitOfWork.Repository<Post>().GetEntityWithSpec(spec);

            return Ok(_mapper.Map<Post, PostToReturnDto>(postToReturn));
        }

        // 1. Obține toate comentariile pentru o postare specifică
        // GET: api/blog/1/comments
        [HttpGet("{postId}/comments")]
        public async Task<ActionResult<IReadOnlyList<CommentToReturnDto>>> GetComments(int postId)
        {
            var spec = new CommentsWithUserSpecification(postId);
            var comments = await _unitOfWork.Repository<Comment>().ListAsync(spec);

            return Ok(_mapper.Map<IReadOnlyList<Comment>, IReadOnlyList<CommentToReturnDto>>(comments));
        }

        // 2. Adaugă un comentariu la o postare
        // POST: api/blog/comments
        [HttpPost("comments")]
        public async Task<ActionResult<CommentToReturnDto>> AddComment(CommentCreateDto commentDto)
        {
            var comment = _mapper.Map<CommentCreateDto, Comment>(commentDto);
            comment.CreatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Comment>().Add(comment);
            var result = await _unitOfWork.Complete();

            if (result <= 0) return BadRequest(new ApiResponse(400, "Problem adding comment"));

            // Îl extragem din nou cu Specificația pentru a avea numele autorului în răspuns
            var spec = new CommentsWithUserSpecification(comment.PostId);
            // Căutăm comentariul proaspăt creat
            var comments = await _unitOfWork.Repository<Comment>().ListAsync(spec);
            var commentToReturn = comments.FirstOrDefault(x => x.Id == comment.Id);

            return Ok(_mapper.Map<Comment, CommentToReturnDto>(commentToReturn));
        }
    }
}