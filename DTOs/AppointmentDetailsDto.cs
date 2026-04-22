namespace Cwiczenia7.DTOs;
public class AppoinmentDetailsDto
{
    public int IdAppointment { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string DoctorFullName {get; set;} = string.Empty;
    public string InternalNotes { get; set; } = string.Empty;
    
}