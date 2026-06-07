using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zenda.Core.DTOs;

namespace Zenda.Core.Interfaces
{
    public interface IAvisoService
    {
        Task<AvisoDto?> GetAvisoActivoAsync();
        Task<IEnumerable<AvisoDto>> GetAllAsync();

        Task<AvisoDto> CreateAsync(AvisoDto avisoDto);
        Task<AvisoDto> UpdateAsync(Guid id, AvisoDto avisoDto);
        Task DeleteAsync(Guid id);

        Task ActivarAvisoAsync(Guid id);
    }
}