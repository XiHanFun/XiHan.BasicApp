# 代码生成

CodeGeneration 是 Tier 3 的可选模块（`BasicApp:Modules:CodeGeneration:Enabled`，缺省开），只在平台一侧：管理外部数据源，按库表结构导入表与列配置，用 Scriban 模板生成一个 6.0 包与前端模块，记生成历史，另有零代码只读运行时。模型与规则沿用 5.x，写法换成 6.0 的。

::: warning 部分落地
N3 第四批分笔落地，现在有数据源、表结构导入与表和列配置；模板与预览、生成与历史、零代码运行时随后补上。5.x 的代码生成文档见 [5.x 文档](https://basicapp.docs.xihanfun.com/backend/code-generation)。
:::

## 页面与权限

页面 `codegeneration.code-gen`（路由 `/codegeneration/code-gen`，菜单按 `codegeneration.codegen-tables.read` 下发），照 5.x 一页四签：表配置、数据源、模板、生成历史，各页签按自己资源的读权限显示。5.x 挂在「开发中心」目录下，6.0 的目录按包。

权限码只在平台一侧。5.x 是一组 `code_gen:{read,create,update,delete,import,execute}`，6.0 权限码的资源段要与操作一致，按资源拆开、动作照 5.x 各接口要的那个码：

| 资源 | 权限码 |
| --- | --- |
| 数据源 `codegen-datasources` | `read`、`create`、`update`（修改、启停、测试连接）、`delete` |
| 表配置 `codegen-tables` | `read`（也是页面读权限，含列出库表）、`update`（修改、启停）、`delete`、`import`（导入、同步结构） |
| 列配置 `codegen-table-columns` | `read`、`update`（整体保存） |

## 数据源

外部库的连接信息，导入表结构与零代码运行时经它连库；表配置的数据源为空即主库。

| 操作 | 说明 |
| --- | --- |
| `codegen-datasources.list` | 分页；关键字匹配名称、描述、主机与库名，数据库类型与状态可多选 |
| `codegen-datasources.options` | 全部数据源（含停用的），给下拉用 |
| `codegen-datasources.get` | 详情；口令与连接串不回显，只说有没有显式连接串 |
| `codegen-datasources.create` / `update` | 名称唯一；口令与显式连接串加密存储，编辑时留空即不改；设为缺省时其余的取消缺省 |
| `codegen-datasources.set-status` | 启停；停用的不能用于读库结构与查询 |
| `codegen-datasources.delete` | 批量；仍被表配置引用的逐项拒绝（`CODEGENERATION_CONFLICT_002`） |
| `codegen-datasources.test-connection` | 按当前设置临时连一次（停用的也能测），记下时间、结果与说明；连不上写在结果里，不是失败的调用 |

- 支持 MySQL、SQL Server、PostgreSQL、Oracle、SQLite。显式连接串优先，没有就按主机、端口、库名、账号拼出，附加参数追加在拼出的连接串之后；SQLite 的库名是库文件路径。
- 口令与连接串经 `ISecretProtector`（数据保护）加密，解不开即 503 `CODEGENERATION_UNAVAILABLE_001`，要重新填写（数据保护密钥变了时会这样）。
- 连接经框架的运行时数据源（`DataSourceRegistry`）：首次使用时按定义登记一个连接（名字 `codegen-<ID>`，每个数据源一条健康项 `persistence.datasource.codegen-<ID>`），修改、启停与删除之后注销，下次使用按新设置重建。连不上、连接串不合格式由框架报 `PERSISTENCE_*`，原样返回。

与 5.x 不同（按缺陷修，方案 C2）：口令与连接串 5.x 用写死密钥的 AES、解不开还把原值当明文用；删除不查引用；测试连接只要查看权限却写库；详情把连接串回成 `******`，编辑后原样提交会把占位符存成连接串；编辑弹窗里改不了状态（5.x 的启停接口没有入口）。

## 表配置与列配置

| 操作 | 说明 |
| --- | --- |
| `codegen-tables.db-tables` | 分页列出主库或某个启用的数据源里的表；关键字匹配表名与注释，分表折叠成逻辑名，页码换算成跳过与取的条数交给框架 |
| `codegen-tables.import` | 导入一批表（1 到 100 张），按表隔离：读不到、已导入、没有列的各记一条失败 |
| `codegen-tables.sync-schema` | 按最新库结构同步：结构字段以库为准，推断字段没被人工改过才跟随新的推断，新列补进来、库里已删的列删掉列配置 |
| `codegen-tables.list` / `get` / `options` / `update` / `set-status` / `delete` | 表配置的增改删查；修改时值变了的被跟踪字段记为人工改过；删除连带列配置 |
| `codegen-table-columns.list` / `save` | 一张表的列配置；整体保存，选项来源（字典、枚举、常量、关联表、关联树）只留它要的那个配置 |

- 读库结构用框架的 `ISchemaReader`（主库传空，外部库传数据源的连接定义），表名与列名照返回的原名，不自己转大小写。
- 类型按「库 + 类型名」映射：长度只给字符与二进制类型，Oracle 的 `NUMBER` 按精度与小数位分整数与定点数、`DATE` 带时分秒，SQL Server 的 `rowversion` 是二进制；`long` 在前端是数字字符串。PostgreSQL、MySQL、SQL Server、Oracle 在真实库上实测（`CodeGenerationSchemaTests`，`XIHAN_TEST_POSTGRES` / `MYSQL` / `SQLSERVER` / `ORACLE`）。
- 推断照 5.x：类名去掉表名前缀（`BasicApp:CodeGeneration:TablePrefixes`，缺省 `sys_`）转 PascalCase，业务名取表注释；字符列按列名语义推控件（图片、文件、备注……），名称、编码、状态、类型、时间、日期类的列缺省可查询；有父级列与名称列即树表。实体基类的列（主键、租户、审计、软删与并发戳）不进表单与列表。
- 「沿用已有实体」：主库里已登记实体的表（框架的 `IEntityCatalog`），类名、命名空间、包名与属性名取实体，枚举属性推为枚举选项，生成目标缺省「加入已有包」。外部库的同名表不算。
- 数据库类型按来源取：主库取主库的连接，外部库取数据源记录（5.x 在导入弹窗里让人选，选错了类型映射就错）。

## 模块开关

`BasicApp:Modules:CodeGeneration:Enabled=false` 时宿主不装配 CodeGeneration：权限码与页面不下发、操作 404，五张表照样建。

## 后续

- 表与列配置：经框架的 `ISchemaReader` 读库结构导入、同步结构（人工改过的字段保留）、列配置整体保存；「沿用已有实体」按 6.0 登记的实体（`IEntityCatalog`）识别。
- 模板：5.x 的 28 个 Scriban 模板按 6.0 写法重写，两种目标「新建包」与「加入已有包」，单表、树表、主从三种模式；生成的一半每次覆盖，手改的一半只在第一次写。
- 生成：产物包存成 Files 的临时文件（7 天）并记历史，写进项目只在 Development 且配置打开时。
- 零代码只读运行时：`codegen-runtime.get-schema` / `list`，按表配置的数据源查。
