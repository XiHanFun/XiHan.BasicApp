-- 通行密钥改按凭据 ID 的 SHA-256 建唯一索引：凭据 ID 最长 1364 个字符，超过 MySQL 与 SQL Server 的索引键上限
-- 已有的行回填哈希，去掉按凭据 ID 的旧唯一索引；新索引 ux_sys_user_passkey_credential_hash 随后由按实体补表补列建出
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = 'sys_user_passkey') THEN
        ALTER TABLE sys_user_passkey ADD COLUMN IF NOT EXISTS credential_hash varchar(64);
        UPDATE sys_user_passkey SET credential_hash = encode(sha256(convert_to(credential_id, 'UTF8')), 'hex') WHERE credential_hash IS NULL;
        ALTER TABLE sys_user_passkey ALTER COLUMN credential_hash SET NOT NULL;
        DROP INDEX IF EXISTS ux_sys_user_passkey_credential_id;
    END IF;
END
$$;
