using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Transactions;
using Cwiczenia7.DTOs;
using Cwiczenia7.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
namespace Cwiczenia7.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    public async Task<CreateAppointmentResponseDto> AddAsync(CreateAppointmentRequestDto dto)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        command.Connection = connection;
        command.Transaction = (SqlTransaction) transaction;

        try {

        command.CommandText = """
        select IsActive from Doctors where IdDoctor = @id
        """;
        command.Parameters.AddWithValue("@id", dto.idDoctor);
        var doctor = await command.ExecuteScalarAsync();
        if(doctor is null)
        {
            throw new NotFoundException($"Doktor o id {dto.idDoctor} nie istnieje");
        }
        var isActiveDoctor = (bool) doctor!;
        if (!isActiveDoctor)
        {
            throw new ConflictException("Lekarz nie jest aktywny");
        }
        command.Parameters.Clear();
        command.CommandText = """
        select isActive from Patients where IdPatient = @id
        """;
        command.Parameters.AddWithValue("@id", dto.idPatient);
        var patient = await command.ExecuteScalarAsync();
        if(patient is null)
        {
            throw new NotFoundException("Nie znaleziono pacjenta");
        }
        var isActivePatient = (bool) patient!;
        if (!isActivePatient)
        {
            throw new ConflictException("Pacjent nie jest aktywny");
        }
        command.Parameters.Clear();

        //czy nowy termin nie jest w przeszlosci
        if(dto.appointmentDate < DateTime.Now)
            {
                throw new BadDataException("termin nie moze byc w przeszlosci");
            }

        //sprawdzanie czy nie ma przypisanych wizyt w danym terminie
        command.Parameters.Clear();
        command.CommandText = "select 1 from Appointments where AppointmentDate = @appointmentDate AND IdDoctor = @idDoctor";
        command.Parameters.AddWithValue("@appointmentDate", dto.appointmentDate);
        command.Parameters.AddWithValue("@idDoctor", dto.idDoctor);
        
        var appointment = await command.ExecuteScalarAsync();
        if(appointment is not null)
        {
            throw new ConflictException("Doktor ma w tym czasie zaplanowaną wizytę");
        }
        
        command.Parameters.Clear();
        command.CommandText = """
        insert into Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, InternalNotes)
        output Inserted.IdAppointment
        values(@idPatient, @idDoctor, @data, 'Scheduled', @reason, @notes)
        """;
        command.Parameters.AddWithValue("@idPatient", dto.idPatient);
        command.Parameters.AddWithValue("@idDoctor", dto.idDoctor);
        command.Parameters.AddWithValue("data", dto.appointmentDate);
        command.Parameters.AddWithValue("@reason", dto.reason);
        command.Parameters.AddWithValue("@notes", DBNull.Value);

        var appointmentId = await command.ExecuteScalarAsync();
        await transaction.CommitAsync();

        return new CreateAppointmentResponseDto
        {
            IdAppointment = (int) appointmentId!,
            PatientId = dto.idPatient,
            DoctorId = dto.idDoctor,
            AppointmentDate = dto.appointmentDate,
            Status = "Scheduled",
            Reason = dto.reason
        };
        } catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
        
    }

    public async Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? lastName)
    {
        var result = new List<AppointmentListDto>();

        var conditions = new List<string>();
        var parameters = new List<SqlParameter>();

        if(status is not null)
        {
            conditions.Add("Status = @status");
            parameters.Add(new SqlParameter("@Status", status));
        }
        if(lastName is not null)
        {
            conditions.Add("Patients.LastName = @lastName");
            parameters.Add(new SqlParameter("@lastName", lastName));
        }
       
        var sqlCommand = new StringBuilder("""
        select IdAppointment, AppointmentDate, Status, Reason, 
        Patients.FirstName || ' ' || Patients.LastName, Patients.Email
        From dbo.Appointments
        join Patients on Appointments.IdPatient = Patients.IdPatient
        """);

         if(parameters.Count > 0)
        {
            sqlCommand.Append(" WHERE ");
            sqlCommand.Append(string.Join(" AND ", conditions));
        }


        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = sqlCommand.ToString();
        command.Parameters.AddRange(parameters.ToArray()); 

        await connection.OpenAsync();

        var reader = await command.ExecuteReaderAsync();
        while(await reader.ReadAsync())
        {
            result.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5)
            });
        }
        return result;
    }

    public async Task<AppoinmentDetailsDto> GetByIdAsync(int id)
    {
        AppoinmentDetailsDto? result = null;
        const string sqlCommand = """
        select IdAppointment, AppointmentDate, Status, Reason, 
        Patients.FirstName || ' ' || Patients.LastName, Patients.Email,
        Doctors.FirstName || ' ' || Doctors.LastName, InternalNotes
        From dbo.Appointments
        join Patients on Appointments.IdPatient = Patients.IdPatient
        join Doctors on Appointments.IdDoctor = Doctors.IdDoctor
        where Appointments.IdAppointment = @Id;
        """;
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = sqlCommand;
        command.Parameters.AddWithValue("@Id", id);

        await connection.OpenAsync();

        var reader = await command.ExecuteReaderAsync();

        while(await reader.ReadAsync())
        {
            result ??= new AppoinmentDetailsDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5),
                DoctorFullName = reader.GetString(6),
                InternalNotes = reader.IsDBNull(7) ? null : reader.GetString(7)
            };
        }
            if(result is null)
        {
            throw new NotFoundException($"Appointment o id {id} nie istnieje");
        }
        return result;
    }

    public async Task RemoveAsync(int id)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        
        command.Connection = connection;

        await connection.OpenAsync();

        command.CommandText = "select 1 from Appointments where IdAppointment = @id";
        command.Parameters.AddWithValue("@id", id);
        
        var response  = await command.ExecuteScalarAsync();
        if(response is null)
        {
            throw new NotFoundException($"Nie znaleziono wizyty o id {id}");
        }

        command.Parameters.Clear();
        command.CommandText = "select Status from Appointments where IdAppointment = @id";
        command.Parameters.AddWithValue("@id", id);
        var scalar = await command.ExecuteScalarAsync();
        string status = (string) scalar!;
        if (status.Equals("Completed"))
        {
            throw new ConflictException("Nie mozna usunac wizyty ktora sie juz odbyla");
        }
        command.Parameters.Clear();
        await using var transaction = await connection.BeginTransactionAsync();
        command.Transaction = (SqlTransaction) transaction;
        try
        {
            command.CommandText = "delete from Appointments where IdAppointment = @id";
            command.Parameters.AddWithValue("@id", id);
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        } catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateAsync(int id, UpdateAppointmentDto dto)
    {
        //czy sie dane zgadzaja
        if(!(dto.status == "Scheduled" || dto.status == "Completed" || dto.status == "Cancelled"))
        {
            throw new BadDataException(
                "Status powinien mieć jedną z podanych wartości: Scheduled, Completed, Cancelled"
            );
        }
        if(dto.appointmentDate < DateTime.Now)
        {
            throw new BadDataException("Data nie moze byc w przeszlosci");
        }

        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        
        await connection.OpenAsync();
        command.CommandText = """
        select 1 from Appointments where IdAppointment = @id
        """;
        command.Parameters.AddWithValue("@id", id);
        var appointment = await command.ExecuteScalarAsync();
        if(appointment is null)
        {
            throw new NotFoundException("wizyta nie istnieje");
        }
        command.Parameters.Clear();

        command.CommandText = """
        select IsActive from Doctors where IdDoctor = @id
        """;
        command.Parameters.AddWithValue("@id", dto.idDoctor);
        var doctor = await command.ExecuteScalarAsync();
        if(doctor is null)
        {
            throw new NotFoundException($"Doktor o id {dto.idDoctor} nie istnieje");
        }
        var isActiveDoctor = (bool) doctor!;
        if (!isActiveDoctor)
        {
            throw new ConflictException("Lekarz nie jest aktywny");
        }
        command.Parameters.Clear();
        command.CommandText = """
        select isActive from Patients where IdPatient = @id
        """;
        command.Parameters.AddWithValue("@id", dto.idPatient);
        var patient = await command.ExecuteScalarAsync();
        if(patient is null)
        {
            throw new NotFoundException("Nie znaleziono pacjenta");
        }
        var isActivePatient = (bool) patient!;
        if (!isActivePatient)
        {
            throw new ConflictException("Pacjent nie jest aktywny");
        }
        command.Parameters.Clear();
        //czy ma w tym terminie inne wizyty
        command.CommandText = """
        select 1 
        from Appointments 
        where AppointmentDate = @appointmentDate 
        AND IdAppointment != @id AND IdDoctor = @idDoctor
        """;

        command.Parameters.AddWithValue("@appointmentDate", dto.appointmentDate);
        command.Parameters.AddWithValue("@idDoctor", dto.idDoctor);
        command.Parameters.AddWithValue("@id", id);
        
        var hasAppointment = await command.ExecuteScalarAsync();
        if(hasAppointment is not null)
        {
            throw new ConflictException("Doktor ma w tym czasie zaplanowaną wizytę");
        }
        
        command.Parameters.Clear();
        command.CommandText = """
        select Status from Appointments where IdAppointment = @id
        """;
        command.Parameters.AddWithValue("@id", id);
        var statusResult = await command.ExecuteScalarAsync();
        if(statusResult is null)
        {
            throw new ConflictException("Pole status jest null");
        }
        command.Parameters.Clear();
        var status = (string) statusResult!;

        command.CommandText = """
        update Appointments set
        IdPatient = @patientId,
        IdDoctor = @doctorId,
        AppointmentDate = ISNULL(@date, AppointmentDate),
        Status = @status,
        Reason = @reason,
        InternalNotes = @notes
        where IdAppointment = @id
        """;
        command.Parameters.AddWithValue("@patientId", dto.idPatient);
        command.Parameters.AddWithValue("@doctorId", dto.idDoctor);
        command.Parameters.AddWithValue("@date", status.Equals("Completed") ? DBNull.Value : dto.appointmentDate);
        command.Parameters.AddWithValue("@status", dto.status);
        command.Parameters.AddWithValue("@notes", dto.internalNotes);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@reason", dto.reason);
        
        await command.ExecuteNonQueryAsync();

    }
}