-- ============================================================================
-- Script: Insert Sample Data into Users Table
-- Database: wadnerejw (Azure SQL Server)
-- ============================================================================

SET NOCOUNT ON;

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    -- Insert sample record 1
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = 'admin_chetan')
    BEGIN
        INSERT INTO [dbo].[Users] ([Username], [Email], [FirstName], [LastName], [PhoneNumber], [CreatedAt], [IsActive])
        VALUES ('admin_chetan', 'chetan.wadnere@wadnerejw.com', 'Chetan', 'Yeole', '+91-9876543210', GETUTCDATE(), 1);
    END

    -- Insert sample record 2
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = 'johndoe')
    BEGIN
        INSERT INTO [dbo].[Users] ([Username], [Email], [FirstName], [LastName], [PhoneNumber], [CreatedAt], [IsActive])
        VALUES ('johndoe', 'john.doe@example.com', 'John', 'Doe', '+1-555-0199', GETUTCDATE(), 1);
    END

    -- Insert sample record 3
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = 'pria_s')
    BEGIN
        INSERT INTO [dbo].[Users] ([Username], [Email], [FirstName], [LastName], [PhoneNumber], [CreatedAt], [IsActive])
        VALUES ('pria_s', 'priya.sharma@example.com', 'Priya', 'Sharma', '+91-9123456789', GETUTCDATE(), 1);
    END

    PRINT 'Sample data inserted into Users table successfully.';
END
ELSE
BEGIN
    PRINT 'Error: Users table does not exist. Please run 01_Create_Users_Table.sql first.';
END
GO

-- Select inserted data to verify
SELECT [Id], [Username], [Email], [FirstName], [LastName], [PhoneNumber], [CreatedAt], [IsActive]
FROM [dbo].[Users];
GO
