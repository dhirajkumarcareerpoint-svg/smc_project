using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmcStreetlight.Api.Contracts;
using SmcStreetlight.Api.Data;
using SmcStreetlight.Api.Middleware;
using SmcStreetlight.Api.Models;
using SmcStreetlight.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
    options.AddPolicy("angular", policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? "CHANGE_THIS_TO_A_32_PLUS_CHARACTER_SECRET";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("angular");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestAuditMiddleware>();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID('dbo.StreetLightDemandApplications', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[StreetLightDemandApplications](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ApplicationNumber] NVARCHAR(30) NOT NULL,
        [ApplicantType] NVARCHAR(20) NOT NULL,
        [ApplicantName] NVARCHAR(100) NOT NULL,
        [MobileNumber] NVARCHAR(20) NOT NULL,
        [WardNumber] NVARCHAR(50) NULL,
        [FullAddress] NVARCHAR(500) NOT NULL,
        [LocationDetails] NVARCHAR(500) NOT NULL,
        [NumberOfLightsRequested] INT NOT NULL,
        [DescriptionOfRequest] NVARCHAR(1000) NOT NULL,
        [LetterReferenceNumber] NVARCHAR(50) NULL,
        [LetterDate] DATETIME2 NULL,
        [AssemblyConstituency] NVARCHAR(100) NULL,
        [LetterFileName] NVARCHAR(300) NULL,
        [LetterFilePath] NVARCHAR(300) NULL,
        [CurrentStatus] NVARCHAR(50) NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL
    );
    CREATE UNIQUE INDEX IX_StreetLightDemandApplications_ApplicationNumber
    ON [dbo].[StreetLightDemandApplications]([ApplicationNumber]);
END");
    await db.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('dbo.StreetLightDemandApplications', 'ApplicantType') IS NULL
BEGIN
    ALTER TABLE [dbo].[StreetLightDemandApplications]
    ADD [ApplicantType] NVARCHAR(20) NOT NULL CONSTRAINT DF_StreetLightDemandApplications_ApplicantType DEFAULT('Citizen');
END");
    await DataSeeder.SeedAsync(db);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database initialization failed. Starting portal without database connectivity.");
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Portal}/{action=Index}/{id?}");

