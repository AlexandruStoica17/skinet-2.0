using API.Dtos;
using AutoMapper;
using Core.Entities;

namespace API.Helpers
{
    public class PostImageUrlResolver : IValueResolver<Post, PostToReturnDto, string>
    {
        private readonly IConfiguration _config;

        public PostImageUrlResolver(IConfiguration config)
        {
            _config = config;
        }

        public string Resolve(
            Post source,
            PostToReturnDto destination,
            string destMember,
            ResolutionContext context)
        {
            if (!string.IsNullOrEmpty(source.ImageUrl))
            {
                return _config["ApiUrl"] + source.ImageUrl;
            }

            return null;
        }
    }
}