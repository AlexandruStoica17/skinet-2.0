using System.Data;
using API.Dtos;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.OrderAggregate;

namespace API.Helpers
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
           CreateMap<Product, ProductToReturnDto>()
    .ForMember(d => d.ProductBrand, o => o.MapFrom(s => s.ProductBrand.Name))
    .ForMember(d => d.ProductType, o => o.MapFrom(s => s.ProductType.Name))
    .ForMember(d => d.PictureUrl, o => o.MapFrom<ProductUrlResolver>())
    .ForMember(d => d.Photos, o => o.MapFrom(s => s.Photos));



            CreateMap<Core.Entities.Identity.Address, AddressDto>().ReverseMap();
            CreateMap<CustomerBasketDto, CustomerBasket>();
            CreateMap<BasketItemDto, BasketItem>();
            CreateMap<AddressDto, Core.Entities.OrderAggregate.Address>();
            CreateMap<Order, OrderToReturnDto>()
            .ForMember(d => d.DeliveryMethod, o => o.MapFrom(s => s.DeliveryMethod.ShortName))
            .ForMember(d => d.ShippingPrice, o => o.MapFrom(s => s.DeliveryMethod.Price));

            CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ItemOrdered.ProductItemId))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.ItemOrdered.ProductName))
            .ForMember(d => d.PictureUrl, o => o.MapFrom(s => s.ItemOrdered.PictureUrl))
            .ForMember(d => d.PictureUrl, o => o.MapFrom<OrderItemUrlResolver>());

            CreateMap<ProductPhoto, ProductPhotoDto>()
    .ForMember(d => d.Url, o => o.MapFrom<ProductPhotoUrlResolver>());

            // --- AICI ESTE CONFIGURAREA NOUĂ PENTRU BLOG ---
            CreateMap<Post, PostToReturnDto>()
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.AppUser.DisplayName));
            CreateMap<PostCreateDto, Post>();

            CreateMap<Comment, CommentToReturnDto>()
    .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.AppUser.DisplayName));

            CreateMap<CommentCreateDto, Comment>();
            CreateMap<Message, MessageDto>();
        }
    }
}