app.MapPost("/api/demand-applications", async (AppDbContext db, HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var applicantType = (form["applicant_type"].ToString() ?? string.Empty).Trim();
    var allowedTypes = new[] { "MLA", "Corporator", "Citizen" };
    if (!allowedTypes.Contains(applicantType))
        return Results.BadRequest(new { message = "applicant_type must be MLA, Corporator, or Citizen" });

    var applicantName = (form["applicant_name"].ToString() ?? string.Empty).Trim();
    var mobileNumber = (form["mobile_number"].ToString() ?? string.Empty).Trim();
    var wardNumber = (form["ward_number"].ToString() ?? string.Empty).Trim();
    var fullAddress = (form["full_address"].ToString() ?? string.Empty).Trim();
    var locationDetails = (form["location_details"].ToString() ?? string.Empty).Trim();
    var description = (form["description_of_request"].ToString() ?? string.Empty).Trim();
    var letterReference = (form["letter_reference_number"].ToString() ?? string.Empty).Trim();
    var assembly = (form["assembly_constituency"].ToString() ?? string.Empty).Trim();
    var lightsText = (form["number_of_lights_requested"].ToString() ?? string.Empty).Trim();
    var letterDateText = (form["letter_date"].ToString() ?? string.Empty).Trim();

    if (string.IsNullOrWhiteSpace(applicantName) ||
        string.IsNullOrWhiteSpace(mobileNumber) ||
        string.IsNullOrWhiteSpace(fullAddress) ||
        string.IsNullOrWhiteSpace(locationDetails) ||
        string.IsNullOrWhiteSpace(description) ||
        !int.TryParse(lightsText, out var lightsCount) ||
        lightsCount <= 0)
    {
        return Results.BadRequest(new { message = "Please fill all required fields." });
    }

    if (applicantType is "MLA" or "Corporator")
    {
        if (string.IsNullOrWhiteSpace(letterReference) || string.IsNullOrWhiteSpace(letterDateText))
            return Results.BadRequest(new { message = "Letter reference and date are required for MLA/Corporator." });
    }

    if (applicantType == "MLA" && string.IsNullOrWhiteSpace(assembly))
        return Results.BadRequest(new { message = "Assembly constituency is required for MLA." });

    DateTime? letterDate = DateTime.TryParse(letterDateText, out var pDate) ? pDate : null;
    if (applicantType is "MLA" or "Corporator" && letterDate is null)
        return Results.BadRequest(new { message = "Letter date format is invalid." });

    var letterFile = form.Files.GetFile("letter_file");
    string? letterFileName = null;
    string? letterFilePath = null;

    if (applicantType is "MLA" or "Corporator")
    {
        if (letterFile is null)
            return Results.BadRequest(new { message = "Letter PDF is required for MLA/Corporator." });
        if (!Path.GetExtension(letterFile.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { message = "Only PDF letter upload is allowed." });

        var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "Uploads", "DemandLetters");
        Directory.CreateDirectory(uploadsRoot);
        var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(letterFile.FileName)}";
        var savePath = Path.Combine(uploadsRoot, safeName);
        await using var stream = File.Create(savePath);
        await letterFile.CopyToAsync(stream);
        letterFileName = letterFile.FileName;
        letterFilePath = savePath;
    }

    var application = new StreetLightDemandApplication
    {
        ApplicationNumber = $"SMC-DMD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
        ApplicantType = applicantType,
        ApplicantName = applicantName,
        MobileNumber = mobileNumber,
        WardNumber = string.IsNullOrWhiteSpace(wardNumber) ? null : wardNumber,
        FullAddress = fullAddress,
        LocationDetails = locationDetails,
        NumberOfLightsRequested = lightsCount,
        DescriptionOfRequest = description,
        LetterReferenceNumber = string.IsNullOrWhiteSpace(letterReference) ? null : letterReference,
        LetterDate = letterDate,
        AssemblyConstituency = string.IsNullOrWhiteSpace(assembly) ? null : assembly,
        LetterFileName = letterFileName,
        LetterFilePath = letterFilePath,
        CurrentStatus = "Submitted",
        CreatedAtUtc = DateTime.UtcNow
    };

    db.DemandApplications.Add(application);
    db.ActionLogs.Add(new ActionLog
    {
        Action = "DemandApplicationSubmitted",
        Details = $"{applicantType} application submitted: {application.ApplicationNumber}"
    });
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        application.ApplicationNumber,
        application.ApplicantType,
        application.CurrentStatus,
        application.CreatedAtUtc
    });
});

app.MapGet("/api/demand-applications/{applicationNumber}/status", async (AppDbContext db, string applicationNumber) =>
{
    var normalized = (applicationNumber ?? string.Empty).Trim().ToUpperInvariant();
    var item = await db.DemandApplications
        .FirstOrDefaultAsync(x => x.ApplicationNumber.ToUpper() == normalized);
    if (item is null) return Results.NotFound(new { message = "Application not found" });

    return Results.Ok(new
    {
        item.ApplicationNumber,
        item.ApplicantType,
        item.CurrentStatus,
        item.CreatedAtUtc
    });
});

app.MapGet("/api/internal/demand-applications/summary", async (AppDbContext db) =>
{
    var counts = await db.DemandApplications
        .GroupBy(x => x.ApplicantType)
        .Select(g => new { ApplicantType = g.Key, Count = g.Count() })
        .ToListAsync();

    var mla = counts.FirstOrDefault(x => x.ApplicantType == "MLA")?.Count ?? 0;
    var corporator = counts.FirstOrDefault(x => x.ApplicantType == "Corporator")?.Count ?? 0;
    var citizen = counts.FirstOrDefault(x => x.ApplicantType == "Citizen")?.Count ?? 0;

    return Results.Ok(new
    {
        MLA = mla,
        Corporator = corporator,
        Citizen = citizen,
        Total = mla + corporator + citizen
    });
}).RequireAuthorization();

