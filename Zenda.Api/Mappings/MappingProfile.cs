using AutoMapper;
using Zenda.Api.DTOs;
using Zenda.Core.Entities;

namespace Zenda.Api.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        #region Prestadores

        CreateMap<Prestador, PrestadorReadDto>();

        CreateMap<PrestadorCreateDto, Prestador>();

        CreateMap<PrestadorUpdateDto, Prestador>();

        #endregion

        #region Disponibilidad

        CreateMap<Disponibilidad, DisponibilidadReadDto>();

        CreateMap<DisponibilidadCreateDto, Disponibilidad>()
            .ForMember(dest => dest.HoraInicio, opt => opt.MapFrom(src => TimeOnly.Parse(src.HoraInicio)))
            .ForMember(dest => dest.HoraFin, opt => opt.MapFrom(src => TimeOnly.Parse(src.HoraFin)));

        #endregion
    }
}