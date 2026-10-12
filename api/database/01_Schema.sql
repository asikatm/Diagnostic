IF OBJECT_ID(N'[admin].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'admin') IS NULL EXEC(N'CREATE SCHEMA [admin];');
    CREATE TABLE [admin].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'admin') IS NULL EXEC(N'CREATE SCHEMA [admin];');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'diag') IS NULL EXEC(N'CREATE SCHEMA [diag];');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[AppGlobalSettings] (
        [Id] int NOT NULL IDENTITY,
        [SettingKey] nvarchar(100) NOT NULL,
        [SettingValue] nvarchar(max) NULL,
        [SettingGroup] nvarchar(250) NULL,
        [Description] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_AppGlobalSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Countries] (
        [Id] int NOT NULL IDENTITY,
        [Nationality] nvarchar(250) NULL,
        [PhoneCode] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Countries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[Doctors] (
        [Id] int NOT NULL IDENTITY,
        [Degree] nvarchar(250) NULL,
        [Designation] nvarchar(250) NULL,
        [Phone] nvarchar(250) NULL,
        [IsReferrer] bit NOT NULL,
        [IsConsultant] bit NOT NULL,
        [IsReportSigner] bit NOT NULL,
        [CommissionPercent] decimal(5,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[Machines] (
        [Id] int NOT NULL IDENTITY,
        [Model] nvarchar(250) NULL,
        [Manufacturer] nvarchar(250) NULL,
        [InterfaceCode] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Machines] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[NumberSequences] (
        [Id] int NOT NULL IDENTITY,
        [SequenceKey] nvarchar(30) NOT NULL,
        [Prefix] nvarchar(10) NOT NULL,
        [Period] nvarchar(10) NOT NULL,
        [LastNumber] int NOT NULL,
        [Padding] int NOT NULL,
        CONSTRAINT [PK_NumberSequences] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[OpdPackages] (
        [Id] int NOT NULL IDENTITY,
        [PackagePrice] decimal(18,2) NOT NULL,
        [ValidFrom] date NULL,
        [ValidTo] date NULL,
        [Description] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_OpdPackages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[ParentGroups] (
        [Id] int NOT NULL IDENTITY,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ParentGroups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[PaymentModes] (
        [Id] int NOT NULL IDENTITY,
        [RequiresReference] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_PaymentModes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Projects] (
        [Id] int NOT NULL IDENTITY,
        [Icon] nvarchar(250) NULL,
        [Url] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Religions] (
        [Id] int NOT NULL IDENTITY,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Religions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Roles] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(250) NULL,
        [IsSuperAdmin] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[Specimens] (
        [Id] int NOT NULL IDENTITY,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Specimens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Tenants] (
        [Id] int NOT NULL IDENTITY,
        [ShortName] nvarchar(250) NULL,
        [Address] nvarchar(250) NULL,
        [Phone] nvarchar(250) NULL,
        [Email] nvarchar(250) NULL,
        [LogoPath] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(150) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[Vacutainers] (
        [Id] int NOT NULL IDENTITY,
        [ColorName] nvarchar(250) NULL,
        [ColorHex] nvarchar(10) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Vacutainers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[PaymentChannels] (
        [Id] int NOT NULL IDENTITY,
        [PaymentModeId] int NOT NULL,
        [AccountNo] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_PaymentChannels] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaymentChannels_PaymentModes_PaymentModeId] FOREIGN KEY ([PaymentModeId]) REFERENCES [admin].[PaymentModes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Menus] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [ParentId] int NULL,
        [Url] nvarchar(250) NULL,
        [Icon] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Menus] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Menus_Menus_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [admin].[Menus] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Menus_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [admin].[Projects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Reports] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NULL,
        [ProcedureName] nvarchar(128) NOT NULL,
        [Template] nvarchar(50) NOT NULL,
        [Orientation] nvarchar(20) NULL,
        [Description] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reports_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [admin].[Projects] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[Patients] (
        [Id] int NOT NULL IDENTITY,
        [PatientCode] nvarchar(20) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Gender] nvarchar(30) NOT NULL,
        [DateOfBirth] date NULL,
        [Phone] nvarchar(20) NULL,
        [Email] nvarchar(250) NULL,
        [Address] nvarchar(250) NULL,
        [BloodGroup] nvarchar(5) NULL,
        [ReligionId] int NULL,
        [CountryId] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Patients_Countries_CountryId] FOREIGN KEY ([CountryId]) REFERENCES [admin].[Countries] ([Id]),
        CONSTRAINT [FK_Patients_Religions_ReligionId] FOREIGN KEY ([ReligionId]) REFERENCES [admin].[Religions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Departments] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Departments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [admin].[Tenants] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[LetterHeads] (
        [Id] int NOT NULL IDENTITY,
        [TenantId] int NULL,
        [Title] nvarchar(250) NULL,
        [SubTitle] nvarchar(250) NULL,
        [Address] nvarchar(250) NULL,
        [Phone] nvarchar(250) NULL,
        [Email] nvarchar(250) NULL,
        [LogoPath] nvarchar(250) NULL,
        [FooterText] nvarchar(1000) NULL,
        [PrePrintedTopMarginMm] int NOT NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_LetterHeads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LetterHeads_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [admin].[Tenants] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Printers] (
        [Id] int NOT NULL IDENTITY,
        [PrinterType] nvarchar(30) NOT NULL,
        [PrinterPath] nvarchar(250) NULL,
        [PaperSize] nvarchar(250) NULL,
        [TenantId] int NULL,
        [IsDefault] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Printers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Printers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [admin].[Tenants] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[MenuPermissions] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [MenuId] int NOT NULL,
        [CanView] bit NOT NULL,
        [CanAdd] bit NOT NULL,
        [CanEdit] bit NOT NULL,
        [CanDelete] bit NOT NULL,
        [CanPrint] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_MenuPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuPermissions_Menus_MenuId] FOREIGN KEY ([MenuId]) REFERENCES [admin].[Menus] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MenuPermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [admin].[Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[ReportPermissions] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ReportId] int NOT NULL,
        [CanView] bit NOT NULL,
        [CanExport] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReportPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportPermissions_Reports_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [admin].[Reports] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReportPermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [admin].[Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[ReportSpParameters] (
        [Id] int NOT NULL IDENTITY,
        [ReportId] int NOT NULL,
        [ParamName] nvarchar(128) NOT NULL,
        [Label] nvarchar(250) NOT NULL,
        [DataType] nvarchar(20) NOT NULL,
        [IsRequired] bit NOT NULL,
        [DefaultValue] nvarchar(250) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReportSpParameters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportSpParameters_Reports_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [admin].[Reports] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[ServiceUnits] (
        [Id] int NOT NULL IDENTITY,
        [DepartmentId] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ServiceUnits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceUnits_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [admin].[Departments] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[TestGroups] (
        [Id] int NOT NULL IDENTITY,
        [ParentGroupId] int NOT NULL,
        [DepartmentId] int NULL,
        [ReportHeading] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_TestGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TestGroups_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [admin].[Departments] ([Id]),
        CONSTRAINT [FK_TestGroups_ParentGroups_ParentGroupId] FOREIGN KEY ([ParentGroupId]) REFERENCES [diag].[ParentGroups] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[Users] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(50) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Email] nvarchar(250) NULL,
        [Phone] nvarchar(250) NULL,
        [Designation] nvarchar(250) NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [TenantId] int NULL,
        [DepartmentId] int NULL,
        [LastLoginAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [admin].[Departments] ([Id]),
        CONSTRAINT [FK_Users_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [admin].[Tenants] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[DiscountSetups] (
        [Id] int NOT NULL IDENTITY,
        [DiscountType] nvarchar(30) NOT NULL,
        [Value] decimal(18,2) NOT NULL,
        [TestGroupId] int NULL,
        [DoctorId] int NULL,
        [ValidFrom] date NULL,
        [ValidTo] date NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_DiscountSetups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DiscountSetups_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [diag].[Doctors] ([Id]),
        CONSTRAINT [FK_DiscountSetups_TestGroups_TestGroupId] FOREIGN KEY ([TestGroupId]) REFERENCES [diag].[TestGroups] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[TestItems] (
        [Id] int NOT NULL IDENTITY,
        [TestGroupId] int NOT NULL,
        [ShortName] nvarchar(250) NULL,
        [Price] decimal(18,2) NOT NULL,
        [VatPercent] decimal(5,2) NOT NULL,
        [IsDiscountAllowed] bit NOT NULL,
        [MaxDiscountPercent] decimal(5,2) NOT NULL,
        [SpecimenId] int NULL,
        [ResultType] nvarchar(30) NOT NULL,
        [DefaultReportText] nvarchar(max) NULL,
        [IsOutsource] bit NOT NULL,
        [RequiresSample] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_TestItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TestItems_Specimens_SpecimenId] FOREIGN KEY ([SpecimenId]) REFERENCES [diag].[Specimens] ([Id]),
        CONSTRAINT [FK_TestItems_TestGroups_TestGroupId] FOREIGN KEY ([TestGroupId]) REFERENCES [diag].[TestGroups] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [admin].[UserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [admin].[Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [admin].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[Investigations] (
        [Id] int NOT NULL IDENTITY,
        [InvoiceNo] nvarchar(20) NOT NULL,
        [InvoiceDate] datetime2 NOT NULL,
        [TenantId] int NOT NULL,
        [PatientId] int NOT NULL,
        [PatientAge] nvarchar(30) NULL,
        [ReferDoctorId] int NULL,
        [OpdPackageId] int NULL,
        [DiscountSetupId] int NULL,
        [GrossAmount] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [VatAmount] decimal(18,2) NOT NULL,
        [NetAmount] decimal(18,2) NOT NULL,
        [PaidAmount] decimal(18,2) NOT NULL,
        [RefundAmount] decimal(18,2) NOT NULL,
        [DueAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Remarks] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Investigations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Investigations_DiscountSetups_DiscountSetupId] FOREIGN KEY ([DiscountSetupId]) REFERENCES [diag].[DiscountSetups] ([Id]),
        CONSTRAINT [FK_Investigations_Doctors_ReferDoctorId] FOREIGN KEY ([ReferDoctorId]) REFERENCES [diag].[Doctors] ([Id]),
        CONSTRAINT [FK_Investigations_OpdPackages_OpdPackageId] FOREIGN KEY ([OpdPackageId]) REFERENCES [diag].[OpdPackages] ([Id]),
        CONSTRAINT [FK_Investigations_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [diag].[Patients] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Investigations_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [admin].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[OpdPackageItems] (
        [Id] int NOT NULL IDENTITY,
        [OpdPackageId] int NOT NULL,
        [TestItemId] int NOT NULL,
        [ItemPrice] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_OpdPackageItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OpdPackageItems_OpdPackages_OpdPackageId] FOREIGN KEY ([OpdPackageId]) REFERENCES [diag].[OpdPackages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OpdPackageItems_TestItems_TestItemId] FOREIGN KEY ([TestItemId]) REFERENCES [diag].[TestItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[ReportDeliveryTimes] (
        [Id] int NOT NULL IDENTITY,
        [TestGroupId] int NULL,
        [TestItemId] int NULL,
        [DeliveryHours] int NOT NULL,
        [DeliveryAt] time NULL,
        [Remarks] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReportDeliveryTimes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportDeliveryTimes_TestGroups_TestGroupId] FOREIGN KEY ([TestGroupId]) REFERENCES [diag].[TestGroups] ([Id]),
        CONSTRAINT [FK_ReportDeliveryTimes_TestItems_TestItemId] FOREIGN KEY ([TestItemId]) REFERENCES [diag].[TestItems] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[ReportParameters] (
        [Id] int NOT NULL IDENTITY,
        [TestItemId] int NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [GroupHeading] nvarchar(250) NULL,
        [Unit] nvarchar(250) NULL,
        [NormalRange] nvarchar(250) NULL,
        [NormalRangeMale] nvarchar(250) NULL,
        [NormalRangeFemale] nvarchar(250) NULL,
        [LowValue] decimal(18,4) NULL,
        [HighValue] decimal(18,4) NULL,
        [Method] nvarchar(250) NULL,
        [InputType] nvarchar(30) NOT NULL,
        [Options] nvarchar(500) NULL,
        [DefaultValue] nvarchar(250) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_ReportParameters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportParameters_TestItems_TestItemId] FOREIGN KEY ([TestItemId]) REFERENCES [diag].[TestItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[VacuMachineMappings] (
        [Id] int NOT NULL IDENTITY,
        [TestGroupId] int NOT NULL,
        [TestItemId] int NULL,
        [VacutainerId] int NOT NULL,
        [MachineId] int NULL,
        [SampleVolume] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_VacuMachineMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VacuMachineMappings_Machines_MachineId] FOREIGN KEY ([MachineId]) REFERENCES [diag].[Machines] ([Id]),
        CONSTRAINT [FK_VacuMachineMappings_TestGroups_TestGroupId] FOREIGN KEY ([TestGroupId]) REFERENCES [diag].[TestGroups] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VacuMachineMappings_TestItems_TestItemId] FOREIGN KEY ([TestItemId]) REFERENCES [diag].[TestItems] ([Id]),
        CONSTRAINT [FK_VacuMachineMappings_Vacutainers_VacutainerId] FOREIGN KEY ([VacutainerId]) REFERENCES [diag].[Vacutainers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[InvestigationPayments] (
        [Id] int NOT NULL IDENTITY,
        [InvestigationId] int NOT NULL,
        [ReceiptNo] nvarchar(20) NOT NULL,
        [PaymentDate] datetime2 NOT NULL,
        [PaymentType] nvarchar(30) NOT NULL,
        [PaymentModeId] int NOT NULL,
        [PaymentChannelId] int NULL,
        [Amount] decimal(18,2) NOT NULL,
        [TransactionRef] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_InvestigationPayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvestigationPayments_Investigations_InvestigationId] FOREIGN KEY ([InvestigationId]) REFERENCES [diag].[Investigations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InvestigationPayments_PaymentChannels_PaymentChannelId] FOREIGN KEY ([PaymentChannelId]) REFERENCES [admin].[PaymentChannels] ([Id]),
        CONSTRAINT [FK_InvestigationPayments_PaymentModes_PaymentModeId] FOREIGN KEY ([PaymentModeId]) REFERENCES [admin].[PaymentModes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[SampleCollections] (
        [Id] int NOT NULL IDENTITY,
        [SampleNo] nvarchar(20) NOT NULL,
        [InvestigationId] int NOT NULL,
        [VacutainerId] int NULL,
        [SpecimenId] int NULL,
        [Status] nvarchar(30) NOT NULL,
        [CollectedAt] datetime2 NOT NULL,
        [CollectedBy] int NULL,
        [ReceivedAt] datetime2 NULL,
        [ReceivedBy] int NULL,
        [RejectReason] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_SampleCollections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SampleCollections_Investigations_InvestigationId] FOREIGN KEY ([InvestigationId]) REFERENCES [diag].[Investigations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SampleCollections_Specimens_SpecimenId] FOREIGN KEY ([SpecimenId]) REFERENCES [diag].[Specimens] ([Id]),
        CONSTRAINT [FK_SampleCollections_Vacutainers_VacutainerId] FOREIGN KEY ([VacutainerId]) REFERENCES [diag].[Vacutainers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[InvestigationDetails] (
        [Id] int NOT NULL IDENTITY,
        [InvestigationId] int NOT NULL,
        [TestItemId] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [VatAmount] decimal(18,2) NOT NULL,
        [NetAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [DeliveryDateTime] datetime2 NULL,
        [SampleCollectionId] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_InvestigationDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvestigationDetails_Investigations_InvestigationId] FOREIGN KEY ([InvestigationId]) REFERENCES [diag].[Investigations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InvestigationDetails_SampleCollections_SampleCollectionId] FOREIGN KEY ([SampleCollectionId]) REFERENCES [diag].[SampleCollections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvestigationDetails_TestItems_TestItemId] FOREIGN KEY ([TestItemId]) REFERENCES [diag].[TestItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[CancelRequests] (
        [Id] int NOT NULL IDENTITY,
        [InvestigationId] int NOT NULL,
        [InvestigationDetailId] int NULL,
        [Reason] nvarchar(500) NOT NULL,
        [RefundAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [ApprovedBy] int NULL,
        [ApprovedAt] datetime2 NULL,
        [ApprovalRemarks] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_CancelRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CancelRequests_InvestigationDetails_InvestigationDetailId] FOREIGN KEY ([InvestigationDetailId]) REFERENCES [diag].[InvestigationDetails] ([Id]),
        CONSTRAINT [FK_CancelRequests_Investigations_InvestigationId] FOREIGN KEY ([InvestigationId]) REFERENCES [diag].[Investigations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[LabReports] (
        [Id] int NOT NULL IDENTITY,
        [InvestigationDetailId] int NOT NULL,
        [ReportText] nvarchar(max) NULL,
        [Comments] nvarchar(2000) NULL,
        [EnteredBy] int NULL,
        [EnteredAt] datetime2 NULL,
        [IsVerified] bit NOT NULL,
        [VerifiedBy] int NULL,
        [VerifiedAt] datetime2 NULL,
        [SignDoctorId] int NULL,
        [DeliveredAt] datetime2 NULL,
        [DeliveredTo] nvarchar(250) NULL,
        [PrintCount] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_LabReports] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LabReports_Doctors_SignDoctorId] FOREIGN KEY ([SignDoctorId]) REFERENCES [diag].[Doctors] ([Id]),
        CONSTRAINT [FK_LabReports_InvestigationDetails_InvestigationDetailId] FOREIGN KEY ([InvestigationDetailId]) REFERENCES [diag].[InvestigationDetails] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE TABLE [diag].[LabResults] (
        [Id] int NOT NULL IDENTITY,
        [LabReportId] int NOT NULL,
        [ReportParameterId] int NOT NULL,
        [ResultValue] nvarchar(500) NULL,
        [Flag] nvarchar(30) NOT NULL,
        [Remarks] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_LabResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LabResults_LabReports_LabReportId] FOREIGN KEY ([LabReportId]) REFERENCES [diag].[LabReports] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LabResults_ReportParameters_ReportParameterId] FOREIGN KEY ([ReportParameterId]) REFERENCES [diag].[ReportParameters] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AppGlobalSettings_SettingKey] ON [admin].[AppGlobalSettings] ([SettingKey]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CancelRequests_InvestigationDetailId] ON [diag].[CancelRequests] ([InvestigationDetailId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CancelRequests_InvestigationId] ON [diag].[CancelRequests] ([InvestigationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CancelRequests_Status] ON [diag].[CancelRequests] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Countries_Code] ON [admin].[Countries] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Departments_Code] ON [admin].[Departments] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Departments_TenantId] ON [admin].[Departments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DiscountSetups_Code] ON [diag].[DiscountSetups] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DiscountSetups_DoctorId] ON [diag].[DiscountSetups] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DiscountSetups_TestGroupId] ON [diag].[DiscountSetups] ([TestGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Doctors_Code] ON [diag].[Doctors] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestigationDetails_InvestigationId] ON [diag].[InvestigationDetails] ([InvestigationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestigationDetails_SampleCollectionId] ON [diag].[InvestigationDetails] ([SampleCollectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestigationDetails_Status] ON [diag].[InvestigationDetails] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestigationDetails_TestItemId] ON [diag].[InvestigationDetails] ([TestItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestigationPayments_InvestigationId] ON [diag].[InvestigationPayments] ([InvestigationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestigationPayments_PaymentChannelId] ON [diag].[InvestigationPayments] ([PaymentChannelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestigationPayments_PaymentDate] ON [diag].[InvestigationPayments] ([PaymentDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestigationPayments_PaymentModeId] ON [diag].[InvestigationPayments] ([PaymentModeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvestigationPayments_ReceiptNo] ON [diag].[InvestigationPayments] ([ReceiptNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Investigations_DiscountSetupId] ON [diag].[Investigations] ([DiscountSetupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Investigations_InvoiceDate] ON [diag].[Investigations] ([InvoiceDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Investigations_InvoiceNo] ON [diag].[Investigations] ([InvoiceNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Investigations_OpdPackageId] ON [diag].[Investigations] ([OpdPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Investigations_PatientId] ON [diag].[Investigations] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Investigations_ReferDoctorId] ON [diag].[Investigations] ([ReferDoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Investigations_TenantId] ON [diag].[Investigations] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LabReports_InvestigationDetailId] ON [diag].[LabReports] ([InvestigationDetailId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LabReports_SignDoctorId] ON [diag].[LabReports] ([SignDoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LabResults_LabReportId_ReportParameterId] ON [diag].[LabResults] ([LabReportId], [ReportParameterId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LabResults_ReportParameterId] ON [diag].[LabResults] ([ReportParameterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LetterHeads_Code] ON [diag].[LetterHeads] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LetterHeads_TenantId] ON [diag].[LetterHeads] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Machines_Code] ON [diag].[Machines] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MenuPermissions_MenuId] ON [admin].[MenuPermissions] ([MenuId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MenuPermissions_RoleId_MenuId] ON [admin].[MenuPermissions] ([RoleId], [MenuId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Menus_Code] ON [admin].[Menus] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Menus_ParentId] ON [admin].[Menus] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Menus_ProjectId] ON [admin].[Menus] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NumberSequences_SequenceKey] ON [admin].[NumberSequences] ([SequenceKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OpdPackageItems_OpdPackageId] ON [diag].[OpdPackageItems] ([OpdPackageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OpdPackageItems_TestItemId] ON [diag].[OpdPackageItems] ([TestItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_OpdPackages_Code] ON [diag].[OpdPackages] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ParentGroups_Code] ON [diag].[ParentGroups] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Patients_CountryId] ON [diag].[Patients] ([CountryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Patients_PatientCode] ON [diag].[Patients] ([PatientCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Patients_Phone] ON [diag].[Patients] ([Phone]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Patients_ReligionId] ON [diag].[Patients] ([ReligionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PaymentChannels_Code] ON [admin].[PaymentChannels] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PaymentChannels_PaymentModeId] ON [admin].[PaymentChannels] ([PaymentModeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PaymentModes_Code] ON [admin].[PaymentModes] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Printers_Code] ON [admin].[Printers] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Printers_TenantId] ON [admin].[Printers] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Projects_Code] ON [admin].[Projects] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Religions_Code] ON [admin].[Religions] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReportDeliveryTimes_TestGroupId] ON [diag].[ReportDeliveryTimes] ([TestGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReportDeliveryTimes_TestItemId] ON [diag].[ReportDeliveryTimes] ([TestItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReportParameters_TestItemId] ON [diag].[ReportParameters] ([TestItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ReportPermissions_ReportId] ON [admin].[ReportPermissions] ([ReportId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ReportPermissions_RoleId_ReportId] ON [admin].[ReportPermissions] ([RoleId], [ReportId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Reports_Code] ON [admin].[Reports] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reports_ProjectId] ON [admin].[Reports] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ReportSpParameters_ReportId_ParamName] ON [admin].[ReportSpParameters] ([ReportId], [ParamName]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Roles_Code] ON [admin].[Roles] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Roles_Name] ON [admin].[Roles] ([Name]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SampleCollections_InvestigationId] ON [diag].[SampleCollections] ([InvestigationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SampleCollections_SampleNo] ON [diag].[SampleCollections] ([SampleNo]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SampleCollections_SpecimenId] ON [diag].[SampleCollections] ([SpecimenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SampleCollections_VacutainerId] ON [diag].[SampleCollections] ([VacutainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ServiceUnits_Code] ON [admin].[ServiceUnits] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ServiceUnits_DepartmentId] ON [admin].[ServiceUnits] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Specimens_Code] ON [diag].[Specimens] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Tenants_Code] ON [admin].[Tenants] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TestGroups_Code] ON [diag].[TestGroups] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestGroups_DepartmentId] ON [diag].[TestGroups] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestGroups_ParentGroupId] ON [diag].[TestGroups] ([ParentGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TestItems_Code] ON [diag].[TestItems] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestItems_Name] ON [diag].[TestItems] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestItems_SpecimenId] ON [diag].[TestItems] ([SpecimenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestItems_TestGroupId] ON [diag].[TestItems] ([TestGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [admin].[UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_DepartmentId] ON [admin].[Users] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_TenantId] ON [admin].[Users] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_UserName] ON [admin].[Users] ([UserName]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VacuMachineMappings_MachineId] ON [diag].[VacuMachineMappings] ([MachineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VacuMachineMappings_TestGroupId_TestItemId] ON [diag].[VacuMachineMappings] ([TestGroupId], [TestItemId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VacuMachineMappings_TestItemId] ON [diag].[VacuMachineMappings] ([TestItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VacuMachineMappings_VacutainerId] ON [diag].[VacuMachineMappings] ([VacutainerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vacutainers_Code] ON [diag].[Vacutainers] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930165305_InitialCreate'
)
BEGIN
    INSERT INTO [admin].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930165305_InitialCreate', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    ALTER TABLE [diag].[SampleCollections] ADD [DispatchedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    ALTER TABLE [diag].[SampleCollections] ADD [DispatchedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    ALTER TABLE [diag].[SampleCollections] ADD [RejectedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    ALTER TABLE [diag].[SampleCollections] ADD [RejectedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    ALTER TABLE [diag].[SampleCollections] ADD [RepeatOfSampleId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    CREATE INDEX [IX_SampleCollections_RepeatOfSampleId] ON [diag].[SampleCollections] ([RepeatOfSampleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    CREATE INDEX [IX_SampleCollections_Status] ON [diag].[SampleCollections] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    ALTER TABLE [diag].[SampleCollections] ADD CONSTRAINT [FK_SampleCollections_SampleCollections_RepeatOfSampleId] FOREIGN KEY ([RepeatOfSampleId]) REFERENCES [diag].[SampleCollections] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930173925_SampleDispatchAndRepeat'
)
BEGIN
    INSERT INTO [admin].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930173925_SampleDispatchAndRepeat', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930180843_ReportFinalization'
)
BEGIN
    ALTER TABLE [diag].[LabReports] ADD [FinalizedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930180843_ReportFinalization'
)
BEGIN
    ALTER TABLE [diag].[LabReports] ADD [FinalizedBy] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930180843_ReportFinalization'
)
BEGIN
    ALTER TABLE [diag].[LabReports] ADD [IsFinalized] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930180843_ReportFinalization'
)
BEGIN
    ALTER TABLE [diag].[LabReports] ADD [ReturnReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260930180843_ReportFinalization'
)
BEGIN
    INSERT INTO [admin].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260930180843_ReportFinalization', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    ALTER TABLE [diag].[Investigations] ADD [MediaCommission] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    ALTER TABLE [diag].[Investigations] ADD [MediaId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    ALTER TABLE [diag].[InvestigationDetails] ADD [MediaCommission] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    ALTER TABLE [diag].[DiscountSetups] ADD [TestItemId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    CREATE TABLE [diag].[Medias] (
        [Id] int NOT NULL IDENTITY,
        [Phone] nvarchar(250) NULL,
        [Organization] nvarchar(250) NULL,
        [Address] nvarchar(250) NULL,
        [DefaultCommissionPercent] decimal(5,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(250) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Medias] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    CREATE TABLE [diag].[MediaCommissionSetups] (
        [Id] int NOT NULL IDENTITY,
        [MediaId] int NULL,
        [TestGroupId] int NULL,
        [TestItemId] int NULL,
        [CommissionType] nvarchar(30) NOT NULL,
        [Value] decimal(18,2) NOT NULL,
        [Remarks] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [CreatedBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_MediaCommissionSetups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MediaCommissionSetups_Medias_MediaId] FOREIGN KEY ([MediaId]) REFERENCES [diag].[Medias] ([Id]),
        CONSTRAINT [FK_MediaCommissionSetups_TestGroups_TestGroupId] FOREIGN KEY ([TestGroupId]) REFERENCES [diag].[TestGroups] ([Id]),
        CONSTRAINT [FK_MediaCommissionSetups_TestItems_TestItemId] FOREIGN KEY ([TestItemId]) REFERENCES [diag].[TestItems] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    CREATE INDEX [IX_Investigations_MediaId] ON [diag].[Investigations] ([MediaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    CREATE INDEX [IX_DiscountSetups_TestItemId] ON [diag].[DiscountSetups] ([TestItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MediaCommissionSetups_MediaId_TestGroupId_TestItemId] ON [diag].[MediaCommissionSetups] ([MediaId], [TestGroupId], [TestItemId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    CREATE INDEX [IX_MediaCommissionSetups_TestGroupId] ON [diag].[MediaCommissionSetups] ([TestGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    CREATE INDEX [IX_MediaCommissionSetups_TestItemId] ON [diag].[MediaCommissionSetups] ([TestItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Medias_Code] ON [diag].[Medias] ([Code]) WHERE [Code] IS NOT NULL AND [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    CREATE INDEX [IX_Medias_Phone] ON [diag].[Medias] ([Phone]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    ALTER TABLE [diag].[DiscountSetups] ADD CONSTRAINT [FK_DiscountSetups_TestItems_TestItemId] FOREIGN KEY ([TestItemId]) REFERENCES [diag].[TestItems] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    ALTER TABLE [diag].[Investigations] ADD CONSTRAINT [FK_Investigations_Medias_MediaId] FOREIGN KEY ([MediaId]) REFERENCES [diag].[Medias] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [admin].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012164346_MediaCommission'
)
BEGIN
    INSERT INTO [admin].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012164346_MediaCommission', N'10.0.12');
END;

COMMIT;
GO

