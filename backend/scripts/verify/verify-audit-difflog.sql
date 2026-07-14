-- ============================================================================
-- 数据变更日志（SysDiffLog）+ 审计脱敏 冒烟验证
--
-- 用法：
--   psql -h <host> -U <user> -d <db> -f verify-audit-difflog.sql
--
-- 运行之前，请先在系统里做这三个动作（顺序无所谓）：
--   1) 改一次密码（任意用户）        —— 验证 diff 触发 + 密码脱敏 + 元数据不被误掩
--   2) 软删除一条业务数据（如删个岗位）—— 验证软删语义识别（应记为 Delete 而非 Update）
--   3) 随便触发一次接口异常           —— 验证异常日志的请求头脱敏（Authorization 不得明文）
--
-- 前提：框架新包已发布并升级 PackageReference，且 XiHan:Data:SqlSugarCore:EnableDiffLog = true。
--   （脱敏代码若不在所引用的 NuGet 包里，第 3/4 项必挂——这正是本脚本要替你抓的。）
-- ============================================================================

\set ON_ERROR_STOP on
\pset border 2
\timing off

-- ---------------------------------------------------------------------------
-- 0. 工具：安全的 JSON 合法性判定（不合法不抛异常，只返回 false）
-- ---------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION pg_temp._is_json(t text) RETURNS boolean AS $$
BEGIN
    IF t IS NULL OR t = '' THEN
        RETURN true;   -- 空值不算非法
    END IF;
    PERFORM t::jsonb;
    RETURN true;
EXCEPTION WHEN others THEN
    RETURN false;
END;
$$ LANGUAGE plpgsql;

-- ---------------------------------------------------------------------------
-- 1. 汇总所有 sys_diff_log 月表（表名/列名的大小写由 information_schema 动态解析，
--    不硬编码——SqlSugar 建 PG 表时是否加引号会影响大小写折叠）
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE _diff (
    src_table      text,
    entity_type    text,
    operation_type int,
    before_data    text,
    after_data     text,
    changed_fields text,
    description    text,
    audit_time     timestamptz
);

DO $$
DECLARE
    t          record;
    c_entity   text;
    c_op       text;
    c_before   text;
    c_after    text;
    c_changed  text;
    c_desc     text;
    c_time     text;
    n_tables   int := 0;
BEGIN
    FOR t IN
        SELECT c.relname
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind = 'r'
          AND n.nspname NOT IN ('pg_catalog', 'information_schema')
          AND lower(c.relname) LIKE 'sys\_diff\_log%'
    LOOP
        SELECT quote_ident(column_name) INTO c_entity  FROM information_schema.columns WHERE table_name = t.relname AND lower(column_name) = 'entity_type';
        SELECT quote_ident(column_name) INTO c_op      FROM information_schema.columns WHERE table_name = t.relname AND lower(column_name) = 'operation_type';
        SELECT quote_ident(column_name) INTO c_before  FROM information_schema.columns WHERE table_name = t.relname AND lower(column_name) = 'before_data';
        SELECT quote_ident(column_name) INTO c_after   FROM information_schema.columns WHERE table_name = t.relname AND lower(column_name) = 'after_data';
        SELECT quote_ident(column_name) INTO c_changed FROM information_schema.columns WHERE table_name = t.relname AND lower(column_name) = 'changed_fields';
        SELECT quote_ident(column_name) INTO c_desc    FROM information_schema.columns WHERE table_name = t.relname AND lower(column_name) = 'description';
        SELECT quote_ident(column_name) INTO c_time    FROM information_schema.columns WHERE table_name = t.relname AND lower(column_name) = 'audit_time';

        CONTINUE WHEN c_entity IS NULL OR c_op IS NULL;   -- 结构不符，跳过

        EXECUTE format(
            'INSERT INTO _diff SELECT %L, %s::text, %s::int, %s::text, %s::text, %s::text, %s::text, %s::timestamptz FROM %I',
            t.relname, c_entity, c_op,
            coalesce(c_before, 'NULL'), coalesce(c_after, 'NULL'), coalesce(c_changed, 'NULL'),
            coalesce(c_desc, 'NULL'), coalesce(c_time, 'NULL'), t.relname);

        n_tables := n_tables + 1;
    END LOOP;

    RAISE NOTICE '发现并汇总了 % 张 sys_diff_log 月表', n_tables;
