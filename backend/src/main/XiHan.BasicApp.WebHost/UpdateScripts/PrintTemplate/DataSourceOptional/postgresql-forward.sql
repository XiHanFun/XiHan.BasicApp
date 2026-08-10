-- 打印模板可选数据源 PostgreSQL 正向迁移。
-- 事务边界：只放宽 Data_Source_Code 的非空约束，不改写现有模板或模板 JSON。
BEGIN;

ALTER TABLE IF EXISTS sys_print_template
    ALTER COLUMN data_source_code DROP NOT NULL;

COMMENT ON COLUMN sys_print_template.data_source_code IS '可选代码数据源；NULL 表示自由模板';

COMMIT;
