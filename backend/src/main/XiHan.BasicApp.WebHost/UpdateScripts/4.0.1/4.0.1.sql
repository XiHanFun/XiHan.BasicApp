-- 4.0.1
-- 租户存储用量的 SUM 查询改走覆盖索引。
--
-- 背景：租户配额落地后，每次上传在文件落库前都会执行一次
--   SELECT SUM(file_size) FROM sys_file WHERE tenant_id = ? AND status IN (...) AND is_deleted = false
-- 原索引 ix_sys_file_teid_st 只含 (tenant_id, status)，is_deleted 与 file_size 都不在索引里，
-- 每命中一行就要回一次表。租户文件越多越慢，偏偏上传是高频路径。
--
-- 新索引把这两列并进去，PostgreSQL 可走 index-only scan 不再回表。
-- 它以 (tenant_id, status) 打头、是旧索引的超集，旧索引的全部用途都能服务，
-- 因此旧的删掉不留冗余——多一个索引就多一份写放大。
--
-- 顺序是先建后删：中间不留无索引窗口。
--
-- 注意 CREATE INDEX 会阻塞该表写入。执行器把整批脚本放进同一事务，
-- 用不了 CONCURRENTLY（它不能在事务中执行），大表升级请安排在维护窗口。
--
-- 这条只降低常数因子，复杂度仍是 O(租户文件数)。若单租户文件数进入百万级，
-- 需要改为增量计数器（上传 +size / 删除 -size + 定期校准），那是另一个量级的工程。
--
-- 全新库不受影响：DbInitializer 建表时已按实体上的 SugarIndex 建出新索引，
-- 下面两条分别被 IF NOT EXISTS / IF EXISTS 跳过，空转安全。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，
-- 实际名是 sys_file / tenant_id / file_size，而非实体上声明的 Sys_File / Tenant_Id / File_Size。

CREATE INDEX IF NOT EXISTS ix_sys_file_teid_st_isde_fisi
    ON sys_file (tenant_id, status, is_deleted, file_size);

DROP INDEX IF EXISTS ix_sys_file_teid_st;