app.MapGet("/api/internal/demand-applications", async (AppDbContext db, string? applicantType) =>
{
    var allowedTypes = new[] { "MLA", "Corporator", "Citizen" };
    var normalizedType = (applicantType ?? string.Empty).Trim();
    if (!string.IsNullOrWhiteSpace(normalizedType) && !allowedTypes.Contains(normalizedType))
        return Results.BadRequest(new { message = "applicantType must be MLA, Corporator, or Citizen" });

    var query = db.DemandApplications.AsQueryable();
    if (!string.IsNullOrWhiteSpace(normalizedType))
        query = query.Where(x => x.ApplicantType == normalizedType);

    var items = await query
        .OrderByDescending(x => x.Id)
        .Select(x => new
        {
            x.Id,
            x.ApplicationNumber,
            x.ApplicantType,
            x.ApplicantName,
            x.MobileNumber,
            x.WardNumber,
            x.AssemblyConstituency,
            x.LetterReferenceNumber,
            x.LetterDate,
            x.NumberOfLightsRequested,
            x.FullAddress,
            x.LocationDetails,
            x.DescriptionOfRequest,
            x.LetterFileName,
            x.CurrentStatus,
            x.CreatedAtUtc
        })
        .ToListAsync();

    return Results.Ok(items);
}).RequireAuthorization();

app.MapPost("/api/citizen/requests", async (AppDbContext db, CitizenRequestCreateDto dto) =>
{
    var request = new StreetLightRequest
    {
        ApplicationNumber = $"SMC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
        CitizenName = dto.CitizenName,
        CitizenMobile = dto.CitizenMobile,
        Address = dto.Address,
        Ward = dto.Ward,
        Description = dto.Description,
        CurrentStatus = "Pending JE Approval"
    };

    db.Requests.Add(request);
    await db.SaveChangesAsync();

    db.StatusHistories.Add(new StatusHistory
    {
        RequestId = request.Id,
        Status = "Pending JE Approval",
        Remarks = "Citizen submitted application",
        ActionByRole = "Citizen"
    });

    db.ActionLogs.Add(new ActionLog
    {
        RequestId = request.Id,
        Action = "CitizenSubmitted",
        Details = "Request submitted by citizen"
    });

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        request.Id,
        request.ApplicationNumber,
        request.CitizenName,
        request.CitizenMobile,
        request.Ward,
        request.CurrentStatus,
        request.CreatedAtUtc
    });
});

app.MapGet("/api/citizen/requests/{applicationNumber}/detail", async (AppDbContext db, string applicationNumber) =>
{
    var normalized = (applicationNumber ?? string.Empty).Trim().ToUpperInvariant();
    var req = await db.Requests.FirstOrDefaultAsync(x => x.ApplicationNumber.ToUpper() == normalized);
    if (req is null) return Results.NotFound(new { message = "Application not found" });

    return Results.Ok(new
    {
        req.Id,
        req.ApplicationNumber,
        req.CitizenName,
        req.CitizenMobile,
        req.Ward,
        req.Address,
        req.Description,
        req.CurrentStatus,
        req.CreatedAtUtc
    });
});

app.MapGet("/api/citizen/requests/{applicationNumber}/status", async (AppDbContext db, string applicationNumber) =>
{
    var normalized = (applicationNumber ?? string.Empty).Trim().ToUpperInvariant();
    var req = await db.Requests.FirstOrDefaultAsync(x => x.ApplicationNumber.ToUpper() == normalized);
    if (req is null)
        return Results.NotFound(new { message = "Application not found" });

    var history = await db.StatusHistories
        .Where(x => x.RequestId == req.Id)
        .OrderBy(x => x.Id)
        .Select(x => new StatusHistoryDto(x.Status, x.Remarks, x.ActionByRole, x.CreatedAtUtc))
        .ToListAsync();

    return Results.Ok(new TrackResponseDto(req.ApplicationNumber, req.CurrentStatus, req.CreatedAtUtc, history));
});

