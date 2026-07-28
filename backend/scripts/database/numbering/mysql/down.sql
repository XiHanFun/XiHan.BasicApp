-- 业务编号模块 MySQL 8 回滚脚本。
-- 警告：分配记录为永久审计数据，执行前必须备份并确认允许不可恢复删除。
-- MySQL DDL 会隐式提交；该回滚不可依赖事务撤销，执行前必须完成数据库备份。
DROP TABLE IF EXISTS `Sys_Numbering_Allocation`;
DROP TABLE IF EXISTS `Sys_Numbering_Rule`;
