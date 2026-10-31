CREATE TABLE UserLogin (
    LoginId INT IDENTITY(1,1) CONSTRAINT PK_UserLogin PRIMARY KEY,
    RegistrationId INT NOT NULL CONSTRAINT FK_UserLogin_UserRegistration REFERENCES UserRegistration(RegistrationId),
    DeviceDetails NVARCHAR(MAX) NOT NULL,
    DeviceToken NVARCHAR(255) NULL, -- Unique hardware identifier or FCM token
    OTP INT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_UserLogin_IsActive DEFAULT 1,
    DateCreated DATETIME NOT NULL CONSTRAINT DF_UserLogin_DateCreated DEFAULT GETDATE(),
    DateLoggedOut DATETIME NULL
);