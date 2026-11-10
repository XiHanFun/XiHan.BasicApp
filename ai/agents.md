来源：https://basicapp.docs.xihanfun.com/ai/agents

# For Agents

本页说明使用 Claude Code、Codex、Cursor、GitHub Copilot 等 AI 编程工具时，如何让生成的代码符合项目约定，适用于借助 AI 工具开发 BasicApp 的开发者。

::: tip 与 AI 模块的区别
本页讨论的是用 AI 工具开发 BasicApp。BasicApp 自带的 AI 功能（提供商、提示词、知识库、助手）见后端手册的 [AI 能力](../backend/ai)。
:::

## 提示词

```text
这是 XiHan.BasicApp：后端 .NET 10 + XiHan.Framework（backend/），前端 Vue + XiHan.UI（frontend/）。
开始前先读仓库根的 AGENTS.md 与对应的 .agents/skills 技能，以代码为准。

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
- 完成前跑 pnpm check、pnpm test、pnpm build；后端跑 dotnet build 与 dotnet test --solution backend/XiHan.BasicApp.slnx。
```

## 仓库自带的 Agent 规范

| 文件 | 内容 |
| --- | --- |
| `AGENTS.md` | 仓库概览、开始前的检查、目录结构、命令、架构约束、前后端契约、配置运维、测试与提交规范、禁止事项、分支与 worktree 规则 |
| `CLAUDE.md` | 链接到 `AGENTS.md`，Claude Code 打开仓库时自动读取 |
| `.agents/skills/` | 按任务划分的技能，`.claude/skills` 链接到这里 |
| `plans/` | 总体方案：全局决定（D1–D12）、包清单与分期计划；与代码不一致时以代码为准 |
| `backend/README.md`、各包 `README.md` | 后端布局、命令与规则 |
| `frontend/README.md` | 前端结构、命令、契约层与会话 / 路由说明 |

技能按任务划分：`backend-development`（后端能力包、操作、数据与测试）、`frontend-development`（前端页面、契约层与视觉）、`fullstack-contracts`（前后端契约、配置与运维），另有 `commit-msg`、`create-pr`、`issue-reply`、`test-review`、`changelog-collect` 与 `version-release` 等流程技能。

## 由门禁检查的约定

以下约定由测试与脚本强制执行，生成代码后运行一次即可发现违规：

| 门禁 | 守什么 |
| --- | --- |
| `XiHan.BasicApp.Architecture.Tests` | 包层级、`Expose` 显式与超出 HTTP 的登记表、权限码文法与资源段一致、匿名白名单、目录名白名单、10 种区域键集、DTO 用 `set`、XML 文档格式、框架锁定提交 |
| `XHG004`（编译期） | 操作没写 `Expose` 即编译失败 |
| `pnpm check` | 类型、lint、裸 px、令牌、文案、模块、契约漂移、权限码存在 |

## 框架与组件库的资料

- XiHan.Framework：以锁定提交的源码为准（同级检出的 `framework/src`，规范在 `framework/spec`，各包带 `README.md`）；文档站 [framework.docs.xihanfun.com](https://framework.docs.xihanfun.com)，框架的离线知识库 MCP 见 [MCP Server](./mcp#开发用的-mcp-服务端)；
- XiHan.UI：[ui.docs.xihanfun.com](https://ui.docs.xihanfun.com)；
- 本站的机读文档见 [LLMs.txt](./llms)。

## 相关

- [目录结构](../project-structure) · [后端开发流程](../backend/development) · [前端开发流程](../frontend/development)
