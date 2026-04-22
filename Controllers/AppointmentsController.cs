using Cwiczenia7.DTOs;
using Cwiczenia7.Exceptions;
using Cwiczenia7.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cwiczenia7.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? patientLastName
    )
    {
        return Ok(await service.GetAllAsync(status, patientLastName));
    }

    [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(
        [FromRoute] int id
        )
    {
        try
        {
            return Ok(await service.GetByIdAsync(id));
        } catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateAppointmentRequestDto dto
    )
    {
        try
        {
            var newAppointment = await service.AddAsync(dto);
            return Created($"/api/appointments/{newAppointment.IdAppointment}", newAppointment);
        } catch (NotFoundException e)
        {
            return NotFound(e.Message);
        } catch (ConflictException e)
        {
            return Conflict(e.Message);
        } catch (BadDataException e)
        {
            return BadRequest(e.Message);
        }
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id
    )
    {
        try
        {
            await service.RemoveAsync(id);
            return NoContent();
        } catch (NotFoundException e)
        {
            return NotFound(e.Message);
        } catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] int id,
        [FromBody] UpdateAppointmentDto dto
    )
    {
        try
        {
            await service.UpdateAsync(id, dto);
            return NoContent();
        } catch (NotFoundException e)
        {
            return NotFound(e.Message);
        } catch (ConflictException e)
        {
            return Conflict(e.Message);
        } catch (BadDataException e)
        {
            return BadRequest(e.Message);
        }
    
    }

}