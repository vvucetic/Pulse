--@ double escaped because it is the NPoco library parameters char

SET NOCOUNT ON
DECLARE @@TARGET_SCHEMA_VERSION INT;
SET @@TARGET_SCHEMA_VERSION = 1;

PRINT 'Installing Pulse SQL objects...';

BEGIN TRANSACTION;

-- Acquire exclusive lock to prevent deadlocks caused by schema creation / version update
DECLARE @@SchemaLockResult INT;
EXEC @@SchemaLockResult = sp_getapplock @@Resource = '$(PulseSchema):SchemaLock', @@LockMode = 'Exclusive'

-- Create the database schema if it doesn't exists
IF NOT EXISTS (SELECT [schema_id] FROM [sys].[schemas] WHERE [name] = '$(PulseSchema)')
BEGIN
    EXEC (N'CREATE SCHEMA [$(PulseSchema)]');
    PRINT 'Created database schema [$(PulseSchema)]';
END
ELSE
    PRINT 'Database schema [$(PulseSchema)] already exists';
    
DECLARE @@SCHEMA_ID int;
SELECT @@SCHEMA_ID = [schema_id] FROM [sys].[schemas] WHERE [name] = '$(PulseSchema)';

-- Create the [$(PulseSchema)].Schema table if not exists
IF NOT EXISTS(SELECT [object_id] FROM [sys].[tables] 
    WHERE [name] = 'Schema' AND [schema_id] = @@SCHEMA_ID)
BEGIN
    CREATE TABLE [$(PulseSchema)].[Schema](
        [Version] [int] NOT NULL,
        CONSTRAINT [PK_Pulse_Schema] PRIMARY KEY CLUSTERED ([Version] ASC)
    );
    PRINT 'Created table [$(PulseSchema)].[Schema]';
END
ELSE
    PRINT 'Table [$(PulseSchema)].[Schema] already exists';
    
DECLARE @@CURRENT_SCHEMA_VERSION int;
SELECT @@CURRENT_SCHEMA_VERSION = [Version] FROM [$(PulseSchema)].[Schema];

PRINT 'Current Pulse schema version: ' + CASE @@CURRENT_SCHEMA_VERSION WHEN NULL THEN 'none' ELSE CONVERT(nvarchar, @@CURRENT_SCHEMA_VERSION) END;

IF @@CURRENT_SCHEMA_VERSION IS NOT NULL AND @@CURRENT_SCHEMA_VERSION > @@TARGET_SCHEMA_VERSION
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(N'Pulse current database schema version %d is newer than the configured SqlServerStorage schema version %d. Please update to the latest Pulse.SqlServer NuGet package.', 11, 1,
        @@CURRENT_SCHEMA_VERSION, @@TARGET_SCHEMA_VERSION);
END

-- Install [$(PulseSchema)] schema objects
IF @@CURRENT_SCHEMA_VERSION IS NULL
BEGIN
    PRINT 'Installing schema version 1';
   
    PRINT 'Created table [$(HangFireSchema)].[Server]';
    
	SET @@CURRENT_SCHEMA_VERSION = 1;
END
	
/*IF @@CURRENT_SCHEMA_VERSION = 5
BEGIN
	PRINT 'Installing schema version 6';

	-- Insert migration here

	SET @@CURRENT_SCHEMA_VERSION = 6;
END*/

UPDATE [$(PulseSchema)].[Schema] SET [Version] = @@CURRENT_SCHEMA_VERSION
IF @@@@ROWCOUNT = 0 
	INSERT INTO [$(PulseSchema)].[Schema] ([Version]) VALUES (@@CURRENT_SCHEMA_VERSION)        

PRINT 'Pulse database schema installed';

COMMIT TRANSACTION;
PRINT 'Pulse SQL objects installed';
