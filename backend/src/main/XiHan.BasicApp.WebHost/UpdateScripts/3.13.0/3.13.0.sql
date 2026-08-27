-- 3.13.0
-- 登录页的 OAuth 提供商清单补上微信、企业微信、飞书、钉钉。
--
-- 框架侧八家提供商已全部内置（见 XiHan.Framework.Authentication 的 OAuth/Handlers），
-- 但登录页展示哪几家是由本条配置决定的，不是由 appsettings 的 Providers 决定：
--   - XiHan:Authentication:OAuth:Providers  → 实际注册成哪些 AuthenticationScheme（能不能登）
--   - saas.auth.oauth.providers（本条）      → 登录页画哪几个按钮（看得见几个）
-- 两边的 name 必须一致，对不上就会点出一个不存在的方案。种子里的默认值已同步扩到八家，
-- 但种子只对新库生效，存量库的这一行需要在此补齐。
--
-- 只改平台级那一行（tenant_id = 0）：sys_config 是租户内唯一（tenant_id + config_key），
-- 租户级同键会覆盖全局。种子只负责平台级默认值，租户自己那一行是租户的决定，不能替他改；
-- 没有租户级覆盖的租户本来就继承全局，改平台级这一行即可生效。
--
-- 只改「仍是旧默认值」的行：这一条允许运维按需删减，改过就不能一把刷掉。
-- 判定方式是拿去掉全部空白后的文本比对旧默认值，避免因缩进差异误判为已定制。
--
-- 这个键历史上有过两版多提供商默认值，两版都要认：
--   3b95e5db(2026-06-16) 起是三家版 [github, google, qq]
--   8ab4f978(2026-07-08) 接入 Gitee 后是四家版
-- 种子对已存在且非空的 ConfigValue 永不覆盖（ApplyDefinition 的 fillValueOnly），
-- 所以在接入 Gitee 之前初始化的库至今仍停在三家版 —— 那是另一版官方默认值，不是运维定制，
-- 只认四家版会让这些库一行都刷不到，且静默成功、无从察觉。
--
-- 不认的两版：`[]` 与单 github 版。`[]` 正是该键的 DefaultValue，
-- 也是运维「关掉全部第三方登录」唯一的合法写法，与历史默认值在库里无法区分，误刷会把关掉的功能重新打开。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，
-- 实际名是 sys_config / config_key / config_value 而非实体上声明的 Sys_Config / Config_Key。

UPDATE sys_config
SET config_value = '[{"name":"github","displayName":"Github"},{"name":"gitee","displayName":"Gitee"},{"name":"google","displayName":"Google"},{"name":"qq","displayName":"QQ"},{"name":"wechat","displayName":"微信"},{"name":"wecom","displayName":"企业微信"},{"name":"feishu","displayName":"飞书"},{"name":"dingtalk","displayName":"钉钉"}]'
WHERE config_key = 'saas.auth.oauth.providers'
  AND tenant_id = 0
  AND is_deleted = false
  AND regexp_replace(coalesce(config_value, ''), '\s', '', 'g') IN (
      -- 三家版（接入 Gitee 之前）
      '[{"name":"github","displayName":"Github"},{"name":"google","displayName":"Google"},{"name":"qq","displayName":"QQ"}]',
      -- 四家版（接入 Gitee 之后）
      '[{"name":"github","displayName":"Github"},{"name":"gitee","displayName":"Gitee"},{"name":"google","displayName":"Google"},{"name":"qq","displayName":"QQ"}]'
  );
