# AI 能力（部分落地 · N4）

本页说明 AI 模块的提供商、Provider 目录、提示词与模块开关，以及还在做的知识库与助手，适用于接入或配置模型的后端与前端开发者。

AI 是 Tier 3 的可选模块（`BasicApp:Modules:AI:Enabled`，缺省开），架在框架 5 的 AI 子系统（`AI.Abstractions`、`AI.OpenAI`）之上。模型与规则沿用 5.x，写法换成 6.0 的；5.x 的 AI 文档见 [5.x 文档](https://basicapp.docs.xihanfun.com/backend/ai)。

::: warning 部分落地
目前落地的是模型提供商与提示词（N4 第一批）。知识库与助手接入聊天还在做，本页对应的小节按总体方案写，落地时以代码为准。

本页说的是 BasicApp 自带的 AI 功能。用 AI 工具开发 BasicApp 见 [For Agents](../ai/agents)。
:::

## 提供商

模型提供商 `SysAiProvider` 是平台库实体、只在平台一侧：平台配置，租户经助手用到（与 5.x 相同）。协议只有 OpenAI 兼容一种，改端点即可接 DeepSeek、Azure OpenAI、Moonshot、自建的 vLLM 等。

| 字段 | 规则 |
| --- | --- |
| 编码 | 框架 Provider 名的文法：小写字母或数字开头，只有小写字母、数字与 `._-`，至多 64 个字符；唯一（部分唯一索引，删了可以重建），建后不可改。5.x 至多 100 个字符、可大写 |
| 厂商 | 只用于分组展示（OpenAI、DeepSeek、Azure、Moonshot、自建、自定义，可自填） |
| 聊天模型 | 必填 |
| 嵌入模型 | 为空即不能用于知识库 |
| 端点 | 绝对的 http 或 https 地址；为空即厂商的官方端点 |
| API 密钥 | 经 `ISecretProtector` 加密存储，明文至多 1000 个字符；响应只给 `hasApiKey`，修改时留空即保留原密钥 |
| 最大输出、温度 | 每次聊天调用带上（温度 0～2）；5.x 存了从没用到 |
| 超时 | 1～3600 秒，为空即框架缺省的 100 秒；流式回复按每个增量计时 |
| 缺省 | 最多一个；助手与知识库没写提供商时用它。停用的不能设为缺省（`AI_CONFLICT_102`） |
| 启停 | 一个状态（5.x 有「是否启用」与状态两个开关）；停用的不进 Provider 目录 |

| 操作 | 权限码 | 说明 |
| --- | --- | --- |
| `ai-providers.list` / `get` | `ai.ai-providers.read` | 列表可按状态、厂商与是否缺省筛选，按编码、名称与聊天模型搜索 |
| `ai-providers.create` / `update` | `ai.ai-providers.create` / `update` | 修改与启停交回读到的 `concurrencyStamp` |
| `ai-providers.set-status` / `set-default` | `ai.ai-providers.update` | 单条 |
| `ai-providers.delete` | `ai.ai-providers.delete` | 批量，逐项结果 |
| `ai-providers.test` | `ai.ai-providers.execute` | 测试连接，见下 |

权限码都只在平台一侧，写操作进操作日志。字段安全登记了提供商的读、建、改。

测试连接按当前配置临时建一次客户端（停用的也能测，用完即释放）：聊天模型发一句 `ping`（最大输出 16 个令牌），配了嵌入模型再嵌入一次并回报维度。连不通不是失败的调用，结果里写提供商给的原因；密钥解不开是 503 `AI_UNAVAILABLE_101`。

## Provider 目录

框架的聊天、嵌入与知识库都经 `IAIProviderCatalog` 按名字解析客户端。框架缺省的目录读配置 `XiHan:AI:Providers`；BasicApp 以落库的 `AiProviderCatalog` 取代它，提供商在页面上维护：

- 启用中的提供商按编码映射成框架的 Provider 配置，种类 `openai`；缺省提供商是标为缺省的那一个（不在只有一个时自动取它）。
- 框架的目录接口是同步的，所以目录持一份内存快照：宿主启动时装载，提供商写入的调用提交之后当场重载，新的调用即用新配置。
- 配了 Redis 时，写入的实例改写共享缓存的戳键 `ai:providers:stamp`，别的实例经共享缓存的变更通知重载；没配 Redis 即单实例。
- 重载只丢掉配置变了或没了的提供商缓存的客户端。解不开密钥的提供商记 Warning 后不进目录，按未配置处理，不当明文用。
- 目录以框架的 `AIProviderConformance` 一致性套件自证。

没写提供商的编码时，聊天、嵌入与知识库都向目录要缺省提供商。不要配 `XiHan:AI:Knowledge:EmbeddingProvider`：它排在目录的缺省之前，缺省提供商改了也不跟着变。

## 提示词

提示词 `SysAiPrompt` 是平台库实体、只在平台一侧（与 5.x 相同）：正文原样当助手的系统提示词，没有变量渲染，版本只是标签、同一编码只有一行。

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

## 知识库与助手（规划中）

| 部分 | 方案 |
| --- | --- |
| 知识库 | 知识文档是租户库实体，平台与各租户各自维护；集合按侧分（`knowledge.platform`、`knowledge.<租户 ID>`），检索只在调用方一侧；只收文本、请求内同步摄取；向量库经 Qdrant 连接器（`BasicApp:AI:Qdrant`），不配即知识库不可用 |
| 助手 | 配置身份、模型与提示词，勾选可用的工具（`Expose.Ai` 的只读操作，以调用方身份执行）；会话复用聊天，经 Contracts 的助手接口流式回复，AI 关掉后聊天照常 |
| 资源 | `ai-assistants`、`knowledge-documents` |

5.x 的 Agent 工厂不迁移，也不做用量统计（5.x 也没有）。

## 操作作为工具

6.0 的操作可以按 `Expose` 发布为 MCP 工具（`Expose.Mcp`）与框架 AI 工具（`Expose.Ai`）：

- 目前发布的是只读目录类操作：用户、部门、岗位、部门成员、权限目录、字典与字典项；
- 工具说明取自操作的本地化 `.title` / `.description`，权限与租户隔离照常生效；
- 宿主映射了 `/mcp` 端点，MCP 客户端可以带用户令牌直接调用，见 [MCP Server](../ai/mcp)。

长操作 ID 作为工具名时会被框架截断加哈希，资源名控制在 30 字符以内；新增 `Expose.Mcp` / `Expose.Ai` 的操作必须只读，并登记在 `OperationExposureTests`。

## 模块开关

`BasicApp:Modules:AI:Enabled=false` 时宿主不装配 AI 包与 OpenAI 兼容的 Provider：权限码与页面不下发、操作 404，`sys_ai_provider` 与 `sys_ai_prompt` 照样建。框架的工具目录随 Full 画像照常装配，MCP 工具不受影响。

## 下一步

- [MCP Server](../ai/mcp) · [在线聊天](./chat) · [实时推送](./realtime) · [配置参考](../configuration#basicapp-modules)
