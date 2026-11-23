# 插件

本页说明插件包的格式、安装前的核对、授予、启用与加载、插件的页面与接口文档、卸载、操作与权限和模块开关，适用于运营插件的平台管理员与编写插件的开发者。

Plugins 是 Tier 3 的可选模块（`BasicApp:Modules:Plugins:Enabled`，缺省开）：平台一侧上传 `.xhplugin` 包，核对通过、批准授予后登记安装。插件由框架的插件宿主隔离运行（缺省只允许进程外），宿主不编译、不执行插件源码；核对只读包，插件代码一行不跑。安装只登记包与授予，装好的插件缺省停用；启用后各节点按库加载，没有任何启用的插件时不起插件进程。

## 插件包

`.xhplugin` 是 zip：

| 路径 | 内容 |
| --- | --- |
| `xihan.manifest.json` | 框架清单：`kind` 为 `plugin`，写明 `isolation` 与 `entry`；`contributes.operations`、`contributes.permissions` 列出插件贡献的操作与权限码 |
| `basicapp.plugin.json` | 旁挂清单：短名、发布方、运行时、权限码的作用侧与审计、页面、回调、扩展点 |
| `runtimes/<运行时标识>/` | 进程外插件各平台的构建产物，入口是其中的 `<entry>`；Windows 上入口没有扩展名时找 `<entry>.exe`。加载时当前平台的这一目录铺到插件目录的根上，与框架清单放在一起 |
| `<entry>` | Wasm 插件的模块，在包根上 |
| `locales/<区域>.json` | 10 种区域的前端文案 |
| `pages/*.json` | 声明式页面的描述，由旁挂清单的 `pages[].view.file` 指向 |

读包的限制：包不超过 `BasicApp:Plugins:MaxPackageSizeMegabytes`；条目不超过 10000 个，解压后合计不超过包上限的 4 倍；条目只收相对的正斜杠路径，不许 `.`、`..`、反斜杠、盘符与重名；两份清单各不超过 1 MB、须是合法 UTF-8。

旁挂清单有未知字段即拒：

```json
{
  "pluginApi": "1",
  "name": "weather",
  "publisher": "Contoso",
  "runtimes": ["linux-x64", "win-x64"],
  "permissions": [{ "code": "weather.weather.read", "scope": "Tenant", "audited": false }],
  "pages": [
    {
      "code": "weather.forecast",
      "route": "/plugins/weather/forecast",
      "titleKey": "weather.pages.forecast",
      "icon": "cloud",
      "order": 10,
      "permission": "weather.weather.read",
      "view": { "kind": "schema", "file": "pages/forecast.json" }
    }
  ],
  "callbacks": [],
  "extends": []
}
```

权限码的 `scope` 取 `Platform` 或 `Tenant`（缺省 `Tenant`），`audited` 缺省 `false`。

## 安装前核对

`plugins.inspect` 按下表依次核对，第一处不过即返回它的错误：

| 核对 | 规则 | 错误 |
| --- | --- | --- |
| 包 | 不超过上限、读得了、条目安全、两份清单都在且合格式 | `PLUGINS_VALIDATION_101`；旁挂清单不合格式 `PLUGINS_VALIDATION_102`，框架清单不合格式返回框架的清单错误 |
| 旁挂清单版本 | `pluginApi` 为 `1` | `PLUGINS_VALIDATION_102` |
| 清单种类 | `kind` 为 `plugin` 且写了 `isolation`，隔离级别在 `BasicApp:Plugins:Isolations` 里，不贡献管道中间件 | `PLUGINS_VALIDATION_103` |
| 短名 | 合 `^[a-z][a-z0-9-]{1,31}$`；不是内置包名，也不是 `plugins`、`installed`、`custom`、`system`、`api`、`embed` | `PLUGINS_VALIDATION_104` |
| 入口 | 当前平台的运行时在 `runtimes` 里，可执行文件在包里，运行时目录里的文件不与包根上的同名；Wasm 查包根的模块 | `PLUGINS_VALIDATION_101` |
| 文案 | 10 种区域的 `locales/<区域>.json` 都在 | `PLUGINS_VALIDATION_101` |
| 操作 | 资源段等于短名或以 `<短名>-` 开头 | `PLUGINS_VALIDATION_105` |
| 权限码 | 与框架清单的 `contributes.permissions` 是同一组；合 `<短名>.<资源>.<动作>`，资源段是本插件某个操作的资源段 | `PLUGINS_VALIDATION_105` |
| 页面 | 码 `<短名>.<页>`、路由 `/plugins/<短名>/<页>`、有标题键；视图种类 `schema` 且文件在包里；所需权限码是本插件的或宿主已登记的 | `PLUGINS_VALIDATION_105` |
| 回调 | 码是宿主已登记的；有回调时框架清单的 `permissions` 要申请 `capability:kernel.pipeline` | `PLUGINS_VALIDATION_105` |
| 扩展点 | `extends` 为空 | `PLUGINS_VALIDATION_105` |
| 登记冲突 | 操作与权限码都还没被登记 | `PLUGINS_CONFLICT_102` |
| 已安装 | 清单 ID 与短名都没被已安装的插件占用 | `PLUGINS_CONFLICT_101` |

