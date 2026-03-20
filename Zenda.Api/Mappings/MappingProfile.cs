using AutoMapper;
using Zenda.Api.DTOs;
using Zenda.Core.Entities;

namespace Zenda.Api.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // De Entidad a DTO (para GET)
        CreateMap<Prestador, PrestadorReadDto>();

        // De DTO a Entidad (para POST)
        CreateMap<PrestadorCreateDto, Prestador>();
    }
}