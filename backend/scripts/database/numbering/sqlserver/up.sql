-- 业务编号模块 SQL Server 正向脚本。
-- 运行时仍以 SqlSugar CodeFirst 为主；本脚本用于受控数据库发布和人工审阅。
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE [Sys_Numbering_Rule] (
    [Basic_Id] BIGINT NOT NULL CONSTRAINT [PK_Sys_Numbering_Rule] PRIMARY KEY,
    [Row_Version] BIGINT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_RowVersion] DEFAULT 0,
    [Tenant_Id] BIGINT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_Tenant] DEFAULT 0,
    [Rule_Code] NVARCHAR(100) NOT NULL,
    [Rule_Name] NVARCHAR(100) NOT NULL,
    [Prefix] NVARCHAR(50) NULL,
    [Separator] NVARCHAR(10) NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_Separator] DEFAULT N'-',
    [Date_Format] INT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_DateFormat] DEFAULT 3,
    [Serial_Length] INT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_SerialLength] DEFAULT 4,
    [Reset_Cycle] INT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_ResetCycle] DEFAULT 1,
    [Time_Zone_Id] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_TimeZone] DEFAULT N'UTC',
    [Current_Value] BIGINT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_CurrentValue] DEFAULT 0,
    [Current_Period] NVARCHAR(32) NULL,
    [Has_Allocated] BIT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_HasAllocated] DEFAULT 0,
    [Allow_Tenant_Use] BIT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_AllowTenantUse] DEFAULT 0,
    [Status] INT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_Status] DEFAULT 1,
    [Sort] INT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_Sort] DEFAULT 0,
    [Remark] NVARCHAR(500) NULL,
    [Created_Time] DATETIMEOFFSET(7) NOT NULL,
    [Created_Id] BIGINT NULL,
    [Created_By] NVARCHAR(100) NULL,
    [Modified_Time] DATETIMEOFFSET(7) NULL,
    [Modified_Id] BIGINT NULL,
    [Modified_By] NVARCHAR(100) NULL,
    [Is_Deleted] BIT NOT NULL CONSTRAINT [DF_Sys_Numbering_Rule_IsDeleted] DEFAULT 0,
    [Deleted_Time] DATETIMEOFFSET(7) NULL,
    [Deleted_Id] BIGINT NULL,
    [Deleted_By] NVARCHAR(100) NULL,
    CONSTRAINT [CK_Sys_Numbering_Rule_Serial_Length] CHECK ([Serial_Length] BETWEEN 1 AND 18),
    CONSTRAINT [CK_Sys_Numbering_Rule_Current_Value] CHECK ([Current_Value] >= 0)
);

CREATE UNIQUE INDEX [UX_Sys_Numbering_Rule_TeId_RuCo] ON [Sys_Numbering_Rule] ([Tenant_Id], [Rule_Code], [Is_Deleted]);
CREATE INDEX [IX_Sys_Numbering_Rule_TeId_CrTi] ON [Sys_Numbering_Rule] ([Tenant_Id], [Created_Time] DESC);
CREATE INDEX [IX_Sys_Numbering_Rule_CrId] ON [Sys_Numbering_Rule] ([Created_Id]);
CREATE INDEX [IX_Sys_Numbering_Rule_TeId_IsDe] ON [Sys_Numbering_Rule] ([Tenant_Id], [Is_Deleted]);
CREATE INDEX [IX_Sys_Numbering_Rule_TeId_St_So] ON [Sys_Numbering_Rule] ([Tenant_Id], [Status], [Sort]);

CREATE TABLE [Sys_Numbering_Allocation] (
    [Basic_Id] BIGINT NOT NULL CONSTRAINT [PK_Sys_Numbering_Allocation] PRIMARY KEY,
    [Row_Version] BIGINT NOT NULL CONSTRAINT [DF_Sys_Numbering_Allocation_RowVersion] DEFAULT 0,
    [Tenant_Id] BIGINT NOT NULL CONSTRAINT [DF_Sys_Numbering_Allocation_Tenant] DEFAULT 0,
    [Rule_Id] BIGINT NOT NULL,
    [Rule_Code] NVARCHAR(100) NOT NULL,
    [Request_Tenant_Id] BIGINT NOT NULL CONSTRAINT [DF_Sys_Numbering_Allocation_RequestTenant] DEFAULT 0,
    [Idempotency_Key] NVARCHAR(100) NOT NULL,
    [Request_Fingerprint] CHAR(64) NOT NULL,
    [Allocation_Count] INT NOT NULL,
    [Start_Value] BIGINT NOT NULL,
    [End_Value] BIGINT NOT NULL,
    [Period_Key] NVARCHAR(32) NOT NULL,
    [Prefix_Snapshot] NVARCHAR(50) NULL,
    [Separator_Snapshot] NVARCHAR(10) NOT NULL,
    [Date_Text_Snapshot] NVARCHAR(16) NULL,
    [Serial_Length_Snapshot] INT NOT NULL,
    [Generated_At_Utc] DATETIMEOFFSET(7) NOT NULL,
    [Business_Type] NVARCHAR(100) NULL,
    [Business_Id] NVARCHAR(100) NULL,
    [Created_Time] DATETIMEOFFSET(7) NOT NULL,
    [Created_Id] BIGINT NULL,
    [Created_By] NVARCHAR(100) NULL,
    CONSTRAINT [CK_Sys_Numbering_Allocation_Count] CHECK ([Allocation_Count] BETWEEN 1 AND 1000),
    CONSTRAINT [CK_Sys_Numbering_Allocation_Range] CHECK ([Start_Value] >= 1 AND [End_Value] >= [Start_Value])
);

CREATE UNIQUE INDEX [UX_Sys_Numbering_Allocation_Ru_ReTe_IdKe] ON [Sys_Numbering_Allocation] ([Tenant_Id], [Rule_Id], [Request_Tenant_Id], [Idempotency_Key]);
CREATE INDEX [IX_Sys_Numbering_Allocation_TeId_CrTi] ON [Sys_Numbering_Allocation] ([Tenant_Id], [Created_Time] DESC);
CREATE INDEX [IX_Sys_Numbering_Allocation_CrId] ON [Sys_Numbering_Allocation] ([Created_Id]);
CREATE INDEX [IX_Sys_Numbering_Allocation_Ru_GeTi] ON [Sys_Numbering_Allocation] ([Rule_Id], [Generated_At_Utc] DESC);
CREATE INDEX [IX_Sys_Numbering_Allocation_ReTe_GeTi] ON [Sys_Numbering_Allocation] ([Request_Tenant_Id], [Generated_At_Utc] DESC);

COMMIT TRANSACTION;
