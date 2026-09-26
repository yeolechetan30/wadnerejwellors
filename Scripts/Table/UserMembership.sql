CREATE TABLE UserMembership (
    MembershipId BIGINT IDENTITY(1,1) CONSTRAINT PK_UserMembership PRIMARY KEY,
    MembershipNumber BIGINT NOT NULL CONSTRAINT UQ_UserMembership_MembershipNumber UNIQUE,
    RegistrationId INT NOT NULL CONSTRAINT FK_UserMembership_UserRegistration REFERENCES UserRegistration(RegistrationId),
    DateCreated DATETIME NOT NULL CONSTRAINT DF_UserMembership_DateCreated DEFAULT GETDATE(),
    ExpiryDate DATETIME NOT NULL,
    Status BIT NOT NULL CONSTRAINT DF_UserMembership_Status DEFAULT 0
);