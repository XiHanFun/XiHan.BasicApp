-- 打印模板模块 PostgreSQL 正向脚本。
-- 事务边界：整张表和全部索引必须同时成功；失败时 PostgreSQL 自动回滚本事务。
-- 当前无历史数据迁移。Basic_Id 由应用的分布式 ID 生成器赋值，不使用数据库 identity。
BEGIN;

CREATE TABLE IF NOT EXISTS sys_print_template
(
    basic_id          bigint                   NOT NULL,
    template_code     varchar(100)             NOT NULL,
    data_source_code  varchar(100)             NULL,
    template_name     varchar(100)             NOT NULL,
    template_json     text                     NOT NULL,
    engine_version    varchar(32)              NOT NULL,
    allow_tenant_use  boolean                  NOT NULL,
    status            integer                  NOT NULL,
    sort              integer                  NOT NULL,
    remark            varchar(500)             NULL,
    tenant_id         bigint                   NOT NULL,
    row_version       bigint                   NOT NULL,
    created_time      timestamp with time zone NOT NULL,
    created_id        bigint                   NULL,
    created_by        varchar(255)             NULL,
    modified_time     timestamp with time zone NULL,
    modified_id       bigint                   NULL,
    modified_by       varchar(255)             NULL,
    is_deleted        boolean                  NOT NULL,
    deleted_time      timestamp with time zone NULL,
    deleted_id        bigint                   NULL,
    deleted_by        varchar(255)             NULL,
    CONSTRAINT sys_print_template_pkey PRIMARY KEY (basic_id)
);

CREATE INDEX IF NOT EXISTS ix_sys_print_template_teid_crti
    ON sys_print_template (tenant_id ASC, created_time DESC);
CREATE INDEX IF NOT EXISTS ix_sys_print_template_crid
    ON sys_print_template (created_id ASC);
CREATE INDEX IF NOT EXISTS ix_sys_print_template_teid_isde
    ON sys_print_template (tenant_id ASC, is_deleted ASC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_print_template_teid_teco
    ON sys_print_template (tenant_id ASC, template_code ASC, is_deleted ASC);
CREATE INDEX IF NOT EXISTS ix_sys_print_template_teid_st_so
    ON sys_print_template (tenant_id ASC, status ASC, sort ASC);

COMMENT ON TABLE sys_print_template IS 'hiprint 打印模板表；tenant_id=0 表示平台全局模板';
COMMENT ON COLUMN sys_print_template.data_source_code IS '可选代码数据源；NULL 表示自由模板';
COMMENT ON COLUMN sys_print_template.template_json IS 'hiprint 模板 JSON；不设置模块专属大小上限';

COMMIT;
