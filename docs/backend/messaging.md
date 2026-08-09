# 消息中心 / 通知 / 实时聊天

XiHan.BasicApp 的消息能力横跨三块：**企业级消息中心（站内通知/公告）**、**多渠道消息投递（邮件/短信/机器人）** 与 **SignalR 实时（通知推送 + 在线聊天）**。它们共享一套模板、偏好门控与发件箱异步机制，源码集中在 `modules/XiHan.BasicApp.Saas` 的 `Application/AppServices/Messaging`、`Application/Services/Messaging`、`Domain/DomainServices/Messaging`、`Domain/Entities` 与 `Hubs` 下。

> 部署提示：消息中心涉及 `Sys_Notification`、`Sys_User_Notification`、`Sys_User_Notification_Preference` 与聊天四表等结构。全新库由 CodeFirst 创建；存量库新增或变更这些结构时应提供前向 `UpdateScripts`，详见[升级与迁移](./upgrade)。

## 全景

```text
                       发布 / 发送（AppService，[DynamicApi] REST）
                                     │
        ┌────────────────────────────┼────────────────────────────┐
        ▼                            ▼                             ▼
  站内通知（消息中心）          多渠道扇出                     在线聊天
  SysNotification            NotificationFanoutService        ChatAppService
   └ 展开 SysUserNotification  ├ 邮箱 → SysEmail  ┐             └ SysChatConversation
   └ 偏好门控                  ├ 短信 → SysSms    ├ 发件箱异步       + Member / Message
   └ 实时推送 ReceiveNotification└ 机器人 → 直发   ┘（IRedisDelayQueue） + Reaction
                                     │                             │
                                     ▼                             ▼
                              框架 Bot / Messaging 管道       SignalR 实时推送
                              （Email/Sms/DingTalk/…）        （ChatRealtimeMethods）
```

三条链路都遵循 BasicApp 全局约定：应用服务经 `[DynamicApi]` 直接暴露 REST（无 Controller），三级权限码门控，响应统一 `ApiResponse`，时间存 UTC 按 `X-Timezone` 头换算输出。

## 通道与业务类型常量

投递通道用 `[Flags]` 枚举 `MessageChannel` 表示，可按位组合：

| 值 | 枚举 | 含义 |
| --- | --- | --- |
| 1 | `SiteNotification` | 站内通知 |
| 2 | `Email` | 邮件 |
| 4 | `Sms` | 短信 |
| 8 | `Bot` | 机器人 |

与框架 Messaging 信封对接的通道字符串集中在 `SaasMessageChannelNames`（`email` / `sms` / `bot`，其中 bot 仅直发、不入发件箱重放），业务类型集中在 `SaasMessageBusinessTypes`（如 `message.notification`），模板编码集中在 `SaasMessageTemplateCodes`。三者都是**单一事实源**，禁止散落魔法字符串——新增通道/类型/模板码必须在此登记。

---

## 一、企业级消息中心（站内通知）

站内通知采用**内容表 + 投递关系表**分离设计，避免"通知 × 用户"直接笛卡尔积：

- `SysNotification`：一条通知的**内容主表**（正文、发布状态、目标范围、展示分级）。
- `SysUserNotification`：`通知 × 用户`的**投递关系表**，承载每人的已读/确认/删除状态。

### 内容主表关键字段（SysNotification）

