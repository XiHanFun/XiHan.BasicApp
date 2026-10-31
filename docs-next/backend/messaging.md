# 消息通知

Notification 包（Tier 2）负责站内通知与收件箱、消息模板、邮件 / 短信 / 机器人渠道与 Telegram 机器人。推送到浏览器经 Realtime 包的 `IRealtimePush`，见[实时推送](./realtime)。

## 站内通知

### 通知

`SysNotification`（租户库实体）：标题、正文（文本 / Markdown / HTML）、类型（`System`、`Security`、`Business`、`Todo`、`Emergency`）、优先级、图标、链接、业务类型与 ID、投递渠道、目标、展示方式与有效期。

| 维度 | 取值 |
| --- | --- |
| 目标 | `All`、`Role`、`Department`（含下级）、`User`；接收人经 `Contracts` 的 `IRecipientSource` 解析 |
| 展示 | 普通、横幅（`IsBanner`）、弹窗（`IsPopup`） |
| 阅读 | 需确认（`NeedConfirm`）、强制阅读（`IsMandatory`，路由守卫据此拦截） |
| 渠道 | 站内之外可同时发邮件、短信、机器人 |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `notifications.list` / `get` / `read-stats` / `recipients` | `notification.notifications.read` | 阅读统计、接收人与阅读确认状态 |
| `notifications.create` / `update` / `delete` | `notification.notifications.create` / `update` / `delete` | 缺省存草稿；`publishImmediately` 另要发布权限 |
| `notifications.publish` | `notification.notifications.publish` | 解析接收人、写收件箱，提交后按人推 `ReceiveNotification`，并按渠道入队 |
| `notifications.remind` | `notification.notifications.publish` | 再推给未读或未确认的接收人 |

### 收件箱（我的消息）

`SysUserNotification` 是按人的投递记录。全部是自助操作（只要求登录）：

| 操作 | 说明 |
| --- | --- |
| `inbox.list` | 我的消息页与顶栏通知中心 |
| `inbox.banners` / `inbox.popups` / `inbox.mandatory` | 未读横幅、还没弹过的弹窗、待强制阅读 |
| `inbox.mark-read` / `mark-all-read` / `confirm` / `mark-popup-shown` | 已读、全部已读、确认、弹窗已展示 |
| `inbox.delete` | 删除自己的投递（5.x 没有，6.0 补齐） |

## 消息模板

`SysMessageTemplate`（`IOwnedByTenant`）：编码、渠道、主题、正文、HTML、变量说明。模板引擎是 **Scriban**（框架不再包装模板引擎），变量写作 `{{name}}`。

| 渠道 | 主题 | HTML |
| --- | --- | --- |
| `Email` | 有 | 有 |
| `InApp` 站内通知 | 有（作通知标题） | — |
| `Sms` | — | — |
| `Bot` | — | — |

- **租户优先、平台兜底**：发送时先找租户的同编码模板，再找平台的；模板停用或不存在即该渠道不可用。
- 租户的模板列表同时列出平台模板（`isPlatform`），平台模板对租户只读（改删给 `NOTIFICATION_DENIED_002`），**新建同编码即覆盖**。
- 平台种子（`notification.templates`）9 个：登录验证码、验证码（绑定 / 换绑 / 两步验证）、重置密码、注册欢迎、系统通知的邮件与短信版，以及机器人通知 `notification-bot`（变量 `title / content / link / brand`）。`brand` 取 `BasicApp:Notification:Brand`。

操作 `message-templates.*`，权限 `notification.message-templates.*`。

## 邮件与短信

| 配置 | 实体 | 说明 |
| --- | --- | --- |
| 邮件 | `SysEmailConfig` | SMTP（MailKit），多通道、缺省通道 |
| 短信 | `SysSmsConfig` | 阿里云（Dysmsapi）、腾讯云（SMS 2021-01-11），直接签名调用、不引 SDK |

- 租户没有可用通道时**回落平台的配置**；机器人不回落。
- 密钥经 `ISecretProtector` 加密存放，不回显。
- 发送记录 `SysEmail` / `SysSms` 本身就是发件箱：`emails.*` / `sms.*` 查询、取消、重发、删除（权限 `notification.emails.*` / `notification.sms.*`）。
- 发送循环按 `BasicApp:Notification` 的 `DispatchInterval`、`RetryDelay`、`MaxAttempts`、`StuckAfter` 工作。

身份相关的发码（验证码登录、换绑、双因素）经 `Contracts` 的 `IMessageSender` 按模板入队。

## 群机器人

`SysBotConfig`：钉钉、飞书、企业微信群机器人的 Webhook（含签名密钥），多个、可设缺省。通知选了机器人渠道时按模板 `notification-bot` 渲染后广播，发送记录 `SysBotMessage` 与邮件短信走同一个发件箱与发送循环。

操作 `bot-configs.*`，权限 `notification.bot-configs.*`。

## Telegram 机器人

`SysTelegramBot`：多个机器人实例，各自 Webhook 或长轮询。

- Webhook 是普通端点 `POST /api/telegram-bot/webhook/{botName}`，校验 secret token；
- 交互按路由与守卫处理消息与回调，内置 `/start`、`/help`、`/myid` 等命令，可由处理器扩展；
- 出站发送带重试（`BasicApp:Notification:Telegram:Retry`），最终失败可通知管理员；
- 回复文案在 `BasicApp:Notification:Telegram:Texts`。

操作 `telegram-bots.*`，权限 `notification.telegram-bots.*`。

## 通知偏好

个人中心的 `profile.notification-preference` / `update-notification-preference`：按渠道与类别开关接收，两者都开着才收；没有设置过的用户缺省全收。偏好存在平台库，跟着账号走、不分租户。

## 页面

`src/modules/notification`：通知公告、发送记录（邮件 / 短信）、消息模板、邮件配置、短信配置、机器人配置、Telegram 机器人、我的消息（收件箱）。通知正文的格式为 Markdown 时用 Markdown 编辑器（`XMdEditor`，异步懒加载），其余格式用文本框。

## 下一步

- [实时推送](./realtime) · [缓存与异步](./caching) · [前端实时通信](../frontend/realtime)
