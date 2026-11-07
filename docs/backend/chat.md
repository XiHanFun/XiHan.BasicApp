# 在线聊天

本页说明在线聊天的会话与消息规则、推送、附件、部门群同步、聊天审计、策略参数与保留清理，适用于后端与前端开发者。

Chat 是 Tier 3 的可选模块：单聊、群聊、部门群与 AI 助手会话，消息的发送、撤回、编辑、回应、置顶、转发、已读与提及，聊天 Hub 与聊天审计。

## 数据

四张租户库表（`TenantScoped*`），平台一侧以 `platform` 为租户落在同一张表里，平台账号之间同样能聊天；独立库的租户在自己的库里。

| 表 | 说明 |
| --- | --- |
| `sys_chat_conversation` | 会话：类型（单聊、群聊、部门群、助手）、名称、头像、单聊唯一键、部门、群主、助手、成员数、最大消息序号、最后消息、公告、描述；软删除，不进差异日志 |
| `sys_chat_conversation_member` | 成员：角色（群主、管理员、成员）、未读数、已读到的消息与序号、免打扰、置顶、禁言、入会时间；退群与移出即软删除 |
| `sys_chat_message` | 消息：会话内序号、发送人与名字快照、类型、正文、附件（JSON）、撤回、编辑、回复快照、提及、置顶；不软删除，按保留期物理删除 |
| `sys_chat_message_reaction` | 表情回应：同一消息、同一用户、同一表情只有一条 |

会话内的消息按序号排序：发消息时对会话行加一取得序号（行锁让同一会话的发送串行），历史的游标、已读位与已读回执都比序号。主键是 GUIDv7，同一毫秒内没有先后，不能用来排序。

## 规则

| 规则 | 说明 |
| --- | --- |
| 单聊 | 同一对用户每侧只有一个，没有即建；不能和自己单聊 |
| 群聊 | 含群主至少两人，发起人为群主；加人、移出、改信息与公告、禁言要群主或管理员，转让群主与设管理员只有群主；群主不能退群或被移出；没有解散 |
| 部门群 | 只有本部门的成员能进，打开时补齐部门现有成员；没有群主与管理员，所以没人能改信息、置顶消息或禁言；名称随部门、不能改；平台没有部门，部门群只在租户一侧 |
| 成员范围 | 加进会话的人必须属于当前一侧：平台是注册在平台的账号，租户是注册在本租户的用户与租户成员（`CHAT_VALIDATION_021`） |
| 发送 | 只有成员、没被禁言；文本必填不超过 4000 个字符，图片与文件至少一个附件（可带说明），语音恰好一个附件、1 到 60 秒、没有正文；系统提示与助手回复只由服务端追加 |
| 提及与回复 | 最多提及 20 名成员；只能回复本会话未撤回的消息，存「发送人: 内容」快照 |
| 撤回 | 本人、发出 2 分钟内，清空正文、保留行与附件 |
| 编辑 | 本人的文本、发出 5 分钟内、没被禁言 |
| 置顶消息 | 单聊双方都能，群与部门群只有群主与管理员，每会话最多 10 条（`CHAT_CONFLICT_001`） |
| 转发 | 服务端单条转发到一个或多个会话，原样复制类型、正文与附件，不带转发来源，助手回复按文本转出；系统提示与已撤回的不能转发，不是成员或被禁言的会话逐项失败 |
| 已读 | 未读清零、已读位前移（不回退，没给消息即读到最新），已读位推给全体成员 |

系统提示（建群、邀请、退群、移出、转让）与会话预览按发起调用的区域写进库里，公告以操作人的身份进消息流。

## 助手会话

一个用户对一个助手只有一个会话，打开时没有即建并发开场白。助手不是成员，不算未读，助手会话也不显示已读回执。回复是前端发出用户消息之后单独发起的调用：

1. 前端发出提问，再调 `chat-messages.reply-assistant`，带自己生成的 `replyId`。
2. 服务端从库里取最近的提问与回复，经 Contracts 的 `IChatAssistants` 流式生成。Chat 不引用 AI，AI 关掉时没有实现，三个助手操作（`chat-conversations.assistants`、`open-assistant`、`chat-messages.reply-assistant`）返回 503 `CHAT_UNAVAILABLE_001`，聊天其余功能照常。
3. 生成期间这次调用还没开事务，增量经立即推送发给本人（`ChatAssistantDelta`）。
4. 结束后过敏感词（与用户输入同一套），落库为一条助手消息（超过 20000 个字符截断），提交之后按普通消息推送，再推 `ChatAssistantCompleted`。

失败（模型不可达、空输出、敏感词、提示词停用、检索失败）时不留消息，操作以 Problem Details 结束，同时立即推 `ChatAssistantCompleted` 带 `errorCode` 与 `error`。

| 错误码 | 说明 |
| --- | --- |
| `CHAT_VALIDATION_026` | 不是助手会话 |
| `CHAT_VALIDATION_027` | 会话里没有可回答的提问 |
| `CHAT_UNAVAILABLE_001` | 助手不可用（AI 没有装配） |
| `CHAT_UNAVAILABLE_002` | 生成失败。原因只有异常类型，完整异常看应用日志 |
| `CHAT_UNAVAILABLE_003` | 模型没有产生任何内容 |

