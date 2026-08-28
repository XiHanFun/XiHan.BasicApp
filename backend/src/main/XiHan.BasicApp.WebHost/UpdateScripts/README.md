# 数据库升级脚本

由 `UpgradeEngine` 在应用初始化阶段执行，执行台账记入 `sys_migration_history`。

## 约定

- **默认只提供PostgreSQL，其他数据库需自行实现**
- **目录名即版本号**：脚本放在 `UpdateScripts/<版本>/` 下，如 `3.10.0/3.10.0.sql`。
  框架的 `FileSystemUpgradeScriptProvider` 只扫**子目录**（`Directory.GetDirectories`），
  同一版本目录内可以放多个 .sql，按文件名升序执行。
- **只有版本号高于库中 `db_version` 的脚本会执行**（记在 `sys_version`，随脚本执行推进）。
  与程序版本 `props/version.props` **无关**——`UpgradeEngine.ExecuteMigrationsAsync` 只比库版本，
  不比较 AppVersion。新库的 `db_version` 初始为 `0.0.0`，因此会把全部脚本走一遍，
  **每个脚本都必须在最新结构上也能安全空转**。
- **标识符一律小写、不加引号。** SqlSugar 建表时未加引号，PostgreSQL 将未加引号的标识符折叠为小写，
  所以库里的实际名是 `sys_oauth_code`、`basic_id`，而不是实体上声明的 `Sys_OAuth_Code`、`Basic_Id`。
  写成 `"Sys_OAuth_Code"` 会因引号带来大小写敏感而报 `42P01 relation does not exist`。
- **写成可重复执行**：用 `IF NOT EXISTS` / `IF EXISTS`。失败的脚本不会记为已执行，下次启动会重试。
- **PostgreSQL 方言**：执行器在 PG 上取事务级建议锁并把本次全部脚本放进同一事务，失败整体回滚。

## 失败会怎样

脚本抛错即整体回滚、写入一条 `Success = false` 的台账，并**中断应用启动** —— 宁可起不来，
也不让应用带着半吊子表结构对外服务。修好脚本后重启即可，失败记录不影响重试。
