-- 5.4.0
-- 一、字段级安全改为「实体 + 字段」：规则不再挂权限资源，读取方式的参数改为明确的列，删去从未生效的写法（见后文）。
-- 二、通知公告改用自己的权限码 saas:notification:*，删除只为通知存在过的 saas:message:publish（见后文）。
-- 三、角色继承只存直接继承边，间接继承按直接边即时推出；系统角色不参与继承（见后文）。
-- 四、代码生成列配置新增「关联表 / 关联树」选项来源所需的两列（见后文）。
-- 五、代码生成列配置新增「唯一」开关（见后文）。
-- 六、代码生成表配置删去「生成路径」：生成到项目改按命名空间与前端工程目录推导位置（见后文）。
--
-- 只在 5.4.0 之前建的库上执行：新建的库按当前实体建表后直接登记为最新版本，不跑本脚本。
-- 本脚本在建表之后、播种之前执行；建表只建缺失的表，存量表的列与索引由本脚本调整。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写。
--
-- 幂等：每一段都先判断再改，跑过一次后整段空转。

-- 一、字段级安全改为「实体 + 字段」。
-- sys_field_level_security 只在平台库建表（[PlatformDataSource]），独立库上整段跳过。
-- 旧表以 resource_id 为判断依据：列还在说明本段没跑过。
--
-- 迁移规则：
-- 1. 权限资源 → 实体：一个资源只对应一个实体的直接换成实体名。
--    多个实体共用的资源（字典、版本、文件、消息、编号、代码生成、工作流）与其他资源上的规则、
--    以及目标为「权限」的规则无法确定落到哪个实体，删除。字段安全在 5.4.0 之前只对用户列表与详情生效，
--    这些规则从未起过作用；需要的请在字段安全页按实体重新配置。
-- 2. 读取方式：is_readable 与「隐藏」重复，并入读取方式；「自定义」与「固定文本」重复，改为固定文本（没写文字的改为全部星号）。
--    部分脱敏的 mask_pattern（keep:前,后）拆成两列，没写的按旧行为保留后 4 位；前后都不保留的改为全部星号。
-- 3. 明文且可编辑的规则什么都没限制，删除；脱敏且可编辑的保留，新语义为「只写」（看不到原值、可填新值，交回脱敏值视为没改）。
-- 4. 优先级从未参与判定，删除；说明并入备注（备注为空时）。
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
          FROM information_schema.columns
         WHERE table_schema = current_schema()
           AND table_name = 'sys_field_level_security'
           AND column_name = 'resource_id'
    ) THEN
        ALTER TABLE sys_field_level_security ADD COLUMN IF NOT EXISTS entity_name varchar(100) NULL;
        ALTER TABLE sys_field_level_security ADD COLUMN IF NOT EXISTS mask_keep_head int4 NULL;
        ALTER TABLE sys_field_level_security ADD COLUMN IF NOT EXISTS mask_keep_tail int4 NULL;
        ALTER TABLE sys_field_level_security ADD COLUMN IF NOT EXISTS mask_replacement varchar(100) NULL;

        -- 目标为「权限」的规则从未生效，删除
        DELETE FROM sys_field_level_security WHERE target_type = 2;

        -- 资源 → 实体（仅一个资源对应一个实体的）
        UPDATE sys_field_level_security f
           SET entity_name = m.entity_name
          FROM sys_resource r
          JOIN (VALUES
                ('user', 'SysUser'), ('user-session', 'SysUserSession'), ('role', 'SysRole'),
                ('permission', 'SysPermission'), ('constraint-rule', 'SysConstraintRule'), ('position', 'SysPosition'),
                ('tenant', 'SysTenant'), ('tenant-edition', 'SysTenantEdition'), ('oauth-app', 'SysOAuthApp'),
                ('config', 'SysConfig'), ('storage-config', 'SysStorageConfig'), ('review', 'SysReview'),
                ('notification', 'SysNotification'), ('message-template', 'SysMessageTemplate'),
                ('email-config', 'SysEmailConfig'), ('sms-config', 'SysSmsConfig'), ('bot-config', 'SysBotConfig'),
                ('telegram-bot', 'SysTelegramBot'), ('access-log', 'SysAccessLog'), ('api-log', 'SysOpenApiLog'),
                ('operation-log', 'SysOperationLog'), ('login-log', 'SysLoginLog'), ('exception-log', 'SysExceptionLog'),
                ('diff-log', 'SysDiffLog'), ('ai', 'SysAiProvider'), ('ai_assistant', 'SysAiAssistant'),
                ('ai_prompt', 'SysAiPrompt'), ('knowledge_base', 'SysKnowledgeDocument'), ('print-template', 'SysPrintTemplate')
               ) AS m (resource_code, entity_name)
            ON m.resource_code = r.resource_code
         WHERE f.resource_id = r.basic_id
           AND f.entity_name IS NULL;

        -- 落不到唯一实体的规则删除（见上文说明）
        DELETE FROM sys_field_level_security WHERE entity_name IS NULL;

        -- 不可读并入「隐藏」
        UPDATE sys_field_level_security
           SET mask_strategy = 1
         WHERE is_readable = false
           AND mask_strategy = 0;

        -- 「自定义」改为固定文本；没写文字的改为全部星号
        UPDATE sys_field_level_security
           SET mask_strategy = CASE WHEN NULLIF(btrim(mask_pattern), '') IS NULL THEN 2 ELSE 5 END
         WHERE mask_strategy = 99;

        -- 部分脱敏：keep:前,后 拆列；没写的按旧行为保留后 4 位
        UPDATE sys_field_level_security
           SET mask_keep_head = LEAST(COALESCE(substring(mask_pattern FROM '^\s*[kK][eE][eE][pP]:\s*(\d{1,4})')::int4, 0), 32),
               mask_keep_tail = LEAST(COALESCE(substring(mask_pattern FROM '^\s*[kK][eE][eE][pP]:\s*\d*\s*,\s*(\d{1,4})')::int4, 0), 32)
         WHERE mask_strategy = 3
           AND mask_pattern ~* '^\s*keep:';

        UPDATE sys_field_level_security
           SET mask_keep_head = 0,
               mask_keep_tail = 4
         WHERE mask_strategy = 3
           AND mask_keep_head IS NULL;

        -- 前后都不保留等同于全部星号
        UPDATE sys_field_level_security
           SET mask_strategy = 2,
               mask_keep_head = NULL,
               mask_keep_tail = NULL
         WHERE mask_strategy = 3
           AND mask_keep_head = 0
           AND mask_keep_tail = 0;

        -- 固定文本：沿用原文字，没写的用旧默认值
        UPDATE sys_field_level_security
           SET mask_replacement = left(COALESCE(NULLIF(btrim(mask_pattern), ''), '[已脱敏]'), 100)
         WHERE mask_strategy = 5
           AND mask_replacement IS NULL;

        -- 明文且可编辑：什么都没限制
        DELETE FROM sys_field_level_security
         WHERE mask_strategy = 0
           AND is_editable = true;

        -- 说明并入备注
        UPDATE sys_field_level_security
           SET remark = left(description, 500)
         WHERE remark IS NULL
           AND NULLIF(btrim(description), '') IS NOT NULL;

        -- 删列：含 resource_id 的旧索引随列一并删除
        ALTER TABLE sys_field_level_security DROP COLUMN resource_id;
        ALTER TABLE sys_field_level_security DROP COLUMN IF EXISTS is_readable;
        ALTER TABLE sys_field_level_security DROP COLUMN IF EXISTS mask_pattern;
        ALTER TABLE sys_field_level_security DROP COLUMN IF EXISTS priority;
        ALTER TABLE sys_field_level_security DROP COLUMN IF EXISTS description;

        ALTER TABLE sys_field_level_security ALTER COLUMN entity_name SET NOT NULL;
    END IF;

    -- 新索引（与实体上的 SugarIndex 同名）
    IF EXISTS (
        SELECT 1
          FROM information_schema.tables
         WHERE table_schema = current_schema()
           AND table_name = 'sys_field_level_security'
    ) THEN
        CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_field_level_security_teid_taty_taid_enna_fina
            ON sys_field_level_security (tenant_id, target_type, target_id, entity_name, field_name, is_deleted);
        CREATE INDEX IF NOT EXISTS ix_sys_field_level_security_enna_st
            ON sys_field_level_security (entity_name, status);
    END IF;
