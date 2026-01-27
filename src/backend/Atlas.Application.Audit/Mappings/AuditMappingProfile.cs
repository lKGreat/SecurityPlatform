using AutoMapper;
using Atlas.Application.Audit.Models;
using Atlas.Domain.Audit.Entities;

namespace Atlas.Application.Audit.Mappings;

public sealed class AuditMappingProfile : Profile
{
    public AuditMappingProfile()
    {
        CreateMap<AuditRecord, AuditListItem>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id.ToString()));
    }
}
