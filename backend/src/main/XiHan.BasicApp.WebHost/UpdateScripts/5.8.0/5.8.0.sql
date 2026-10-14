-- 5.8.0
-- 一、审批统一走工作流：撤掉 Saas 自带的「系统审查」（sys_review / sys_review_log），权限申请改由工作流审批。
-- 二、工作流实例记录流程结果、按发起人查询。
-- 三、「我的待办」并入审批中心。
--
-- 只在 5.8.0 之前建的库上执行：新建的库按当前实体建表后直接登记为最新版本，不跑本脚本。
-- 本脚本在建表之后、播种之前执行；新表（审批记录 sys_workflow_task_record）由建表阶段创建，这里不涉及。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写。
--
-- 幂等：表与列先判存在，删除按条件，跑过一次后整段空转。

-- 一、权限申请改由工作流审批。
-- 申请原先关联一张系统审查单（review_id），改为关联工作流实例（workflow_instance_id，提交时发起「权限申请」流程后回填）。
-- 存量的待审批申请没有对应的流程实例，审批入口随系统审查一起撤掉，无法再被审批：
-- 统一置为已撤回（status 3）并注明原因，申请人在权限申请里重新提交即走新流程；已完结的申请保持原样。
-- sys_permission_request 是严格隔离的租户数据，平台库与独立库都有，逐库执行。
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'sys_permission_request') THEN
        ALTER TABLE sys_permission_request ADD COLUMN IF NOT EXISTS workflow_instance_id int8 NULL;
        COMMENT ON COLUMN sys_permission_request.workflow_instance_id IS '审批流程实例ID';
        CREATE INDEX IF NOT EXISTS ix_sys_permission_request_woinid ON sys_permission_request (workflow_instance_id);

        IF EXISTS (SELECT 1 FROM information_schema.columns
                    WHERE table_schema = current_schema() AND table_name = 'sys_permission_request' AND column_name = 'review_id') THEN
            UPDATE sys_permission_request
               SET request_status = 3,
                   remark = '审批改由工作流处理，原审批单已撤销，请重新提交申请'
             WHERE request_status = 0
               AND workflow_instance_id IS NULL;

            DROP INDEX IF EXISTS ix_sys_permission_request_reid;
            ALTER TABLE sys_permission_request DROP COLUMN review_id;
        END IF;
    END IF;
END
$$;

-- 二、撤掉系统审查的两张表。审批过程改由工作流实例与审批记录承载，旧审查单不迁移。
DROP TABLE IF EXISTS sys_review_log;
DROP TABLE IF EXISTS sys_review;

-- 三、系统审查的权限码（saas:review:*、saas:review-log:read）没有接口了，连同所有引用一起删除（权限变更日志保留原样）。
-- 先删引用、再删权限行，最后删只被它们引用的资源行。授权、套餐、委托与菜单表只在平台库建表，独立库上整段跳过。
DO $$
DECLARE
    review_codes text[] := ARRAY[
        'saas:review:read', 'saas:review:create', 'saas:review:update', 'saas:review:status',
        'saas:review:audit', 'saas:review:withdraw', 'saas:review:delete', 'saas:review:export',
        'saas:review-log:read'];
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'sys_permission') THEN
        IF EXISTS (SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = 'sys_role_permission') THEN
            DELETE FROM sys_role_permission
             WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = ANY (review_codes));
        END IF;

        IF EXISTS (SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = 'sys_user_permission') THEN
            DELETE FROM sys_user_permission
             WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = ANY (review_codes));
        END IF;

        IF EXISTS (SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = 'sys_tenant_edition_permission') THEN
            DELETE FROM sys_tenant_edition_permission
             WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = ANY (review_codes));
        END IF;

        IF EXISTS (SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = 'sys_permission_delegation') THEN
            DELETE FROM sys_permission_delegation
             WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = ANY (review_codes));
        END IF;

        IF EXISTS (SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = 'sys_permission_request') THEN
            DELETE FROM sys_permission_request
             WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = ANY (review_codes));
        END IF;

        IF EXISTS (SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = 'sys_menu') THEN
            UPDATE sys_menu SET permission_id = NULL
             WHERE permission_id IN (SELECT basic_id FROM sys_permission WHERE permission_code = ANY (review_codes));
        END IF;

        DELETE FROM sys_permission WHERE permission_code = ANY (review_codes);

        IF EXISTS (SELECT 1 FROM information_schema.tables
                    WHERE table_schema = current_schema() AND table_name = 'sys_resource') THEN
            DELETE FROM sys_resource AS r
             WHERE r.resource_code IN ('review', 'review-log', 'saas:review', 'saas:review-log')
               AND NOT EXISTS (SELECT 1 FROM sys_permission AS p WHERE p.resource_id = r.basic_id);
        END IF;
    END IF;
END
$$;

-- 四、菜单：撤掉「审批中心（系统审查）」页与其按钮；「我的待办」并入工作流登记的新「审批中心」（/approval/center，由菜单种子补入）。
-- 菜单种子只对齐、不删行，从页面登记表里去掉的页要由脚本删除；按钮（menu_type = 2）随页删除。
-- 「审批」目录的默认跳转改到「审批约束」，由菜单种子对齐。sys_menu 只在平台库建表，独立库上整段跳过。
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'sys_menu') THEN
        DELETE FROM sys_menu
         WHERE tenant_id = 0
           AND menu_type = 2
           AND parent_id IN (SELECT basic_id FROM sys_menu WHERE tenant_id = 0 AND menu_code IN ('approval.review', 'workflow_todo'));

        DELETE FROM sys_menu
         WHERE tenant_id = 0
           AND menu_code IN ('approval.review', 'workflow_todo');
    END IF;
END
$$;

-- 五、工作流实例记录流程结果（结果结束节点写入 approved / rejected），并按发起人查询「我发起的」。
-- sys_workflow_instance 是严格隔离的租户数据，平台库与独立库都有，逐库执行；存量实例没有结果，保持为空。
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'sys_workflow_instance') THEN
        ALTER TABLE sys_workflow_instance ADD COLUMN IF NOT EXISTS outcome varchar(100) NULL;
        COMMENT ON COLUMN sys_workflow_instance.outcome IS '流程结果';
        CREATE INDEX IF NOT EXISTS ix_sys_workflow_instance_teid_stid_crti
            ON sys_workflow_instance (tenant_id, starter_id, creation_time DESC);
    END IF;
END
$$;
