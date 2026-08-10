-- 打印模板可选数据源 PostgreSQL 回滚迁移。
-- 回滚前置条件：必须先为所有自由模板补齐 data_source_code；存在 NULL 时 SET NOT NULL 会失败并整段回滚。
BEGIN;

ALTER TABLE IF EXISTS sys_print_template
    ALTER COLUMN data_source_code SET NOT NULL;

COMMENT ON COLUMN sys_print_template.data_source_code IS '代码数据源编码';

COMMIT;
