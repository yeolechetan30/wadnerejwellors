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
    
    -- Personal / Marketing Details
    DateOfBirth DATE NULL,
    AnniversaryDate DATE NULL,
    
    -- System & Status Flags
    IsActive BIT NOT NULL CONSTRAINT DF_UserRegistration_IsActive DEFAULT 1,
    IsAdmin BIT NOT NULL CONSTRAINT DF_UserRegistration_IsAdmin DEFAULT 0,
    DateCreated DATETIME NOT NULL CONSTRAINT DF_UserRegistration_DateCreated DEFAULT GETDATE(),
    DateModified DATETIME NULL
);