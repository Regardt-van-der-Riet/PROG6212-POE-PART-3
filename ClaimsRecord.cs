using System.ComponentModel.DataAnnotations;

public partial class ClaimsRecord
{
    [Key]
    public int ClaimId { get; set; }
    public int LecturerId { get; set; }
    public string LecturerName { get; set; }
    public string LecturerSurname { get; set; }
    public decimal HourlyRate { get; set; }
    public int OvertimeHoursWorked { get; set; }
    public decimal OvertimeHourlyRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public string AdditionalNotes { get; set; }
    public string Status { get; set; }
    public string RejectionReason { get; set; } // New property
    public byte[] UploadedFile { get; set; }
}
