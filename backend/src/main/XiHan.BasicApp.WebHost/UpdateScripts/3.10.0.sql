-- 3.10.0
-- OIDC：授权码表新增 nonce 列，换取令牌时原样写进 id_token 的 nonce 声明。
-- PostgreSQL 方言。IF NOT EXISTS 使脚本可重复执行。
ALTER TABLE "Sys_OAuth_Code" ADD COLUMN IF NOT EXISTS "Nonce" varchar(200) NULL;

COMMENT ON COLUMN "Sys_OAuth_Code"."Nonce" IS 'OIDC随机串';
