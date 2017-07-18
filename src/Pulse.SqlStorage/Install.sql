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
	
	PRINT 'Creating table [$(PulseSchema)].[Job]';

	-- JOB

	CREATE TABLE [$(PulseSchema)].[Job](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StateId] [int] NULL,
	[State] [nvarchar](20) NULL,
	[InvocationData] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[NextJobs] [nvarchar](max) NOT NULL,
	[ContextId] [uniqueidentifier] NULL,
	[ExpireAt] [datetime2](7) NULL,
	[MaxRetries] [int] NULL,
	[RetryCount] [int] NULL,
	[NextRetry] [datetime2](7) NULL,
	[Queue] [nvarchar](50) NULL,
	[WorkflowId] [uniqueidentifier] NULL,
	[ScheduleName] [nvarchar](100) NULL,
	[Description] [nvarchar(1000)] NULL,

	 CONSTRAINT [PK_Pulse_Job] PRIMARY KEY CLUSTERED ([Id] ASC)
	);

	CREATE NONCLUSTERED INDEX [IX_Pulse_Job_WorkflowId] ON [$(PulseSchema)].[Job]
	(
		[WorkflowId] ASC
	);

	CREATE NONCLUSTERED INDEX [IX_Pulse_Job_NextRetry] ON [$(PulseSchema)].[Job]
	(
		[NextRetry] ASC
	);
	
    PRINT 'Created table [$(PulseSchema)].[Job]';

	PRINT 'Creating table [$(PulseSchema)].[JobCondition]';

	-- JobCondition

	CREATE TABLE [$(PulseSchema)].[JobCondition](
	[JobId] [int] NOT NULL,
	[ParentJobId] [int] NOT NULL,
	[Finished] [bit] NOT NULL,
	[FinishedAt] [datetime2](7) NULL,

	 CONSTRAINT [PK_Pulse_JobCondition] PRIMARY KEY CLUSTERED ([JobId] ASC, [ParentJobId] ASC)
	);

	ALTER TABLE [$(PulseSchema)].[JobCondition] ADD CONSTRAINT [DF_Pulse_JobCondition_Finished]  DEFAULT ((0)) FOR [Finished];

	ALTER TABLE [$(PulseSchema)].[JobCondition]  WITH CHECK ADD  CONSTRAINT [PK_Pulse_JobCondition_Job] FOREIGN KEY([JobId])
	REFERENCES [$(PulseSchema)].[Job] ([Id])
	ON UPDATE CASCADE
	ON DELETE CASCADE;

	ALTER TABLE [$(PulseSchema)].[JobCondition] CHECK CONSTRAINT [PK_Pulse_JobCondition_Job];
	
	CREATE NONCLUSTERED INDEX [IX_Pulse_JobCondition_ParentJobId] ON [$(PulseSchema)].[JobCondition]
	(
		[ParentJobId] ASC
	);

	CREATE NONCLUSTERED INDEX [IX_Pulse_JobCondition_JobId] ON [$(PulseSchema)].[JobCondition]
	(
		[JobId] ASC
	);

	PRINT 'Created table [$(PulseSchema)].[JobCondition]';

	PRINT 'Creating table [$(PulseSchema)].[Schedule]';

	-- Schedule

	CREATE TABLE [$(PulseSchema)].[Schedule](
	[Name] [nvarchar](100) NOT NULL,
	[Cron] [nvarchar](50) NOT NULL,
	[LastInvocation] [datetime2](7) NOT NULL,
	[NextInvocation] [datetime2](7) NOT NULL,
	[JobInvocationData] [nvarchar](max) NULL,
	[WorkflowInvocationData] [nvarchar](max) NULL,
	[OnlyIfLastFinishedOrFailed] [bit] NOT NULL,

	 CONSTRAINT [PK_Pulse_Schedule] PRIMARY KEY CLUSTERED ([Name] ASC)
	);

	ALTER TABLE [$(PulseSchema)].[Schedule] ADD  CONSTRAINT [DF_Schedule_OnlyIfLastFinishedOrFailed]  DEFAULT ((0)) FOR [OnlyIfLastFinishedOrFailed];

	CREATE NONCLUSTERED INDEX [IX_Pulse_Schedule_NextInvocation] ON [$(PulseSchema)].[Schedule]
	(
		[NextInvocation] ASC
	);

	PRINT 'Created table [$(PulseSchema)].[Schedule]';

	-- Server

	CREATE TABLE [$(PulseSchema)].[Server](
	[Id] [nvarchar](100) NOT NULL,
	[Data] [nvarchar](max) NULL,
	[LastHeartbeat] [datetime2](7) NOT NULL,

	 CONSTRAINT [PK_Pulse_Server] PRIMARY KEY CLUSTERED ([Id] ASC)
	);

	PRINT 'Created table [$(PulseSchema)].[Server]';

	PRINT 'Creating table [$(PulseSchema)].[State]';

	-- State

	CREATE TABLE [$(PulseSchema)].[State](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobId] [int] NOT NULL,
	[Name] [nvarchar](20) NOT NULL,
	[Reason] [nvarchar](200) NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Data] [nvarchar](max) NULL,

	 CONSTRAINT [PK_Pulse_State] PRIMARY KEY CLUSTERED ([Id] ASC)
	);

	ALTER TABLE [$(PulseSchema)].[State]  WITH CHECK ADD  CONSTRAINT [FK_Pulse_State_Job] FOREIGN KEY([JobId])
	REFERENCES [$(PulseSchema)].[Job] ([Id])
	ON UPDATE CASCADE
	ON DELETE CASCADE;

	ALTER TABLE [$(PulseSchema)].[State] CHECK CONSTRAINT [FK_Pulse_State_Job];

	CREATE NONCLUSTERED INDEX [IX_Pulse_State_JobId] ON [$(PulseSchema)].[State]
	(
		[JobId] ASC
	);

	PRINT 'Created table [$(PulseSchema)].[State]';

	PRINT 'Creating table [$(PulseSchema)].[Worker]';

	-- Worker

	CREATE TABLE [$(PulseSchema)].[Worker](
	[Id] [nvarchar](100) NOT NULL,
	[Server] [nvarchar](100) NOT NULL,

	 CONSTRAINT [PK_Pulse_Worker] PRIMARY KEY CLUSTERED ([Id] ASC)
	);

	ALTER TABLE [$(PulseSchema)].[Worker]  WITH CHECK ADD  CONSTRAINT [FK_Pulse_Worker_Server] FOREIGN KEY([Server])
	REFERENCES [$(PulseSchema)].[Server] ([Id])
	ON UPDATE CASCADE
	ON DELETE CASCADE;
	
	ALTER TABLE [$(PulseSchema)].[Worker] CHECK CONSTRAINT [FK_Pulse_Worker_Server];

	
	CREATE NONCLUSTERED INDEX [IX_Pulse_Worker_Server] ON [$(PulseSchema)].[Worker]
	(
		[Server] ASC
	);
	    
	PRINT 'Created table [$(PulseSchema)].[Worker]';

	PRINT 'Creating table [$(PulseSchema)].[Queue]';

	-- Queue

	CREATE TABLE [$(PulseSchema)].[Queue](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[JobId] [int] NOT NULL,
	[Queue] [nvarchar](50) NOT NULL,
	[FetchedAt] [datetime2](7) NULL,
	[WorkerId] [nvarchar](100) NULL,

	 CONSTRAINT [PK_Pulse_Queue] PRIMARY KEY CLUSTERED ([Id] ASC)
	);

	ALTER TABLE [$(PulseSchema)].[Queue]  WITH CHECK ADD  CONSTRAINT [FK_Pulse_Queue_Job] FOREIGN KEY([JobId])
	REFERENCES [$(PulseSchema)].[Job] ([Id])
	ON UPDATE CASCADE
	ON DELETE CASCADE;	

	ALTER TABLE [$(PulseSchema)].[Queue] CHECK CONSTRAINT [FK_Pulse_Queue_Job];

	ALTER TABLE [$(PulseSchema)].[Queue]  WITH CHECK ADD  CONSTRAINT [FK_Pulse_Queue_Worker] FOREIGN KEY([WorkerId])
	REFERENCES [$(PulseSchema)].[Worker] ([Id])
	ON UPDATE CASCADE
	ON DELETE SET NULL;

	ALTER TABLE [$(PulseSchema)].[Queue] CHECK CONSTRAINT [FK_Pulse_Queue_Worker];

	CREATE NONCLUSTERED INDEX [IX_Pulse_Queue_Queue] ON [$(PulseSchema)].[Queue]
	(
		[Queue] ASC
	);

	CREATE NONCLUSTERED INDEX [IX_Pulse_Queue_WorkerId] ON [$(PulseSchema)].[Queue]
	(
		[WorkerId] ASC
	);

	PRINT 'Created table [$(PulseSchema)].[Queue]';

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
