-- 5.3.0
-- 零、聚合根公共列统一命名（最先执行，后文一律按新列名写）。
-- 一、权限目录新增作用侧 side：平台 = 1、租户 = 2、两侧 = 3。
-- 二、会话标识改为全局唯一（见后文）。
-- 三、数据范围覆盖从账号挪到成员关系（见后文）。
-- 四、数据范围权限码收口为「查看 / 设置」（见后文）。
-- 五、导出任务记下发起会话与模仿者（见后文）。
-- 六、租户所有者角色改为系统角色（见后文）。
-- 七、账号新增「需要本人改密」标记（见后文）。
-- 八、参数配置按功能合并为一条 JSON（见文末）。
--
-- 只在 5.3.0 之前建的库上执行：新建的库（平台库与库隔离租户的独立库）按当前实体建表后直接登记为最新版本，不跑本脚本。
-- 本脚本在建表之后、播种之前执行；建表只建缺失的表，存量表的新列、改名由本脚本补齐。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，库里实际是 sys_permission / side。
--
-- 幂等：每一段都先判断再改，跑过一次后整段空转。

-- 零、聚合根公共列统一命名。
-- 聚合根基类的公共列此前没有声明列名，落库成 basicid / tenantid / isdeleted …，其余实体是 basic_id / tenant_id / is_deleted …；
-- 框架已给聚合根补齐列名，与其余实体同名。存量库按旧名改成新名，主键、索引与约束随列改名保留。
DO $$
DECLARE
    v_table text;
    v_old text;
    v_new text;
BEGIN
    FOREACH v_table IN ARRAY ARRAY[
        'sys_constraint_rule', 'sys_department', 'sys_oauth_app', 'sys_operation', 'sys_permission', 'sys_resource',
        'sys_review', 'sys_role', 'sys_task', 'sys_tenant', 'sys_user'] LOOP
        FOR v_old, v_new IN
            SELECT *
              FROM (VALUES
                    ('basicid', 'basic_id'), ('tenantid', 'tenant_id'),
                    ('createdtime', 'created_time'), ('createdid', 'created_id'), ('createdby', 'created_by'),
                    ('modifiedtime', 'modified_time'), ('modifiedid', 'modified_id'), ('modifiedby', 'modified_by'),
                    ('isdeleted', 'is_deleted'), ('deletedtime', 'deleted_time'), ('deletedid', 'deleted_id'), ('deletedby', 'deleted_by')
                   ) AS renames (old_column, new_column)
        LOOP
            IF EXISTS (
                SELECT 1
                  FROM information_schema.columns
                 WHERE table_schema = current_schema()
                   AND table_name = v_table
                   AND column_name = v_old
            ) THEN
                EXECUTE format('ALTER TABLE %I RENAME COLUMN %I TO %I', v_table, v_old, v_new);
            END IF;
        END LOOP;
    END LOOP;
END
$$;

-- 一、权限目录新增作用侧。
-- 存量行按改造前的语义（平台专属清单以外的权限两侧都生效）先补成两侧，
-- 随后权限种子把内置权限同步为各模块声明的作用侧；管理员自建的权限保持两侧，需要收窄的到权限管理页调整。
-- 列不留默认值：新行由应用必填写入，未声明的作用侧不会被静默补成某一侧。
ALTER TABLE sys_permission ADD COLUMN IF NOT EXISTS side int4 NULL;

UPDATE sys_permission
   SET side = 3
 WHERE side IS NULL OR side = 0;

ALTER TABLE sys_permission ALTER COLUMN side SET NOT NULL;

COMMENT ON COLUMN sys_permission.side IS '作用侧';

-- 会话标识改为全局唯一：令牌里的会话声明跨租户定位会话行，切换租户换新会话（不再复用标识），
-- 唯一索引去掉租户维度。存量标识均为随机 GUID，不会冲突。
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_user_session_usseid ON sys_user_session (user_session_id ASC, is_deleted ASC);

DROP INDEX IF EXISTS ux_sys_user_session_teid_usseid;