| 维度 | 字段 | 说明 |
| --- | --- | --- |
| 分类 | `NotificationType` | 五类：`System`/`Security`/`Business`/`Todo`/`Emergency`（内容分类，与展示分级正交） |
| 优先级 | `Priority` | `Low`/`Normal`/`High`/`Urgent`，决定排序权重、紧急置顶、分级推送强度 |
| 正文格式 | `ContentFormat` | `Text`/`Markdown`/`Html`，决定前端渲染方式（默认 Markdown，配合 MdEditor） |
| 富文本 | `Content` | 大文本列；`Title` + `Icon` + `Link`（跳转） |
| 投递渠道 | `DeliveryChannels` | `MessageChannel` 位组合；**必含站内信**，可叠加 邮箱/短信/机器人 |
| 定向 | `TargetType` + `TargetValue` | `All`/`Role`/`Department`/`User`；非全员时 `TargetValue` 存目标 ID 数组（JSON） |
| 强制阅读 | `IsMandatory` | 有未读则拦截，须读毕方可进入系统 |
| 顶部横幅 | `IsBanner` | 置于页面顶部通知条（系统维护/版本升级等） |
| 登录弹窗 | `IsPopup` | 登录后弹窗，每用户仅弹一次（由 `SysUserNotification.PopupShownTime` 记录） |
| 确认 | `NeedConfirm` | 可选确认，记录 `ConfirmTime`，不阻断进入系统 |
| 有效期 | `StartTime` / `ExpirationTime` | 生效区间，到期自动隐藏 |
| 状态 | `IsPublished` | `false`=草稿 / `true`=已发布（发布后不可编辑/删除） |

`NotificationType`（五类分类）与 `NotificationPriority`（四级优先级）**正交**：分类用于按类筛选/图标着色，优先级决定强提醒；同一条紧急停服公告可以是 `Emergency` 类 + `Urgent` 级 + 顶部横幅 + 强制阅读。

### 投递关系表关键字段（SysUserNotification）

- `NotificationStatus`：`Unread`/`Read`/`Deleted`；`Deleted` 为预留状态位（查询侧已按其过滤），但当前 `UserInboxAppService`/`UserInboxDomainService` 尚未暴露"删除消息"的写入端点，也没有配套的物理清理定时任务——用户侧删除消息能力暂未接线。
- `ReadTime` / `ConfirmTime` / `PopupShownTime`：分别记录已读、确认、弹窗已展示时间。
- 唯一约束 `(TenantId, NotificationId, UserId)`；未读计数与消息中心列表各有覆盖索引。

### 发布 → 展开 → 门控 → 推送

发布走 `NotificationAppService.PublishNotificationAsync`（`[UnitOfWork(true)]` + 权限码 `SaasPermissionCodes.Message.Publish`），一个事务内完成：

1. **展开**：`NotificationDomainService` 按 `TargetType` 解析收件人（All/Role/Department/User），批量 `INSERT` `SysUserNotification` 行。
2. **偏好门控**：站内信落行前经 `FilterByPreferenceAsync` 过滤（见下节）；**强制阅读 / 紧急通知一律送达，不受门控**。
3. **多渠道扇出**：`NotificationFanoutService.FanoutAsync` 按 `DeliveryChannels` 落邮箱/短信行、调度机器人广播（见下节，与发布同事务保证原子一致）。
4. **实时推送**：提交后调 `IRealtimeNotificationService<BasicAppNotificationHub>` 推 `ReceiveNotification`——全员目标 `SendToAllAsync`，定向目标 `SendToUsersAsync`（收件箱即时刷新，无需等下次拉取；推送失败只记日志、不影响发布）。

### 运营闭环（发 N 读 M / 未读人员 / 催办）

- **发 N 读 M / 已确认 K**：发布结果返回 `RecipientCount`；管理侧另有专用统计接口 `NotificationQueryService.GetNotificationReadStatsAsync`，一次性给出 `RecipientCount`/`ReadCount`/`UnreadCount`/`ConfirmCount`（按 `ConfirmTime` 非空统计）+ `NeedConfirm`，供详情页展示阅读进度。
- **未读人员**：`GetNotificationUnreadUserPageAsync` 按通知 ID 分页查询未读用户（`NotificationStatus=Unread`），带出 `UserName`/`RealName`/`ReceivedTime`，可经通用导出机制导 CSV。
- **催办**：`RemindAsync(id)` 查出该通知未读用户（`NotificationStatus=Unread`），对**在线未读者**重新实时推送 `ReceiveNotification`——**不改库**，仅即时再提醒（同样受权限码 `Message.Publish` 门控）。

### 用户侧：收件箱 API（UserInboxAppService）

面向登录用户自己的站内信读取，全部方法以当前用户为作用域（`[Authorize]` 类级）：

