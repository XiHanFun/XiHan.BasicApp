-- 5.4.0 页面码统一
-- 前端页面 schema 的 pageCode 统一为该页面在后端页面登记表（各模块 PageRegistry）里的页面码，
-- 一页有多张表时用「页面码.子表」；用户导出的业务类型随之由 system.user 改为 identity.user。
-- 以旧码为键的存量数据一并改名，改过之后再跑不再命中：
-- 1. 导出任务的业务类型：导出中心按它显示名称与筛选，待执行的任务也要按新名找到 Provider。
-- 2. 导入历史的页面码：导入弹窗按页面码列出最近导入。
-- 3. 页面设置（列设置、搜索设置、个人视图；scene = 1 即 Page，设置键为 pageCode）。
--
-- 与 5.4.0.sql 相互独立，同版本目录内按文件名顺序执行。
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写。
--
-- sys_export_task、sys_import_history 是租户库实体，平台库与独立库都有；
-- sys_user_setting 只在平台库建表（[PlatformDataSource]），独立库上跳过。
-- sys_user_setting 的唯一索引为 (user_id, scene, setting_key, is_deleted)：同一用户已有新码记录时保留新码、不改旧码。

UPDATE sys_export_task SET business_type = 'identity.user' WHERE business_type = 'system.user';

DO $$
DECLARE
    has_user_setting boolean := EXISTS (
        SELECT 1
          FROM information_schema.tables
         WHERE table_schema = current_schema()
           AND table_name = 'sys_user_setting'
    );
    m record;
BEGIN
    FOR m IN
        SELECT *
          FROM (VALUES
                ('system.user', 'identity.user'),
                ('system.role', 'identity.role'),
                ('system.org', 'identity.org'),
                ('system.permission', 'identity.permission'),
                ('system.field-security', 'identity.field-security'),
                ('system.authorization.request', 'identity.authorization.request'),
                ('system.authorization.delegation', 'identity.authorization.delegation'),
                ('platform.tenant', 'tenant.list'),
                ('platform.approval', 'approval.review'),
                ('platform.file', 'file.library'),
                ('platform.app', 'openapi.app'),
                ('platform.config', 'setting.config'),
                ('platform.dict', 'setting.dict'),
                ('platform.dict.item', 'setting.dict.item'),
                ('platform.job', 'setting.job'),
                ('platform.menu', 'setting.menu'),
                ('message.email', 'message.record.email'),
                ('message.sms', 'message.record.sms'),
                ('develop.ai.assistant', 'ai_assistant'),
                ('develop.ai.prompt', 'ai_prompt'),
                ('develop.ai.provider', 'ai_provider'),
                ('develop.ai.knowledge', 'knowledge_base'),
                ('develop.codegen.datasource', 'code_gen.datasource'),
                ('develop.codegen.history', 'code_gen.history'),
                ('develop.codegen.table', 'code_gen.table'),
                ('develop.codegen.template', 'code_gen.template'),
                ('workflow.definition', 'workflow_definition'),
                ('workflow.instance', 'workflow_instance'),
                ('workflow.todo', 'workflow_todo')
               ) AS v (old_code, new_code)
    LOOP
        UPDATE sys_import_history SET page_code = m.new_code WHERE page_code = m.old_code;

        IF has_user_setting THEN
            UPDATE sys_user_setting s
               SET setting_key = m.new_code
             WHERE s.scene = 1
               AND s.setting_key = m.old_code
               AND NOT EXISTS (
                       SELECT 1
                         FROM sys_user_setting t
                        WHERE t.user_id = s.user_id
                          AND t.scene = s.scene
                          AND t.is_deleted = s.is_deleted
                          AND t.setting_key = m.new_code
                   );
        END IF;
    END LOOP;
END
$$;
