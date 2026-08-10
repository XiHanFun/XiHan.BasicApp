-- 打印模板可选数据源 MariaDB 回滚迁移。
-- 回滚前置条件：先为所有自由模板补齐 Data_Source_Code；存在 NULL 时 ALTER 会失败并保留当前可空结构。
ALTER TABLE `Sys_Print_Template`
    MODIFY COLUMN `Data_Source_Code` varchar(100) NOT NULL COMMENT '代码数据源编码';
