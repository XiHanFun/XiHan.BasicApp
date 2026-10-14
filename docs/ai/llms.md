# LLMs.txt

文档站在构建时产出一组给大语言模型读的机读资产，内容与站点正文同源。AI 工具读取它们即可了解 BasicApp 的结构、约定与功能。

## 可用资源

| 文件 | 说明 |
| --- | --- |
| [llms.txt](https://basicapp.docs.xihanfun.com/llms.txt) | 索引：全部页面的标题、一句话摘要与地址，以及下列资产的入口 |
| [llms-full.txt](https://basicapp.docs.xihanfun.com/llms-full.txt) | 全站正文 |
| [llms-backend.txt](https://basicapp.docs.xihanfun.com/llms-backend.txt) | 后端手册：模块、Dynamic API、权限、租户、数据与可选业务模块 |
| [llms-frontend.txt](https://basicapp.docs.xihanfun.com/llms-frontend.txt) | 前端手册：请求与类型化 API、路由与菜单、权限、组件与 SchemaPage、主题、国际化与实时通信 |

**单页 Markdown**：任何一页的站点地址后加 `.md` 即可取到这一页的 Markdown，如 [api-guide.md](https://basicapp.docs.xihanfun.com/api-guide.md)、[project-structure.md](https://basicapp.docs.xihanfun.com/project-structure.md)。每页正文上方的「取本页 Markdown」也指向它。

BasicApp 建立在 XiHan.Framework 与 XiHan.UI 之上，两者各有一套机读资产：

| 站点 | 索引 |
| --- | --- |
| 开发框架 | [framework.docs.xihanfun.com/llms.txt](https://framework.docs.xihanfun.com/llms.txt) |
| 视图组件 | [ui.docs.xihanfun.com/llms.txt](https://ui.docs.xihanfun.com/llms.txt) |

## 在 AI 工具中使用

所有工具都可以用同一段提示词：

```text
阅读 https://basicapp.docs.xihanfun.com/llms.txt 了解 XiHan.BasicApp；后端用到框架能力时再读 https://framework.docs.xihanfun.com/llms.txt，前端用到组件时再读 https://ui.docs.xihanfun.com/llms.txt。按需读取其中列出的页面（站点地址后加 .md 取 Markdown），以文档和仓库根的 AGENTS.md 为准。
```

| 工具 | 做法 |
| --- | --- |
| Claude Code | 写进仓库的 `CLAUDE.md`（它链接到 `AGENTS.md`），或用 `/memory` 记下 |
| Cursor | 在对话中用 `@Docs` 添加上面三个索引，或写进 `.cursor/rules` |
| Codex | 写进仓库的 `AGENTS.md` |
| GitHub Copilot | 写进 `.github/copilot-instructions.md` |

能运行命令或接入 MCP 的工具，优先用 [MCP Server](./mcp)：数据离线且与项目引用的版本一致。

## 相关

- [For Agents](./agents) · [MCP Server](./mcp)
