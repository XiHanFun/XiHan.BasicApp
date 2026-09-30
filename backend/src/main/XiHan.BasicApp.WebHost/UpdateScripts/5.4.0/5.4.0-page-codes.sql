-- 5.4.0 页面码统一
-- 导出业务类型统一取导出按钮所属的页面码（PageRegistry），用户导出由 system.user 改为 identity.user。
-- 存量任务一并改名：导出中心按业务类型显示名称与筛选，待执行的任务也要按新名找到 Provider；改过之后再跑不再命中。
--
-- 与 5.4.0.sql 相互独立，同版本目录内按文件名顺序执行。
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写。
--
-- sys_export_task 是租户库实体，平台库与独立库都有。

UPDATE sys_export_task SET business_type = 'identity.user' WHERE business_type = 'system.user';
