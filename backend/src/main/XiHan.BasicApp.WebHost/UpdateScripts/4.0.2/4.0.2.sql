-- 4.0.2
-- 四张表的唯一索引补上 tenant_id（与 is_deleted），以及 AI 供应商密钥列扩容。
--
-- ## 唯一索引缺租户维度
--
-- 这四张表读写两侧都是按租户维度的：实体继承 BasicAppFullAuditedEntity（含非空 tenant_id），
-- 仓储经 SaasRepository / CreateQueryable 做租户行过滤。唯独唯一约束是全局的，于是：
--
--   租户 A 建了 code='leave' version=1 之后，租户 B 再建同名同版本会命中唯一键冲突而报错，
--   可租户 B 从任何查询里都看不到那一行（被租户过滤掉）——
--   表现为「编码明明没被占用，却建不出来」，且无从排查。
--
-- 同仓库 SysPrintTemplate 用的就是 UNIQUE(tenant_id, template_code, is_deleted)，
-- SysAiProvider 用的是 UX_TeId_CoCd 末列附 is_deleted；框架 SugarMultiTenantEntity 的
-- tenant_id 注释也写明「非空确保 UNIQUE(TenantId, XxCode) 等复合唯一索引对全局记录生效」。
-- 这四张是漏网的。
--
-- 顺带补 is_deleted：缺了它软删之后名字/编码无法复用，用户删掉一条再想用同名建新的会被拒。
--
-- **不需要做冲突排查**。往唯一索引里增列只会让约束**更宽松**：旧索引能容纳的组合，
-- 新索引一定也能容纳。所以存量库里不可能存在违反新索引的数据，
-- 建新索引这一步不会因既有数据失败。（缺陷清单里"旧库若已有跨租户同码数据需先排查"
-- 的说法反了——正因为旧索引更严，那种数据根本存不进去。）
--
-- 顺序是先建后删：中间不留无唯一约束的窗口，避免并发写入插进重复行。
--
-- ## AI 供应商密钥列扩容
--
-- api_key 存的是 Data Protection 密文而非明文，长度约 (明文 + 84) / 3 * 4 + 3
-- （"dp:" 前缀 + base64(4 字节头 + 16 字节密钥 id + 16 字节 IV + 明文补齐到 16 的倍数 + 32 字节 HMAC)）。
-- 原来的 varchar(500) 只装得下约 289 字符明文，而 JWT 形态的供应商密钥轻易超过它。
-- 超长时写库被截断——密文一旦截断就永久解不开，且直到下次调用该供应商才会暴露。
-- 放到 2000 可容纳约 1400 字符明文，实际上限由领域层校验兜住。
-- 加宽 varchar 在 PostgreSQL 里是纯目录变更、不重写表，也因此可以反复执行。
--
-- ## 空转安全
--
-- 全新库的 db_version 从 0.0.0 起跑、会把全部脚本走一遍。此时 DbInitializer 已按实体上的
-- SugarIndex 建出新索引、按 Length=2000 建出列，下面每条都会被 IF NOT EXISTS / IF EXISTS
-- 跳过或成为同类型赋值，空转安全。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，
-- 实际名是 sys_workflow_definition / tenant_id，而非实体上声明的 Sys_Workflow_Definition / Tenant_Id。
--
-- 注意 CREATE INDEX 会阻塞对应表的写入。执行器把整批脚本放进同一事务，用不了 CONCURRENTLY
-- （它不能在事务中执行），这四张都是配置类小表，影响有限。

-- 工作流定义：(code, version, is_deleted) -> (tenant_id, code, version, is_deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_workflow_definition_teid_co_ve
    ON sys_workflow_definition (tenant_id, code, version, is_deleted);
DROP INDEX IF EXISTS ux_sys_workflow_definition_co_ve;

-- 代码生成数据源：(source_name) -> (tenant_id, source_name, is_deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_codegen_datasource_teid_sona
    ON sys_codegen_datasource (tenant_id, source_name, is_deleted);
DROP INDEX IF EXISTS ux_sys_codegen_datasource_sona;

-- 代码生成表配置：(table_name) -> (tenant_id, table_name, is_deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_codegen_table_teid_tana
    ON sys_codegen_table (tenant_id, table_name, is_deleted);
DROP INDEX IF EXISTS ux_sys_codegen_table_tana;

-- 代码生成模板：(template_code) -> (tenant_id, template_code, is_deleted)
CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_codegen_template_teid_teco
    ON sys_codegen_template (tenant_id, template_code, is_deleted);
DROP INDEX IF EXISTS ux_sys_codegen_template_teco;

-- AI 供应商密钥列：varchar(500) -> varchar(2000)
ALTER TABLE sys_ai_provider
    ALTER COLUMN api_key TYPE varchar(2000);
