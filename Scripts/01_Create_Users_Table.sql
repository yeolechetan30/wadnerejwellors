-- ============================================================================
-- Script: Create Users Table
-- Database: wadnerejw (Azure SQL Server)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id]          INT IDENTITY(1,1) NOT NULL,
        [Username]    NVARCHAR(100)     NOT NULL,
        [Email]       NVARCHAR(150)     NOT NULL,
        [FirstName]   NVARCHAR(100)     NULL,
        [LastName]    NVARCHAR(100)     NULL,
        [PhoneNumber] NVARCHAR(20)      NULL,
        [CreatedAt]   DATETIME2         NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT (GETUTCDATE()),
        [IsActive]    BIT               NOT NULL CONSTRAINT [DF_Users_IsActive]  DEFAULT (1),
        
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_Users_Username] UNIQUE ([Username]),
        CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
    );

    PRINT 'Users table created successfully.';
END
ELSE
BEGIN
    PRINT 'Users table already exists.';
END
GO