END;
$$;

-- ---------------------------------------------------------------------------
-- 2. 断言
--    OperationType: Create=3  Update=4  Delete=5  Restore=12
-- ---------------------------------------------------------------------------
WITH pwd AS (
    -- 改密码产生的那条记录（SysUserSecurity 的 Update）
    SELECT
        d.*,
        jsonb_path_query_first(d.before_data::jsonb,
            '$[*].Columns[*] ? (@.ColumnName == "Password").Value') #>> '{}'                    AS pwd_before,
        jsonb_path_query_first(d.after_data::jsonb,
            '$[*].Columns[*] ? (@.ColumnName == "Password").Value') #>> '{}'                    AS pwd_after,
        jsonb_path_query_first(d.after_data::jsonb,
            '$[*].Columns[*] ? (@.ColumnName == "Last_Password_Change_Time").Value') #>> '{}'   AS pwd_time
    FROM _diff d
    WHERE d.entity_type = 'SysUserSecurity'
      AND pg_temp._is_json(d.before_data)
      AND pg_temp._is_json(d.after_data)
)
SELECT * FROM (

    -- 【0】前置：表里到底有没有数据（若为 0，后面全部 SKIP，先去查 EnableDiffLog 开关）
    SELECT 0 AS seq,
           '差异日志表有数据' AS "检查项",
           CASE WHEN count(*) > 0 THEN 'PASS' ELSE 'FAIL ← EnableDiffLog 没开？' END AS "结果",
           count(*)::text || ' 条' AS "详情"
    FROM _diff

    UNION ALL
    -- 【1】diff 真的被触发了（且 SaasUserStore 的 EnableDiffLogEvent 生效）
    --      这是唯一没被探针覆盖的形态：Updateable(entity).UpdateColumns(...) + EnableDiffLogEvent
    SELECT 1,
           '改密码产生了 SysUserSecurity 变更记录',
           CASE WHEN count(*) > 0 THEN 'PASS' ELSE 'FAIL ← UpdateColumns+EnableDiffLogEvent 没产出 diff' END,
           count(*)::text || ' 条'
    FROM pwd

    UNION ALL
    -- 【2】密码被掩码（脱敏生效）
    SELECT 2,
           '密码值被掩成 ***',
           CASE
               WHEN count(*) = 0 THEN 'SKIP ← 无改密码记录'
               WHEN count(*) FILTER (WHERE pwd_after IS NOT NULL) = 0
                   THEN 'WARN ← Password 列未出现在快照中'
               WHEN count(*) FILTER (WHERE pwd_after = '***')
                  = count(*) FILTER (WHERE pwd_after IS NOT NULL) THEN 'PASS'
               ELSE 'FAIL ← 密码明文落库！立刻关掉 EnableDiffLog 并清表'
           END,
           coalesce(string_agg(DISTINCT coalesce(pwd_after, '<null>'), ' | '), '-')
    FROM pwd

    UNION ALL
    -- 【3】★本轮核心修复★ 元数据不被误掩：Last_Password_Change_Time 必须是真实时间，不是 ***
    --      （旧逻辑把它当敏感字段掩掉了，等于把审计最需要看的东西一起掩了）
    SELECT 3,
           'Last_Password_Change_Time 未被误掩（是真实时间）',
           CASE
               WHEN count(*) = 0 THEN 'SKIP ← 无改密码记录'
               WHEN count(*) FILTER (WHERE pwd_time = '***') > 0 THEN 'FAIL ← 元数据后缀排除没生效'
               WHEN count(*) FILTER (WHERE pwd_time IS NOT NULL) > 0 THEN 'PASS'
               ELSE 'WARN ← 该列未出现在快照中'
           END,
           coalesce(string_agg(DISTINCT coalesce(pwd_time, '<null>'), ' | '), '-')
    FROM pwd

    UNION ALL
    -- 【4】ChangedFields 仍记录了"密码变过"这个事实（掩码不能把变更记录一起吃掉）
    SELECT 4,
           'ChangedFields 里仍有 Field=Password',
           CASE
               WHEN count(*) = 0 THEN 'SKIP ← 无改密码记录'
               WHEN count(*) FILTER (WHERE changed_fields LIKE '%"Field":"Password"%') > 0 THEN 'PASS'
               ELSE 'FAIL ← 掩码把变更事实一起吃了（比对用了掩码值？）'
           END,
           count(*) FILTER (WHERE changed_fields LIKE '%"Field":"Password"%')::text || ' 条'
    FROM pwd

    UNION ALL
    -- 【5】快照是合法 JSON（截断修复：超长时不得从中间切断）
    SELECT 5,
           'Before/After/ChangedFields 都是合法 JSON',
           CASE WHEN count(*) FILTER (
                    WHERE NOT (pg_temp._is_json(before_data)
                           AND pg_temp._is_json(after_data)
                           AND pg_temp._is_json(changed_fields))) = 0
                THEN 'PASS' ELSE 'FAIL ← 存在被硬截断的非法 JSON' END,
           count(*) FILTER (
               WHERE NOT (pg_temp._is_json(before_data)
                      AND pg_temp._is_json(after_data)
                      AND pg_temp._is_json(changed_fields)))::text || ' 条非法'
    FROM _diff

    UNION ALL
    -- 【6】软删语义识别（修复前：软删全被记成 Update=4，因为比对的是 IsDeleted 而非 Is_Deleted）
    SELECT 6,
           '软删除被识别为 Delete/Restore（而非 Update）',
           CASE
               WHEN count(*) FILTER (WHERE operation_type IN (5, 12)) > 0 THEN 'PASS'
               WHEN count(*) FILTER (WHERE operation_type = 4) > 0
                   THEN 'WARN ← 只有 Update；若你确实软删过，说明识别仍失败'
               ELSE 'SKIP ← 没有变更记录'
           END,
           'Delete=' || count(*) FILTER (WHERE operation_type = 5)::text ||
           ' Restore=' || count(*) FILTER (WHERE operation_type = 12)::text ||
           ' Update='  || count(*) FILTER (WHERE operation_type = 4)::text
    FROM _diff

) x ORDER BY seq;

