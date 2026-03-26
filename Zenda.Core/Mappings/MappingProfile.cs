using AutoMapper;
using Zenda.Core.DTOs;
using Zenda.Core.Entities;

namespace Zenda.Core.Mappings;

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

        CreateMap<Disponibilidad, DisponibilidadReadDto>()
            .ForMember(dest => dest.HoraInicio, opt => opt.MapFrom(src => src.HoraInicio.ToString("HH:mm")))
            .ForMember(dest => dest.HoraFin, opt => opt.MapFrom(src => src.HoraFin.ToString("HH:mm")));

        #endregion

        #region Turnos

        // De Entidad a Lectura: Como nombramos las propiedades igual en ambos lados 
        // (FechaHoraInicioUtc, EmailClienteInvitado, etc.), AutoMapper hace la magia solo.
        CreateMap<Turno, TurnoReadDto>();

        // De Creación a Entidad: Ajustado a las nuevas propiedades
        CreateMap<TurnoCreateDto, Turno>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NegocioId, opt => opt.Ignore()) // Lo asignamos en el servicio
            .ForMember(dest => dest.FechaHoraInicioUtc, opt => opt.MapFrom(src => src.Inicio))
            .ForMember(dest => dest.FechaHoraFinUtc, opt => opt.Ignore()) // Lo calcula el servicio según la duración del barbero
            .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => "Pendiente"));

        #endregion

        #region Sedes

        CreateMap<Sede, SedeReadDto>();
        CreateMap<SedeCreateDto, Sede>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        #endregion
    }
}