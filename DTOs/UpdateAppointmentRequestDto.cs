using System.ComponentModel.DataAnnotations;

namespace Cwiczenia7.DTOs;
public class UpdateAppointmentDto
{
    [Required]
    public int idPatient { get; set; }
    [Required]
    public int idDoctor { get; set; } 
    [Required]
    public string status { get; set; } = string.Empty; 
    [Required]
    public DateTime appointmentDate { get; set; }
    [MaxLength(250)]
    public string reason {get; set;} = string.Empty;
    [MaxLength(500)]
    public string internalNotes {get; set;} = string.Empty;
}