-- 3.10.0 幂等 PostgreSQL 脚本 
-- OIDC：授权码表新增 nonce 列，换取令牌时原样写进 id_token 的 nonce 声明。

ALTER TABLE sys_oauth_code ADD COLUMN IF NOT EXISTS nonce varchar(200);

COMMENT ON COLUMN sys_oauth_code.nonce IS 'OIDC随机串';
