using AutoMapper;
using Atlas.Application.Alert.Models;
using Atlas.Domain.Alert.Entities;

namespace Atlas.Application.Alert.Mappings;

public sealed class AlertMappingProfile : Profile
{
    public AlertMappingProfile()
    {
        CreateMap<AlertRecord, AlertListItem>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id.ToString()));
    }
}