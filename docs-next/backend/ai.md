# AI 能力（规划中 · N4）

::: warning 尚未实现
6.0 还没有 AI 包：没有 AI 提供商、提示词、知识库与助手的页面和操作，也没有接入任何模型。本页记录总体方案里的设计，落地时以代码为准。5.x 的 AI 文档见 [5.x 文档](https://basicapp.docs.xihanfun.com/backend/ai)。

本页说的是 BasicApp **自带的 AI 功能**。用 AI 工具**开发** BasicApp 见 [For Agents](../ai/agents)。
:::

## 已经可用的：操作即工具

虽然 AI 包还没有，6.0 的操作契约已经为 AI 准备好了：

- 每个操作可以按 `Expose` 发布为 MCP 工具（`Expose.Mcp`）与框架 AI 工具（`Expose.Ai`）；
- 目前发布的是 12 个只读目录类操作：用户、部门、岗位、部门成员、权限目录、字典与字典项；
- 工具说明就是操作的本地化 `.title` / `.description`，权限与租户隔离照常生效；
- 宿主映射了 `/mcp` 端点，MCP 客户端可以带用户令牌直接调用，见 [MCP Server](../ai/mcp)。

## AI 包的设计（N4）

| 部分 | 方案 |
| --- | --- |
| 底座 | 框架 5 的 `AI.Abstractions`（`IAIProviderCatalog`、`IAIChat`、`IKnowledgeStore`）、`AI.OpenAI`、`AI.VectorData` |
| 提供商 | 配置落库（平台库实体，密钥加密），以 `IChatClientFactory` 自定义工厂接入框架的提供商目录 |
| 知识库 | `IKnowledgeIngestor` / `IKnowledgeRetriever`；向量库经 `Microsoft.Extensions.VectorData` 的 Qdrant 连接器 |
| 助手 | 配置身份、模型与提示词；会话复用 Chat 包，回复流式推送 |
| 工具 | `Expose.Ai` 的只读操作自动成为助手可调用的工具，按调用人的权限与租户执行 |
| 资源 | `ai-providers`、`ai-prompts`、`ai-assistants`、`knowledge-documents` |

5.x 的 Agent 工厂不迁移。N4 的退出条件：配置提供商、知识库问答、助手调用 `Expose.Ai` 工具。

## 注意

- 长操作 ID 作为工具名时会被框架截断加哈希，资源名控制在 30 字符以内；
- 新增 `Expose.Mcp` / `Expose.Ai` 的操作必须只读，并登记在 `OperationExposureTests`。

## 下一步

- [MCP Server](../ai/mcp) · [实时推送](./realtime) · [架构总览](./introduction)
