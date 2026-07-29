-- 业务编号模块 SQL Server 回滚脚本。
-- 警告：分配记录为永久审计数据，执行前必须备份并确认允许不可恢复删除。
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'[Sys_Numbering_Allocation]', N'U') IS NOT NULL DROP TABLE [Sys_Numbering_Allocation];
IF OBJECT_ID(N'[Sys_Numbering_Rule]', N'U') IS NOT NULL DROP TABLE [Sys_Numbering_Rule];
COMMIT TRANSACTION;
