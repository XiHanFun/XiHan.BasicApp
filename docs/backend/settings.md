# 系统设置

本页说明参数配置、字典与业务编号，适用于后端开发者与运营配置人员。

Settings 包（Tier 2）提供参数配置、字典与业务编号三类「运行时可调」的数据，并通过 `Contracts` 的 `IConfigValueReader` 与 `INumberGenerator` 供其他包使用。三类都是平台库实体、实现 `IOwnedByTenant`：平台一份，租户可以有自己的；读共享，写只在本侧。

::: tip 和 appsettings 的区别
`appsettings.json` 的 `BasicApp:*` 是部署配置（改了要重启，见[配置参考](../configuration)）；本页的参数是运营配置，存在库里、后台改了即生效，租户可以覆盖。
:::

## 参数配置

`SysConfig`：键、名称、分组、值、缺省值、数据类型、是否加密、是否内置、是否允许租户覆盖、状态、排序、说明。

| 规则 | 说明 |
| --- | --- |
| 键 | 3–128 位，创建后不可改；本侧唯一 |
| 数据类型 | `Text`、`Number`、`Boolean`、`Json`、`Array`，按类型校验值与缺省值 |
| 加密 | 值经 `ISecretProtector` 加密存放，不回显（`value` 为空、`hasValue` 标出）；更新时留空即保留原值；开关加密会就地加密或解密现值 |
| 租户覆盖 | 租户新建与平台参数同键的参数即覆盖它；平台参数 `allowTenantOverride = false` 时拒绝 |
| 生效值 | `IConfigValueReader`：只看启用的参数 → 当前租户的同键参数优先、其次平台的 → 值为空回落缺省值 → 加密值解密后返回 |

操作 `configs.create / get / list / update / delete`，权限 `settings.configs.*`（两侧）；内置参数不可删，也不能改数据类型。

### 内建参数

读参数的包在 `Configure` 里 `AddConfigCatalog` 登记 `ConfigDescriptor`（键的首段是包名），种子 `settings.configs` 据此写成平台的内建参数：名称、分组、数据类型、缺省值、是否允许租户覆盖、是否加密与说明每次对齐（没变不写，加密与否变了即就地加密或解密现值），值、状态与排序号只首写、之后归运营；值留空，生效值即缺省值。运营先手建了同键的平台参数即收为内建，但它现有的值不符合登记的数据类型时种子报 `SETTINGS_VALIDATION_002`（消息带参数键）、不改它，要由人把值改对。下表的参数都由种子建好，在参数配置页直接改值；数据类型固定，JSON 与数组类型的值保存时即校验。

### 代码读取的参数

