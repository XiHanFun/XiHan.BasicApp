来源：https://basicapp.docs.xihanfun.com/backend/ai

# AI 能力

本页说明 AI 模块的提供商、Provider 目录、提示词、知识库、助手、操作作为工具与模块开关，适用于接入或配置模型的后端与前端开发者。

AI 是 Tier 3 的可选模块（`BasicApp:Modules:AI:Enabled`，缺省开），架在框架的 AI 子系统（`AI.Abstractions`、`AI.OpenAI`、`AI.VectorData`）之上。四个页面都在「AI 中心」目录下：

| 页面码 | 路由 | 菜单权限 |
| --- | --- | --- |
| `ai.providers` | `/ai/providers` | `ai.ai-providers.read` |
| `ai.assistants` | `/ai/assistants` | `ai.ai-assistants.read` |
| `ai.prompts` | `/ai/prompts` | `ai.ai-prompts.read` |
| `ai.knowledge-documents` | `/ai/knowledge-documents` | `ai.knowledge-documents.read` |

::: tip
本页说的是 BasicApp 自带的 AI 功能。用 AI 工具开发 BasicApp 见 [For Agents](../ai/agents)。
:::

## 提供商

模型提供商 `SysAiProvider` 是平台库实体、只在平台一侧：平台配置，租户经助手用到。协议只有 OpenAI 兼容一种，改端点即可接 DeepSeek、Azure OpenAI、Moonshot、自建的 vLLM 等。

| 字段 | 规则 |
| --- | --- |
| 编码 | 框架 Provider 名的文法：小写字母或数字开头，只有小写字母、数字与 `._-`，至多 64 个字符；唯一（部分唯一索引，删了可以重建），重复报 409 `AI_CONFLICT_101`；建后不可改 |
| 厂商 | 只用于分组展示（OpenAI、DeepSeek、Azure、Moonshot、自建、自定义，可自填），至多 50 个字符 |
| 聊天模型 | 必填 |
| 嵌入模型 | 为空即不能用于知识库。DeepSeek 没有嵌入接口，要另配一个带嵌入模型的提供商，助手在表单的「嵌入提供商」里指定它 |
| 端点 | 绝对的 http 或 https 地址。厂商是 OpenAI 时可留空，走它的官方端点；别的厂商必填（DeepSeek 填 `https://api.deepseek.com`），否则密钥会发给 OpenAI |
| API 密钥 | 经 `ISecretProtector` 加密存储，明文至多 1000 个字符；响应只给 `hasApiKey`。修改时留空即保留原密钥，但改了端点必须重新填写（`AI_VALIDATION_107`），旧密钥不会发往新地址 |
| 最大输出、温度 | 每次聊天调用带上（温度 0～2） |
| 超时 | 1～3600 秒，为空即框架缺省的 100 秒；流式回复按每个增量计时 |
| 缺省 | 最多一个；助手与知识库没写提供商时用它。停用的不能设为缺省（`AI_CONFLICT_102`） |
| 启停 | 一个状态；停用的不进 Provider 目录 |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `ai-providers.list` / `get` | `ai.ai-providers.read` | 列表可按状态、厂商与是否缺省筛选，按编码、名称与聊天模型搜索 |
| `ai-providers.create` / `update` | `ai.ai-providers.create` / `update` | 修改与启停交回读到的 `concurrencyStamp` |
| `ai-providers.set-status` / `set-default` | `ai.ai-providers.update` | 单条 |
| `ai-providers.delete` | `ai.ai-providers.delete` | 批量，逐项结果 |
| `ai-providers.test` | `ai.ai-providers.execute` | 测试连接，见下 |

权限码都只在平台一侧，写操作进操作日志。

测试连接按当前配置临时建一次客户端（停用的也能测，用完即释放）：聊天模型发一句 `ping`（最大输出 16 个令牌），配了嵌入模型再嵌入一次并回报维度。连不通不是失败的调用，结果里写提供商给的原因；密钥解不开是 503 `AI_UNAVAILABLE_101`。

