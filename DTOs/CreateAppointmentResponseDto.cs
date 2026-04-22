using System.ComponentModel.DataAnnotations;

namespace Cwiczenia7.DTOs;
public class CreateAppointmentResponseDto
{
    public int IdAppointment { get; set; }
   public int PatientId { get; set; }
   public int DoctorId { get; set; } 
   public DateTime AppointmentDate { get; set; }
   public string Status {get; set;} = string.Empty;
   public string Reason {get; set;} = string.Empty;
   
}