助手本身的配置见 [AI 能力](./ai#助手)。

## 操作与权限

| 资源 | 操作 |
| --- | --- |
| `chat-conversations` | `mine`、`members`、`read-positions`、`department-tree`、`user-options`、`assistants`（只读）；`open-single`、`open-assistant`、`create-group`、`open-department`、`add-members`、`remove-members`、`leave`、`update-info`、`transfer-owner`、`silence-members`、`set-member-role`、`pin`、`mute`、`mark-read` |
| `chat-messages` | `history`、`search`、`pinned`、`presign`（只读）；`send`、`recall`、`edit`、`react`、`pin`、`unpin`、`forward`、`fast-upload`、`reply-assistant` |
| `chat-audit` | `list`、`get`、`members` |

普通端点 `POST /api/chat-messages/attachments` 上传附件（multipart 字段 `file`，要 `chat.chat-messages.send`，访问日志记作 `chat-messages.upload`）。

| 权限码 | 说明 |
| --- | --- |
| `chat.chat-conversations.read` | 查看与参与会话（打开、退群、置顶、免打扰、已读也用它） |
| `chat.chat-messages.read` | 查看历史、搜索、置顶消息与换附件地址 |
| `chat.chat-messages.send` | 发送、撤回、编辑、回应、置顶消息、转发与上传附件（进操作日志） |
| `chat.chat-conversations.manage` | 建群与群治理（进操作日志） |
| `chat.chat-audit.read` | 聊天审计 |

权限码两侧都有。页面 `chat.conversations`（`/chat/conversations`）与 `chat.audit`（`/chat/audit`）在「消息中心」目录下。

## 推送

聊天 Hub `/hubs/chat` 由 Chat 登记、宿主一并映射，认证、会话门禁与连接登记同通知 Hub（见[实时推送](./realtime)）。推送按成员点发给会话这一侧的用户，在调用提交之后发出；配了 Redis 时跨实例。

| 方向 | 方法 |
| --- | --- |
| 服务端推送 | `ReceiveChatMessage`、`ChatMessageRecalled`、`ChatConversationChanged`（11 种变更）、`ChatTyping`、`ChatMessageEdited`、`ChatReactionChanged`、`ChatReadPositionChanged`、`ChatAssistantDelta`、`ChatAssistantCompleted` |
| 客户端可调 | `JoinConversation`（核对成员后加入会话组）、`LeaveConversation`、`Typing`（核对成员后组播给组里的其他连接，不落库） |

`ChatConversationChanged` 的变更种类：`created`、`member-added`、`member-removed`、`info-changed`、`owner-transferred`、`member-silenced`、`member-role-changed`、`pinned-changed`、`member-setting-changed`、`department-joined`、`department-kicked`。

## 附件

附件以会话为访问范围，经 Files 的附件契约 `IAttachmentFiles` 存取：上传与秒传只要聊天的发送权限，文件存在调用方这一侧、来源记 `chat`，大小上限与存储配额同普通上传；发送时核对附件是自己在聊天里上传的（`CHAT_DENIED_009`），文件名与大小以文件记录为准；会话成员经 `chat-messages.presign` 凭会话换签名地址，附件须出现在这个会话的消息里，或是自己上传、还没发出的文件。文件管理照旧要 `files.files.read`。保留清理不删附件文件。

群头像是文件主键或直链：给文件主键时同样须是自己在聊天里上传的（`CHAT_DENIED_009`），设上之后会话成员经 `chat-messages.presign` 凭会话换它的地址。单聊的头像是对端用户的头像。

## 部门群同步

Organization 在加入与移出部门时记领域事件 `basicapp.organization.membership-changed`（随写入进发件箱），Chat 订阅它：已经建了的部门群里，进了部门即入群、移出部门即出群，并推会话变更（被移出的人也收到）。处理时按当时的归属核对，消息乱序或重复投递都只做一次；写库失败即投递失败、由消息代理重试。

## 聊天审计

持 `chat.chat-audit.read` 即看当前一侧全部会话的消息，不要求是成员：

- 列表按关键字（正文、发送人名、附件）与会话、发送人、消息类型、发送时间、是否撤回筛选，缺省不含已撤回的，筛选 `includeRecalled` 为 `true` 即都看；
- 消息详情带所在会话、附件、提及、回复与表情回应；
- 会话成员列出会话与它现在的成员。

撤回清空的正文与编辑前的正文不另留。

## 策略与保留清理

参数 `chat.policy`（JSON，类型选 Json），没建即用缺省值：

```json
{ "retentionDays": 365, "sensitiveWords": [] }
```

| 字段 | 说明 |
| --- | --- |
| `retentionDays` | 消息保留天数，1 到 3650，只读平台的值 |
| `sensitiveWords` | 敏感词，不区分大小写的包含匹配；租户建了同键参数即用租户的；管发送与编辑的正文（含图片与文件的说明），不管群名、公告与文件名（`CHAT_VALIDATION_023`） |

JSON 不合法或保留天数越界即 `CHAT_VALIDATION_024`，发送与清理都不按错误的值执行。

内建作业 `chat.messages.cleanup` 每天 04:00（UTC）按平台与每个租户逐个清理（独立库的租户清它自己的库）：先删所属消息已过期的表情回应，再删过期的消息；不删会话、成员与附件文件。一侧失败只记日志，不影响其他侧。

## 开关

`BasicApp:Modules:Chat:Enabled`（缺省开）。关掉后宿主不装配 Chat：权限目录、页面、Hub、附件上传端点与部门归属订阅都不在，菜单不下发、操作 404；Organization 照常发出领域事件，没有订阅方。四张表仍由正式库的基线脚本建出（开发库的 `InitTables` 只建装配了的包的表）。

## 下一步

- [实时推送](./realtime) · [文件与存储](./file) · [组织架构](./organization) · [消息通知](./messaging)