- 列表 / 未读列表：`GetListAsync(unreadOnly)`。
- 强制阅读拦截：`GetMandatoryUnreadAsync()`（前端路由守卫据此拦截进入）。
- 顶部横幅：`GetBannerAsync()`；登录弹窗：`GetPopupAsync()` + `MarkPopupShownAsync`（保证仅弹一次）。
- 已读：`MarkReadAsync` / `MarkAllReadAsync`；确认：`ConfirmAsync`。

---

## 二、消息模板（Scriban 渲染）

`SysMessageTemplate` 承载**邮件/短信/站内通知**三渠道的内容模板，模板只管"内容如何渲染"，"发给谁/何时发"由投递链路决定。

| 字段 | 说明 |
| --- | --- |
| `TemplateCode` | 渠道内唯一标识（如 `auth-email-login-code`） |
| `Channel` | `MessageChannel`（站内通知/邮件/短信） |
| `Subject` | 主题模板（邮件主题/通知标题，短信不使用） |
| `Content` | 内容模板（Scriban 语法，大文本列） |
| `IsHtml` | 内容是否 HTML（邮件渠道有效） |
| `Status` | `EnableStatus` 启用/停用（停用则查找跳过，回退全局或调用方内置内容） |

**渲染引擎**：复用框架 `XiHan.Framework.Templating`，Scriban 语法。模板正文里的变量占位、条件、循环等写法见框架文档；此处内容里的双花括号占位形如 <code v-pre>{{ variable }}</code>。

**租户覆盖默认**：唯一约束 `(TenantId, Channel, TemplateCode)`；`TenantId=0` 为平台全局模板（共享读）。发送链路按 `(Channel, TemplateCode)` 查找，**当前租户模板优先 → 全局兜底**；查找结果走分布式缓存，模板增删改由 `MessageTemplateAppService` 触发缓存失效（`InvalidateMessageTemplateAsync`），渲染侧即时可见。

**渲染缺失即回退**：模板不存在/停用/渲染失败时，投递链路保留调用方原始内容继续处理（fail-safe，不阻断发送）。

种子模板编码见 `SaasMessageTemplateCodes`，例如认证链路的 `auth-email-login-code`（登录验证码）、`auth-password-reset`（找回密码，携一次性重置链接），以及扇出用的 `notification-email` / `notification-sms`。

---

## 三、多渠道扇出与异步投递

### 扇出：一次发布可达四渠道

`NotificationFanoutService.FanoutAsync` 按通知的 `DeliveryChannels` 逐位处理（站内信已由发布链路落行，此处处理邮箱/短信/机器人）：

| 渠道 | 收件人解析 | 联系方式校验 | 落地方式 |
| --- | --- | --- | --- |
| 邮箱 | 偏好门控 `Email` 渠道 | `Email` 字段非空 | **通知级渲染一次**（`notification-email`，变量 `title`/`content`/`brand`）后逐用户落 `SysEmail` 行走发件箱 |
| 短信 | 偏好门控 `Sms` 渠道 | `Phone` 非空 **且 `SysUserSecurity.PhoneVerified`** | 逐用户落 `SysSms` 行；行保留内部模板码 `notification-sms`，供云厂商 `TemplateMap` 映射 |
| 机器人 | 通知级广播（无用户维度） | 无 | UoW 提交后经 `IMessageDispatcher` 以 bot 通道**直发** |

要点：

- **邮箱只渲染一次**：`title`/`content`/`brand` 都是通知级变量、无用户维度，避免逐行重复渲染；模板缺失回退通知纯内容（`IsHtml` 按通知正文格式）。
- **短信 fail-closed**：未验证手机号不可信，一律跳过；`Content` 为纯文本兜底，云厂商发送须在短信配置 `TemplateMap` 中登记 `notification-sms` → 服务商模板码的映射，否则按纯文本尝试。
- **机器人不入发件箱**：无用户绑定，仅通知级广播；照 `DbMessageOutbox.OnCompleted` 先例在 **UoW 提交后**投递，失败仅记日志、不回滚已提交的发布。当前用户偏好里的 `ChannelBot` 开关留作未来"按用户投递"的门。
- **原子一致**：邮箱/短信落行在发布事务内，事务回滚则扇出行一并回滚，保证渠道扇出与站内信原子一致；发送本身由发件箱异步承载，不阻塞发布接口。

