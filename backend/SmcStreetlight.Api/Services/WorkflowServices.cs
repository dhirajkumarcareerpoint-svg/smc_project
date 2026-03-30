using Microsoft.EntityFrameworkCore;
using SmcStreetlight.Api.Data;
using SmcStreetlight.Api.Models;

namespace SmcStreetlight.Api.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        db.Users.AddRange(
            new AppUser { Name = "JE User", Mobile = "9000000001", Role = "JE" },
            new AppUser { Name = "AE User", Mobile = "9000000002", Role = "AE" },
            new AppUser { Name = "Deputy Engineer", Mobile = "9000000003", Role = "DeputyEngineer" },
            new AppUser { Name = "Executive Engineer", Mobile = "9000000004", Role = "ExecutiveEngineer" },
            new AppUser { Name = "Admin User", Mobile = "9000000005", Role = "Admin" }
        );

        await db.SaveChangesAsync();
    }
}

public static class WorkflowRules
{
    public static readonly HashSet<string> AllowedStatuses =
    [
        "Submitted",
        "Pending JE Verification",
        "Site Verified (Feasible)",
        "Site Verified (Not Feasible)",
        "Estimate Uploaded by JE",
        "Approved by AE",
        "Re-Visit Required",
        "Recommended by Deputy Engineer",
        "Approved (Sanctioned)",
        "Rejected",
        "HOLD",
        "Send Back",
        "Completed"
    ];

    public static bool IsTransitionAllowed(string role, string status) => role switch
    {
        "JE" => status is "Pending JE Verification" or "Site Verified (Feasible)" or "Site Verified (Not Feasible)" or "Estimate Uploaded by JE" or "Re-Visit Required",
        "AE" => status is "Approved by AE" or "Re-Visit Required",
        "DeputyEngineer" => status is "Recommended by Deputy Engineer" or "Rejected",
        "ExecutiveEngineer" => status is "Approved (Sanctioned)" or "Rejected" or "HOLD" or "Send Back" or "Completed",
        "Admin" => AllowedStatuses.Contains(status),
        _ => false
    };
}
