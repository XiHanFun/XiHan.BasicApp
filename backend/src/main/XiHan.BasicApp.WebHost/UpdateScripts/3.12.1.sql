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

-- 收敛代码生成的生成方式取值。
--
-- gen_type 原有三个取值 0=Zip 1=CustomPath 2=Preview，其中 Preview 从来不是一种「生成方式」：
-- 预览走独立入口、引擎的 PreviewAsync 直接渲染、根本不读该字段。它出现在表配置下拉里，
-- 选中后既不影响预览、也让「生成」无从判断该下载还是落盘。枚举已收敛为两个取值，
-- 存量选了 Preview 的表回落到 Zip（生成并下载）。

UPDATE sys_code_gen_table
SET gen_type = 0
WHERE gen_type = 2;

-- 模板的「通用」由 NULL 改为显式枚举值。
--
-- 此前 sys_code_gen_template.template_type 为空表示「适用全部表类型」，这个语义只有后端知道，
-- 前端列表只能把 NULL 渲染成「-」、表单只能拿占位符冒充选项。现在 TemplateType 增加 Universal(3)，
-- 模板侧改为非空、默认 Universal；表配置侧仍只允许单表/树表/主子表（领域校验拒绝 Universal）。

UPDATE sys_code_gen_template
SET template_type = 3
WHERE template_type IS NULL;

ALTER TABLE sys_code_gen_template
    ALTER COLUMN template_type SET NOT NULL;
