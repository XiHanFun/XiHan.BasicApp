# AI 能力

BasicApp 的 AI 能力落在独立模块 `XiHan.BasicApp.AI` 里，与代码生成模块同构（独立工程、独立种子段、独立权限资源）。它坐落在框架薄层 [XiHan.Framework.AI](https://framework.docs.xihanfun.com/packages/ai) 之上：**框架**提供 provider 解析、会话/嵌入门面、RAG 切片检索、提示词模板、Agent 与 MCP 桥接的抽象与默认实现；**BasicApp** 在其上做三件业务落地——把 AI Provider 配置**库化管理**（存库 + 加密 + 前端可维护 + 热切换），把 RAG 知识库**接上真实向量库（Qdrant）**并暴露为文档管理 / 检索问答 / MCP 工具，以及把**提示词模板库化**（覆盖框架默认 Options 实现，前端可维护）。

模块只依赖 `XiHanBasicAppSaasModule`（复用 RBAC 表、`SaasRepository`、DataProtection 密文前缀），框架 AI 经 Saas → Core 传递进来。

## 三大能力

| 能力 | 做什么 | 关键实体 / 服务 | 权限资源 |
| --- | --- | --- | --- |
| **Provider 库化管理** | AI 服务商配置存库、加密密钥、CRUD、设默认、测连接、改配置即时热切换 | `SysAiProvider`、`AiProviderAppService`、`SaasAiProviderConfigStore` | `ai` |
| **RAG 知识库** | 文档摄取（切片 → 嵌入 → 写 Qdrant）、重建索引、语义检索、注入 prompt 生成带引用答案 | `SysKnowledgeDocument`、`KnowledgeDocumentAppService`、`KnowledgeQueryAppService` | `knowledge_base` |
| **提示词库** | 提示词模板存库、CRUD、按编码/版本取用，覆盖框架默认提示词库 | `SysAiPrompt`、`AiPromptAppService`、`SaasAiPromptStore` | `ai_prompt` |

此外还有一条**Agent / MCP** 支线：知识检索被封装为技能（`KnowledgeRetrieveSkill`），框架技能注册表自动收纳，进而投影为对话工具与 MCP tools，供外部 AI 客户端（Claude Code / Cursor 等）调用。

模块前端页面挂在「开发工具（develop）」目录下：**AI 提供商**（`/develop/aiProvider`）、**知识库**（`/develop/knowledge`）与**AI 提示词**（`/develop/aiPrompt`），三者均仅对持有 `ai:read` / `knowledge_base:read` / `ai_prompt:read` 者（默认=超管通配 `*`）可见。

---

## Provider 库化管理

### 为什么存库而不写 appsettings

框架默认的 provider 配置源 `OptionsAiProviderConfigStore` 读 `XiHan:AI` 配置节兜底。BasicApp 把它**替换**为数据库实现 `SaasAiProviderConfigStore`，好处是：配置可在前端维护、天然按租户隔离、密钥加密落库、且改完能不重启热切换。

`SysAiProvider`（表 `Sys_Ai_Provider`）关键字段：

| 字段 | 列 | 说明 |
| --- | --- | --- |
| `ConfigCode` | `Config_Code` | 配置编码，**租户内唯一**（`UX_TeId_CoCd`，含 `IsDeleted` 末列，软删后可复用），上层解析器/配置源以它为 provider 键；**创建后不可变** |
| `ConfigName` | `Config_Name` | 配置名称（展示） |
| `Provider` | `Provider` | 提供商/协议标识，**自由字符串**（`OpenAI`/`DeepSeek`/`Azure`/`vLLM`/`Custom` 等），仅作分组与展示，**不是枚举** |
| `Model` | `Model` | 会话模型名 |
| `EmbeddingModel` | `Embedding_Model` | 嵌入模型名（RAG 用；与会话同端点同密钥、仅模型 id 不同，如 `text-embedding-3-small`；空则该 provider 不支持嵌入） |
| `BaseUrl` | `Base_Url` | 端点地址（空则用提供商默认端点，如 OpenAI 官方） |
| `ApiKey` | `Api_Key` | API 密钥，**可逆加密落库**，双 `[JsonIgnore]` 防外泄 |
| `MaxOutputTokens` / `Temperature` / `TimeoutSeconds` / `ExtraJson` | 同名列 | 采样与扩展参数，`ExtraJson` 免改表结构扩展 |
| `IsDefault` | `Is_Default` | 是否默认 provider（**租户内单默认**，软约束） |
| `IsEnabled` / `Status` | `Is_Enabled` / `Status` | 运行开关与状态；禁用或非 `Enabled` 均不参与配置源解析 |

::: tip 一个适配器打天下
OpenAI / DeepSeek / Azure / vLLM / Ollama / 自训模型多数走 OpenAI 兼容协议——`BaseUrl` 指向对应端点、填 `Model` 与 `ApiKey` 即可，无须每家写一个适配器。`Provider` 字段只是分组标签，真正决定行为的是 `BaseUrl` + `Model`。
:::

### 密钥加密

`ApiKey` 经 `DataProtectionAiProviderSecretProtector` 用 ASP.NET Core Data Protection 加密，密文带统一前缀，使用**独立 Purpose**（与存储密钥/短信密钥/邮件密码/租户连接串互不影响）。要点：

- **写入**：创建时明文提交、服务端加密；更新时 `ApiKey` **留空表示保留原密钥**，非空才替换重加密。
- **读出**：列表/详情 DTO **永不回读密钥**——只暴露布尔标志 `HasApiKey`；`[JsonIgnore]` 双注解在实体层再兜一道底。
- **解密**：只解本保护器写入的密文，去前缀直接解密，**失败即抛**（fail-closed，不做旧明文兼容）。
- **多实例部署**须共享 Data Protection 密钥环（持久化到共享存储），否则其它实例无法解密。

### 配置源解析

`SaasAiProviderConfigStore`（Singleton，经 `IServiceScopeFactory` 开作用域取 Scoped 仓储）实现框架的 `IAiProviderConfigStore`：

- `GetAsync(null)`：取**默认且启用**行（`IsDefault && IsEnabled && Status==Enabled`，按 `Sort` 取首条）。
- `GetAsync(configCode)`：按 `ConfigCode` 取启用行；无匹配返回 `null`（框架按「未配置」处理）。
- `GetAllAsync`：枚举全部启用行；单行解密失败**跳过并告警**，不阻断全表、不回退明文（fail-closed）。
- 映射为框架 `AiProviderOptions` 时，**`Provider` 键即 `ConfigCode`**，供解析器按此缓存。配置源本身**不做进程内缓存**——缓存在解析器那层。

### 热切换（改配置即时生效）

框架解析器（`IAiChatClientResolver` / `IAiEmbeddingGeneratorResolver`）按 provider 名缓存构建好的 `IChatClient` / 嵌入生成器，**不会自动感知 DB 变更**。所以 `AiProviderAppService` 在**每个写操作后**都调 `InvalidateResolvers()`：

```csharp
private void InvalidateResolvers()
{
    _chatClientResolver.Invalidate();      // 清空会话客户端缓存
    _embeddingResolver.Invalidate();       // 清空嵌入生成器缓存
}
```

`Create` / `Update` / `UpdateStatus` / `SetDefault` / `Delete` 全部在返回前 `Invalidate`——下次解析按最新 DB 配置重建，运维改完 key/baseUrl/model **无须重启**即生效。这是「库化管理 + 配置热切换」的闭环关键。

### 应用服务动作

`AiProviderAppService`（`[DynamicApi]`，无 Controller）暴露的动作：

| 动作 | 权限码 | 事务 | 说明 |
| --- | --- | --- | --- |
| `CreateAsync` | `ai:create` | `[UnitOfWork]` | 新增配置；`ConfigCode` 不能含空白、租户内不重复 |
| `UpdateAsync` | `ai:update` | `[UnitOfWork]` | 更新；`ConfigCode` 不可变，`ApiKey` 留空保留原值 |
| `UpdateStatusAsync` | `ai:update` | `[UnitOfWork]` | 启停状态 |
| `SetDefaultAsync` | `ai:update` | `[UnitOfWork]` | 设为默认（已禁用者不可设默认）；领域服务清除其它行 `IsDefault`（单默认互斥） |
| `DeleteAsync` | `ai:delete` | `[UnitOfWork]` | 软删 |
| `TestConnectionAsync` | `ai:execute` | 无 | `[HttpPost]`；用**一次性客户端**（不入解析器缓存）向 provider 发一句 `ping`，返回成功/耗时/finishReason——**可测尚未启用的配置** |

查询侧 `AiProviderQueryService`：`GetPageAsync`（`[HttpPost]`，`ai:read`，FLS 门控排序/过滤 + 默认兜底排序）与 `GetDetailAsync`。列表/详情均走 `HasApiKey` 布尔而非明文。

采样温度限定 `0~2`，越界抛异常。

---

## RAG 知识库

一条最小可用的 RAG：**摄取**（文档切片 → 嵌入 → 写向量库）与**检索问答**（query 嵌入 → 向量检索 → 注入 prompt → 会话生成带引用答案）。切片与向量落在 **Qdrant**，本模块的 `SysKnowledgeDocument`（表 `Sys_Knowledge_Document`）只存文档元信息与**原文**（`RawContent`，用于免重传重建索引）。

### 向量库与嵌入约定（以框架为准）

RAG 的底层实现由框架 `AddXiHanRAG()` 提供（切片/摄取/检索/增强），BasicApp 负责登记具体向量连接器与嵌入模型选择。以下约定源自框架 `VectorStoreKnowledgeRecord`，是**部署前必须对齐**的硬约束：

| 约定 | 值 | 说明 |
| --- | --- | --- |
| 向量库 | **Qdrant**（当前 preview） | BasicApp 经 `AddQdrantVectorStore(...)` 登记（gRPC），框架本身不引用 Qdrant |
| 集合名 | `xihan_knowledge` | 固定常量 |
| 主键 | `Guid`（Qdrant 只支持 `Guid`/`ulong` 键） | 由 `MD5(documentId:index)` **确定性派生**——同文档同序号恒等，重复摄取即覆盖（幂等） |
| 嵌入模型 | 取自 `SysAiProvider.EmbeddingModel` | 与会话同端点同密钥、仅模型 id 不同；空则该 provider 不支持嵌入 |
| 向量维度 | **`1536`（编译期常量，硬编码）** | 对齐 `text-embedding-3-small`。换用不同维度的嵌入模型**必须改框架常量并重建集合**，否则维度不匹配 |
| 距离 / 索引 | `CosineSimilarity` / `Hnsw` | — |

::: warning 换嵌入模型 = 改常量 + 重建集合
维度 `1536` 是框架编译期常量。若换到维度不同的嵌入模型（如某些 1024/3072 维模型），仅在前端改 `EmbeddingModel` 会导致 upsert/检索维度不匹配。这属于需改框架源码并重建 Qdrant 集合的动作，不是运行期配置项。拿不准的维度**以仓库为准**。
:::

### 文档摄取与索引

`SysKnowledgeDocument` 关键字段：`Title`、`SourceType`（`PasteText` / `UploadFile`，上传文件由前端读为文本后提交）、`Source`（文件名/标签，引用溯源用）、`RawContent`（原文）、`ChunkCount`（已入库切片数）、`EmbeddingProviderCode`（嵌入 provider 编码，空=默认 provider）、`Status`（`Pending` / `Indexed` / `Failed`）、`ErrorMessage`。

`KnowledgeDocumentAppService` 动作（均 `[HttpPost]` 除删除）：

| 动作 | 权限码 | 说明 |
| --- | --- | --- |
| `IngestAsync` | `knowledge_base:create` | 摄取：落库 `Pending` → 切片嵌入写 Qdrant → 回写 `Indexed`/`Failed` |
| `ReindexAsync` | `knowledge_base:update` | 重建：先按当前 `ChunkCount` 清旧向量，再用 `RawContent` 重新索引 |
| `DeleteAsync` | `knowledge_base:delete` | 先删向量再删文档 |

::: tip 摄取不套 UnitOfWork
摄取/重建含外部 I/O（嵌入 API + 向量库网络调用），领域服务**刻意不套 `[UnitOfWork]`**：先落库 `Pending`、索引后独立提交 `Indexed`/`Failed`，避免网络 I/O 期间长事务。进程中断则停留 `Pending`，可重建恢复。
:::

### 补偿清理（防孤儿向量）

摄取的一致性难点：向量已写进 Qdrant（N 条），但回写 `ChunkCount` 的 DB 更新失败——此后 DB 记 0、向量库有 N，后续按 `ChunkCount(=0)` 无法清理，**孤儿向量持续污染检索**。领域服务的处理：回写 DB 失败时，**补偿清理**刚写入的向量（`RemoveDocumentAsync(documentId, writtenChunkCount)`）以保证 DB 与向量库一致；补偿本身再失败则落错误日志（可能残留孤儿，需人工介入）。删除/重建也据 `ChunkCount` 枚举全部确定性主键来清向量。

### 检索与问答

`KnowledgeQueryAppService.QueryAsync`（`[HttpPost]`，`knowledge_base:execute`）：

1. `topK` 收敛到 `1~20`（默认取 RAG 配置的 `DefaultTopK`）。
2. 按**当前租户**过滤检索（无租户上下文=平台全局 `TenantId=0`）：`IKnowledgeRetriever.RetrieveAsync(query, topK, filter, provider)`。
3. 返回命中片段（`Citations`：文档 id / 序号 / 标题 / 来源 / 相似度 / 文本）。
4. 若 `Answer=true` 且有命中：`IRagPromptAugmenter.Augment` 把片段注入 prompt，走会话门面 `IXiHanAiService.ChatAsync` 生成**带引用的答案**（`Answer` 字段）。

请求里的 `Provider` 编码**同时用于嵌入与会话**；`Answer=false` 时只返回命中片段不调用大模型。

### RAG 部署配置

向量库连接属**部署级基础设施配置**，走 `appsettings` 的 `XiHan:AI:Rag` 节（`XiHanRagOptions`）：

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `QdrantHost` | `localhost` | Qdrant 主机（gRPC） |
| `QdrantPort` | `6334` | gRPC 端口 |
| `QdrantHttps` | `false` | Qdrant Cloud 用 `true` + `QdrantApiKey` |
| `QdrantApiKey` | `null` | 云端鉴权 |
| `DefaultTopK` | `5` | 检索默认返回条数 |

```json
{
  "XiHan": {
    "AI": {
      "Rag": {
        "QdrantHost": "localhost",
        "QdrantPort": 6334,
        "QdrantHttps": false,
        "DefaultTopK": 5
      }
    }
  }
}
```

---

## 提示词库

独立于 Provider 与 RAG 的第三条能力：把框架 `IAiPromptStore` 抽象的默认 Options 实现**替换**为数据库实现，让「发给大模型的提示词模板」从代码/配置搬进库里维护。提示词**非机密**，无需加密。

`SysAiPrompt`（表 `Sys_Ai_Prompt`）关键字段：

| 字段 | 列 | 说明 |
| --- | --- | --- |
| `PromptCode` | `Prompt_Code` | 提示词编码，**租户内唯一**（`UX_TeId_PrCd`，含 `IsDeleted` 末列，软删后可复用），上层按此取用；**创建后不可变** |
| `PromptName` | `Prompt_Name` | 提示词名称（展示） |
| `Category` | `Category` | 分组/分类（可选） |
| `Version` | `Version` | 版本（可选；空代表当前/最新，按名 + 版本显式选取，无默认标记） |
| `Content` | `Content` | 提示词正文（可含占位/Scriban 变量） |
| `IsEnabled` / `Status` | `Is_Enabled` / `Status` | 运行开关与状态；禁用或非 `Enabled` 均不参与库解析 |

`SaasAiPromptStore`（Singleton，经 `IServiceScopeFactory` 开作用域取 Scoped 仓储）实现框架 `IAiPromptStore`：

- `GetAsync(name, version)`：按 `PromptCode`（+可选 `Version`）取启用行，映射为框架 `AiPromptTemplate`（`Name`=`PromptCode`、`Content`、`Version`、`Description`=`PromptName`）；无匹配返回 `null`。
- `ListAsync`：枚举全部启用行。

::: tip Replace 而非 TryAdd
框架 `AddXiHanAI` 已 `TryAddSingleton` 默认 Options 提示词库，`AddPromptStore` 必须用 `Replace` 覆盖——`TryAdd` 会被静默忽略，导致 DB 里的提示词永不生效。这与 provider 配置源的覆盖方式（`AddAIConfigStore`）同构。
:::

`AiPromptAppService`（`[DynamicApi]`，无 Controller）暴露的动作：`CreateAsync`（`ai_prompt:create`）、`UpdateAsync`（`ai_prompt:update`，`PromptCode` 不可变）、`UpdateStatusAsync`（`ai_prompt:update`）、`DeleteAsync`（`ai_prompt:delete`，软删），均 `[UnitOfWork]`。查询侧 `AiPromptQueryService`：`GetPageAsync`（`[HttpPost]`，`ai_prompt:read`，FLS 门控排序/过滤 + 默认排序 `Sort` 升序→`CreatedTime` 降序）与 `GetDetailAsync`。

::: warning 当前无消费方
v1 仅提供提示词的库化 CRUD 与框架存储层替换（`IAiPromptStore`），BasicApp 内尚无对话/技能/RAG 环节显式调用 `IAiPromptStore.GetAsync` 取用——RAG 问答走的是独立的 `IRagPromptAugmenter.Augment` 拼装 prompt，不经此库。提示词库是面向未来对话/技能场景预置的模板仓库。
:::

---

## Agent 与 MCP

BasicApp 通过框架把知识检索能力对外暴露为标准工具，两条投影链路：

**技能 → 对话工具 / MCP 工具**：`KnowledgeRetrieveSkill` 实现框架 `IAiSkill`（名 `knowledge_retrieve`），在模块 `AddAISkills()` 里注册为单例。框架 `DefaultAiSkillRegistry` **构造时自动收纳** DI 里全部 `IAiSkill`，一处注册即同时可用于：

- **对话工具**：`skill.AsFunction()` 产出 `AIFunction`，可挂进 Agent 的工具集（框架 `IXiHanAgentFactory` 经 `IChatClient.AsAIAgent(...)` 产出 MAF 原生 `AIAgent`，支持工具调用与多轮会话）。
- **MCP tools**：框架把技能 `AsFunction()` 经官方 `McpServerTool.Create` 桥接进 MCP 工具集。

`KnowledgeRetrieveSkill` **只读、无副作用**，故 MCP 暴露安全（无需批准）。经 MCP 调用时无用户/租户上下文（应用管理的 key 为平台级凭据），检索**不加租户过滤**（知识库文档视为平台级）；`topK` 内部收敛 `1~20`。

::: warning 工具会被自动执行
框架工厂套了 `UseFunctionInvocation`、MAF Agent 内部也套 `FunctionInvokingChatClient`——工具/技能会被**自动调用、无人工批准**。当前 v1 技能均只读（知识检索）安全；将来接入有副作用的技能须自行加批准/审计。
:::

### MCP Server（应用管理 key，fail-closed）

MCP Server 的启用与暴露由框架包 [XiHan.Framework.Web.Mcp](https://framework.docs.xihanfun.com/packages/web-mcp) 负责；BasicApp 通过 `XiHanBasicAppWebCoreModule` 依赖 `XiHanWebMcpModule`，WebHost 无需重复声明。配置节 `XiHan:AI:Mcp`（`XiHanMcpOptions`）：

| 字段 | 默认 | 说明 |
| --- | --- | --- |
| `Enabled` | `false` | 是否启用（默认关） |
| `ApiKey` | `null` | **应用管理的**访问密钥，外部 MCP 客户端须携带 |
| `HeaderName` | `X-Api-Key` | 携带密钥的请求头（或 `Authorization: Bearer`） |
| `Path` | `/mcp` | 端点路径 |
| `Stateless` | `true` | 无状态 HTTP（检索类工具足够） |

鉴权是**应用管理的 key**、不是用户令牌：`/mcp` 端点 `AllowAnonymous()` 绕过全局授权 FallbackPolicy，改由 `McpApiKeyEndpointFilter` 端点过滤器守门——请求头不匹配即 401。

**fail-closed**：`IsExposable = Enabled && !string.IsNullOrWhiteSpace(ApiKey)`。**未启用或未配 `ApiKey` 则根本不注册、不映射 `/mcp` 端点**——宁可不暴露，也不裸奔。

---

## 部署前置

启用 AI / 知识库能力前，须准备：

1. **Qdrant 向量库**：可连接的 Qdrant 实例，`XiHan:AI:Rag` 节配好 `QdrantHost`/`QdrantPort`（云端另加 `QdrantHttps` + `QdrantApiKey`）。未注册向量库而调用 RAG 会因缺依赖失败。
2. **嵌入模型对齐 `1536` 维**：默认 `text-embedding-3-small`（1536 维）。换维度须改框架常量并**重建 Qdrant 集合**。
3. **准备数据库结构**：全新数据库首次启动会自动建立 AI 相关表并执行种子；存量数据库应通过对应版本的 `UpdateScripts` 前向迁移。
4. **provider 配置入库**：库化管理下 provider 不写 appsettings——建库后在前端「AI 提供商」页新增配置、填好 `Model`/`EmbeddingModel`/`ApiKey`、设默认并测连接。
5. **（可选）MCP**：需要对外暴露 MCP tools 时，`XiHan:AI:Mcp` 显式 `Enabled=true` 并配 `ApiKey`。
6. **多实例**：共享 Data Protection 密钥环，否则各实例无法互相解密 provider 密钥。

种子说明：AI provider、知识库与提示词库各占独立种子 Order 段（`200-204` / `205-208` / `209-212`，晚于 Saas 的 `10-37` 与代码生成的 `100-105`），操作字典复用 AI provider 段的 `SysOperationSeeder`（200），链内顺序均为「（操作字典 →）资源 → 权限 → 菜单 → 角色授权」，权限**仅授超管**（其余角色/租户按需在权限管理里下放）。

---

## 相关文档

- [框架 · XiHan.Framework.AI](https://framework.docs.xihanfun.com/packages/ai)：底层 provider 解析/热切换、RAG 切片检索、提示词模板、Agent/MCP 桥接的抽象与默认实现
- [权限模型](./permission)：`module:resource:action` 权限码、数据范围、字段级脱敏
- [多租户](./multi-tenancy)：字段级隔离与 `TenantId=0` 全局约定
- [部署](../deployment)：环境要求、升级入口与生产配置
