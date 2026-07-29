-- 业务编号模块 SQLite 回滚脚本。
-- 警告：分配记录为永久审计数据，执行前必须备份并确认允许不可恢复删除。
BEGIN IMMEDIATE;
DROP TABLE IF EXISTS "Sys_Numbering_Allocation";
DROP TABLE IF EXISTS "Sys_Numbering_Rule";
COMMIT;
