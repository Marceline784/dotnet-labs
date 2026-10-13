using AutoMapper;
using Recommendations.Bll.Dtos;
using Recommendations.Domain.Models;

namespace Recommendations.Bll.Mapping;

/// <summary>Усі профілі AutoMapper зібрано в одному місці BLL.</summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UserProfile, UserProfileDto>();
        CreateMap<WardrobeItem, WardrobeItemDto>();
        CreateMap<WardrobeItem, EligibleItemDto>();
        CreateMap<WeatherSnapshot, WeatherDto>();
        CreateMap<OutfitItem, OutfitItemDto>();
        CreateMap<Outfit, OutfitDto>();
        CreateMap<Recommendation, RecommendationDto>();
        CreateMap<Recommendation, RecommendationSummaryDto>();
    }
}
