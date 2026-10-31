-- ==============================================================================================
-- Table Script: MonthlyPayments
-- Description: Stores customer monthly savings scheme installments, derived month details,
--              redemption tracking (IsRedeemed = 0 by default), and passbook ledger entries.
-- Note: Records are created ONLY after payment is SUCCESS / PAID.
-- ==============================================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MonthlyPayments')
BEGIN
    CREATE TABLE MonthlyPayments (
        Id INT IDENTITY(1,1) CONSTRAINT PK_MonthlyPayments PRIMARY KEY,
        
        -- Customer Reference
        RegistrationId INT NOT NULL,
        
        -- Payment Gateway Transaction Reference (Cashfree)
        PaymentTransactionId INT NULL,
        
        -- Monthly Installment Amount
        Amount DECIMAL(18, 2) NOT NULL,
        
        -- Payment Date & Derived Month Info
        PaymentDate DATETIME NOT NULL CONSTRAINT DF_MonthlyPayments_PaymentDate DEFAULT GETUTCDATE(),
        PaymentMonth INT NOT NULL,               -- e.g. 10 (October)
        PaymentYear INT NOT NULL,                -- e.g. 2026
        MonthYearDisplay NVARCHAR(30) NOT NULL,  -- e.g. 'October 2026'
        
        -- Redemption Management (Default: 0 - Not Redeemed)
        IsRedeemed BIT NOT NULL CONSTRAINT DF_MonthlyPayments_IsRedeemed DEFAULT 0,
        RedeemedDate DATETIME NULL,
        
        -- Notes / Remarks
        Remarks NVARCHAR(250) NULL,
        
        -- Audit Timestamp
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_MonthlyPayments_CreatedDate DEFAULT GETUTCDATE(),

        -- Foreign Key Constraints
        CONSTRAINT FK_MonthlyPayments_UserRegistration 
            FOREIGN KEY (RegistrationId) 
            REFERENCES UserRegistration(RegistrationId) 
            ON DELETE CASCADE,

        CONSTRAINT FK_MonthlyPayments_PaymentTransactions 
            FOREIGN KEY (PaymentTransactionId) 
            REFERENCES PaymentTransactions(Id) 
            ON DELETE SET NULL
    );

    -- Performance Indexes for Passbook Queries and Month lookups
    CREATE INDEX IX_MonthlyPayments_RegistrationId ON MonthlyPayments (RegistrationId);
    CREATE INDEX IX_MonthlyPayments_PaymentDate ON MonthlyPayments (PaymentDate DESC);
    CREATE INDEX IX_MonthlyPayments_MonthLookup ON MonthlyPayments (RegistrationId, PaymentYear, PaymentMonth);
    CREATE INDEX IX_MonthlyPayments_IsRedeemed ON MonthlyPayments (RegistrationId, IsRedeemed);

    PRINT 'Table MonthlyPayments created successfully.';
END
ELSE
BEGIN
    PRINT 'Table MonthlyPayments already exists.';
END
GO
