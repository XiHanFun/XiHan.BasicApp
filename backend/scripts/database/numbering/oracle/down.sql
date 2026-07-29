-- 业务编号模块 Oracle 回滚脚本。
-- 警告：回滚会永久删除规则与发号审计记录，执行前必须备份并确认业务停机。

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE "Sys_Numbering_Allocation" CASCADE CONSTRAINTS PURGE';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -942 THEN
            RAISE;
        END IF;
END;
/
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE "Sys_Numbering_Rule" CASCADE CONSTRAINTS PURGE';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -942 THEN
            RAISE;
        END IF;
END;
/