-- 数据范围覆盖从账号挪到成员关系：同一个人在不同租户各自设置，互不影响。
-- 改造前覆盖挂在账号上、在账号加入的每个租户都生效，这里原样落到它的每一条成员关系，已有覆盖的成员关系不动；
-- 随后删除账号上的列。
ALTER TABLE sys_tenant_user ADD COLUMN IF NOT EXISTS data_scope_override int4 NULL;

COMMENT ON COLUMN sys_tenant_user.data_scope_override IS '数据范围覆盖';

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
          FROM information_schema.columns
         WHERE table_schema = current_schema()
           AND table_name = 'sys_user'
           AND column_name = 'data_scope_override'
    ) THEN
        UPDATE sys_tenant_user tu
           SET data_scope_override = u.data_scope_override
          FROM sys_user u
         WHERE tu.user_id = u.basic_id
           AND u.data_scope_override IS NOT NULL
           AND tu.data_scope_override IS NULL;

        ALTER TABLE sys_user DROP COLUMN data_scope_override;
    END IF;
END
$$;

-- 数据范围改为「档位 + 部门一次设置」，权限码只剩查看（read）与设置（update）；
-- 授予、撤销、状态三组码没有接口了，存量库里连同所有引用一起删除（权限变更日志保留原样）。
-- 先删引用、最后删权限行。
DELETE FROM sys_role_permission WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code IN (
       'saas:role-data-scope:grant', 'saas:role-data-scope:revoke', 'saas:role-data-scope:status',
       'saas:user-data-scope:grant', 'saas:user-data-scope:revoke', 'saas:user-data-scope:status'));

DELETE FROM sys_user_permission WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code IN (
       'saas:role-data-scope:grant', 'saas:role-data-scope:revoke', 'saas:role-data-scope:status',
       'saas:user-data-scope:grant', 'saas:user-data-scope:revoke', 'saas:user-data-scope:status'));

DELETE FROM sys_tenant_edition_permission WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code IN (
       'saas:role-data-scope:grant', 'saas:role-data-scope:revoke', 'saas:role-data-scope:status',
       'saas:user-data-scope:grant', 'saas:user-data-scope:revoke', 'saas:user-data-scope:status'));

DELETE FROM sys_permission_delegation WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code IN (
       'saas:role-data-scope:grant', 'saas:role-data-scope:revoke', 'saas:role-data-scope:status',
       'saas:user-data-scope:grant', 'saas:user-data-scope:revoke', 'saas:user-data-scope:status'));

DELETE FROM sys_permission_request WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code IN (
       'saas:role-data-scope:grant', 'saas:role-data-scope:revoke', 'saas:role-data-scope:status',
       'saas:user-data-scope:grant', 'saas:user-data-scope:revoke', 'saas:user-data-scope:status'));

UPDATE sys_menu SET permission_id = NULL WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code IN (
       'saas:role-data-scope:grant', 'saas:role-data-scope:revoke', 'saas:role-data-scope:status',
       'saas:user-data-scope:grant', 'saas:user-data-scope:revoke', 'saas:user-data-scope:status'));

DELETE FROM sys_permission
 WHERE permission_code IN (
       'saas:role-data-scope:grant', 'saas:role-data-scope:revoke', 'saas:role-data-scope:status',
       'saas:user-data-scope:grant', 'saas:user-data-scope:revoke', 'saas:user-data-scope:status');

-- 导出任务记下发起会话与模仿者：后台执行按发起时的身份重建主体，会话失效即失败，模仿态禁用的权限照样禁用。
-- 存量任务没有这些信息，按非会话型发起处理。
ALTER TABLE sys_export_task ADD COLUMN IF NOT EXISTS requester_session_id varchar(100) NULL;
ALTER TABLE sys_export_task ADD COLUMN IF NOT EXISTS impersonator_user_id int8 NULL;
ALTER TABLE sys_export_task ADD COLUMN IF NOT EXISTS impersonator_tenant_id int8 NULL;