app.MapPost("/api/auth/login", async (AppDbContext db, LoginDto dto) =>
{
    var loginId = (dto.LoginId ?? string.Empty).Trim();
    var loginKey = loginId.ToLowerInvariant();
    var loginAliasMap = new Dictionary<string, string>
    {
        ["je"] = "9000000001",
        ["ae"] = "9000000002",
        ["ee"] = "9000000004",
        ["executive"] = "9000000004"
    };
    if (loginAliasMap.TryGetValue(loginKey, out var mappedMobile))
        loginId = mappedMobile;

    var password = dto.Password ?? string.Empty;
    var user = await db.Users.FirstOrDefaultAsync(x => x.Mobile == loginId && x.IsActive);
    if (user is null) return Results.Unauthorized();

    var userPassword = builder.Configuration[$"Auth:UserPasswords:{loginKey}"]
        ?? builder.Configuration[$"Auth:UserPasswords:{loginId}"];
    var defaultPassword = builder.Configuration["Auth:DefaultPassword"] ?? "smc@123";
    var effectivePassword = string.IsNullOrWhiteSpace(userPassword) ? defaultPassword : userPassword;
    if (!string.Equals(password, effectivePassword, StringComparison.Ordinal))
        return Results.Unauthorized();

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.MobilePhone, user.Mobile),
        new(ClaimTypes.Role, user.Role),
        new(ClaimTypes.Name, user.Name)
    };

    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);

    db.ActionLogs.Add(new ActionLog { UserId = user.Id, Action = "PasswordLogin", Details = "User logged in with ID/password" });
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token),
        role = user.Role,
        name = user.Name
    });
});

app.MapPost("/api/internal/requests/{requestId}/decision", async (
    AppDbContext db,
    int requestId,
    DecisionUpdateDto dto,
    ClaimsPrincipal principal) =>
{
    var role = principal.FindFirstValue(ClaimTypes.Role);
    var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(role) || !int.TryParse(userIdText, out var userId))
        return Results.Unauthorized();

    var req = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId);
    if (req is null) return Results.NotFound();

    var decision = (dto.Decision ?? string.Empty).Trim().ToUpperInvariant();
    if (decision is not ("ACCEPT" or "REJECT" or "HOLD"))
        return Results.BadRequest(new { message = "Decision must be ACCEPT, REJECT, or HOLD" });

    static string? GetOwnerRole(string status) => status switch
    {
        "Submitted" => "JE",
        "Pending JE Approval" => "JE",
        "Pending AE Approval" => "AE",
        "Pending Executive Approval" => "ExecutiveEngineer",
        "On Hold by JE" => "JE",
        "On Hold by AE" => "AE",
        "On Hold by ExecutiveEngineer" => "ExecutiveEngineer",
        _ => null
    };

    static string RoleMarathi(string? r) => r switch
    {
        "JE" => "JE",
        "AE" => "AE",
        "ExecutiveEngineer" => "Executive Engineer",
        "Admin" => "Admin",
        _ => "\u0938\u0902\u092c\u0902\u0927\u093f\u0924 \u0905\u0927\u093f\u0915\u093e\u0930\u0940"
    };

    var ownerRole = GetOwnerRole(req.CurrentStatus);
    if (role != "Admin" && ownerRole != role)
        return Results.BadRequest(new
        {
            message = $"{RoleMarathi(ownerRole)} \u0915\u0921\u0942\u0928 \u0928\u093f\u0930\u094d\u0923\u092f \u092a\u094d\u0930\u0932\u0902\u092c\u093f\u0924 \u0906\u0939\u0947. \u0938\u0927\u094d\u092f\u093e\u091a\u0940 \u0938\u094d\u0925\u093f\u0924\u0940: {req.CurrentStatus}"
        });

    var effectiveRole = role == "Admin" ? ownerRole : role;
    if (effectiveRole is null)
        return Results.BadRequest(new { message = "\u0939\u093e \u0905\u0930\u094d\u091c \u0905\u0902\u0924\u093f\u092e \u091f\u092a\u094d\u092a\u094d\u092f\u093e\u0924 \u0906\u0939\u0947. \u092a\u0941\u0922\u0940\u0932 \u0928\u093f\u0930\u094d\u0923\u092f \u0915\u0930\u0924\u093e \u092f\u0947\u0923\u093e\u0930 \u0928\u093e\u0939\u0940." });

    string nextStatus;
    if (decision == "ACCEPT")
    {
        nextStatus = effectiveRole switch
        {
            "JE" => "Pending AE Approval",
            "AE" => "Pending Executive Approval",
            "ExecutiveEngineer" => "Approved",
            _ => req.CurrentStatus
        };
    }
    else if (decision == "REJECT")
    {
        nextStatus = $"Rejected by {effectiveRole}";
    }
    else
    {
        nextStatus = $"On Hold by {effectiveRole}";
    }

    req.CurrentStatus = nextStatus;

    db.StatusHistories.Add(new StatusHistory
    {
        RequestId = requestId,
        Status = nextStatus,
        Remarks = dto.Remarks,
        ActionByRole = effectiveRole,
        ActionByUserId = userId
    });

    db.ActionLogs.Add(new ActionLog
    {
        RequestId = requestId,
        UserId = userId,
        Action = "DecisionUpdated",
        Details = $"{effectiveRole} decision {decision}; status now {nextStatus}"
    });

    await db.SaveChangesAsync();
    return Results.Ok(new { req.Id, req.CurrentStatus, Decision = decision });
}).RequireAuthorization();