END
$$;

-- 二、通知公告改用 saas:notification:*。
-- 通知的接口与菜单此前要求的是 saas:message:*，saas:message:publish 只为通知发布而设；
-- 现在通知只认 saas:notification:*，这个码没有接口了，存量库里连同所有引用一起删除（权限变更日志保留原样）。
-- 先删引用、最后删权限行。版本与演示角色已授予 saas:notification:*；自建角色若靠 saas:message:* 维护通知，需补授通知权限。
DELETE FROM sys_role_permission WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = 'saas:message:publish');

DELETE FROM sys_user_permission WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = 'saas:message:publish');

DELETE FROM sys_tenant_edition_permission WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = 'saas:message:publish');

DELETE FROM sys_permission_delegation WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = 'saas:message:publish');

DELETE FROM sys_permission_request WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = 'saas:message:publish');

UPDATE sys_menu SET permission_id = NULL WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = 'saas:message:publish');

DELETE FROM sys_permission WHERE permission_code = 'saas:message:publish';

-- 三、角色继承只存直接继承边。
-- sys_role_hierarchy 此前是闭包表：自身行（depth = 0）、直接继承（depth = 1）与间接继承（depth > 1）都存。
-- 旧的维护方式下 depth = 1 的行恰好就是全部直接继承边，只留这些行即可，间接继承与深度、路径改为读取时从边推出；
-- 删去 depth、path 两列，带 depth 的索引随列一并删除。
-- 系统角色（super_admin / tenant_owner，role_type = 0）的权限由系统按上下文整体给出，不再参与继承，涉及它们的边一并删除。
-- sys_role_hierarchy 与 sys_role 只在平台库建表（[PlatformDataSource]），独立库上整段跳过；depth 列还在说明本段没跑过。
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
          FROM information_schema.columns
         WHERE table_schema = current_schema()
           AND table_name = 'sys_role_hierarchy'
           AND column_name = 'depth'
    ) THEN
        DELETE FROM sys_role_hierarchy WHERE depth <> 1;

        ALTER TABLE sys_role_hierarchy DROP COLUMN depth;
        ALTER TABLE sys_role_hierarchy DROP COLUMN IF EXISTS path;
    END IF;

    IF EXISTS (
        SELECT 1
          FROM information_schema.tables
         WHERE table_schema = current_schema()
           AND table_name = 'sys_role_hierarchy'
    ) THEN
        DELETE FROM sys_role_hierarchy h
         USING sys_role r
         WHERE r.role_type = 0
           AND (h.ancestor_id = r.basic_id OR h.descendant_id = r.basic_id);
    END IF;
