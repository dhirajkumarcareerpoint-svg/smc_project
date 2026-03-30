using System.ComponentModel.DataAnnotations;

namespace SmcStreetlight.Api.Models;

public class AppUser
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(15)] public string Mobile { get; set; } = string.Empty;
    [MaxLength(50)] public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class OtpCode
{
    public int Id { get; set; }
    [MaxLength(15)] public string Mobile { get; set; } = string.Empty;
    [MaxLength(6)] public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }
}

public class StreetLightRequest
{
    public int Id { get; set; }
    [MaxLength(25)] public string ApplicationNumber { get; set; } = string.Empty;
    [MaxLength(100)] public string CitizenName { get; set; } = string.Empty;
    [MaxLength(15)] public string CitizenMobile { get; set; } = string.Empty;
    [MaxLength(500)] public string Address { get; set; } = string.Empty;
    [MaxLength(50)] public string Ward { get; set; } = string.Empty;
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    [MaxLength(50)] public string CurrentStatus { get; set; } = "Submitted";
    public decimal? EstimatedAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class StatusHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public StreetLightRequest? Request { get; set; }
    [MaxLength(50)] public string Status { get; set; } = string.Empty;
    [MaxLength(1000)] public string Remarks { get; set; } = string.Empty;
    [MaxLength(50)] public string ActionByRole { get; set; } = string.Empty;
    public int? ActionByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RequestFile
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public StreetLightRequest? Request { get; set; }
    [MaxLength(50)] public string FileType { get; set; } = string.Empty;
    [MaxLength(300)] public string FileName { get; set; } = string.Empty;
    [MaxLength(300)] public string StoragePath { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    [MaxLength(50)] public string UploadedByRole { get; set; } = string.Empty;
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsLocked { get; set; } = true;
}

public class ActionLog
{
    public int Id { get; set; }
    public int? RequestId { get; set; }
    public int? UserId { get; set; }
    [MaxLength(100)] public string Action { get; set; } = string.Empty;
    [MaxLength(1000)] public string Details { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class StreetLightDemandApplication
{
    public int Id { get; set; }
    [MaxLength(30)] public string ApplicationNumber { get; set; } = string.Empty;
    [MaxLength(20)] public string ApplicantType { get; set; } = string.Empty; // MLA | Corporator | Citizen

    // Shared/common fields
    [MaxLength(100)] public string ApplicantName { get; set; } = string.Empty;
    [MaxLength(20)] public string MobileNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string? WardNumber { get; set; }
    [MaxLength(500)] public string FullAddress { get; set; } = string.Empty;
    [MaxLength(500)] public string LocationDetails { get; set; } = string.Empty;
    public int NumberOfLightsRequested { get; set; }
    [MaxLength(1000)] public string DescriptionOfRequest { get; set; } = string.Empty;

    // MLA/Corporator specific
    [MaxLength(50)] public string? LetterReferenceNumber { get; set; }
    public DateTime? LetterDate { get; set; }
    [MaxLength(100)] public string? AssemblyConstituency { get; set; }
    [MaxLength(300)] public string? LetterFileName { get; set; }
    [MaxLength(300)] public string? LetterFilePath { get; set; }

    // Workflow status
    [MaxLength(50)] public string CurrentStatus { get; set; } = "Submitted";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
