-- 4.0.3
-- sys_user_session 补模仿登录六列，并建按模仿者查询的索引。
--
-- DbInitializer 对已存在的表只 continue、不补列，故存量库须由本脚本补齐；
-- 全新库建表时已含这些列与索引，下面每条被 IF NOT EXISTS 跳过，空转安全。
--
-- impersonation_start_time 对应实体上的 DateTimeOffset?，其 PostgreSQL 类型由 SqlSugar 决定，
-- 这里从同表同类型的 expiration_time 反查真实类型再建列，不写死。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，
-- 库里实际是 sys_user_session / impersonator_user_id。

ALTER TABLE sys_user_session ADD COLUMN IF NOT EXISTS impersonator_user_id int8 NULL;
ALTER TABLE sys_user_session ADD COLUMN IF NOT EXISTS impersonator_user_name varchar(50) NULL;
ALTER TABLE sys_user_session ADD COLUMN IF NOT EXISTS impersonator_tenant_id int8 NULL;
ALTER TABLE sys_user_session ADD COLUMN IF NOT EXISTS impersonator_session_id varchar(100) NULL;
ALTER TABLE sys_user_session ADD COLUMN IF NOT EXISTS impersonation_reason varchar(200) NULL;

DO $$
DECLARE
    v_time_type text;
BEGIN
    SELECT format_type(attribute.atttypid, attribute.atttypmod)
      INTO v_time_type
      FROM pg_attribute AS attribute
      JOIN pg_class AS relation ON relation.oid = attribute.attrelid
      JOIN pg_namespace AS schema ON schema.oid = relation.relnamespace
     WHERE relation.relname = 'sys_user_session'
       AND attribute.attname = 'expiration_time'
       AND attribute.attnum > 0
       AND NOT attribute.attisdropped
       AND schema.nspname = current_schema();

    IF v_time_type IS NULL THEN
        RAISE EXCEPTION 'sys_user_session.expiration_time 不存在，无法推导 impersonation_start_time 的列类型';
    END IF;

    EXECUTE format(
        'ALTER TABLE sys_user_session ADD COLUMN IF NOT EXISTS impersonation_start_time %s NULL',
        v_time_type);
END $$;

CREATE INDEX IF NOT EXISTS ix_sys_user_session_imusid ON sys_user_session (impersonator_user_id ASC);
