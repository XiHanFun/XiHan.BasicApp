-- 5.6.0
-- 一、仪表盘不再登记页面按钮：小组件改为前端示例数据，删去只为「今日统计」登记的「用户统计」按钮（见后文）。
--
-- 只在 5.6.0 之前建的库上执行：新建的库按当前实体建表后直接登记为最新版本，不跑本脚本。
-- 本脚本在建表之后、播种之前执行。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写。
--
-- 幂等：按条件删除，跑过一次后整段空转。

-- 一、仪表盘不再登记页面按钮。
-- 菜单种子只对齐、不删行，从页面登记表里去掉的按钮要由脚本删除。
-- 仪表盘（workbench.dashboard）下的按钮行（menu_type = 2）全部删除：按钮没有下级，授权挂在权限上，
-- 删按钮行不影响「用户统计查看」权限本身，用户管理与个人中心仍按它读取用户统计。
-- sys_menu 只在平台库建表（[PlatformDataSource]），独立库上整段跳过。
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'sys_menu') THEN
        DELETE FROM sys_menu
         WHERE tenant_id = 0
           AND menu_type = 2
           AND parent_id IN (SELECT basic_id FROM sys_menu WHERE tenant_id = 0 AND menu_code = 'workbench.dashboard');
    END IF;
END
$$;
