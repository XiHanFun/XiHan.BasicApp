-- 3.10.0
-- OIDC：授权码表新增 nonce 列，换取令牌时原样写进 id_token 的 nonce 声明。
--
-- 标识符一律小写不加引号：SqlSugar 建表时未加引号，PostgreSQL 把未加引号的标识符
-- 折叠为小写，故库里的实际名为 sys_oauth_code / nonce，而非实体上声明的
-- Sys_OAuth_Code / Nonce。写成 "Sys_OAuth_Code" 会因引号带来大小写敏感而匹配不到。
--
-- IF NOT EXISTS 使脚本可重复执行。
ALTER TABLE sys_oauth_code ADD COLUMN IF NOT EXISTS nonce varchar(200);

COMMENT ON COLUMN sys_oauth_code.nonce IS 'OIDC随机串';
