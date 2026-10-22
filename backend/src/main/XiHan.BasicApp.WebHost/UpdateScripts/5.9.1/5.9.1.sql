-- 5.9.1
-- 全局 IP 黑名单换成访问策略：删掉参数 saas.security.ip-blacklist。
-- 访问策略的三张表（sys_access_policy、sys_access_policy_condition 与按月分表的命中记录）由启动时的建表步骤建出，不在这里。
--
-- 只在 5.9.1 之前建的库上执行：新建的库按当前实体建表后直接登记为最新版本，不跑本脚本。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写。
--
-- 幂等：表先判存在，删过一次后再执行不删任何行。

-- 独立库可能没有 sys_config，先判表存在。
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'sys_config') THEN
        DELETE FROM sys_config WHERE config_key = 'saas.security.ip-blacklist';
    END IF;
END
$$;
