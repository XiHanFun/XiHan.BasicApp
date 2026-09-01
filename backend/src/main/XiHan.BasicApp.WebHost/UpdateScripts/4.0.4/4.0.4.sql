-- 4.0.4
-- 存量代码生成列配置里的 bigint 列改按字符串承载。
--
-- 后端全局 LongJsonConverter 把所有 long 写成 JSON 字符串（雪花 ID 超出 JavaScript Number
-- 安全范围），所以前端产物应当声明成 string + 文本框。类型映射表已改（DefaultTypeMappingProvider）。
--
-- 产物正确性不依赖这个脚本：ScribanTemplateRenderer.BuildColumn 会按 C# 类型把 long 列的 TsType
-- 归一化成 string，库里存着 number 也照样产出正确的代码。本脚本只是把存量配置刷成一致，
-- 免得列配置界面上显示的类型/控件与实际产物对不上。
--
-- 只刷未被人工冻结的字段：user_modified_fields 是实体属性名的 JSON 数组，
-- 命中 TsType / HtmlType 表示用户手工改过，此处保留其值。
--
-- 列类型判定与 DefaultTypeMappingProvider.Normalize 同口径：去括号内的长度/精度、
-- 去 unsigned 与数组后缀、去空白后小写比较。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，
-- 库里实际是 sys_codegen_tablecolumn / user_modified_fields。
--
-- 幂等：条件里带 ts_type/html_type 的当前值，跑过一次后匹配不到行，空转安全。

DO $$
BEGIN
    -- 未装代码生成模块的库直接跳过
    IF to_regclass('public.sys_codegen_tablecolumn') IS NULL THEN
        RETURN;
    END IF;

    UPDATE sys_codegen_tablecolumn
       SET ts_type = 'string'
     WHERE ts_type = 'number'
       AND lower(btrim(replace(replace(split_part(coalesce(column_type, ''), '(', 1), '[]', ''), ' unsigned', '')))
           IN ('bigint', 'long', 'int8', 'bigserial', 'serial8')
       AND coalesce(user_modified_fields, '') !~* '"tstype"';

    -- HtmlType.InputNumber = 11 → HtmlType.Input = 0
    UPDATE sys_codegen_tablecolumn
       SET html_type = 0
     WHERE html_type = 11
       AND lower(btrim(replace(replace(split_part(coalesce(column_type, ''), '(', 1), '[]', ''), ' unsigned', '')))
           IN ('bigint', 'long', 'int8', 'bigserial', 'serial8')
       AND coalesce(user_modified_fields, '') !~* '"htmltype"';
END $$;
