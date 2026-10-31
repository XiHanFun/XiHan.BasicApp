# For Agents

在 6.0（`next` 分支）上做开发时，怎样让 Claude Code、Codex、Cursor、GitHub Copilot 这类 AI 编程工具写出符合项目约定的代码。

::: tip 和「AI 模块」不是一回事
本页讲的是**用 AI 工具开发 BasicApp**。BasicApp 自带的 AI 功能（提供商、知识库、助手）属于 N4，规划中，见后端手册的 [AI 能力](../backend/ai)。
:::

## 复制这段提示词

```text
这是 XiHan.BasicApp 6.0（next 分支）：后端 .NET 10 + XiHan.Framework 5（backend-next/），前端 Vue 3 + XiHan.UI（frontend-next/）。
5.x 代码在 backend/、frontend/、docs/，不要改、也不要照抄它的写法。
开始前先读仓库根的 AGENTS.md 与 nextdocs/下一代总体方案.md 的相关章节，以代码为准。

后端规则：
- 每个公开方法一个 [Operation("<资源>.<动词>")]，显式写 Expose（XHG004 编译期强制），返回 Result；不写 Controller 或 Minimal API 业务接口。
- 权限码 <包>.<资源>.<动作>，在 <包>Permissions.cs 用 PermissionDescriptor 声明；按钮即操作，没有按钮码。
- 平台库实体不实现 IMultiTenant（需要属主用 OwnerTenantId），租户库实体用 TenantScoped* 基类；分页查询以 TenantScopedQuery.For<T> 起手。
- 文案不进代码：<包>Messages + Localization/<区域>.json × 10，键集一致。
- 请求 DTO 有缺省值的属性用 set 不用 init；时间用 DateTimeOffset（UTC）。

前端规则：
- 不手写 API：改了后端就 contracts export → pnpm gen:contracts，调用 import { xxx } from '~/contracts'。
- 页面放 src/modules/<包>/views/<路径>/index.vue，列表用 SchemaPage，按钮 permission 填操作的权限码。
- 只用 XiHan.UI 与语义令牌，不新增裸 px；10 种区域文案键集对称。
- 完成前跑 pnpm check、pnpm test、pnpm build；后端跑 dotnet build 与 dotnet test --solution backend-next/XiHan.BasicApp.slnx。
```

## 仓库自带的 Agent 规范

| 文件 | 内容 |
| --- | --- |
| `AGENTS.md` | 仓库概览、开始前的检查、目录结构、命令、架构约束、前后端契约、配置运维、测试与提交规范、禁止事项、分支与 worktree 规则 |
| `CLAUDE.md` | 指向 `AGENTS.md`，Claude Code 打开仓库时自动读取 |
| `.agents/skills/` | 按任务划分的技能，`.claude/skills` 链接到这里 |
| `nextdocs/下一代总体方案.md` | 6.0 的全局决定（D1–D12）、包清单、分期计划与各批次的落地状态 |
| `backend-next/README.md`、各包 `README.md` | 后端布局、命令与规则 |
| `frontend-next/README.md` | 前端结构、命令、契约层与会话 / 路由说明 |

::: warning 技能多数针对 5.x
`.agents/skills/` 里的 `backend-development`、`frontend-development`、`fullstack-contracts` 等技能描述的是 5.x 的 Dynamic API、`*.types.ts` 与手写 facade 写法。在 `backend-next/`、`frontend-next/` 上工作时，以本站、`nextdocs/` 方案与代码为准，技能里与 6.0 冲突的说法不要照做。
:::

## 6.0 的门禁就是规范

很多约定由测试与脚本强制，AI 写完代码跑一遍即可发现违规：

| 门禁 | 守什么 |
| --- | --- |
| `XiHan.BasicApp.Architecture.Tests` | 包层级、`Expose` 显式与超出 HTTP 的登记表、权限码文法与资源段一致、匿名白名单、目录名白名单、10 种区域键集、DTO 用 `set`、XML 文档格式、框架锁定提交 |
| `XHG004`（编译期） | 操作没写 `Expose` 即编译失败 |
| `pnpm check` | 类型、lint、裸 px、令牌、文案、模块、契约漂移、权限码存在 |

## 框架与组件库的资料

- XiHan.Framework 5 的源码与规范在框架仓库的 `next/`（`next/README.md`、`next/spec/`、各包 `README.md`），文档站在框架仓库的 `docs-next/`；
- XiHan.UI 见 [ui.docs.xihanfun.com](https://ui.docs.xihanfun.com)；
- 机读文档见 [LLMs.txt](./llms)，MCP 见 [MCP Server](./mcp)。

## 相关

- [目录结构](../project-structure) · [后端开发流程](../backend/development) · [前端开发流程](../frontend/development)
