-- Table Script for 6-Digit WhatsApp OTP Storage (2-Factor Authentication)
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