app.MapPost("/api/internal/requests/{requestId}/files", async (
    AppDbContext db,
    int requestId,
    HttpRequest request,
    ClaimsPrincipal principal) =>
{
    var role = principal.FindFirstValue(ClaimTypes.Role);
    var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(role) || !int.TryParse(userIdText, out var userId))
        return Results.Unauthorized();

    var req = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId);
    if (req is null) return Results.NotFound();

    var form = await request.ReadFormAsync();
    var formFile = form.Files.GetFile("file");
    var fileType = form["fileType"].ToString();

    if (formFile is null || string.IsNullOrWhiteSpace(fileType))
        return Results.BadRequest(new { message = "file and fileType are required" });

    var uploadAllowedRoles = new[] { "JE", "AE", "ExecutiveEngineer", "Admin" };
    if (!uploadAllowedRoles.Contains(role))
        return Results.Forbid();

    var allowedFileTypes = new[] { "EngineerDocument", "GeoTaggedPhoto" };
    if (!allowedFileTypes.Contains(fileType))
        return Results.BadRequest(new { message = "fileType must be EngineerDocument or GeoTaggedPhoto" });

    var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "Uploads");
    Directory.CreateDirectory(uploadsRoot);
    var safeName = $"{Guid.NewGuid():N}_{Path.GetFileName(formFile.FileName)}";
    var savePath = Path.Combine(uploadsRoot, safeName);

    await using (var stream = File.Create(savePath))
    {
        await formFile.CopyToAsync(stream);
    }

    decimal? lat = decimal.TryParse(form["latitude"], out var pLat) ? pLat : null;
    decimal? lng = decimal.TryParse(form["longitude"], out var pLng) ? pLng : null;
    DateTime? capturedAt = DateTime.TryParse(form["capturedAtUtc"], out var pDate) ? pDate : null;

    db.RequestFiles.Add(new RequestFile
    {
        RequestId = requestId,
        FileType = fileType,
        FileName = formFile.FileName,
        StoragePath = savePath,
        Latitude = lat,
        Longitude = lng,
        CapturedAtUtc = capturedAt,
        UploadedByRole = role,
        UploadedByUserId = userId,
        IsLocked = true
    });

    db.ActionLogs.Add(new ActionLog
    {
        RequestId = requestId,
        UserId = userId,
        Action = "FileUploaded",
        Details = $"{role} uploaded {fileType}"
    });

    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Uploaded" });
}).RequireAuthorization();

app.MapPatch("/api/internal/requests/{requestId}/estimate", async (
    AppDbContext db,
    int requestId,
    decimal estimatedAmount,
    ClaimsPrincipal principal) =>
{
    var role = principal.FindFirstValue(ClaimTypes.Role);
    var userIdText = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    if (role != "JE" || !int.TryParse(userIdText, out var userId)) return Results.Forbid();

    var req = await db.Requests.FirstOrDefaultAsync(x => x.Id == requestId);
    if (req is null) return Results.NotFound();

    req.EstimatedAmount = estimatedAmount;
    db.ActionLogs.Add(new ActionLog
    {
        RequestId = requestId,
        UserId = userId,
        Action = "EstimateUpdated",
        Details = $"JE uploaded estimate amount {estimatedAmount}"
    });

    await db.SaveChangesAsync();
    return Results.Ok(new { req.Id, req.EstimatedAmount });
}).RequireAuthorization(policy => policy.RequireRole("JE"));