核对通过返回清单 ID、短名、版本、发布方、隔离级别、包的 SHA-256 与大小、贡献的操作 / 权限码 / 页面 / 回调，以及申请的授予。

## 授予

| 授予 | 来源 | 批准 |
| --- | --- | --- |
| 框架清单 `permissions` 的每一项 | 插件要的框架能力，如 `capability:kernel.pipeline` | 必须 |
| `config:<配置节>` | 框架清单 `contributes.config.section` | 必须 |
| `binding:<绑定名>` | 框架清单 `bindings` 的每一项 | 必须 |
| `callback:<权限码>` | 旁挂清单 `callbacks` 的每一项 | 必须 |
| `data-scope:all` | 插件主体看租户的全部数据（缺省数据范围为无） | 可选 |

安装（`plugins.install`）与改授予（`plugins.grant`）交回的授予要含全部必须项，缺了即 403 `PLUGINS_DENIED_101`；另外只能有可选项，其余即 400 `PLUGINS_VALIDATION_106`；重复的合并。改授予是整组替换，要交回读到的 `concurrencyStamp`，与当前的不同即 409 `PERSISTENCE_CONFLICT_*`。

## 操作与权限

| 操作 | 路由 | 权限 |
| --- | --- | --- |
| `plugins.list` | `POST /api/plugins:list` | `read` |
| `plugins.get` | `GET /api/plugins/{id}` | `read` |
| `plugins.inspect` | `GET /api/plugins:inspect?fileId=` | `install` |
| `plugins.install` | `POST /api/plugins:install` | `install` |
| `plugins.grant` | `POST /api/plugins/{id}:grant` | `grant` |
| `plugins.enable` | `POST /api/plugins:enable` | `update` |
| `plugins.disable` | `POST /api/plugins:disable` | `update` |
| `plugins.status` | `GET /api/plugins/{id}:status` | `read` |
| `plugins.delete-preview` | `GET /api/plugins/{id}:delete-preview` | `delete` |
| `plugins.delete` | `DELETE /api/plugins?ids=` | `delete` |
| `plugin-views.list` | `GET /api/plugin-views` | 登录即可 |

