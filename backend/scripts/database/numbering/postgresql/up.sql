-- 业务编号模块 PostgreSQL 正向脚本。
-- 运行时仍以 SqlSugar CodeFirst 为主；本脚本用于受控数据库发布和人工审阅。
BEGIN;

CREATE TABLE "Sys_Numbering_Rule" (
    "Basic_Id" BIGINT PRIMARY KEY,
    "Row_Version" BIGINT NOT NULL DEFAULT 0,
    "Tenant_Id" BIGINT NOT NULL DEFAULT 0,
    "Rule_Code" VARCHAR(100) NOT NULL,
    "Rule_Name" VARCHAR(100) NOT NULL,
    "Prefix" VARCHAR(50) NULL,
    "Separator" VARCHAR(10) NOT NULL DEFAULT '-',
    "Date_Format" INTEGER NOT NULL DEFAULT 3,
    "Serial_Length" INTEGER NOT NULL DEFAULT 4,
    "Reset_Cycle" INTEGER NOT NULL DEFAULT 1,
    "Time_Zone_Id" VARCHAR(100) NOT NULL DEFAULT 'UTC',
    "Current_Value" BIGINT NOT NULL DEFAULT 0,
    "Current_Period" VARCHAR(32) NULL,
    "Has_Allocated" BOOLEAN NOT NULL DEFAULT FALSE,
    "Allow_Tenant_Use" BOOLEAN NOT NULL DEFAULT FALSE,
    "Status" INTEGER NOT NULL DEFAULT 1,
    "Sort" INTEGER NOT NULL DEFAULT 0,
    "Remark" VARCHAR(500) NULL,
    "Created_Time" TIMESTAMPTZ NOT NULL,
    "Created_Id" BIGINT NULL,
    "Created_By" VARCHAR(100) NULL,
    "Modified_Time" TIMESTAMPTZ NULL,
    "Modified_Id" BIGINT NULL,
    "Modified_By" VARCHAR(100) NULL,
    "Is_Deleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "Deleted_Time" TIMESTAMPTZ NULL,
    "Deleted_Id" BIGINT NULL,
    "Deleted_By" VARCHAR(100) NULL,
    CONSTRAINT "CK_Sys_Numbering_Rule_Serial_Length" CHECK ("Serial_Length" BETWEEN 1 AND 18),
    CONSTRAINT "CK_Sys_Numbering_Rule_Current_Value" CHECK ("Current_Value" >= 0)
);

CREATE UNIQUE INDEX "UX_Sys_Numbering_Rule_TeId_RuCo" ON "Sys_Numbering_Rule" ("Tenant_Id", "Rule_Code", "Is_Deleted");
CREATE INDEX "IX_Sys_Numbering_Rule_TeId_CrTi" ON "Sys_Numbering_Rule" ("Tenant_Id", "Created_Time" DESC);
CREATE INDEX "IX_Sys_Numbering_Rule_CrId" ON "Sys_Numbering_Rule" ("Created_Id");
CREATE INDEX "IX_Sys_Numbering_Rule_TeId_IsDe" ON "Sys_Numbering_Rule" ("Tenant_Id", "Is_Deleted");
CREATE INDEX "IX_Sys_Numbering_Rule_TeId_St_So" ON "Sys_Numbering_Rule" ("Tenant_Id", "Status", "Sort");

CREATE TABLE "Sys_Numbering_Allocation" (
    "Basic_Id" BIGINT PRIMARY KEY,
    "Row_Version" BIGINT NOT NULL DEFAULT 0,
    "Tenant_Id" BIGINT NOT NULL DEFAULT 0,
    "Rule_Id" BIGINT NOT NULL,
    "Rule_Code" VARCHAR(100) NOT NULL,
    "Request_Tenant_Id" BIGINT NOT NULL DEFAULT 0,
    "Idempotency_Key" VARCHAR(100) NOT NULL,
    "Request_Fingerprint" VARCHAR(64) NOT NULL,
    "Allocation_Count" INTEGER NOT NULL,
    "Start_Value" BIGINT NOT NULL,
    "End_Value" BIGINT NOT NULL,
    "Period_Key" VARCHAR(32) NOT NULL,
    "Prefix_Snapshot" VARCHAR(50) NULL,
    "Separator_Snapshot" VARCHAR(10) NOT NULL,
    "Date_Text_Snapshot" VARCHAR(16) NULL,
    "Serial_Length_Snapshot" INTEGER NOT NULL,
    "Generated_At_Utc" TIMESTAMPTZ NOT NULL,
    "Business_Type" VARCHAR(100) NULL,
    "Business_Id" VARCHAR(100) NULL,
    "Created_Time" TIMESTAMPTZ NOT NULL,
    "Created_Id" BIGINT NULL,
    "Created_By" VARCHAR(100) NULL,
    CONSTRAINT "CK_Sys_Numbering_Allocation_Count" CHECK ("Allocation_Count" BETWEEN 1 AND 1000),
    CONSTRAINT "CK_Sys_Numbering_Allocation_Range" CHECK ("Start_Value" >= 1 AND "End_Value" >= "Start_Value")
);

CREATE UNIQUE INDEX "UX_Sys_Numbering_Allocation_Ru_ReTe_IdKe" ON "Sys_Numbering_Allocation" ("Tenant_Id", "Rule_Id", "Request_Tenant_Id", "Idempotency_Key");
CREATE INDEX "IX_Sys_Numbering_Allocation_TeId_CrTi" ON "Sys_Numbering_Allocation" ("Tenant_Id", "Created_Time" DESC);
CREATE INDEX "IX_Sys_Numbering_Allocation_CrId" ON "Sys_Numbering_Allocation" ("Created_Id");
CREATE INDEX "IX_Sys_Numbering_Allocation_Ru_GeTi" ON "Sys_Numbering_Allocation" ("Rule_Id", "Generated_At_Utc" DESC);
CREATE INDEX "IX_Sys_Numbering_Allocation_ReTe_GeTi" ON "Sys_Numbering_Allocation" ("Request_Tenant_Id", "Generated_At_Utc" DESC);

COMMIT;
