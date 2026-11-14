# 消息通知

本页说明站内通知与收件箱、消息模板、邮件与短信、群机器人与 Telegram 机器人，适用于后端开发者与运营配置人员。

Notification 包（Tier 2）负责以上全部。推送到浏览器经 Realtime 包的 `IRealtimePush`，见[实时推送](./realtime)。

## 站内通知

### 通知

`SysNotification`（租户库实体）：标题、正文（`Text` / `Markdown` / `Html`）、类型（`System`、`Security`、`Business`、`Todo`、`Emergency`）、优先级（`Low`、`Normal`、`High`、`Urgent`）、图标、链接、业务类型与 ID、投递渠道、目标、展示方式与生效 / 到期时间。HTML 正文渲染前净化：去掉脚本、事件处理器、`javascript:` 链接、`<style>` 与表单控件，行内样式保留。

| 维度 | 取值 |
| --- | --- |
| 目标 | `All`、`Role`、`Department`（含下级）、`User`；接收人经 `Contracts` 的 `IRecipientSource` 解析，再按接收人的通知偏好过滤 |
| 展示 | 普通、横幅（`IsBanner`）、弹窗（`IsPopup`） |
| 阅读 | 需确认（`NeedConfirm`）、强制阅读（`IsMandatory`，路由守卫据此拦截） |
| 渠道 | 站内之外可同时发邮件、短信、机器人 |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `notifications.list` / `get` / `read-stats` / `recipients` | `notification.notifications.read` | 阅读统计、接收人与阅读确认状态 |
| `notifications.create` / `update` / `delete` | `notification.notifications.create` / `update` / `delete` | 缺省存草稿；`publishImmediately` 另要发布权限 |
| `notifications.publish` | `notification.notifications.publish` | 解析接收人、写收件箱，提交后按人推 `ReceiveNotification`，并按渠道入队；进度经 `TaskProgress` 推给发布人（见[任务进度](./realtime#任务进度)） |
| `notifications.remind` | `notification.notifications.publish` | 再推给未读或未确认的接收人 |

### 收件箱（我的消息）

`SysUserNotification` 是按人的投递记录。全部是自助操作（只要求登录）：

| 操作 | 说明 |
| --- | --- |
| `inbox.list` | 我的消息页与顶栏通知中心 |
| `inbox.banners` / `inbox.popups` / `inbox.mandatory` | 未读横幅、还没弹过的弹窗、待强制阅读 |
| `inbox.mark-read` / `mark-all-read` / `confirm` / `mark-popup-shown` | 已读、全部已读、确认、弹窗已展示 |
| `inbox.delete` | 删除自己的投递 |

别的包直接给某些人投递站内信（工作流的待办、Identity 的安全提醒）经 `Contracts` 的 `IInboxDelivery`。

## 消息模板

`SysMessageTemplate`（平台库实体，`IOwnedByTenant`）：编码、渠道、主题、正文、是否 HTML、变量说明。模板引擎是 Scriban，变量写作 `{{name}}`。

| 渠道 | 主题 | HTML |
| --- | --- | --- |
| `Email` | 有 | 有 |
| `InApp` 站内通知 | 有（作通知标题） | — |
| `Sms` | — | — |
| `Bot` | — | — |

- 租户优先、平台回退：发送时先找当前一侧启用的同编码模板，再找平台的；都没有即该渠道不可用。
- 租户的模板列表同时列出平台模板（`isPlatform`），平台模板对租户只读（改删给 `NOTIFICATION_DENIED_002`），新建同编码即覆盖。
- 平台种子 `notification.templates` 写 9 个，只首写：登录验证码邮件与短信、验证码（绑定 / 换绑 / 两步验证）邮件与短信、重置密码邮件、注册欢迎邮件、系统通知的邮件与短信，以及机器人消息 `notification-bot`（变量 `title / content / link / brand`）。`brand` 取 `BasicApp:Notification:Brand`，不写即应用名。
- 模板不加载别的文件，循环与递归有上限，语法树限深（`TemplateLimits`，保存与渲染前都判），一次渲染最多 5 秒；HTML 模板里的参数值先做 HTML 编码。

操作 `message-templates.list / get / create / update / delete`，权限 `notification.message-templates.*`。

## 邮件与短信

| 配置 | 实体 | 操作 / 权限 | 说明 |
| --- | --- | --- | --- |
| 邮件 | `SysEmailConfig` | `email-configs.*` / `notification.email-configs.*` | SMTP（MailKit），多通道、`set-default` 设缺省通道；连接过[出站地址策略](../configuration#xihan-security-outbound)，内网中继要放行 |
| 短信 | `SysSmsConfig` | `sms-configs.*` / `notification.sms-configs.*` | 阿里云（Dysmsapi）、腾讯云，直接签名调用、不引 SDK；模板编码到服务商模板的映射在配置里登记 |

- 租户没有可用通道时回落平台的配置（平台代发）；机器人不回落。
- 密钥经 `ISecretProtector` 加密存放，不回显。
- 发送记录 `SysEmail` / `SysSms` 本身即发件箱：`emails.list / get / resend / cancel / delete`、`sms.*` 同（权限 `notification.emails.read / resend / delete`，`cancel` 用 `resend`；短信同理）。验证码（业务类型 `identity.verification`）的记录不返回正文与参数，发送结束即清空，也不能重发：读记录的权限不能拿它接管账号。
- 发送记录按平台参数 `notification.retention-days`（缺省 180 天）保留：作业 `notification.messages.cleanup` 每天删掉更早的、已结束（已发送、失败、已取消）的邮件、短信与机器人消息记录，连同 Telegram 出站审计；排队中与发送中的再老也不动。
- 改通道的地址（SMTP 的主机、端口，关掉加密或不再验证书）时，留空的口令不再保留，要重新填（`NOTIFICATION_VALIDATION_016`）。AI 提供商的地址（`AI_VALIDATION_107`）、代码生成数据源的地址（`CODEGENERATION_VALIDATION_014`）同样。
- 发送循环按 `BasicApp:Notification` 的 `DispatchInterval`、`RetryDelay`、`MaxAttempts`、`StuckAfter` 工作；启动时把上个进程发到一半的记录改回排队中。

身份相关的发码（验证码登录、换绑、双因素）经 `Contracts` 的 `IMessageSender` 按模板入队。

## 群机器人

`SysBotConfig`：钉钉、飞书、企业微信群机器人的 Webhook（含签名密钥），两侧都能配，多个、每个服务商可设缺省。通知选了机器人渠道时按模板 `notification-bot` 渲染，给本侧每个服务商的缺省机器人各写一条；平台一侧另给每个启用的 Telegram 机器人的广播群各写一条。发送记录 `SysBotMessage` 与邮件短信走同一个发件箱与发送循环。

Webhook 地址带凭据：经 `ISecretProtector` 加密存放，响应只给主机（`https://oapi.dingtalk.com/***`），修改时交回这个值即保留原地址；差异日志打码，请求日志不记。解不开（数据保护密钥变了，或是没加密的数据）时不当明文用：响应只给 `***`，发送失败并在发送记录里写明原因，交回遮过的值报 503 `NOTIFICATION_UNAVAILABLE_004`，重新填完整地址即恢复。

操作 `bot-configs.list / get / create / update / set-default / delete`，权限 `notification.bot-configs.*`。

## Telegram 机器人

`SysTelegramBot`：平台托管（权限码只在平台一侧），多个机器人实例，令牌加密存储、不回显，带管理员、群组白名单、命令白名单与广播群。

- 平台的系统参数 `notification.telegram`（JSON）管总开关（缺省关）、Webhook 对外根地址、刷新周期与网络（代理、Bot API 根地址、超时），运行中改了下一个刷新周期生效；自建 Bot API Server 的主机要在 `XiHan:Security:Outbound:AllowedHosts` 放行。
- 配了 Webhook 根地址即走普通端点 `POST /api/telegram-bot/webhook/{botName}`，校验 secret token（加密参数 `notification.telegram.webhook-secret-token`）；没配即长轮询（`BasicApp:Notification:Telegram:PollingTimeoutSeconds`）。
- 交互按路由与守卫处理消息与回调，内置 `/start`、`/help`、`/myid`，可由处理器扩展；处理器登记有误（命令重复、正则写错）启动即失败。
- 出站发送带重试（`BasicApp:Notification:Telegram:Retry`），最终失败可私发管理员告警；回复文案在 `BasicApp:Notification:Telegram:Texts`。

操作 `telegram-bots.list / get / create / update / delete`，权限 `notification.telegram-bots.*`。

## 通知偏好

个人中心的 `profile.notification-preference` / `profile.update-notification-preference`（Identity）：按渠道与类别开关接收，两者都开着才收；没有设置过的用户缺省全收。偏好存在平台库，跟着账号走、不分租户；Notification 发布与扇出时经 `Contracts` 的 `INotificationPreferences` 过滤接收人。

## 页面

`src/modules/notification`：我的消息（收件箱）、通知公告、消息模板、邮件短信（发送记录）、邮件配置、短信配置、机器人配置、Telegram 机器人。通知正文的格式为 Markdown 时用 Markdown 编辑器（`XMdEditor`），其余格式用文本框。

## 下一步

- [实时推送](./realtime) · [缓存与异步](./caching) · [前端实时通信](../frontend/realtime)
