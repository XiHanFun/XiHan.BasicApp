# 代码生成

本页说明代码生成的数据源、表与列配置、模板与生成的代码、交付、零代码运行时和退出验证，适用于用它生成业务包的开发者与维护模板的人。

CodeGeneration 是 Tier 3 的可选模块（`BasicApp:Modules:CodeGeneration:Enabled`，缺省开），只在平台一侧：管理外部数据源，按库表结构导入表与列配置，用 Scriban 模板生成一个 6.0 包与前端模块，记生成历史，另有零代码只读运行时。模型与规则沿用 5.x，写法换成 6.0 的；5.x 的代码生成文档见 [5.x 文档](https://basicapp.docs.xihanfun.com/v5/backend/code-generation)。

## 页面与权限

页面 `codegeneration.code-gen`（路由 `/codegeneration/code-gen`，菜单按 `codegeneration.codegen-tables.read` 下发），照 5.x 一页四签：表配置、数据源、模板、生成历史，各页签按自己资源的读权限显示。5.x 挂在「开发中心」目录下，6.0 的目录按包。

权限码只在平台一侧。5.x 是一组 `code_gen:{read,create,update,delete,import,execute}`，6.0 权限码的资源段要与操作一致，按资源拆开、动作照 5.x 各接口要的那个码：

| 资源 | 权限码 |
| --- | --- |
| 数据源 `codegen-datasources` | `read`、`create`、`update`（修改、启停、测试连接）、`delete` |
| 表配置 `codegen-tables` | `read`（也是页面读权限，含列出库表与预览）、`update`（修改、启停）、`delete`、`import`（导入、同步结构）、`execute`（生成代码） |
| 列配置 `codegen-table-columns` | `read`、`update`（整体保存） |
| 模板 `codegen-templates` | `read`（查看、检查语法）、`create`、`update`（修改、启停）、`delete` |
| 生成历史 `codegen-histories` | `read`（查看、下载产物包） |
| 零代码运行时 `codegen-runtime` | `read`（查看表结构与数据） |

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
- 类型按「库 + 类型名」映射：长度只给字符与二进制类型，Oracle 的 `NUMBER` 按精度与小数位分整数与定点数、`DATE` 带时分秒，SQL Server 的 `rowversion` 是二进制；`long` 在 6.0 的报文里是数字，TypeScript 是 `number`、控件是数字框（5.x 由全局转换器写成字符串）。PostgreSQL、MySQL、SQL Server、Oracle 在真实库上实测（`CodeGenerationSchemaTests`，`XIHAN_TEST_POSTGRES` / `MYSQL` / `SQLSERVER` / `ORACLE`）。
- 推断照 5.x：类名去掉表名前缀（`BasicApp:CodeGeneration:TablePrefixes`，缺省 `sys_`）转 PascalCase，业务名取表注释；字符列按列名语义推控件（图片、文件、备注……），名称、编码、状态、类型、时间、日期类的列缺省可查询；有父级列与名称列即树表。实体基类的列（主键、租户、审计、软删与并发戳）不进表单与列表。
- 「沿用已有实体」：主库里已登记实体的表（框架的 `IEntityCatalog`），类名、命名空间、包名与属性名取实体，枚举属性推为枚举选项，生成目标缺省「加入已有包」。外部库的同名表不算。
- 数据库类型按来源取：主库取主库的连接，外部库取数据源记录（5.x 在导入弹窗里让人选，选错了类型映射就错）。

## 模板与预览

| 操作 | 说明 |
| --- | --- |
| `codegen-templates.list` / `get` | 分页（不带正文；分组、生成模式、写入方式、状态可筛选，关键字匹配编码、名称与描述）与详情（带正文与路径表达式） |
| `codegen-templates.validate` | 只解析不渲染，返回带行列的语法错误；正文可能很长，走 POST |
| `codegen-templates.create` / `update` / `set-status` / `delete` | 自有模板可增删改；编码唯一、不含空白、建好后不能改；内置模板不能改、不能删（`CODEGENERATION_CONFLICT_006`），可以启停 |
| `codegen-tables.preview` | 按表配置渲染，返回全部文件，不打包、不记历史；配置检查不过 `CODEGENERATION_VALIDATION_007`，模板渲染失败或产物越出根目录 `VALIDATION_008`（带模板编码），没有可用模板 `VALIDATION_009` |

- 内置模板随程序版本走：包里带的 `Seeds/Templates/**/*.sbn` 每次启动对齐到模板表（正文、名称、分组、生成模式、路径表达式、写入方式与排序），启停归运维、不动；要定制就新建一份自有模板。
- 选模板：显式给了编码就用那几个，否则取启用的通用模板与表的生成模式的模板；按生成范围裁（分组含 `backend` / `frontend`）；目标是「加入已有包」时不出包级模板（分组 `*-package`）；沿用已有实体时不出实体。一份模板渲染出空白即不产出这个文件（按勾选的操作一份模板一个文件）。
- 渲染用 Scriban：变量以 PascalCase 原名注入（<code v-pre>{{ ClassName }}</code>、<code v-pre>{{ for col in Columns }}</code>），带 5.x 的转义过滤器（`cs_string`、`xml_doc`、`ts_string`、`html_attr`、`html_text`、`block_comment`、`html_comment`、`i18n_message`、`js_literal`、`select_options`）与 `kebab` / `snake` / `camel`；不能引入别的文件，循环与递归有上限。
- 生成前的检查照 5.x，不过即整次失败：模块名与命名空间要合规；表已有手写实体时类名要与它一致；系统字典列要是文本、有字典码；选了枚举的列要解析得到、C# 类型就是它（5.x 出一个禁用的占位选项再告警）；关联列要是 `Guid`（5.x 要 long），关联的表要在、显示列要是文本，关联树的目标要是配好父级列的树表；唯一列不能是布尔或二进制；树表的父级列要是 `Guid`、名称列要进列表；主从的明细表要配好主表与外键列；勾了状态切换要有一列启停枚举（有 `Active` 与 `Disabled` 成员）且进列表；新建实体时，属性名为 `Status`、没选枚举的整数列也可以，生成器在实体里出 `<类>Status`（`Active` 0、`Disabled` 1）。

### 生成的后端代码

一张表生成一个资源：资源名是类名的 kebab 复数（`SalesOrder` → `sales-orders`），操作 `<资源>.list / get / create / update / delete / set-status / tree / <列>-options`，权限码 `<包>.<资源>.read / create / update / delete`（只在租户侧，写操作记审计），页面 `<包>.<资源>`（路由 `/<包>/<资源>`）。实体是租户库实体（`TenantScopedAuditedEntity`），表名或列名与按约定换算的不同时才写 `[SugarTable]` / `[SugarColumn]`，唯一列是部分唯一索引。

| 文件 | 写入方式 |
| --- | --- |
| `Domain/Entities/<类>.Generated.cs` | 每次覆盖 |
| `Domain/Entities/<类>.cs` | 只首次（partial，写计算属性与领域方法） |
| `Operations/<资源>/<类>Contracts.Generated.cs` | 每次覆盖：响应、请求与映射 |
| `Operations/<资源>/<类>Contracts.cs` | 只首次（partial） |
| `Operations/<资源>/<动作>.cs` | 只首次，按勾选的操作；树表另有树，关联列另有选项操作 |
| `Queries/<类>Queries.Generated.cs` | 每次覆盖：分页、筛选与排序的白名单（模糊查询进关键字，日期与创建时间按区间，选项列可多选） |
| `Queries/<类>Queries.cs` | 只首次：`Customize` 里写缺省条件 |
| `Operations/<资源>/<类>PrintDataSource.Generated.cs` | 每次覆盖，勾了打印才有 |

两种目标：

- **新建包**另出包级文件（工程文件、Feature、Capabilities、Permissions、Pages、Errors、Messages、JsonContext、README，都只首次）与 `Localization/<区域>.json` 共 10 种区域。按 Tier 3 登记，只依赖 Contracts 与 Runtime（用到别的包的枚举或关联实体时另加引用）。
- **加入已有包**只出资源的文件。

两种目标都出 `_registration/<资源>.md`：新建包写明加进解决方案、宿主（`UseFeature`、OpenAPI 分组）与架构测试 Tier 表的行；加入已有包按文件列出要合入的权限码、页面、错误码（序号待填）、消息键、JSON 序列化类型、Feature 登记与 10 种区域的文案。操作 ID 已被别的包占用时在开头提示。

生成的文案：固定用语（「新建」「删除」「查看」……）按 10 种区域内置，表注释与列注释在 10 种区域里原样使用（不机器翻译），生成之后按需改写。业务名、表注释与列注释当名字用或拼进句子时去掉句末的标点（6.0 实体的注释来自文档注释、句末带「。」，导入时业务名也取表注释），表配置里存的照原样。

### 生成的前端代码

照 5.x 按生成模式出三组模板（单表与主从共用一份，树表另一份），落在 `frontend-next/src/modules/<包>/views/<资源>/`：

| 文件 | 写入方式 |
| --- | --- |
| `index.vue` | 只首次：SchemaPage 列表页与新建 / 编辑弹窗（每个字段有可见标签），主表行展开出明细表 |
| `<资源>.schema.generated.ts` | 每次覆盖：字段、按钮、表单模型与缺省值、表单与请求的换算、导入换算、搜索字段到后端筛选的登记、SchemaPage 的资源 |
| `<资源>.schema.ts` | 只首次：在上面的 base* 之上做 transform，页面只从这里导入 |

- 取数走生成的契约客户端（`~/contracts` 的 `<资源 camel>`，类型取 `ResponseOf` / `RequestOf`），不再生成 5.x 的 `api/modules` 接口与类型文件；契约在后端登记之后导出、`pnpm gen:contracts` 生成。
- 控件由列的类型与配置单点决定（开关、日期、时间、上传、关联下拉、关联树、选项下拉、数字框、文本框），表单模型、缺省值、回填、提交与导入共用同一组判据（照 5.x）。日期按 `DateTimeOffset` 收发（5.x 发本地时间文本）；上传控件 `XFileRefUpload` 从 5.x 搬进公共组件，存文件库的文件 ID。
- 必填在提交前校验并挂到字段上，后端的字段错误也挂回字段（6.0 的弹窗写法；5.x 弹提示）；按钮按权限码门控：导出用读、导入用新建、启停用修改。
- 文案一律写键，键在 `<包>.<资源>` 下：页面标题（也是菜单标题键）、字段名、按钮、弹窗标题、必填提示与枚举选项（6.0 没有枚举元数据接口，选项静态写出，`Active` / `Disabled` 用固定用语，其余成员名原样）。
- 新建包另出 `setup.ts`（登记文案）与 `locales/<区域>.ts` 共 10 种区域；登记片段写明把模块名加进 `scripts/check-modules.mjs` 的 `KNOWN`，视图键 `<资源>/index` 要全局唯一。加入已有包时，登记片段按区域列出要合进 `locales` 的文案块。

## 生成与生成历史

| 操作 / 端点 | 说明 |
| --- | --- |
| `codegen-tables.generate` | 按表配置生成，交付方式缺省取表配置的（也可在请求里指定）；不论成败都记一条生成历史，成功才回写表的生成状态与时间 |
| `codegen-histories.list` / `get` | 生成历史：关键字匹配表名与批次号，结果可多选、生成时间按区间；详情带产物文件、用到的模板、表配置快照与失败原因 |
| `GET /api/codegen-histories/{id}/package` | 普通端点，以附件下载这次的产物包（Zip），要生成历史的读权限 |

- **打包下载**（照 5.x 的包结构）：每次覆盖的文件进 `_generated/`、只首次写的进 `_manual/`（各自下面再分 `backend/` 与 `frontend/`），登记片段在 `_registration/`，包根有 `README.txt`。产物包经 Files 的临时文件契约 `ITemporaryFiles`（Contracts）存成 7 天的临时文件（同导出中心），从上面的端点下载；写进项目、生成失败或已到期的历史没有产物包（`CODEGENERATION_NOTFOUND_005`）。5.x 把产物包以 Base64 放进生成的响应。
- **写进项目**只在 Development、且打开 `BasicApp:CodeGeneration:EnableGenerateToProject` 时可用，其余环境 `CODEGENERATION_VALIDATION_010`。位置取配置的 `BackendRootPath`（`backend-next`）与 `FrontendRootPath`（`frontend-next`），不接受表配置给的任意路径；全部产物定好位置、都落在所属根目录之内才动磁盘。每次覆盖的直接覆盖，只首次写的已存在即跳过（响应列出跳过的文件）；只首次写的文件已在、对应的生成部分却没有，说明是项目里原有的手写文件，整体拒绝。「加入已有包」要求 `src/<命名空间>` 下已有这个包。登记片段写在那一侧根目录的 `_registration/`。宿主的 `appsettings.Development.json` 照 5.x 打开写盘并指向两个工程。
- 失败的那条历史在独立的短事务里写（调用失败会回滚自己的写入），失败原因记错误码与调用方语言的文案。

## 零代码运行时

表配置的「运行时数据」按表配置只读查看表里的数据，不生成也不编译代码（照 5.x）。

| 操作 | 说明 |
| --- | --- |
| `codegen-runtime.get-schema` | 表结构：按列配置给出列名、数据行里的键（属性名的 camelCase）、显示名（列注释）与控件 |
| `codegen-runtime.list` | 分页只读数据，每页最多 500 条；只带配置过的列，键是属性名 |

- 表名只取已配置且启用的表配置，不接受请求给的表名；停用的表配置 409 `CODEGENERATION_CONFLICT_007`。
- 按表配置的数据源读：主库的表照 5.x 补上与实体读同口径的条件（有租户列的只看调用方这一侧的行，有软删列的不看已删的）；外部库的表不是 BasicApp 的表，照原样读。5.x 一律查主库。
- 按列配置逐列取、列名照原样加引号，大小写混合的表名与列名照原样查到；列配置里有、库里已没有的列读不出（503 `CODEGENERATION_UNAVAILABLE_003`，带表名与库的报错），先同步结构。数字、布尔按原类型，时间与 Guid 按文本，二进制按 Base64。PostgreSQL、MySQL、SQL Server、Oracle 在真实库上实测（`CodeGenerationSchemaTests`）。
- 5.x 的数据行按库里的列名给键，前端按属性名取，取不到；5.x 取 `*`（Oracle 的分页不接受 `*` 与行号并列），库里读不出时原样抛异常。

## 退出验证

生成的代码要跟得上各包的写法：改了 Feature、权限、页面登记、错误与消息目录、操作或 SchemaPage 的写法，要同时改内置模板（`Seeds/Templates/**/*.sbn`），再跑一次退出验证。前端产物照 lint 的换行规则出（含调用的对象一个属性一行、带嵌套的数组一项一行、含调用的三元式拆三行），前端的 lint 规则变了同样要对齐模板。它是手动触发的集成测试，不进缺省 CI：

```bash
cd backend-next
XIHAN_TEST_CODEGEN_COMPILE=<框架检出>/next/src/ \
  artifacts/pin/bin/XiHan.BasicApp.Integration.Tests/release/XiHan.BasicApp.Integration.Tests.exe \
  -class XiHan.BasicApp.Integration.Tests.CodeGenerationCompileTests
```

- 变量是框架源码的 `src/` 目录（同 `-p:XiHanFrameworkSrc`），没设即跳过；前端要先在本检出的 `frontend-next` 装好依赖。
- 把当前提交的 `backend-next` 与 `frontend-next` 导出到临时目录，四张演示表各配成新建包：树表 Catalog（带生成的启停枚举）、单表 Shop（关联树、常量下拉、系统字典、图片上传、时间、布尔、日期时间与定点数）、主从 Sales / SalesItems，按「生成到项目」写进去。
- 照生成的登记片段登记进宿主（工程引用、`UseFeature`、OpenAPI 分组）与前端模块门禁，宿主连同四个新包 Release 构建、警告即失败；导出契约、`pnpm gen:contracts` 之后前端 `pnpm check` 八项通过。

## 模块开关

`BasicApp:Modules:CodeGeneration:Enabled=false` 时宿主不装配 CodeGeneration：权限码与页面不下发、操作 404，五张表照样建。