COMMENT ON COLUMN sys_export_task.requester_session_id IS '发起会话标识';
COMMENT ON COLUMN sys_export_task.impersonator_user_id IS '模仿者用户主键';
COMMENT ON COLUMN sys_export_task.impersonator_tenant_id IS '模仿者所在租户';

-- 租户所有者角色改为系统角色：持有者在所属租户拿到租户生效的全部权限，再经套餐白名单收窄，不再靠授权行，
-- 套餐升级、新增权限码都即时反映。开通时建的 tenant_owner 原是自定义角色、按当时的白名单写了授权行，升级后补不上；
-- 这里改成系统角色（租户里不能再编辑、授予或移出，随所有权转移移交），删掉那些授权行，
-- 并让它只留在所有者身上：不是所有者（成员类型 0）的人手里的这条绑定置为无效（0）。
UPDATE sys_role
   SET role_type = 0, max_members = 1
 WHERE role_code = 'tenant_owner' AND tenant_id > 0 AND role_type <> 0;

DELETE FROM sys_role_permission
 WHERE role_id IN (SELECT basic_id FROM sys_role WHERE role_code = 'tenant_owner' AND tenant_id > 0);

UPDATE sys_user_role AS ur
   SET status = 0
 WHERE ur.status <> 0
   AND ur.role_id IN (SELECT basic_id FROM sys_role WHERE role_code = 'tenant_owner' AND tenant_id > 0)
   AND NOT EXISTS (
       SELECT 1
         FROM sys_tenant_user AS tu
        WHERE tu.tenant_id = ur.tenant_id
          AND tu.user_id = ur.user_id
          AND tu.member_type = 0
          AND tu.is_deleted = false);

-- 七、账号新增「需要本人改密」标记。
-- 密码由他人设置（管理员创建或重置、平台开通、种子写入）时置位，本人改密或找回密码后清除；
-- 参数 saas.auth.password 的 forceChange 开启时，置位的账号登录后先锁定到改密为止（默认关闭）。
-- 取代原先「用内置默认密码登录即锁定」的判定；存量账号无从知道密码由谁设置，一律按未置位处理。
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'sys_user_security') THEN
        ALTER TABLE sys_user_security ADD COLUMN IF NOT EXISTS password_change_required bool NULL;

        UPDATE sys_user_security
           SET password_change_required = false
         WHERE password_change_required IS NULL;

        ALTER TABLE sys_user_security ALTER COLUMN password_change_required SET NOT NULL;

        COMMENT ON COLUMN sys_user_security.password_change_required IS '是否需要本人改密';
    END IF;
END
$$;

-- 八、参数配置按功能合并为一条 JSON。
--   saas.auth.login.methods + saas.auth.oauth.providers          → saas.auth.login（methods / oauthProviders）
--   saas.auth.impersonation.session-minutes + .notify-target     → saas.auth.impersonation（sessionMinutes / notifyTarget）
--   saas.bot.telegram.* 共 9 项                                  → saas.bot.telegram（Webhook 密钥令牌加密存储，仍单列不动）
--   chat:retention-days + chat:sensitive-words                   → chat.policy（retentionDays / sensitiveWords）
--   saas:log:retention-days                                      → saas.log.retention-days
-- 合并后读到的值与合并前一致：
--   - 登录、模仿、Telegram 原先经配置服务读取，租户的行优先、平台的行兜底，按上下文（平台与每个有旧行的租户）逐个合并，
--     每个字段取本上下文启用的行，没有再取平台启用的行；空白或解析不了的值原先按默认值处理，合并后不写这个字段，照样按默认值运行；
--   - 聊天与日志原先只读平台的行，只合并平台的；租户的旧行原本不生效，直接删除（合并后租户的同键参数会生效，不能把它们带过去）。
-- 合并结果写回本上下文的一条旧行（优先启用的行；改键、改值、改类型，启停不变），其余旧行删除；本上下文已有新键时只删旧行。
-- 元数据与参数种子一致，平台的行随后由种子再对齐一次。

-- 旧参数在某上下文的有效值：本上下文启用的行优先、平台启用的行兜底，去掉首尾空白，空白视为未配置。
CREATE OR REPLACE FUNCTION pg_temp.xihan_old_config(p_tenant_id int8, p_key text) RETURNS text
LANGUAGE plpgsql AS $f$
DECLARE
    v_value text;
