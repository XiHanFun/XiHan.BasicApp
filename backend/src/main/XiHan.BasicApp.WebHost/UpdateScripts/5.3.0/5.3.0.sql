-- 5.3.0
-- 零、聚合根公共列统一命名（最先执行，后文一律按新列名写）。
-- 一、权限目录新增作用侧 side：平台 = 1、租户 = 2、两侧 = 3。
-- 二、会话标识改为全局唯一（见后文）。
-- 三、数据范围覆盖从账号挪到成员关系（见后文）。
-- 四、数据范围权限码收口为「查看 / 设置」（见后文）。
-- 五、导出任务记下发起会话与模仿者（见文末）。
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
