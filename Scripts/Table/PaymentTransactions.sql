-- ==============================================================================================
-- Table Script: PaymentTransactions
-- Description: Stores Cashfree Payment Gateway Orders, Transactions, and Webhook Status for App/Web
-- ==============================================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions')
BEGIN
    CREATE TABLE PaymentTransactions (
        Id INT IDENTITY(1,1) CONSTRAINT PK_PaymentTransactions PRIMARY KEY,
        
        -- Customer / User Link (Nullable if guest checkout is permitted)
        RegistrationId INT NULL,
        
        -- Cashfree Order & Session Identifiers
        OrderId NVARCHAR(100) NOT NULL CONSTRAINT UQ_PaymentTransactions_OrderId UNIQUE,
        CfOrderId NVARCHAR(100) NULL,
        PaymentSessionId NVARCHAR(255) NULL,
        
        -- Financials
        Amount DECIMAL(18, 2) NOT NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_PaymentTransactions_Currency DEFAULT 'INR',
        
        -- Status: PENDING, PAID, ACTIVE, FAILED, USER_DROPPED, CANCELLED
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_PaymentTransactions_Status DEFAULT 'PENDING',
        
        -- Customer Info Snapshot
        CustomerName NVARCHAR(100) NULL,
        CustomerPhone NVARCHAR(20) NULL,
        CustomerEmail NVARCHAR(150) NULL,
        OrderNote NVARCHAR(255) NULL,
        
        -- Payment Fulfillment / Bank Details (Populated on Payment Verification/Webhook)
        PaymentMode NVARCHAR(50) NULL,        -- upi, card, netbanking, wallet, etc.
        ReferenceId NVARCHAR(100) NULL,        -- Bank UTR / Cashfree Payment ID
        FailureReason NVARCHAR(500) NULL,      -- Error message if failed
        
        -- Audit Timestamps
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PaymentTransactions_CreatedAt DEFAULT GETUTCDATE(),
        PaymentCompletionTime DATETIME NULL,
        UpdatedAt DATETIME NULL,

        -- Foreign Key to UserRegistration
        CONSTRAINT FK_PaymentTransactions_UserRegistration 
            FOREIGN KEY (RegistrationId) 
            REFERENCES UserRegistration(RegistrationId) 
            ON DELETE SET NULL
    );

    -- Performance Indexes
    CREATE INDEX IX_PaymentTransactions_OrderId ON PaymentTransactions (OrderId);
    CREATE INDEX IX_PaymentTransactions_RegistrationId ON PaymentTransactions (RegistrationId);
    CREATE INDEX IX_PaymentTransactions_Status ON PaymentTransactions (Status);
    CREATE INDEX IX_PaymentTransactions_CreatedAt ON PaymentTransactions (CreatedAt DESC);

    PRINT 'Table PaymentTransactions created successfully.';
END
ELSE
BEGIN
    PRINT 'Table PaymentTransactions already exists.';
END
GO