权限码 `plugins.plugins.{read,install,update,grant,delete}` 只在平台一侧，写操作记审计。`fileId` 是平台一侧经 `POST /api/files/upload` 上传的包。详情含贡献、申请与已批准的授予、全部版本（新的在前）。列表可筛选 `name`、`manifestId`（`eq` / `contains`）、`isEnabled`、`isolation`，排序 `name`、`manifestId`、`version`、`createdAt`。删除是软删除，连同版本与授予，并清掉对插件权限码的引用（见[卸载](#卸载)）；删了可以重装。接口文档分组 `plugins`。

插件、版本与授予分别存 `sys_plugin`、`sys_plugin_version`（包文件、SHA-256、大小与两份清单原文）、`sys_plugin_grant`，都在平台库。

## 启用与加载

每个节点的对齐服务把本节点加载的插件对齐到库里启用的插件：启动时（结构升级之后、种子之前）对齐一次，完成之前节点不就绪；启停、改授予、卸载提交之后本节点立即再对齐，等对齐完才应答。

| 环节 | 规则 |
| --- | --- |
| 解包 | 每个版本解到 `<CacheDirectory>/<清单 ID>/<版本 ID>/`，目录在就直接用；不在时从 Files 取包，按安装时的 SHA-256 核对后解开，对不上或存储读不出即 `PLUGINS_UNAVAILABLE_102`。当前平台的 `runtimes/<运行时标识>/` 铺到目录的根上，别的平台的不解 |
| 加载 | 按隔离级别交给对应的插件宿主（没装该宿主即 `PLUGINS_UNAVAILABLE_101`），带上批准的授予（`callback:` 与 `data-scope:` 由 BasicApp 自己解释，不交给宿主）。宿主把握手的消息目录、权限码与代理操作一次登记，插件的操作随即有 HTTP 路由；成功后贡献的权限码等接进来（见下节） |
| 文案核对 | 加载成功后核对插件的消息目录：接口文档的分组名与说明 `openapi.groups.<短名>.name` / `.description`、插件每个资源的标签名 `openapi.tags.<资源>.name` 要有 en-US（键里的连字符写成下划线），缺了即卸掉、记 `PLUGINS_VALIDATION_108` |
| 重新加载 | 版本或授予变了先卸后载；换版本失败即重载旧版本，新版本的错误码留着 |
| 停用、卸载 | 先撤回权限码与回调授予，再让宿主停掉插件；在途调用照常完成或以框架的 `PLUGINS_UNAVAILABLE_001` 结束 |
| 失败 | 只让这个插件失败：错误码记进 `lastErrorCode`，健康项 `basicapp.plugins` 降级；原因排除后再启用一次即重试 |
| 时限 | 一次对齐不超过 `ReconcileTimeoutSeconds`，超时没轮到的插件留到下一次 |

运行状态是各节点自己的：`plugins.status` 报应答节点是否已加载、加载的版本、宿主报告的生命周期状态与本节点最近一次的错误码；加载成功的插件另有框架宿主的健康项 `plugins.<清单 ID>`。

## 加载后的权限与主体

插件加载之后，它贡献的东西接进宿主现有的授权链与菜单，停用即撤回：

| 方面 | 规则 |
| --- | --- |
| 权限目录 | 旁挂清单的权限码带着作用侧与审计标记进权限目录：按作用侧下发、能授给角色与用户，`audited` 的操作写操作日志 |
| 套餐 | 租户侧的插件权限码不在任何套餐里，平台在套餐页放行后租户才用得上 |
| 回调授权 | 插件回调宿主操作时以插件主体执行（主体 ID 是清单 ID、认证方案 `Plugin`），只持有批准的 `callback:<权限码>`，照常经作用侧与套餐收窄，其余即 `SECURITY_DENIED_004` |
| 数据范围 | 插件主体在租户侧缺省什么都看不到（受数据范围约束的列表查不到行），批准了 `data-scope:all` 才不受限；平台侧不受限 |
| 菜单 | 旁挂清单的页面装好即进「插件应用」目录（`custom.plugin-apps`），与内置页面一样按「页面菜单归登记」对齐、可在菜单管理里定制；插件没在本节点加载时不下发，节点与运营的展示调整留着；卸载才删 |
| 文案 | 插件的失败、权限码的显示名与说明（`permissions.<码>.name` / `.description`）、操作标题按握手的消息目录翻译成调用方的语言 |

## 插件的页面与接口文档

`plugin-views.list` 给前端运行期用：本节点加载成功、调用方能打开的插件页面。每个插件给出短名、清单 ID 与加载的版本，每个页面给出页面码、路由、组件路径（与菜单节点上的一致）、标题键、图标、次序、所需权限码、视图种类与视图描述的 JSON 原文（包里 `view.file` 的内容，加载时读入），以及插件发布到 HTTP 的操作的路由。页面要的权限码按调用方当前的有效权限判定，没有可见页面的插件不列出。

每个加载成功的插件在接口文档里单独一组：组名是短名，归属是清单 ID，排在各包的分组之后（序号从 1000 起）。全量 `/openapi.json` 的 `x-tagGroups` 多出这一组，`/openapi/<短名>.json` 只含这个插件的操作，标题与说明取插件的消息目录；停用或卸载即撤回。导出的契约快照（`frontend/contracts`）不加载插件，只有内置操作。

`backend/samples/CrmPlugin` 是一个进程外插件示例（短名 `crm`）：按租户在进程内存客户，演示两份清单、握手的消息目录、前端文案与页面描述怎样写；集成测试用它走安装到卸载的全程。

## 卸载

`plugins.delete-preview` 先列出卸载会清掉什么：插件全部版本贡献过的权限码，以及各处对它们的引用。卸载（`plugins.delete`）在同一个工作单元里把这些引用清掉，重装同一个插件也不会让旧的授予复活：

| 引用 | 所在 | 卸载时 |
| --- | --- | --- |
| 角色与用户的授予 | 平台库的授予表（各租户的授予都在这里，按租户列区分） | 撤销，授予缓存随之失效 |
| 授予的生效时段与拒绝规则、授予上的条件 | 平台库 | 删掉 |
| 按权限码的委托 | 平台库 | 删掉 |
| 互斥约束的项 | 平台库 | 删掉；剩不到两个权限码的规则整条删掉 |
| 运营建的目录、外链与内嵌节点的门控权限码 | 平台库 | 去掉权限码，节点留着 |
| 套餐白名单 | 平台库 | 删掉，用到这些套餐的租户的门控缓存失效 |
| 待审的授权申请 | 各租户库 | 只列出、不改：申请是留档，批准时会因权限码不存在被拒 |

预览逐个租户库读授权申请，有一侧读不出即 `ACCESS_UNAVAILABLE_003`，不给出不全的清单。操作日志、权限变更日志与条件命中记录里的权限码是留档，不算引用；访问策略与约束规则的条件表达式里手写的权限码也不在此列，要运营自己改。插件的页面节点在卸载提交之后随菜单对齐删掉。

## 开关

```json
{ "BasicApp": { "Modules": { "Plugins": { "Enabled": false } } } }
```

关掉后宿主不装 Plugins 与进程外插件宿主，权限目录与操作都不在；三张表也不建（开发库的 `InitTables` 与正式库的表结构升级器都只按装配了的包的实体建表补列），关掉之前建好的表与数据留着，不删也不再补列。隔离级别、包上限、解包目录与对齐时限见[配置参考](../configuration#basicapp-plugins)。

## 下一步

- [架构总览](./introduction) · [文件与存储](./file) · [配置参考](../configuration#basicapp-plugins)