BEGIN
    SELECT config_value INTO v_value
      FROM sys_config
     WHERE config_key = p_key AND is_deleted = false AND status = 1 AND tenant_id IN (p_tenant_id, 0)
     ORDER BY (tenant_id = p_tenant_id) DESC, basic_id
     LIMIT 1;
    RETURN NULLIF(btrim(v_value, E' \t\r\n'), '');
END
$f$;

-- 原布尔解析：1/true/yes/y/on 与 0/false/no/n/off，其余按默认值（返回 NULL，不写字段）。
CREATE OR REPLACE FUNCTION pg_temp.xihan_old_bool(p_value text) RETURNS jsonb
LANGUAGE sql IMMUTABLE AS $f$
    SELECT CASE
               WHEN lower(p_value) IN ('1', 'true', 'yes', 'y', 'on') THEN 'true'::jsonb
               WHEN lower(p_value) IN ('0', 'false', 'no', 'n', 'off') THEN 'false'::jsonb
           END
$f$;

-- 原整数解析：32 位整数，其余按默认值（返回 NULL，不写字段）。
CREATE OR REPLACE FUNCTION pg_temp.xihan_old_int(p_value text) RETURNS jsonb
LANGUAGE plpgsql AS $f$
BEGIN
    -- 先判格式再转换：同一个条件里的 AND 不保证求值顺序
    IF p_value !~ '^[+-]?[0-9]{1,10}$' THEN
        RETURN NULL;
    END IF;
    IF p_value::int8 NOT BETWEEN -2147483648 AND 2147483647 THEN
        RETURN NULL;
    END IF;
    RETURN to_jsonb(p_value::int4);
END
$f$;

-- 原分隔文本解析：按分隔符拆开、去空白、去空项，按首次出现的顺序大小写不敏感去重。
CREATE OR REPLACE FUNCTION pg_temp.xihan_old_list(p_value text, p_separators text) RETURNS jsonb
LANGUAGE sql IMMUTABLE AS $f$
    SELECT COALESCE(jsonb_agg(item ORDER BY position), '[]'::jsonb)
      FROM (SELECT DISTINCT ON (lower(item)) item, position
              FROM (SELECT btrim(part, E' \t') AS item, position
                      FROM regexp_split_to_table(p_value, p_separators) WITH ORDINALITY AS parts (part, position)) AS split
             WHERE item <> ''
             ORDER BY lower(item), position) AS items
$f$;

-- 把一个上下文的旧行合成新键：写回其中一条旧行，删除其余旧行。
CREATE OR REPLACE FUNCTION pg_temp.xihan_merge_config(
    p_tenant_id int8, p_old_keys text[], p_new_key text, p_value text,
    p_name text, p_group text, p_data_type int4, p_default text, p_description text, p_sort int4) RETURNS void
LANGUAGE plpgsql AS $f$
DECLARE
    v_keeper int8;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys_config WHERE tenant_id = p_tenant_id AND config_key = p_new_key AND is_deleted = false) THEN
        SELECT basic_id INTO v_keeper
          FROM sys_config
         WHERE tenant_id = p_tenant_id AND config_key = ANY (p_old_keys) AND is_deleted = false
         ORDER BY status DESC, basic_id
         LIMIT 1;

        IF v_keeper IS NOT NULL THEN
            UPDATE sys_config
               SET config_key = p_new_key,
                   config_value = COALESCE(p_value, p_default),
                   config_name = p_name,
                   config_group = p_group,
                   config_type = 5,
                   data_type = p_data_type,
                   default_value = p_default,
                   config_description = p_description,
                   is_encrypted = false,
                   sort = p_sort
             WHERE basic_id = v_keeper;
        END IF;
    END IF;

    DELETE FROM sys_config WHERE tenant_id = p_tenant_id AND config_key = ANY (p_old_keys);
END
$f$;

