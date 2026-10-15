来源：https://basicapp.docs.xihanfun.com/ai/agents

# For Agents

在 XiHan.BasicApp 上做二次开发时，怎样让 Claude Code、Cursor、Codex、GitHub Copilot 这类 AI 编程工具写出符合项目约定的代码。

::: tip 和「AI 模块」不是一回事
本册讲的是**用 AI 工具开发 BasicApp**。BasicApp 自带的大模型接入、智能体与知识库功能见后端手册的 [AI 模块](../backend/ai)。
:::

BasicApp 是克隆下来二次开发的应用，没有单独的命令行或 MCP 服务端；AI 工具需要的资料分三处：仓库自带的 Agent 规范与技能、后端依赖的 [XiHan.Framework](https://framework.docs.xihanfun.com/ai/agents) 与前端依赖的 [XiHan.UI](https://ui.docs.xihanfun.com/ai/agents) 各自的 MCP 服务端，以及本站的机读文档。

## 复制这段提示词

把下面这段放进对话，或补进仓库根 `AGENTS.md` 的开头：

```text
这是基于 XiHan.BasicApp 的项目：后端 .NET + XiHan.Framework（backend/），前端 Vue 3 + XiHan.UI（frontend/）。
开始前先读仓库根的 AGENTS.md，并按任务读取 .agents/skills/ 下对应的技能：
后端读 backend-development，前端读 frontend-development，跨前后端的 DTO、权限码、菜单与租户契约读 fullstack-contracts。

框架与组件库的 API 以项目引用的版本为准，不要凭记忆写：
- 后端的包、模块类、[DependsOn] 与配置键用 xihan-framework MCP 工具查（xihan_framework_info / xihan_framework_config / xihan_framework_search）；
- 前端的组件名、部件、Props 与设计令牌用 xihan-ui MCP 工具查（xihan_ui_info / xihan_ui_demo / xihan_ui_parts / xihan_ui_token）。
没有 MCP 时读 https://basicapp.docs.xihanfun.com/llms.txt、https://framework.docs.xihanfun.com/llms.txt 与 https://ui.docs.xihanfun.com/llms.txt。
```

## 仓库自带的 Agent 规范

| 文件 | 内容 |
| --- | --- |
| `AGENTS.md` | 仓库概览、开始前必须做的检查、目录结构、常用命令、后端与前端架构约束、前后端契约、配置运维、测试与提交规范、禁止事项 |
| `CLAUDE.md` | 指向 `AGENTS.md` 的链接，Claude Code 打开仓库时自动读取 |
| `.agents/skills/` | 按任务划分的技能，`.claude/skills` 链接到这里 |

技能按任务加载，不必一次读完：

| 技能 | 什么时候用 |
| --- | --- |
| `backend-development` | 实现或重构后端模块：Dynamic API、DDD/CQRS、SqlSugar、实体、仓储、权限、租户、事务、种子 |
| `frontend-development` | 实现或重构 Vue 管理端：XiHan.UI、类型化 API、Pinia、动态路由、插件模块、权限可见性、统一视觉 |
| `fullstack-contracts` | 改动跨前后端的契约：Dynamic API 服务与 DTO、权限码、租户隔离、菜单路由、可选模块、配置与部署边界 |
| `test-review` | 审查测试是否有效、重复、过度 mock |
| `commit-msg` · `create-pr` · `changelog-collect` · `version-release` · `issue-reply` | 提交、PR、更新日志、发版与 Issue 回复 |

Claude Code 与 Codex 打开仓库即可读到 `AGENTS.md`；Cursor、GitHub Copilot 等工具可以在各自的规则文件里写一句「先读仓库根的 AGENTS.md」。

## 接入两个 MCP 服务端

后端用 XiHan.Framework 的 MCP 服务端查包、模块与配置键，前端用 XiHan.UI 的 MCP 服务端查组件 API、示例与令牌。两者都离线、按版本钉住，配置方法见 [MCP Server](./mcp)。

最省事的做法是在仓库根运行两条 `setup`，各自读出后端与前端引用的版本，写进同一个 `.mcp.json`：

```bash
dnx XiHan.Framework.Cli --yes -- setup --client claude
npx -y @xihan-ui/cli setup --client claude
```

## 机读文档

本站在构建时产出 [机读资产](./llms)：`llms.txt` 索引、`llms-full.txt` 全站正文、`llms-backend.txt` 后端手册与 `llms-frontend.txt` 前端手册，每一页在站点地址后加 `.md` 即可取到 Markdown。

## 相关

- [目录结构与代码地图](../project-structure) · [接口对接指南](../api-guide) · [开发环境](../dev-environment)
- [XiHan.Framework 的 AI 辅助开发](https://framework.docs.xihanfun.com/ai/agents) · [XiHan.UI 的 AI 辅助开发](https://ui.docs.xihanfun.com/ai/agents)
