using AutoMapper;
using Atlas.Application.Assets.Models;
using Atlas.Domain.Assets.Entities;

namespace Atlas.Application.Assets.Mappings;

public sealed class AssetsMappingProfile : Profile
{
    public AssetsMappingProfile()
    {
        CreateMap<Asset, AssetListItem>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id.ToString()));
    }
}