### 偏好门控（SysUserNotificationPreference）

每用户 1:1 的通知偏好（与 `SysUser` 一对一，独立表演进），按**渠道 × 类型**两组布尔开关门控：

- 渠道开关：`ChannelInApp`（默认开）、`ChannelEmail`（默认开）、`ChannelSms`（默认关）、`ChannelPush`（默认开）、`ChannelBot`（默认关，机器人需用户侧存在可达绑定才有意义）。
- 类型开关：`TypeAnnouncement` / `TypeTask` / `TypeApproval` / `TypeSecurity`（默认开）、`TypeMarketing`（默认关，营销可随时关闭，GDPR 合规）。个人中心（`ProfileAppService`）可读写全部五个开关，但当前 `NotificationDomainService.FilterByPreferenceAsync` 的门控只按 `NotificationType` 落到三档：`Security→TypeSecurity`、`Todo→TypeTask`，其余（`System`/`Business`/`Emergency`）一律落 `TypeAnnouncement`；`TypeApproval`/`TypeMarketing` 目前未接入任何 `NotificationType` 判定分支，是为未来业务类型预留的偏好位。
- 首次读取无记录时由应用层按默认值惰性创建。**强制阅读 / 紧急通知一律送达，不受门控**（安全告警建议始终开启）。

### 异步投递：落库 Pending → 发件箱 → 后台消费

邮件/短信统一异步，走业务层发件箱（Redis 延迟队列），不阻塞调用接口：

1. `MessageDeliveryService.CreateEmailAsync` / `CreateSmsAsync`：先渲染模板（若带 `TemplateCode`），落 `SysEmail`/`SysSms` 为 `Pending`，再 `DbMessageOutbox.EnqueueAsync(channel, entityId)` 入队（事务提交后才入队，确保 DB 行先可见）。
2. `MessageOutboxHostedService`（继承框架 `XiHanBackgroundServiceBase`）拉取消费：原子领取 `TryClaimForSendingAsync`（`Pending`/可重试 `Failed` → `Sending`，防重复发送）→ 经 `IMessageDispatcher` 分发到框架 Bot 管道发送 → 更新 `Sent`/`Failed`（失败重试，`MaxRetryCount` 默认 3）。
3. **启动恢复**：`ResetInFlightAndCollectPendingAsync` 把崩溃残留的 `Sending` 复位为 `Pending` 并重新收集在途/可重试行入队。

`SysEmail` 记录承载完整投递语义：`EmailStatus`（`Pending`/`Sending`/`Success`/`Failed`/`Cancelled`）、`RetryCount`/`MaxRetryCount`、`ErrorMessage`、`ScheduledTime`（定时发送）、`BusinessType`+`BusinessId`（关联业务实体）。将状态改回 `Pending`（`MessageAppService.UpdateEmailStatusAsync`）会重新入队。