| 键 | 谁读 | 类型 | 含义 |
| --- | --- | --- | --- |
| `identity.login-methods` | Identity | 数组 | 登录页开放的方式（只认平台的，不许租户覆盖）：`password`、`email`、`phone`、`passkey`（还要宿主配了 `XiHan:Security:WebAuthn`）；缺省 `["password"]` 即只有密码登录，写错即登录配置报错 |
| `identity.password` | Identity | JSON | 密码设置（只认平台的）：`forceChange` 缺省 `false`，打开后密码由他人设置的账号凭密码登录要先改密，见[统一认证](./authentication#强制修改由他人设置的密码) |
| `identity.impersonation` | Identity | JSON | 模仿登录（租户可覆盖）：`sessionMinutes` 缺省 30、`notifyTarget` 缺省 `true`；写错即不能发起，见[统一认证](./authentication#模仿登录) |
| `access.security.login-auto-ban` | Access | JSON | 登录失败自动封禁（只认平台的，缺省关闭），见[访问策略](./access-policy) |
| `auditing.retention-days` | Auditing | 数字 | 日志保留天数（1–3650，只认平台的）；不给缺省值，为空即配置项 `BasicApp:Auditing:DefaultRetentionDays`（缺省 180） |
| `notification.telegram` | Notification | JSON | Telegram 的总开关、Webhook 根地址、刷新周期、兜底回复与网络设置（只认平台的，缺省关） |
| `notification.telegram.webhook-secret-token` | Notification | 文本（加密） | Telegram Webhook 密钥（只认平台的） |
| `chat.policy` | Chat | JSON | 聊天策略（Chat 模块开着才有，敏感词租户可覆盖）：`retentionDays`（1–3650，缺省 365）与 `sensitiveWords`；写错即失败，不按错误的值删数据或放过敏感词 |

## 字典

`SysDict` + `SysDictItem`：字典码、名称、状态；字典项是同一字典内无环的树，带值、标签、排序、缺省项（字典内唯一，设新的即清掉旧的）与 JSON 元数据。

| 规则 | 说明 |
| --- | --- |
| 字典码 | 创建后不可改；平台新建要求全库无同码，租户新建要求平台与本租户都无同码（字典不覆盖） |
| 可见性 | 读共享：平台字典 + 本租户字典；租户改不了平台字典 |
| 停用 | 停用的字典不再出现在下拉选项里 |
| 内建 | 各包登记的字典，不能删，登记的项也不能删（`SETTINGS_CONFLICT_006`） |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `dicts.create / get / list / update / delete` | `settings.dicts.*` | 管理字典 |
| `dict-items.create / get / list / tree / update / delete` | `settings.dict-items.*` | 管理字典项 |
| `dicts.options` | 登录即可 | 业务表单的下拉（`GET /api/dicts:options?code=`） |

`dict-items.list` 分页检索字典项（按 `dictId` 筛选某本字典，关键字匹配编码、名称与附加值），`dict-items.tree` 一次取整本字典的项（深度优先、带层级，同级按排序号再按编码，给上级选择与级联取值用）。`dicts.list / get / options` 与 `dict-items.list / tree` 也是 MCP / AI 工具。前端 SchemaPage 的字典列与壳层的 `dictApi.options` 都接在 `dicts.options` 上。

### 内建字典

业务按编码依赖某本字典的包在 `Configure` 里 `AddDictCatalog` 登记 `DictDescriptor`（编码的首段是包名，项的上级排在下级前面、缺省项至多一个），种子 `settings.dicts` 据此写成平台的内建字典：字典的名称、分类、说明与登记的项（名称、附加值、上级）每次对齐，没变不写；状态、排序号与缺省项只首写，之后归运营；运营另加的项不动，照常可删。运营先手建了同码的平台字典即收为内建；租户已有同码的字典时种子报 `SETTINGS_CONFLICT_002`、不改它，要由人处理。目前没有包登记内建字典。

演示字典由种子 `settings.demo` 在演示开关打开时写入，不是内建的。

## 业务编号

`SysNumberingRule` + `SysNumberingAllocation`：编码、前缀、分隔符、日期格式（`None`、`Yyyy`、`YyyyMM`、`YyyyMMdd`、`YyMM`、`YyMMdd`、`MMdd`）、流水长度、重置周期（`Never`、`Daily`、`Monthly`、`Yearly`）、时区（缺省 `UTC`）。

- 发号在独立的作用域与事务里完成（`StoreScope`），号一经发出永久消耗、外层调用失败也不回收、允许空洞；先对规则行加锁更新，同一规则的发号由此串行，并发发号不重复。
- 幂等键：同一幂等键参数相同即重放（`isReplay`），不同即 `SETTINGS_CONFLICT_013`。
- 日期格式要能区分重置周期，时区要能解析，流水位数要装得下已发出的流水值。
- 租户看得见自己的规则与平台标为「供租户使用」的规则，只能改自己的；编码在所属一侧内唯一。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `numbering-rules.create / get / list / update / delete` | `settings.numbering-rules.*` | |
| `numbering-rules.preview` | `settings.numbering-rules.read` | 按格式在当前时刻拼出号码，不读库、不发号 |
| `numbering-rules.generate` | `settings.numbering-rules.generate` | 发号（也可由其他包经 `INumberGenerator` 调用） |
| `numbering-rules.allocations` | `settings.numbering-rules.read` | 发号记录 |
| `numbering-rules.reset` | `settings.numbering-rules.reset` | 重置流水，理由必填（进操作审计） |

## 用户设置

个人偏好不在 Settings，而在 Identity 的 `SysUserSetting`（用户 × 场景 `Preference` / `Page` × 键，值是 JSON）：`user-settings.get` / `update`（只要求登录）。壳层的偏好、收藏、列表的个人视图与列设置、工作台看板布局都存在这里，改了经 `UserSettingChanged` 推到同一用户的其他设备。

## 页面

`src/modules/settings`：参数配置（租户行可「覆盖此参数」）、字典（主从两栏）、业务编号（预览与发号记录）。

## 下一步

- [配置参考](../configuration) · [消息通知](./messaging) · [访问策略与权限条件](./access-policy)
