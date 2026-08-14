-- 3.12.1
-- 修正非库隔离租户的配置状态。
--
-- config_status 默认 0（Pending），而全仓只有库隔离租户的数据库初始化流程会把它推到 2（Configured）。
-- 字段隔离租户没有任何路径能变成已配置，登录链路又要求 config_status = 2，
-- 于是这样建出来的租户永远登不进去。创建/更新逻辑已改为按隔离模式落配置状态，此处修正存量数据。
--
-- isolation_mode：0=Field 1=Database 2=Schema
-- config_status：0=Pending 1=Configuring 2=Configured 3=Failed 4=Disabled
-- 只改 Pending 的行：Failed / Disabled 是运维显式置下的状态，不能一把刷掉。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，
-- 实际名是 sys_tenant / config_status 而非实体上声明的 Sys_Tenant / Config_Status。

UPDATE sys_tenant
SET config_status = 2
WHERE isolation_mode <> 1
  AND config_status = 0;
