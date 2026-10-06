-- Table Script for Live Gold and Silver Rates
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GoldSilverRates')
BEGIN
    CREATE TABLE GoldSilverRates (
        Id INT IDENTITY(1,1) CONSTRAINT PK_GoldSilverRates PRIMARY KEY,
        Gold24K_PerGram DECIMAL(18,2) NOT NULL,
        Gold22K_PerGram DECIMAL(18,2) NOT NULL,
        Gold18K_PerGram DECIMAL(18,2) NOT NULL,
        Silver_PerGram DECIMAL(18,2) NOT NULL,
        Silver_PerKg DECIMAL(18,2) NOT NULL,
        Source NVARCHAR(50) NOT NULL CONSTRAINT DF_GoldSilverRates_Source DEFAULT 'LiveMarket',
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_GoldSilverRates_Currency DEFAULT 'INR',
        Timestamp DATETIME NOT NULL CONSTRAINT DF_GoldSilverRates_Timestamp DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_GoldSilverRates_Timestamp ON GoldSilverRates (Timestamp DESC);
END
GO
