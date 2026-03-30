namespace SmcStreetlight.Api.Contracts;

public record CitizenRequestCreateDto(string CitizenName, string CitizenMobile, string Address, string Ward, string Description);
public record LoginDto(string LoginId, string Password);
public record StatusUpdateDto(string Status, string Remarks);
public record DecisionUpdateDto(string Decision, string Remarks);
public record StatusHistoryDto(string Status, string Remarks, string ActionByRole, DateTime CreatedAtUtc);
public record TrackResponseDto(string ApplicationNumber, string CurrentStatus, DateTime SubmittedAtUtc, IReadOnlyList<StatusHistoryDto> History);