DO $$
DECLARE
    v_login_keys text[] := ARRAY['saas.auth.login.methods', 'saas.auth.oauth.providers'];
    v_impersonation_keys text[] := ARRAY['saas.auth.impersonation.session-minutes', 'saas.auth.impersonation.notify-target'];
    v_telegram_keys text[] := ARRAY[
        'saas.bot.telegram.enabled', 'saas.bot.telegram.webhook-base-url', 'saas.bot.telegram.webhook-route-prefix',
        'saas.bot.telegram.manager-refresh-seconds', 'saas.bot.telegram.config-cache-seconds', 'saas.bot.telegram.enable-fallback-reply',
        'saas.bot.telegram.proxy-url', 'saas.bot.telegram.base-url', 'saas.bot.telegram.timeout-seconds'];
    v_chat_keys text[] := ARRAY['chat:retention-days', 'chat:sensitive-words'];
    v_log_keys text[] := ARRAY['saas:log:retention-days'];
    v_tenant_id int8;
    v_methods text;
    v_value jsonb;
    v_network jsonb;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = 'sys_config') THEN
        RETURN;
    END IF;

    -- 登录设置：methods 原为 JSON 数组或逗号 / 分号 / 竖线分隔的文本；oauthProviders 原为 JSON 数组（写错原本就报错，这里同样报错）
    FOR v_tenant_id IN SELECT DISTINCT tenant_id FROM sys_config WHERE config_key = ANY (v_login_keys) ORDER BY tenant_id LOOP
        v_methods := pg_temp.xihan_old_config(v_tenant_id, 'saas.auth.login.methods');
        v_value := jsonb_strip_nulls(jsonb_build_object(
            'methods', CASE WHEN v_methods LIKE '[%' THEN v_methods::jsonb ELSE pg_temp.xihan_old_list(v_methods, '[,;|]+') END,
            'oauthProviders', pg_temp.xihan_old_config(v_tenant_id, 'saas.auth.oauth.providers')::jsonb));
        IF v_value -> 'methods' = '[]'::jsonb THEN
            v_value := v_value - 'methods';
        END IF;

        PERFORM pg_temp.xihan_merge_config(
            v_tenant_id, v_login_keys, 'saas.auth.login', NULLIF(v_value, '{}'::jsonb)::text,
            '登录设置', 'auth', 3, '{"methods":["password"],"oauthProviders":[]}',
            '登录页开放的登录方式（methods）与展示的第三方登录（oauthProviders，name 须与已注册的认证方案一致）', 10);
    END LOOP;

    -- 模仿登录设置
    FOR v_tenant_id IN SELECT DISTINCT tenant_id FROM sys_config WHERE config_key = ANY (v_impersonation_keys) ORDER BY tenant_id LOOP
        v_value := jsonb_strip_nulls(jsonb_build_object(
            'sessionMinutes', pg_temp.xihan_old_int(pg_temp.xihan_old_config(v_tenant_id, 'saas.auth.impersonation.session-minutes')),
            'notifyTarget', pg_temp.xihan_old_bool(pg_temp.xihan_old_config(v_tenant_id, 'saas.auth.impersonation.notify-target'))));

        PERFORM pg_temp.xihan_merge_config(
            v_tenant_id, v_impersonation_keys, 'saas.auth.impersonation', NULLIF(v_value, '{}'::jsonb)::text,
            '模仿登录设置', 'auth', 3, '{"sessionMinutes":30,"notifyTarget":true}',
            'sessionMinutes：模仿会话存活分钟数（按 1~480 归一）；notifyTarget：是否向被模仿者投递安全通知', 30);
    END LOOP;

    -- Telegram 机器人平台设置
    FOR v_tenant_id IN SELECT DISTINCT tenant_id FROM sys_config WHERE config_key = ANY (v_telegram_keys) ORDER BY tenant_id LOOP
        v_network := jsonb_strip_nulls(jsonb_build_object(
            'proxyUrl', to_jsonb(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.proxy-url')),
            'baseUrl', to_jsonb(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.base-url')),
            'timeoutSeconds', pg_temp.xihan_old_int(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.timeout-seconds'))));
        v_value := jsonb_strip_nulls(jsonb_build_object(
            'enabled', pg_temp.xihan_old_bool(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.enabled')),
            'webhookBaseUrl', to_jsonb(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.webhook-base-url')),
            'webhookRoutePrefix', to_jsonb(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.webhook-route-prefix')),
            'managerRefreshSeconds', pg_temp.xihan_old_int(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.manager-refresh-seconds')),
            'configCacheSeconds', pg_temp.xihan_old_int(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.config-cache-seconds')),
            'enableFallbackReply', pg_temp.xihan_old_bool(pg_temp.xihan_old_config(v_tenant_id, 'saas.bot.telegram.enable-fallback-reply')),
            'network', NULLIF(v_network, '{}'::jsonb)));

        PERFORM pg_temp.xihan_merge_config(
            v_tenant_id, v_telegram_keys, 'saas.bot.telegram', NULLIF(v_value, '{}'::jsonb)::text,
            'Telegram 机器人平台设置', 'bot', 3,
            '{"enabled":false,"webhookBaseUrl":"","webhookRoutePrefix":"/api/telegram-bot/webhook","managerRefreshSeconds":5,"configCacheSeconds":5,"enableFallbackReply":false,"network":{"proxyUrl":"","baseUrl":"","timeoutSeconds":100}}',
            'enabled 总开关；webhookBaseUrl 留空走长轮询；webhookRoutePrefix 接收路由前缀；managerRefreshSeconds / configCacheSeconds 刷新与缓存周期；enableFallbackReply 兜底回复；network 代理、自建 API 地址与超时',
            100);
    END LOOP;

    -- 聊天策略：只合并平台的；保留天数原为整数（写错原本就让清理任务失败，这里同样报错），敏感词原为分隔文本
    DELETE FROM sys_config WHERE tenant_id <> 0 AND config_key = ANY (v_chat_keys);
    IF EXISTS (SELECT 1 FROM sys_config WHERE tenant_id = 0 AND config_key = ANY (v_chat_keys)) THEN
        v_value := jsonb_strip_nulls(jsonb_build_object(
            'retentionDays', to_jsonb(pg_temp.xihan_old_config(0, 'chat:retention-days')::int4),
            'sensitiveWords', pg_temp.xihan_old_list(pg_temp.xihan_old_config(0, 'chat:sensitive-words'), '[\n\r,，;；、]+')));
        IF v_value -> 'sensitiveWords' = '[]'::jsonb THEN
            v_value := v_value - 'sensitiveWords';
        END IF;

        PERFORM pg_temp.xihan_merge_config(
            0, v_chat_keys, 'chat.policy', NULLIF(v_value, '{}'::jsonb)::text,
            '聊天策略', 'chat', 3, '{"retentionDays":365,"sensitiveWords":[]}',
            'retentionDays：消息保留天数（清理任务物理删除更早的消息）；sensitiveWords：敏感词数组，空表示不拦截', 10);
    END IF;

    -- 日志保留天数：只改键（值原本就是整数文本）
    DELETE FROM sys_config WHERE tenant_id <> 0 AND config_key = ANY (v_log_keys);
    IF EXISTS (SELECT 1 FROM sys_config WHERE tenant_id = 0 AND config_key = ANY (v_log_keys)) THEN
        PERFORM pg_temp.xihan_merge_config(
            0, v_log_keys, 'saas.log.retention-days', pg_temp.xihan_old_config(0, 'saas:log:retention-days'),
            '日志保留天数', 'log', 1, '180',
            '访问、接口、异常、操作、差异、登录日志的保留天数，清理任务删除更早的记录', 200);
    END IF;
END
$$;

DROP FUNCTION pg_temp.xihan_merge_config(int8, text[], text, text, text, text, int4, text, text, int4);
DROP FUNCTION pg_temp.xihan_old_list(text, text);
DROP FUNCTION pg_temp.xihan_old_int(text);
DROP FUNCTION pg_temp.xihan_old_bool(text);
DROP FUNCTION pg_temp.xihan_old_config(int8, text);
