CREATE DATABASE SmcStreetlightDb;
GO

USE SmcStreetlightDb;
GO

CREATE TABLE AppUsers (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Mobile NVARCHAR(15) NOT NULL UNIQUE,
    Role NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE OtpCodes (
    Id INT IDENTITY PRIMARY KEY,
    Mobile NVARCHAR(15) NOT NULL,
    Code NVARCHAR(6) NOT NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0
);

CREATE TABLE StreetLightRequests (
    Id INT IDENTITY PRIMARY KEY,
    ApplicationNumber NVARCHAR(25) NOT NULL UNIQUE,
    CitizenName NVARCHAR(100) NOT NULL,
    CitizenMobile NVARCHAR(15) NOT NULL,
    Address NVARCHAR(500) NOT NULL,
    Ward NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    CurrentStatus NVARCHAR(50) NOT NULL,
    EstimatedAmount DECIMAL(18,2) NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE StatusHistories (
    Id INT IDENTITY PRIMARY KEY,
    RequestId INT NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    Remarks NVARCHAR(1000) NULL,
    ActionByRole NVARCHAR(50) NOT NULL,
    ActionByUserId INT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_StatusHistories_Request FOREIGN KEY (RequestId) REFERENCES StreetLightRequests(Id)
);

CREATE TABLE RequestFiles (
    Id INT IDENTITY PRIMARY KEY,
    RequestId INT NOT NULL,
    FileType NVARCHAR(50) NOT NULL,
    FileName NVARCHAR(300) NOT NULL,
    StoragePath NVARCHAR(300) NOT NULL,
    Latitude DECIMAL(10,7) NULL,
    Longitude DECIMAL(10,7) NULL,
    CapturedAtUtc DATETIME2 NULL,
    UploadedByRole NVARCHAR(50) NOT NULL,
    UploadedByUserId INT NOT NULL,
    UploadedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsLocked BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_RequestFiles_Request FOREIGN KEY (RequestId) REFERENCES StreetLightRequests(Id)
);

CREATE TABLE ActionLogs (
    Id INT IDENTITY PRIMARY KEY,
    RequestId INT NULL,
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    Details NVARCHAR(1000) NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ActionLogs_Request FOREIGN KEY (RequestId) REFERENCES StreetLightRequests(Id),
    CONSTRAINT FK_ActionLogs_User FOREIGN KEY (UserId) REFERENCES AppUsers(Id)
);
GO

CREATE TRIGGER TR_StatusHistories_Immutable
ON StatusHistories
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR ('Status history is immutable and cannot be changed.', 16, 1);
END;
GO

INSERT INTO AppUsers (Name, Mobile, Role)
VALUES
('JE User', '9000000001', 'JE'),
('AE User', '9000000002', 'AE'),
('Deputy Engineer', '9000000003', 'DeputyEngineer'),
('Executive Engineer', '9000000004', 'ExecutiveEngineer'),
('Admin User', '9000000005', 'Admin');
GO

IF OBJECT_ID('dbo.StreetLightDemandApplications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StreetLightDemandApplications (
        Id INT IDENTITY PRIMARY KEY,
        ApplicationNumber NVARCHAR(30) NOT NULL UNIQUE,
        ApplicantType NVARCHAR(20) NOT NULL, -- MLA | Corporator | Citizen
        ApplicantName NVARCHAR(100) NOT NULL,
        MobileNumber NVARCHAR(20) NOT NULL,
        WardNumber NVARCHAR(50) NULL,
        FullAddress NVARCHAR(500) NOT NULL,
        LocationDetails NVARCHAR(500) NOT NULL,
        NumberOfLightsRequested INT NOT NULL,
        DescriptionOfRequest NVARCHAR(1000) NOT NULL,
        LetterReferenceNumber NVARCHAR(50) NULL,
        LetterDate DATETIME2 NULL,
        AssemblyConstituency NVARCHAR(100) NULL,
        LetterFileName NVARCHAR(300) NULL,
        LetterFilePath NVARCHAR(300) NULL,
        CurrentStatus NVARCHAR(50) NOT NULL DEFAULT 'Submitted',
        CreatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO
