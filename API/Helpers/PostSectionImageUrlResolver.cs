using API.Dtos;
using AutoMapper;
using Core.Entities;

namespace API.Helpers
{
    // NOU: transformă "images/blog/x.jpg" în URL complet pentru imaginile din secțiuni
    public class PostSectionImageUrlResolver : IValueResolver<PostSection, PostSectionToReturnDto, string>
    {
        private readonly IConfiguration _config;

        public PostSectionImageUrlResolver(IConfiguration config)
        {
            _config = config;
        }

        public string Resolve(
            PostSection source,
            PostSectionToReturnDto destination,
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