-- ---------------------------------------------------------------------------
-- 3. 异常日志：请求头里绝不能有明文 JWT / Cookie
--    （脱敏若不在所引用的 NuGet 包里，这里会直接抓到 Bearer 明文）
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    t       record;
    c_hdr   text;
    n_bad   int;
    n_total int := 0;
    n_leak  int := 0;
BEGIN
    FOR t IN
        SELECT c.relname
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind = 'r'
          AND n.nspname NOT IN ('pg_catalog', 'information_schema')
          AND lower(c.relname) LIKE 'sys\_exception\_log%'
    LOOP
        SELECT quote_ident(column_name) INTO c_hdr
        FROM information_schema.columns
        WHERE table_name = t.relname AND lower(column_name) = 'request_headers';

        CONTINUE WHEN c_hdr IS NULL;

        -- Bearer eyJ... 是 JWT 的明文特征；Cookie 同理
        EXECUTE format(
            'SELECT count(*) FROM %I WHERE %s ILIKE %L OR %s ILIKE %L',
            t.relname, c_hdr, '%Bearer ey%', c_hdr, '%"Cookie":"%')
        INTO n_bad;

        n_total := n_total + 1;
        n_leak  := n_leak + n_bad;
    END LOOP;

    IF n_total = 0 THEN
        RAISE NOTICE '[7] 异常日志请求头脱敏 : SKIP  (没有 sys_exception_log 表)';
    ELSIF n_leak = 0 THEN
        RAISE NOTICE '[7] 异常日志请求头脱敏 : PASS  (% 张表，0 条明文凭证)', n_total;
    ELSE
        RAISE WARNING '[7] 异常日志请求头脱敏 : FAIL  (% 条记录含明文 Bearer/Cookie —— 脱敏代码不在所引用的包里？)', n_leak;
    END IF;
END;
$$;

-- ---------------------------------------------------------------------------
-- 4. 附：把改密码那条记录整个打出来，供人工核对
-- ---------------------------------------------------------------------------
SELECT src_table AS "月表",
       description AS "摘要",
       audit_time AS "时间",
       changed_fields AS "变更字段"
FROM _diff
WHERE entity_type = 'SysUserSecurity'
ORDER BY audit_time DESC
LIMIT 3;

DROP TABLE _diff;
