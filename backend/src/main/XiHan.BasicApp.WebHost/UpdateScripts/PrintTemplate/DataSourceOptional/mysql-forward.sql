-- 打印模板可选数据源 MySQL 8 正向迁移。
-- MySQL DDL 会隐式提交；本脚本只放宽列约束，不改写现有数据。
ALTER TABLE `Sys_Print_Template`
    MODIFY COLUMN `Data_Source_Code` varchar(100) NULL COMMENT '可选代码数据源；NULL 表示自由模板';