END
$$;

-- 四、代码生成列配置的关联选项来源。
-- 选项来源新增关联表（3）与关联树（4），指向另一张表配置并记显示列；存量列不涉及这两种来源，只补列。
-- sys_codegen_tablecolumn 只在平台库建表（[PlatformDataSource]），独立库上整段跳过。
DO $$
BEGIN
    IF to_regclass('sys_codegen_tablecolumn') IS NOT NULL THEN
        ALTER TABLE sys_codegen_tablecolumn ADD COLUMN IF NOT EXISTS relation_table_id int8 NULL;
        ALTER TABLE sys_codegen_tablecolumn ADD COLUMN IF NOT EXISTS relation_label_column varchar(100) NULL;
    END IF;
END
$$;

-- 五、代码生成列配置的「唯一」开关。
-- 存量列一律不唯一；sys_codegen_tablecolumn 只在平台库建表，独立库上整段跳过。
DO $$
BEGIN
    IF to_regclass('sys_codegen_tablecolumn') IS NOT NULL THEN
        ALTER TABLE sys_codegen_tablecolumn ADD COLUMN IF NOT EXISTS is_unique bool NOT NULL DEFAULT false;
    END IF;
END
$$;

-- 六、代码生成表配置的「生成路径」。
-- 生成到项目不再按表配置的任意路径落盘，改由开发环境配置推导后端项目与前端工程的位置，这一列不再使用。
-- sys_codegen_table 只在平台库建表，独立库上整段跳过。
DO $$
BEGIN
    IF to_regclass('sys_codegen_table') IS NOT NULL THEN
        ALTER TABLE sys_codegen_table DROP COLUMN IF EXISTS gen_path;
    END IF;
END
$$;
