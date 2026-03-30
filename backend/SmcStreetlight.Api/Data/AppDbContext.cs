using Microsoft.EntityFrameworkCore;
using SmcStreetlight.Api.Models;

namespace SmcStreetlight.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<StreetLightRequest> Requests => Set<StreetLightRequest>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<RequestFile> RequestFiles => Set<RequestFile>();
    public DbSet<ActionLog> ActionLogs => Set<ActionLog>();
    public DbSet<StreetLightDemandApplication> DemandApplications => Set<StreetLightDemandApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().ToTable("AppUsers");
        modelBuilder.Entity<OtpCode>().ToTable("OtpCodes");
        modelBuilder.Entity<StreetLightRequest>().ToTable("StreetLightRequests");
        modelBuilder.Entity<StatusHistory>().ToTable("StatusHistories");
        modelBuilder.Entity<RequestFile>().ToTable("RequestFiles");
        modelBuilder.Entity<ActionLog>().ToTable("ActionLogs");
        modelBuilder.Entity<StreetLightDemandApplication>().ToTable("StreetLightDemandApplications");

        modelBuilder.Entity<AppUser>().HasIndex(x => x.Mobile).IsUnique();
        modelBuilder.Entity<StreetLightRequest>().HasIndex(x => x.ApplicationNumber).IsUnique();
        modelBuilder.Entity<StreetLightDemandApplication>().HasIndex(x => x.ApplicationNumber).IsUnique();

        modelBuilder.Entity<StreetLightRequest>()
            .Property(x => x.EstimatedAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RequestFile>()
            .Property(x => x.Latitude)
            .HasPrecision(10, 7);

        modelBuilder.Entity<RequestFile>()
            .Property(x => x.Longitude)
            .HasPrecision(10, 7);

        modelBuilder.Entity<StatusHistory>()
            .HasOne(x => x.Request)
            .WithMany()
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RequestFile>()
            .HasOne(x => x.Request)
            .WithMany()
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
