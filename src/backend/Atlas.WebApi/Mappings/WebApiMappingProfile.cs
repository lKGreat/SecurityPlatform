using AutoMapper;
using Atlas.Application.Models;
using Atlas.WebApi.Models;

namespace Atlas.WebApi.Mappings;

public sealed class WebApiMappingProfile : Profile
{
    public WebApiMappingProfile()
    {
        CreateMap<AuthTokenViewModel, AuthTokenRequest>();
        CreateMap<AuthRefreshViewModel, AuthRefreshRequest>();
        CreateMap<ChangePasswordViewModel, ChangePasswordRequest>();
    }
}
