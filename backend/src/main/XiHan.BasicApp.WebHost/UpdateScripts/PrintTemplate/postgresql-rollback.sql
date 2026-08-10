-- 打印模板模块 PostgreSQL 回滚脚本。
-- 警告：会删除全部打印模板数据；执行前必须备份并确认没有业务页面正在依赖模板编码。
BEGIN;
DROP TABLE IF EXISTS sys_print_template;
COMMIT;
