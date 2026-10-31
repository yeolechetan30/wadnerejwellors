-- ==============================================================================================
-- Table Script: UserMemberships
-- Description: Stores customer membership details, validity period (FromDate to ToDate),
--              membership code (mem_YYYYNNNN), and links with UserRegistration and PaymentTransactions.
-- Note: Records are created ONLY after payment is SUCCESS / PAID.
-- ==============================================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserMemberships')
BEGIN
    CREATE TABLE UserMemberships (
        MembershipId INT IDENTITY(1,1) CONSTRAINT PK_UserMemberships PRIMARY KEY,
        
        -- Customer Reference
        RegistrationId INT NOT NULL,
        
        -- Membership Code (e.g., 'mem_20260001')
        MembershipCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_UserMemberships_MembershipCode UNIQUE,
        
        -- Membership Fee Amount Paid
        Amount DECIMAL(18, 2) NOT NULL,
        
        -- Membership Validity Window (Validity in years is read from appsettings.json to compute ToDate)
        FromDate DATETIME NOT NULL,
        ToDate DATETIME NOT NULL,
        
        -- Status: 1 = Active, 0 = Inactive / Expired
        IsActive BIT NOT NULL CONSTRAINT DF_UserMemberships_IsActive DEFAULT 1,
        
        -- Payment Gateway Audit Reference (Cashfree)
        PaymentTransactionId INT NULL,
        
        -- Audit Timestamps
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_UserMemberships_CreatedDate DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME NULL,

        -- Foreign Key Constraints
        CONSTRAINT FK_UserMemberships_UserRegistration 
            FOREIGN KEY (RegistrationId) 
            REFERENCES UserRegistration(RegistrationId) 
            ON DELETE CASCADE,

        CONSTRAINT FK_UserMemberships_PaymentTransactions 
            FOREIGN KEY (PaymentTransactionId) 
            REFERENCES PaymentTransactions(Id) 
            ON DELETE SET NULL
    );

    -- Performance Indexes
    CREATE INDEX IX_UserMemberships_RegistrationId ON UserMemberships (RegistrationId);
    CREATE INDEX IX_UserMemberships_MembershipCode ON UserMemberships (MembershipCode);
    CREATE INDEX IX_UserMemberships_Validity ON UserMemberships (RegistrationId, FromDate, ToDate, IsActive);
    CREATE INDEX IX_UserMemberships_CreatedDate ON UserMemberships (CreatedDate DESC);

    PRINT 'Table UserMemberships created successfully.';
END
ELSE
BEGIN
    PRINT 'Table UserMemberships already exists.';
END
GO