app.MapGet("/api/internal/requests", async (AppDbContext db, ClaimsPrincipal principal) =>
{
    var items = await db.Requests.OrderByDescending(x => x.Id).Take(200).ToListAsync();
    return Results.Ok(items);
}).RequireAuthorization();

app.MapGet("/api/internal/requests/{requestId}/history", async (AppDbContext db, int requestId) =>
{
    var history = await db.StatusHistories
        .Where(x => x.RequestId == requestId)
        .OrderBy(x => x.Id)
        .ToListAsync();
    return Results.Ok(history);
}).RequireAuthorization();

app.MapGet("/api/internal/requests/by-application/{applicationNumber}", async (AppDbContext db, string applicationNumber) =>
{
    var normalized = (applicationNumber ?? string.Empty).Trim().ToUpperInvariant();
    var req = await db.Requests.FirstOrDefaultAsync(x => x.ApplicationNumber.ToUpper() == normalized);
    if (req is null) return Results.NotFound(new { message = "Application not found" });

    var files = await db.RequestFiles
        .Where(x => x.RequestId == req.Id)
        .OrderByDescending(x => x.Id)
        .Select(x => new
        {
            x.Id,
            x.FileType,
            x.FileName,
            x.UploadedByRole,
            x.UploadedAtUtc,
            x.Latitude,
            x.Longitude,
            x.CapturedAtUtc
        })
        .ToListAsync();

    var history = await db.StatusHistories
        .Where(x => x.RequestId == req.Id)
        .OrderBy(x => x.Id)
        .Select(x => new
        {
            x.Id,
            x.Status,
            x.Remarks,
            x.ActionByRole,
            x.CreatedAtUtc
        })
        .ToListAsync();

    return Results.Ok(new
    {
        req.Id,
        req.ApplicationNumber,
        req.CitizenName,
        req.CitizenMobile,
        req.Ward,
        req.Address,
        req.Description,
        req.EstimatedAmount,
        req.CurrentStatus,
        req.CreatedAtUtc,
        Files = files,
        History = history
    });
}).RequireAuthorization();

app.MapGet("/api/internal/request-files/{fileId}/download", async (
    AppDbContext db,
    int fileId,
    ClaimsPrincipal principal) =>
{
    var role = principal.FindFirstValue(ClaimTypes.Role);
    var allowedRoles = new[] { "JE", "AE", "ExecutiveEngineer", "Admin" };
    if (string.IsNullOrWhiteSpace(role) || !allowedRoles.Contains(role))
        return Results.Forbid();

    var file = await db.RequestFiles.FirstOrDefaultAsync(x => x.Id == fileId);
    if (file is null)
        return Results.NotFound(new { message = "File not found" });

    if (string.IsNullOrWhiteSpace(file.StoragePath) || !File.Exists(file.StoragePath))
        return Results.NotFound(new { message = "Physical file not found" });

    var contentType = file.FileType == "GeoTaggedPhoto" ? "image/jpeg" : "application/octet-stream";
    return Results.File(file.StoragePath, contentType, file.FileName);
}).RequireAuthorization();

app.MapGet("/api/internal/dashboard-summary", async (AppDbContext db) =>
{
    var items = await db.Requests.Select(x => x.CurrentStatus).ToListAsync();
    var summary = new
    {
        JePending = items.Count(s => s == "Pending JE Approval" || s == "On Hold by JE"),
        AePending = items.Count(s => s == "Pending AE Approval" || s == "On Hold by AE"),
        ExecutivePending = items.Count(s => s == "Pending Executive Approval" || s == "On Hold by ExecutiveEngineer"),
        Approved = items.Count(s => s == "Approved"),
        Rejected = items.Count(s => s.StartsWith("Rejected")),
        OnHold = items.Count(s => s.StartsWith("On Hold"))
    };
    return Results.Ok(summary);
}).RequireAuthorization();

app.MapGet("/api/internal/audit/logs", async (AppDbContext db) =>
{
    var logs = await db.ActionLogs.OrderByDescending(x => x.Id).Take(500).ToListAsync();
    return Results.Ok(logs);
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();



