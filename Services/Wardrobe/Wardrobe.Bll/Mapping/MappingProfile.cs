using AutoMapper;
using Wardrobe.Bll.Dtos;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Bll.Mapping;

/// <summary>
/// Профілі AutoMapper для проєціювання Entity -> DTO. DTO є record-ами, тому нестандартні поля
/// задаються через ForCtorParam (мапінг іде через конструктор).
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ItemDetail, ItemDetailDto>();
        CreateMap<ItemImage, ItemImageDto>();

        CreateMap<ItemCategory, CategoryRefDto>()
            .ForCtorParam(nameof(CategoryRefDto.Id), o => o.MapFrom(s => s.CategoryId))
            .ForCtorParam(nameof(CategoryRefDto.Name), o => o.MapFrom(s => s.Category.Name));

        CreateMap<WardrobeItem, WardrobeItemListDto>()
            .ForCtorParam(nameof(WardrobeItemListDto.Warmth),
                o => o.MapFrom(s => s.Detail != null ? (byte?)s.Detail.Warmth : null))
            .ForCtorParam(nameof(WardrobeItemListDto.PrimaryImageUrl),
                o => o.MapFrom(s => s.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()));

        CreateMap<WardrobeItem, WardrobeItemDetailsDto>()
            .ForCtorParam(nameof(WardrobeItemDetailsDto.Categories), o => o.MapFrom(s => s.ItemCategories));

        CreateMap<Category, CategoryDto>();

        CreateMap<ItemCategory, CategoryItemDto>()
            .ForCtorParam(nameof(CategoryItemDto.Name), o => o.MapFrom(s => s.Item.Name))
            .ForCtorParam(nameof(CategoryItemDto.Slot), o => o.MapFrom(s => s.Item.Slot));

        CreateMap<Category, CategoryWithItemsDto>()
            .ForCtorParam(nameof(CategoryWithItemsDto.Items), o => o.MapFrom(s => s.ItemCategories));
    }
}
