# 系统设置

本页说明参数配置、字典与业务编号，适用于后端开发者与运营配置人员。

Settings 包（Tier 2）提供参数配置、字典与业务编号三类「运行时可调」的数据，并通过 `Contracts` 的 `IConfigValueReader` 与 `INumberGenerator` 供其他包使用。三类都是平台库实体、实现 `IOwnedByTenant`：平台一份，租户可以有自己的。

::: tip 和 appsettings 的区别
`appsettings.json` 的 `BasicApp:*` 是部署配置（改了要重启，见[配置参考](../configuration)）；本页的参数是运营配置，存在库里、后台改了即生效，租户可以覆盖。框架的 `Configuration.*`（远程配置、Nacos）只用于远程配置与密钥，不与参数混用。
:::

## 参数配置

`SysConfig`：键、名称、分组、值、缺省值、数据类型、是否加密、是否内置、是否允许租户覆盖、状态、排序。

| 规则 | 说明 |
| --- | --- |
| 键 | 3–128 位，创建后不可改；本侧唯一 |
| 数据类型 | `Text`、`Number`、`Boolean`、`Json`、`Array`，按类型校验值与缺省值 |
| 加密 | 值经 `ISecretProtector` 加密存放，不回显（`value` 为空、`hasValue` 标出）；更新时留空即保留原值；开关加密会就地加密或解密现值 |
| 租户覆盖 | 租户新建与平台参数同键的参数即覆盖它；平台参数 `allowTenantOverride = false` 时拒绝 |
| 生效值 | `IConfigValueReader`：租户覆盖优先 → 停用视为不存在 → 没有值回落缺省值 → 加密值解密后返回 |

操作 `configs.create / get / list / update / delete`，权限 `settings.configs.*`（两侧）；内置参数不可删。

### 代码读取的参数

| 键 | 谁读 | 含义 |
| --- | --- | --- |
| `identity.login-methods` | Identity | 登录页开放的方式：`password`、`email`、`phone`；写错即登录配置报错 |
| `auditing.retention-days` | Auditing | 日志保留天数，没建即 180 天 |
| `access.security.login-auto-ban` | Access | 登录失败自动封禁（JSON，只认平台的，缺省关闭） |
| `notification.telegram`、`notification.telegram.webhook-secret-token` | Notification | Telegram 交互设置与 Webhook 密钥 |

## 字典

`SysDict` + `SysDictItem`：字典码、名称、状态；字典项是同一字典内无环的树，带值、标签、缺省项（字典内唯一，设新的即清掉旧的）与 JSON 元数据。

| 规则 | 说明 |
| --- | --- |
| 字典码 | 创建后不可改；平台新建要求全库无同码，租户新建要求平台与本租户都无同码（字典不覆盖） |
| 可见性 | 读共享：平台字典 + 本租户字典 |
| 停用 | 停用的字典不再出现在下拉选项里 |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `dicts.*` / `dict-items.*` | `settings.dicts.*` / `settings.dict-items.*` | 管理 |
| `dicts.options` | 登录即可 | 业务表单的下拉（`GET /api/dicts:options?code=`） |

`dicts.list / get / options` 与 `dict-items.list` 也是 MCP / AI 工具。前端 SchemaPage 的字典列与壳层的 `dictApi.options` 都接在 `dicts.options` 上。

## 业务编号

`SysNumberingRule` + `SysNumberingAllocation`：前缀、分隔符、日期格式（`None`、`Yyyy`、`YyyyMM`、`YyyyMMdd`、`YyMM`、`YyMMdd`、`MMdd`）、流水长度、重置周期（`Never`、`Daily`、`Monthly`、`Yearly`）、时区。

- 发号在独立事务里完成，号一经发出永久消耗、允许空洞；同一规则行加锁，并发发号不重复（PostgreSQL 上验证过多键并发与同键争用）。
- 幂等键：同一幂等键参数相同即重放（`isReplay`），不同即 `SETTINGS_CONFLICT_013`。
- 日期格式要能区分重置周期，时区要能解析。
- 租户可使用平台标为「供租户使用」的规则。

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `numbering-rules.create / get / list / update / delete` | `settings.numbering-rules.*` | |
| `numbering-rules.preview` | `settings.numbering-rules.read` | 按格式在当前时刻拼出号码，不读库、不发号 |
| `numbering-rules.generate` | `settings.numbering-rules.generate` | 发号（也可由其他包经 `INumberGenerator` 调用） |
| `numbering-rules.allocations` | `settings.numbering-rules.read` | 发号记录 |
| `numbering-rules.reset` | `settings.numbering-rules.reset` | 重置流水，理由必填（进操作审计） |

## 用户设置

个人偏好不在 Settings，而在 Identity 的 `SysUserSetting`（用户 × 场景 × 键，值是 JSON）：`user-settings.get` / `update`（只要求登录）。壳层的偏好、收藏、列表的个人视图与列设置、工作台看板布局都存在这里，改了经 `UserSettingChanged` 推到同一用户的其他设备。

## 页面

`src/modules/settings`：参数配置（租户行可「覆盖此参数」）、字典（主从两栏）、业务编号。

## 下一步

- [配置参考](../configuration) · [消息通知](./messaging) · [访问策略与权限条件](./access-policy)
