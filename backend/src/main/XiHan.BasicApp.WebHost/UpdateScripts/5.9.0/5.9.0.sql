-- 5.9.0
-- 打印换成 XiHan 打印设计器：打印模板 JSON 改存新格式（schemaVersion 1 的打印模板），
-- 记录 hiprint 引擎版本的 engine_version 列不再使用，删掉。存量的 hiprint 模板不迁移。
--
-- 只在 5.9.0 之前建的库上执行：新建的库按当前实体建表后直接登记为最新版本，不跑本脚本。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写。
--
-- 幂等：表与列先判存在，跑过一次后整段空转。

-- sys_print_template 标了 [PlatformDataSource]，只在平台库建表；独立库没有这张表，先判表存在。
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables
                WHERE table_schema = current_schema() AND table_name = 'sys_print_template') THEN
        ALTER TABLE sys_print_template DROP COLUMN IF EXISTS engine_version;
        COMMENT ON COLUMN sys_print_template.template_json IS '打印模板JSON';
    END IF;
END
$$;