模型调用经出站地址策略（`XiHan:Security:Outbound`）：缺省只能连公网，本机或内网的模型端点要在那里放行，见[配置参考](../configuration#xihan-security-outbound)。

## Provider 目录

框架的聊天、嵌入与知识库都经 `IAIProviderCatalog` 按名字解析客户端。BasicApp 以落库的 `AiProviderCatalog` 取代框架按配置建的目录，框架的 `XiHan:AI:Providers`、`DefaultProvider`、`DefaultEmbeddingProvider` 不生效，提供商在页面上维护：

- 启用中的提供商按编码映射成框架的 Provider 配置，种类 `openai`；缺省提供商是标为缺省的那一个（只有一个时也不自动取它），聊天与嵌入共用这一个缺省。
- 框架的目录接口是同步的，所以目录持一份内存快照：宿主启动时装载，提供商写入的调用提交之后当场重载，新的调用即用新配置。
- 配了 Redis 时，写入的实例改写共享缓存的戳键 `ai:providers:stamp`，别的实例经共享缓存的变更通知重载；没配 Redis 即单实例。
- 重载只丢掉配置变了或没了的提供商缓存的客户端。解不开密钥的提供商记 Warning 后不进目录，按未配置处理，不当明文用。
- 目录以框架的 AI Provider 一致性套件自证（`AIProviderCatalogConformanceTests`）。

没写提供商的编码时，聊天、嵌入与知识库都向目录要缺省提供商。`XiHan:AI:Knowledge:EmbeddingProvider` 不要配：写了它，没指定嵌入提供商的摄入与检索就固定用它，排在目录的缺省之前，缺省提供商改了也不跟着变。

## 提示词

提示词 `SysAiPrompt` 是平台库实体、只在平台一侧：正文原样当助手的系统提示词，没有变量渲染，版本只是标签、同一编码只有一行。

| 字段 | 规则 |
| --- | --- |
| 编码 | 必填，至多 100 个字符；唯一（部分唯一索引，删了可以重建），重复报 409 `AI_CONFLICT_103`；建后不可改 |
| 名称 | 必填，至多 200 个字符 |
| 分类、版本 | 可选的标签，各至多 100 个字符 |
| 正文 | 必填，不限长度；列表不带正文，`ai-prompts.get` 才返回 |
| 启停 | 新建时选，之后走 `ai-prompts.set-status` |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `ai-prompts.list` / `get` | `ai.ai-prompts.read` | 列表可按状态与分类筛选，按编码与名称搜索 |
| `ai-prompts.create` / `update` | `ai.ai-prompts.create` / `update` | 修改与启停交回读到的 `concurrencyStamp` |
| `ai-prompts.set-status` | `ai.ai-prompts.update` | 单条 |
| `ai-prompts.delete` | `ai.ai-prompts.delete` | 批量，逐项结果 |

提示词与提供商的必填文本都不接受纯空白（校验错误，消息键 `ai.fields.blank`）。

提示词还被助手引用时不能删除，逐项报 `AI_CONFLICT_107`。

## 知识库

知识文档 `SysKnowledgeDocument` 是租户库实体，平台与各租户各自维护、互不可见。向量也按侧分集合：平台 `knowledge.platform`，租户 `knowledge.<租户 ID>`；检索只在调用方一侧的集合里做，不靠元数据过滤。向量不在数据库里，库和向量库要一起备份与还原，见[部署](../deployment#备份与还原)。

| 字段 | 规则 |
| --- | --- |
| 标题 | 必填，至多 200 个字符 |
| 来源类型、来源标识 | 粘贴文本（`PasteText`）或上传文件（`UploadFile`）；上传的文件由前端读成文本提交，来源标识记文件名 |
| 原文 | 必填，只收文本，留在库里供重建 |
| 嵌入提供商 | 提供商的编码，为空即缺省提供商；写了但不存在或已停用报 400 `AI_VALIDATION_102` |
| 块数、状态、失败原因 | 状态是待索引（`Pending`）、已索引（`Indexed`）或失败（`Failed`）；失败时记错误码与发起人区域的文案，切不出块是 `AI_VALIDATION_103`。模型或向量库调用失败时原因只有异常类型，完整异常看应用日志 |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `knowledge-documents.list` / `get` | `ai.knowledge-documents.read` | 列表可按来源类型与状态筛选，按标题与来源搜索，不带原文 |
| `knowledge-documents.ingest` | `ai.knowledge-documents.create` | 请求内同步切块、嵌入并写入向量库 |
| `knowledge-documents.reindex` | `ai.knowledge-documents.update` | 用原文重建 |
| `knowledge-documents.delete` | `ai.knowledge-documents.delete` | 批量（1～100 个），逐项结果；连同向量一起删 |
| `knowledge-documents.query` | `ai.knowledge-documents.execute` | 知识问答，`GET`；问题至多 2000 个字符、条数 1～20，`answer` 为假只返回片段；`provider` 同时用于嵌入与聊天 |
| `knowledge-documents.search` | `ai.knowledge-documents.execute` | 只检索，`GET`；也发布为 MCP 与 AI 工具 |

权限码两侧都有，写操作进操作日志。

- **事务**：摄入与重建有网络 I/O，不放在调用的事务里。摄入先写一行待索引，索引之后再回写结果，两次写入各自提交；进程在途中中断即停在待索引，可以重建。回写失败时删掉刚写入的向量。
- **索引失败不是失败的调用**：没配向量库、模型不可达、维度不符时文档以失败状态保存并成功返回，原因记在文档上，修好之后重建。
- **重建先写后删**：新块写入成功之后才裁掉旧版本多出来的块；失败时旧块照常可检索，块数不变。
- **删除**：先删向量再软删行。向量库不可达时这一项失败、行不删；索引失败且块数为 0 的文档没写入过向量，不碰向量库，没配向量库也删得掉。
- **问答**：要答案且有命中时才调模型，用框架内置的增强模板，最大输出与温度取提供商的配置。片段在列表里的顺序就是答案里的引用编号：`[n]` 对应第 n 个片段。检索失败照框架的错误码返回，不退化成纯模型回答。

向量库是 Qdrant，配置见[配置参考](../configuration#basicapp-ai-qdrant)：写了 `BasicApp:AI:Qdrant:Host` 才装知识库，健康检查多一项 `ai.vectordata.store`。不配即知识库不可用：摄入的文档以失败保存，问答与检索返回 503 `AI_UNAVAILABLE_002`，不悄悄退到内存。向量维度要与嵌入模型一致（`XiHan:AI:Knowledge:Dimensions`，缺省 1536），不一致报 `AI_VALIDATION_002`；检索条数与切块参数也在这一节，缺省 5 条、块 1000 个字符、重叠 100。

提供商还被知识文档或助手引用时不能删除，逐项报 `AI_CONFLICT_104`。文档可能在任何一侧，所以删除时要逐个租户查一遍，独立库的租户查它自己的库；哪一侧查不了，这次删除就失败（503 `AI_UNAVAILABLE_104`）。

## 助手

助手 `SysAiAssistant` 是平台库实体：只在平台一侧配置，平台与租户的聊天都用同一批助手。会话复用聊天，聊天里的操作见[在线聊天](./chat#助手会话)。

| 字段 | 规则 |
| --- | --- |
| 编码 | 必填，至多 100 个字符、不含空白；唯一（部分唯一索引，删了可以重建），重复报 409 `AI_CONFLICT_105`；建后不可改 |
| 名称、简介、开场白、头像 | 名称必填；头像是图片地址，为空用首字母；开场白在新建会话时作为第一条助手消息，为空不发 |
| 提示词的编码 | 为空用内置的系统提示词（按调用方区域取）；写了须存在且启用，否则报 400 `AI_VALIDATION_105` |
| 聊天提供商、嵌入提供商的编码 | 为空用缺省提供商；写了须存在且启用，否则报 400 `AI_VALIDATION_104` |
| 是否检索知识库 | 缺省开，检索的片段数 1～20，缺省 5；没配向量库时表单给出提示，开着的助手回复会失败 |
| 带入上下文的历史消息条数 | 0～50，缺省 10；至少带上本轮提问 |
| 可用的工具 | 缺省不带；只能勾框架工具目录里的（`Expose.Ai` 的只读操作），否则报 400 `AI_VALIDATION_106` |
| 缺省 | 最多一个；新建会话时缺省选中它。停用的不能设为缺省（`AI_CONFLICT_106`） |
| 启停 | 一个状态；停用的不再出现在聊天里，已有会话不能继续提问 |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `ai-assistants.list` / `get` | `ai.ai-assistants.read` | 列表可按状态与是否缺省筛选，按编码与名称搜索 |
| `ai-assistants.tools` | `ai.ai-assistants.read` | 可勾选的工具（说明按调用方区域）与有没有向量库 |
| `ai-assistants.create` / `update` | `ai.ai-assistants.create` / `update` | 修改与启停交回读到的 `concurrencyStamp` |
| `ai-assistants.set-status` / `set-default` | `ai.ai-assistants.update` | 单条 |
| `ai-assistants.delete` | `ai.ai-assistants.delete` | 批量，逐项结果；已有会话不删，但不能继续提问 |

权限码都只在平台一侧，写操作进操作日志。

字段安全登记了提供商、提示词、助手（读、新建、修改、启停）与知识文档（读、摄入），规则由 Access 统一套用，见[数据权限与字段安全](./data-permission)。

生成一次回复的流程（`IChatAssistants`，Contracts 定义，AI 实现，Chat 只认这个接口）：

1. 系统提示词：配了提示词用它的正文，引用的提示词后来停用或没了即回复失败（`AI_UNAVAILABLE_103`），不回落到内置的；没配用内置的。
2. 历史：Chat 从库里取最近的提问与回复，不信前端传的。
3. 检索增强：开了知识库时，在调用方一侧检索本轮提问，有命中才把提问换成增强后的（框架内置的模板，片段带编号）。检索失败回复失败，不退化成纯模型回答。
4. 生成：带上助手勾选的工具，最大输出与温度取提供商的配置，流式产出。工具以调用方身份执行，权限与租户隔离照常判定；没有权限时模型收到拒绝，回复本身不失败。聊天页把助手回复按 Markdown 显示，图片语法只留成链接、不自动加载：回复内容受知识库片段与工具结果影响，自动加载的地址能把对话带到外部。

不做用量统计。

## 操作作为工具

操作可以按 `Expose` 发布为 MCP 工具（`Expose.Mcp`）与框架 AI 工具（`Expose.Ai`）：

- 发布的是只读目录类操作：`users.list` / `get`、`departments.get` / `tree`、`positions.list` / `get`、`memberships.list`、`permissions.list`、`dicts.list` / `get` / `options`、`dict-items.list` / `tree`，我的待办 `tasks.list`，以及知识检索 `knowledge-documents.search`；助手从这些工具里勾选。
- 工具说明取自操作的本地化 `.title` / `.description`，权限与租户隔离照常生效。
- 宿主映射了 `/mcp` 端点，MCP 客户端可以带用户令牌直接调用，见 [MCP Server](../ai/mcp)。

工具名由操作 ID 推出（`-` 与 `.` 换成 `_`），超过 64 个字符时截成前 55 个字符加 `_` 与 8 位哈希；要改名用 `XiHan:AI:Tools` / `XiHan:Protocols:Mcp:Tools`（键为操作 ID）。新增 `Expose.Mcp` / `Expose.Ai` 的操作必须只读，并登记在 `OperationExposureTests`。

## 模块开关

`BasicApp:Modules:AI:Enabled` 设为 `false` 时宿主不装配 AI 包与 OpenAI 兼容的 Provider：权限码与页面不下发、操作 404，也不装向量库连接器与知识库 Provider；`sys_ai_provider`、`sys_ai_prompt`、`sys_ai_assistant` 与 `sys_knowledge_document` 仍由正式库的基线脚本建出（开发库的 `InitTables` 只建装配了的包的表）。写成 `true` / `false` 以外的值启动即失败。框架的工具目录随 Full Profile 照常装配，`XiHan:AI` 节照常校验，其他操作的 MCP 工具不受影响。

聊天不依赖 AI：关掉之后聊天照常，聊天的三个助手操作返回 503 `CHAT_UNAVAILABLE_001`，前端首次展开「新建」菜单时据此隐藏助手入口。

## 下一步

- [MCP Server](../ai/mcp) · [在线聊天](./chat) · [实时推送](./realtime) · [配置参考](../configuration#basicapp-modules)
