-- 业务编号模块 SQLite 正向脚本。
-- 运行时仍以 SqlSugar CodeFirst 为主；SQLite 以 INTEGER 0/1 表示布尔值，以 ISO-8601 TEXT 保存时间。
BEGIN IMMEDIATE;

CREATE TABLE "Sys_Numbering_Rule" (
    "Basic_Id" INTEGER NOT NULL PRIMARY KEY,
    "Row_Version" INTEGER NOT NULL DEFAULT 0,
    "Tenant_Id" INTEGER NOT NULL DEFAULT 0,
    "Rule_Code" TEXT NOT NULL,
    "Rule_Name" TEXT NOT NULL,
    "Prefix" TEXT NULL,
    "Separator" TEXT NOT NULL DEFAULT '-',
    "Date_Format" INTEGER NOT NULL DEFAULT 3,
    "Serial_Length" INTEGER NOT NULL DEFAULT 4 CHECK ("Serial_Length" BETWEEN 1 AND 18),
    "Reset_Cycle" INTEGER NOT NULL DEFAULT 1,
    "Time_Zone_Id" TEXT NOT NULL DEFAULT 'UTC',
    "Current_Value" INTEGER NOT NULL DEFAULT 0 CHECK ("Current_Value" >= 0),
    "Current_Period" TEXT NULL,
    "Has_Allocated" INTEGER NOT NULL DEFAULT 0 CHECK ("Has_Allocated" IN (0, 1)),
    "Allow_Tenant_Use" INTEGER NOT NULL DEFAULT 0 CHECK ("Allow_Tenant_Use" IN (0, 1)),
    "Status" INTEGER NOT NULL DEFAULT 1,
    "Sort" INTEGER NOT NULL DEFAULT 0,
    "Remark" TEXT NULL,
    "Created_Time" TEXT NOT NULL,
    "Created_Id" INTEGER NULL,
    "Created_By" TEXT NULL,
    "Modified_Time" TEXT NULL,
    "Modified_Id" INTEGER NULL,
    "Modified_By" TEXT NULL,
    "Is_Deleted" INTEGER NOT NULL DEFAULT 0 CHECK ("Is_Deleted" IN (0, 1)),
    "Deleted_Time" TEXT NULL,
    "Deleted_Id" INTEGER NULL,
    "Deleted_By" TEXT NULL
);

CREATE UNIQUE INDEX "UX_Sys_Numbering_Rule_TeId_RuCo" ON "Sys_Numbering_Rule" ("Tenant_Id", "Rule_Code", "Is_Deleted");
CREATE INDEX "IX_Sys_Numbering_Rule_TeId_CrTi" ON "Sys_Numbering_Rule" ("Tenant_Id", "Created_Time" DESC);
CREATE INDEX "IX_Sys_Numbering_Rule_CrId" ON "Sys_Numbering_Rule" ("Created_Id");
CREATE INDEX "IX_Sys_Numbering_Rule_TeId_IsDe" ON "Sys_Numbering_Rule" ("Tenant_Id", "Is_Deleted");
CREATE INDEX "IX_Sys_Numbering_Rule_TeId_St_So" ON "Sys_Numbering_Rule" ("Tenant_Id", "Status", "Sort");

CREATE TABLE "Sys_Numbering_Allocation" (
    "Basic_Id" INTEGER NOT NULL PRIMARY KEY,
    "Row_Version" INTEGER NOT NULL DEFAULT 0,
    "Tenant_Id" INTEGER NOT NULL DEFAULT 0,
    "Rule_Id" INTEGER NOT NULL,
    "Rule_Code" TEXT NOT NULL,
    "Request_Tenant_Id" INTEGER NOT NULL DEFAULT 0,
    "Idempotency_Key" TEXT NOT NULL,
    "Request_Fingerprint" TEXT NOT NULL,
    "Allocation_Count" INTEGER NOT NULL CHECK ("Allocation_Count" BETWEEN 1 AND 1000),
    "Start_Value" INTEGER NOT NULL CHECK ("Start_Value" >= 1),
    "End_Value" INTEGER NOT NULL CHECK ("End_Value" >= "Start_Value"),
    "Period_Key" TEXT NOT NULL,
    "Prefix_Snapshot" TEXT NULL,
    "Separator_Snapshot" TEXT NOT NULL,
    "Date_Text_Snapshot" TEXT NULL,
    "Serial_Length_Snapshot" INTEGER NOT NULL,
    "Generated_At_Utc" TEXT NOT NULL,
    "Business_Type" TEXT NULL,
    "Business_Id" TEXT NULL,
    "Created_Time" TEXT NOT NULL,
    "Created_Id" INTEGER NULL,
    "Created_By" TEXT NULL
);

CREATE UNIQUE INDEX "UX_Sys_Numbering_Allocation_Ru_ReTe_IdKe" ON "Sys_Numbering_Allocation" ("Tenant_Id", "Rule_Id", "Request_Tenant_Id", "Idempotency_Key");
CREATE INDEX "IX_Sys_Numbering_Allocation_TeId_CrTi" ON "Sys_Numbering_Allocation" ("Tenant_Id", "Created_Time" DESC);
CREATE INDEX "IX_Sys_Numbering_Allocation_CrId" ON "Sys_Numbering_Allocation" ("Created_Id");
CREATE INDEX "IX_Sys_Numbering_Allocation_Ru_GeTi" ON "Sys_Numbering_Allocation" ("Rule_Id", "Generated_At_Utc" DESC);
CREATE INDEX "IX_Sys_Numbering_Allocation_ReTe_GeTi" ON "Sys_Numbering_Allocation" ("Request_Tenant_Id", "Generated_At_Utc" DESC);

COMMIT;
