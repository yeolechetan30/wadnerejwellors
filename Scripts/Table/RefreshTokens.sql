-- Table Script for JWT Refresh Tokens
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