> 队列/发件箱这套"提交后入队 + 原子领取 + 启动恢复"的模式是 BasicApp 后台异步的通用范式（导出、发件箱等复用），底层为 `IRedisDelayQueue<T>` + `XiHanBackgroundServiceBase`，消息路由见框架 [Messaging](https://framework.docs.xihanfun.com/packages/messaging)。

### 网关配置（全部 store 化，禁 appsettings）

邮件/短信/机器人的网关连接与凭据都存库、由前端配置页维护，运行期从 store 取默认并热切换：

| 配置实体 | 服务 | 关键字段 | 默认约束 |
| --- | --- | --- | --- |
| `SysEmailConfig` | `EmailConfigAppService` | `SmtpHost`/`SmtpPort`/`UseSsl`/`FromEmail`/`FromName`/`UserName`/`Password` | 每租户单一 `IsDefault`，`Password` 经 Data Protection 加密存储、读取永不回显 |
| `SysBotConfig` | `BotConfigAppService` | `Provider`（`DingTalk`/`Lark`/`WeCom`）/`WebhookUrl`/`Secret`/`Keyword` | 每 `(TenantId, Provider)` 单一 `IsDefault`，`Secret` 加密存储 |
| Telegram | `TelegramBotAppService` | Token（经 `ITelegramBotTokenProtector` 保护） | 独立于 Webhook 机器人（长轮询模型），单列成服务 |
| 短信 | `SmsConfigAppService` | 服务商 + `TemplateMap`（内部模板码 → 服务商模板码） | 每租户单一 `IsDefault`（跨服务商互斥，与邮件同范式；不同于机器人按服务商互斥） |

`SetDefaultXxxAsync` 保证同租户/服务商内默认互斥；配置变更热重载。机器人族的 `ChannelBot` 与 Webhook 机器人（钉钉加签/飞书签名，企微不用 `Secret`）由框架 store 在运行时映射为各 Provider 的 Options。

---

## 四、SignalR 实时（通知 + 在线聊天）

实时基于框架 `XiHan.Framework.Web.RealTime`：Hub 基类 `XiHanHub`、进程内连接管理 `IConnectionManager`、泛型 `IRealtimeNotificationService<THub>`（按用户/用户列表/组/全体推送）。详见 [Web.RealTime](https://framework.docs.xihanfun.com/packages/web-realtime)。BasicApp 侧有两个 Hub。

### 通知 Hub（BasicAppNotificationHub）

标注 `[AuthorizeHub]`。它**不暴露客户端可调方法、也不建组**：仅在连接/断开时刷新用户会话 `LastActivityTime`（补 HTTP 无状态的活跃度缺口）——在线用户状态 = 活跃会话（DB）+ 实时连接标记（`IConnectionManager`）。会话刷新失败只记日志，绝不阻断连接生命周期。通知**推送**（`ReceiveNotification` / `TaskProgress` / `ForceLogout` / `UserSettingChanged` 等，方法名见框架 `SignalRConstants.ClientMethods`）由服务端 `IRealtimeNotificationService<BasicAppNotificationHub>` 主动发起。

### 聊天 Hub（BasicAppChatHub）

标注 `[AuthorizeHub]`。设计上只承载**轻量实时语义**：会话组加入/退出（输入广播边界）与"正在输入"提示（不持久化）。所有**持久化操作**（发消息/撤回/已读）走 `ChatAppService`（REST），落库后再经 `IRealtimeNotificationService<BasicAppChatHub>` **按 userId 逐用户**广播（不依赖组，断线重连也不丢消息）。

Hub 服务端方法（客户端可调）：

```csharp
Task JoinConversation(string conversationId);   // 校验成员身份后加入组
Task LeaveConversation(string conversationId);  // 退出组
Task Typing(string conversationId);             // 向组内其他连接广播 ChatTyping
```

组命名：`ChatRealtimeMethods.ConversationGroup(id)` → `chat:conv:{id}`。每次 `JoinConversation` 都校验成员身份（`IsMemberAsync`），非成员静默拒绝。

### SignalR 契约（重要）

- **ID 一律用 `string`**：Hub 方法参数 `conversationId` 声明为 `string` 而非 `long`——雪花 ID 超过 JS `number` 的 2^53 精度，客户端只能传字符串；Hub 内 `TryParse` 到 `long`（`> 0` 才通过，否则 fail-closed 静默拒绝）。
- **无自动转换器**：链路**没有** long→string 或枚举→string 的自动转换器。**载荷需应用侧手动投影**：服务端推送的 payload 是显式构造的匿名对象（如 `Typing` 的 `{ conversationId, userId, userName }`、通知的 `{ notificationId, notificationType=(int)…, … }`），枚举手动转 `int`、long 手动转 `string`。
- **前端 `useSignalR` 多实例**：两个 Hub 各建一条连接（各自 `useSignalR` 实例），互不复用。前端消费时同样需按契约手动投影/解析 payload。

客户端回调方法名集中在 `ChatRealtimeMethods`（应用级聊天专用）：

| 回调方法 | 触发 | 载荷要点 |
| --- | --- | --- |
| `ReceiveChatMessage` | 新消息 | 消息项 + 会话摘要 |
| `ChatMessageRecalled` | 撤回 | `conversationId`, `messageId` |
| `ChatMessageEdited` | 编辑 | `conversationId`, `messageId`, `content`, `editedTime` |
| `ChatReactionChanged` | 表情回应增删 | `messageId`, `emoji`, `userId`, `added` |
| `ChatReadPositionChanged` | 已读位变更（群已读回执） | `conversationId`, `userId`, `lastReadMessageId` |
| `ChatConversationChanged` | 成员增删/进退群 | `conversationId`, `changeType`（提示客户端刷新会话列表） |
| `ChatTyping` | 对端输入中 | `conversationId`, `userId`, `userName`（组广播，不持久化） |

---

## 五、在线聊天数据模型与能力

### 数据模型

| 实体 | 角色 | 关键字段 |
| --- | --- | --- |
| `SysChatConversation` | 会话聚合根（单聊/群聊/部门群） | `ConversationType`（`Single`/`Group`/`Department`）、单聊 `PairKey`（`{小ID}_{大ID}` 租户唯一）、群 `OwnerUserId`、部门群 `DepartmentId`、冗余 `MemberCount`/`LastMessageId`/`LastMessageTime`/`LastMessagePreview`、`Announcement` |
| `SysChatConversationMember` | 成员 × 会话的每人状态 | `MemberRole`（`Owner`/`Admin`/`Member`）、`UnreadCount`、`LastReadMessageId`/`LastReadTime`（已读位）、`IsMuted`（免打扰）、`IsPinned`（个人置顶）、`IsSilenced`（禁言，禁发/禁编辑） |
| `SysChatMessage` | 单条消息（追加式 + 撤回标记） | `MessageType`（`Text`/`Image`/`File`/`System`）、`Content`、`Attachments`（JSON `{fileId,fileName,fileSize}`）、`IsRecalled`/`RecallTime`、`EditedTime`、`ReplyToMessageId`/`ReplyPreview`、`MentionedUserIds`、`IsPinned`/`PinnedByUserId`、`ClientMessageId`（前端生成用于去重/多端对齐）、`SenderUserName`（发送时快照） |
| `SysChatMessageReaction` | 消息表情回应（切换语义） | `MessageId`、`UserId`、`Emoji`（Unicode），唯一 `(TenantId, MessageId, UserId, Emoji)`，取消即删行 |

会话三态：`Single` 用 `PairKey` 定位（open 即取即建）；`Group` 有属主与成员治理；`Department` 随组织结构自动建群。系统消息（`MessageType=System`，`SenderUserId=0`）承载进退群/撤回等时间线提示。消息表**只追加 + 撤回打标**，不支持软删（按保留天数清理），也不做按月分表（会破坏按 ID 更新）。

### 能力（ChatAppService）

`[DynamicApi]` 暴露，权限码分三档：`Chat.Read`（读/个人会话设置）、`Chat.Send`（发/撤回/编辑/表情/Pin）、`Chat.Manage`（建群/成员/治理）。持久化走领域服务（UoW），实时推送**尽力而为**、在提交后进行（失败不回滚）。

| 分类 | 能力 |
| --- | --- |
| 会话 | 打开单聊 / 部门群、建群、更新群信息（名/公告/头像）、转让群主、个人置顶/免打扰会话（仅推自己，多端同步） |
| 成员 | 加/移成员、设成员角色、设禁言（治理操作自动生成系统消息） |
| 消息 | 发送、**限时撤回（2 分钟内）**、**限时编辑（5 分钟内，仅文本）**、回复（`ReplyToMessageId` + 预览快照）、@提及（`MentionedUserIds`，发送时校验，单条上限 20 人）、Pin/Unpin（每会话最多置顶 10 条）、表情回应（切换）、标记已读（广播已读位到全体成员，群已读回执） |

**限时窗口**在领域服务 `ChatDomainService` 中硬编码：撤回窗口 `RecallWindowMinutes = 2`、编辑窗口 `EditWindowMinutes = 5`；仅可操作自己发送的消息，已撤回不能编辑，被禁言成员不能发/编辑。

### 聊天审计（第四档权限，合规查询）

独立权限码 `Chat.Audit`（`saas:chat:audit`），由 `ChatAuditQueryService.GetChatMessagePageAsync` 承载：管理侧**跨会话**分页查询聊天消息（按会话/发送人/发送时间区间/关键字过滤，关键字匹配正文、发送人名、附件文件名），默认排除已撤回消息（`IncludeRecalled` 可选纳入），批量带出会话名称/类型免逐行 JOIN。菜单页 `/message/chat-audit`（`message.chat-audit`）独立于普通聊天入口，供管理员做内容合规审计，不占用 `Chat.Read`/`Chat.Send`/`Chat.Manage` 三档。

**敏感词拦截**：发送/编辑文本前经 `IChatSensitiveWordGuard.EnsureAllowedAsync` 校验，命中即 fail-closed 抛业务异常拒绝。词库取自系统设置 `SysConfig` 键 `saas:chat:sensitive-words`（全局 `TenantId=0`，换行/中英文逗号/分号分隔，空=关闭），进程内缓存 60 秒，`OrdinalIgnoreCase` 包含匹配。

---

## 现状边界（如源码所示）

- **短信网关**：投递链路、`SysSms` 记录、`SmsConfig` + `TemplateMap` 齐备，但**内置服务商网关适配**依赖外部厂商配置（阿里/腾讯等），需在短信配置登记服务商模板映射；未映射则按纯文本尝试。
- **机器人**：Webhook 机器人（钉钉/飞书/企微）与 Telegram 已能作**通知级广播**；`ChannelBot` 的**按用户定向投递**留作未来能力（当前无用户↔机器人绑定，机器人位只广播）。
- **聊天转发**：`ChatAppService` 未提供"转发"端点，`SysChatMessage` 也无转发字段——**转发未实现**（当前支持回复而非转发）。
- **推送渠道 `ChannelPush`**：偏好里有 `Push` 开关，但独立的移动/浏览器推送通道未在本模块落地（实时到达仍走 SignalR）。
- **站内信"删除消息"**：`NotificationStatus.Deleted` 状态位已定义、查询侧也已按其过滤，但 `UserInboxAppService`/`UserInboxDomainService` 目前没有把消息状态置为 `Deleted` 的写入端点，也没有对应的物理清理定时任务（不同于日志/聊天消息已有 `LogRetentionCleanupTask` 按保留期清理）——用户侧删除消息是预留能力，尚未接线。
- **偏好类型开关 `TypeApproval`/`TypeMarketing`**：个人中心可设置，但 `NotificationDomainService.FilterByPreferenceAsync` 的门控只区分 `Security`/`Todo`/其余（落 `TypeAnnouncement`）三档，这两个开关当前不影响任何通知的送达判定，是面向未来审批/营销类型的预留位。

以上以仓库源码为准；本页所述字段名/枚举/常量/窗口值均可在对应源码文件核对（`modules/XiHan.BasicApp.Saas` 下 `Domain/Entities`、`Application/AppServices/Messaging`、`Application/Services/Messaging`、`Domain/DomainServices/Messaging`、`Hubs`）。

## 相关文档

- 权限码与门控：[权限模型](./permission)
- 多租户隔离与全局 `TenantId=0`：[多租户](./multi-tenancy)
- 框架实时通信：[Web.RealTime](https://framework.docs.xihanfun.com/packages/web-realtime)
- 框架消息路由与发件箱：[Messaging](https://framework.docs.xihanfun.com/packages/messaging)
- 动态 API 概念：[dynamic-api](https://framework.docs.xihanfun.com/guide/dynamic-api)
