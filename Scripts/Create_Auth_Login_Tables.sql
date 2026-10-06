-- ====================================================================
-- Script: Create Authentication & Login Purpose Tables
-- Target: SQL Server / Azure SQL Database
-- Description: Creates UserRegistration, UserOtps, and RefreshTokens tables
-- ====================================================================

-- 1. Create UserRegistration Table (if not exists)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRegistration')
BEGIN
    CREATE TABLE UserRegistration (
        RegistrationId INT IDENTITY(1,1) CONSTRAINT PK_UserRegistration PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        EmailAddress NVARCHAR(150) NULL CONSTRAINT UQ_UserRegistration_Email UNIQUE,
        MobileNumber BIGINT NOT NULL CONSTRAINT UQ_UserRegistration_MobileNumber UNIQUE,
        IsMobileVerified BIT NOT NULL CONSTRAINT DF_UserRegistration_IsMobileVerified DEFAULT 0,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        
        -- Address Details
        AddressLine1 NVARCHAR(100) NOT NULL,
        AddressLine2 NVARCHAR(100) NULL,
        Village_City NVARCHAR(100) NOT NULL,
        Taluka NVARCHAR(100) NOT NULL,
        District NVARCHAR(100) NOT NULL,
        State NVARCHAR(100) NOT NULL CONSTRAINT DF_UserRegistration_State DEFAULT 'Maharashtra',
        Pincode VARCHAR(10) NOT NULL,
        
        -- Personal Details
        DateOfBirth DATE NULL,
        AnniversaryDate DATE NULL,
        
        -- Status & Admin Flags
        IsActive BIT NOT NULL CONSTRAINT DF_UserRegistration_IsActive DEFAULT 1,
        IsAdmin BIT NOT NULL CONSTRAINT DF_UserRegistration_IsAdmin DEFAULT 0,
        DateCreated DATETIME NOT NULL CONSTRAINT DF_UserRegistration_DateCreated DEFAULT GETUTCDATE(),
        DateModified DATETIME NULL
    );
END
GO

-- 2. Create UserOtps Table (for 6-digit WhatsApp 2FA OTP)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserOtps')
BEGIN
    CREATE TABLE UserOtps (
        Id INT IDENTITY(1,1) CONSTRAINT PK_UserOtps PRIMARY KEY,
        RegistrationId INT NOT NULL,
        MobileNumber BIGINT NOT NULL,
        OtpCode NVARCHAR(6) NOT NULL,
        ExpiryTime DATETIME NOT NULL,
        IsUsed BIT NOT NULL CONSTRAINT DF_UserOtps_IsUsed DEFAULT 0,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_UserOtps_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_UserOtps_UserRegistration FOREIGN KEY (RegistrationId) REFERENCES UserRegistration(RegistrationId) ON DELETE CASCADE
    );

    CREATE INDEX IX_UserOtps_MobileNumber_OtpCode ON UserOtps (MobileNumber, OtpCode, IsUsed, ExpiryTime);
END
GO

-- 3. Create RefreshTokens Table (for JWT Refresh Token Storage)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE RefreshTokens (
        Id INT IDENTITY(1,1) CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        RegistrationId INT NOT NULL,
        Token NVARCHAR(256) NOT NULL,
        ExpiryDate DATETIME NOT NULL,
        IsRevoked BIT NOT NULL CONSTRAINT DF_RefreshTokens_IsRevoked DEFAULT 0,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT FK_RefreshTokens_UserRegistration FOREIGN KEY (RegistrationId) REFERENCES UserRegistration(RegistrationId) ON DELETE CASCADE
    );

    CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens (Token);
    CREATE INDEX IX_RefreshTokens_RegistrationId ON RefreshTokens (RegistrationId);
END
GO
