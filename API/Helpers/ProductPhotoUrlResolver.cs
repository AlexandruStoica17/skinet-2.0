using API.Dtos;
using AutoMapper;
using Core.Entities;

namespace API.Helpers
{
    public class ProductPhotoUrlResolver : IValueResolver<ProductPhoto, ProductPhotoDto, string>
    {
        private readonly IConfiguration _config;

        public ProductPhotoUrlResolver(IConfiguration config)
        {
            _config = config;
        }

        public string Resolve(
            ProductPhoto source,
            ProductPhotoDto destination,
            string destMember,
            ResolutionContext context)
        {
            if (!string.IsNullOrEmpty(source.Url))
            {
                return _config["ApiUrl"] + source.Url;
            }

            return null;
        }
    }
}