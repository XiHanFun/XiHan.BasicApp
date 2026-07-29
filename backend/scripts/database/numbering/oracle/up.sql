-- 业务编号模块 Oracle 正向脚本。
-- 运行时仍以 SqlSugar CodeFirst 为主；本脚本用于受控数据库发布和人工审阅。

CREATE TABLE "Sys_Numbering_Rule" (
    "Basic_Id" NUMBER(19) NOT NULL,
    "Row_Version" NUMBER(19) DEFAULT 0 NOT NULL,
    "Tenant_Id" NUMBER(19) DEFAULT 0 NOT NULL,
    "Rule_Code" NVARCHAR2(100) NOT NULL,
    "Rule_Name" NVARCHAR2(100) NOT NULL,
    "Prefix" NVARCHAR2(50),
    "Separator" NVARCHAR2(10) DEFAULT '-' NOT NULL,
    "Date_Format" NUMBER(10) DEFAULT 3 NOT NULL,
    "Serial_Length" NUMBER(10) DEFAULT 4 NOT NULL,
    "Reset_Cycle" NUMBER(10) DEFAULT 1 NOT NULL,
    "Time_Zone_Id" NVARCHAR2(100) DEFAULT 'UTC' NOT NULL,
    "Current_Value" NUMBER(19) DEFAULT 0 NOT NULL,
    "Current_Period" NVARCHAR2(32),
    "Has_Allocated" NUMBER(1) DEFAULT 0 NOT NULL,
    "Allow_Tenant_Use" NUMBER(1) DEFAULT 0 NOT NULL,
    "Status" NUMBER(10) DEFAULT 1 NOT NULL,
    "Sort" NUMBER(10) DEFAULT 0 NOT NULL,
    "Remark" NVARCHAR2(500),
    "Created_Time" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Created_Id" NUMBER(19),
    "Created_By" NVARCHAR2(100),
    "Modified_Time" TIMESTAMP WITH TIME ZONE,
    "Modified_Id" NUMBER(19),
    "Modified_By" NVARCHAR2(100),
    "Is_Deleted" NUMBER(1) DEFAULT 0 NOT NULL,
    "Deleted_Time" TIMESTAMP WITH TIME ZONE,
    "Deleted_Id" NUMBER(19),
    "Deleted_By" NVARCHAR2(100),
    CONSTRAINT "PK_Sys_Numbering_Rule" PRIMARY KEY ("Basic_Id"),
    CONSTRAINT "CK_NumRule_SerialLength" CHECK ("Serial_Length" BETWEEN 1 AND 18),
    CONSTRAINT "CK_NumRule_CurrentValue" CHECK ("Current_Value" >= 0),
    CONSTRAINT "CK_NumRule_HasAllocated" CHECK ("Has_Allocated" IN (0, 1)),
    CONSTRAINT "CK_NumRule_AllowTenant" CHECK ("Allow_Tenant_Use" IN (0, 1)),
    CONSTRAINT "CK_NumRule_IsDeleted" CHECK ("Is_Deleted" IN (0, 1))
);

CREATE UNIQUE INDEX "UX_NumRule_Tenant_Code_Delete" ON "Sys_Numbering_Rule" ("Tenant_Id", "Rule_Code", "Is_Deleted");
CREATE INDEX "IX_NumRule_Tenant_Created" ON "Sys_Numbering_Rule" ("Tenant_Id", "Created_Time" DESC);
CREATE INDEX "IX_NumRule_Creator" ON "Sys_Numbering_Rule" ("Created_Id");
CREATE INDEX "IX_NumRule_Tenant_Delete" ON "Sys_Numbering_Rule" ("Tenant_Id", "Is_Deleted");
CREATE INDEX "IX_NumRule_Tenant_Status_Sort" ON "Sys_Numbering_Rule" ("Tenant_Id", "Status", "Sort");

CREATE TABLE "Sys_Numbering_Allocation" (
    "Basic_Id" NUMBER(19) NOT NULL,
    "Row_Version" NUMBER(19) DEFAULT 0 NOT NULL,
    "Tenant_Id" NUMBER(19) DEFAULT 0 NOT NULL,
    "Rule_Id" NUMBER(19) NOT NULL,
    "Rule_Code" NVARCHAR2(100) NOT NULL,
    "Request_Tenant_Id" NUMBER(19) DEFAULT 0 NOT NULL,
    "Idempotency_Key" NVARCHAR2(100) NOT NULL,
    "Request_Fingerprint" CHAR(64) NOT NULL,
    "Allocation_Count" NUMBER(10) NOT NULL,
    "Start_Value" NUMBER(19) NOT NULL,
    "End_Value" NUMBER(19) NOT NULL,
    "Period_Key" NVARCHAR2(32) NOT NULL,
    "Prefix_Snapshot" NVARCHAR2(50),
    "Separator_Snapshot" NVARCHAR2(10) NOT NULL,
    "Date_Text_Snapshot" NVARCHAR2(16),
    "Serial_Length_Snapshot" NUMBER(10) NOT NULL,
    "Generated_At_Utc" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Business_Type" NVARCHAR2(100),
    "Business_Id" NVARCHAR2(100),
    "Created_Time" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Created_Id" NUMBER(19),
    "Created_By" NVARCHAR2(100),
    CONSTRAINT "PK_Sys_Numbering_Allocation" PRIMARY KEY ("Basic_Id"),
    CONSTRAINT "CK_NumAlloc_Count" CHECK ("Allocation_Count" BETWEEN 1 AND 1000),
    CONSTRAINT "CK_NumAlloc_Range" CHECK ("Start_Value" >= 1 AND "End_Value" >= "Start_Value")
);

CREATE UNIQUE INDEX "UX_NumAlloc_Rule_Tenant_Key" ON "Sys_Numbering_Allocation" ("Tenant_Id", "Rule_Id", "Request_Tenant_Id", "Idempotency_Key");
CREATE INDEX "IX_NumAlloc_Tenant_Created" ON "Sys_Numbering_Allocation" ("Tenant_Id", "Created_Time" DESC);
CREATE INDEX "IX_NumAlloc_Creator" ON "Sys_Numbering_Allocation" ("Created_Id");
CREATE INDEX "IX_NumAlloc_Rule_Generated" ON "Sys_Numbering_Allocation" ("Rule_Id", "Generated_At_Utc" DESC);
CREATE INDEX "IX_NumAlloc_Request_Generated" ON "Sys_Numbering_Allocation" ("Request_Tenant_Id", "Generated_At_Utc" DESC);
