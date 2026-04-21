using Cwiczenia7.DTOs;
namespace Cwiczenia7.Services;
public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? LastName);
    Task<AppoinmentDetailsDto> GetByIdAsync(int id);
    Task<CreateAppointmentResponseDto> AddAsync(CreateAppointmentRequestDto dto);
    Task RemoveAsync(int id);
    Task UpdateAsync(int id, UpdateAppointmentDto dto);
} 