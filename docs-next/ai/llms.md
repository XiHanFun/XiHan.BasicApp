# LLMs.txt

文档站在构建时产出一组给大语言模型读的机读资产，内容与站点正文同源。

::: info 站点地址
6.0 文档还没有单独的域名。本站构建产物里的绝对地址按 6.0 GA 时替换 5.x 文档的假设，写的是 `basicapp.docs.xihanfun.com`；预览期请以实际部署地址为准，或直接读仓库 `next` 分支的 `docs-next/` 源文件。
:::

## 可用资源

构建后在站点根目录下：

| 文件 | 说明 |
| --- | --- |
| `llms.txt` | 索引：全部页面的标题、一句话摘要与地址，以及下列资产的入口 |
| `llms-full.txt` | 全站正文 |
| `llms-backend.txt` | 后端手册：能力包、操作契约、认证授权、多租户、数据与平台能力 |
| `llms-frontend.txt` | 前端手册：契约与请求、路由与菜单、权限、SchemaPage、主题、国际化与实时通信 |

**单页 Markdown**：任何一页的地址后加 `.md` 即可取到这一页的 Markdown（如 `/api-guide.md`）；每页正文上方的「取本页 Markdown」也指向它。开发服务器上同样可用。

## 在 AI 工具中使用

```text
阅读 docs-next 站点的 llms.txt 了解 XiHan.BasicApp 6.0；按需读取其中列出的页面（地址后加 .md 取 Markdown）。
以文档、仓库根的 AGENTS.md、nextdocs/下一代总体方案.md 与 backend-next / frontend-next 的代码为准，不要沿用 5.x（backend/、frontend/、docs/）的写法。
```

| 工具 | 做法 |
| --- | --- |
| Claude Code | 写进仓库的 `CLAUDE.md`（它链接到 `AGENTS.md`） |
| Codex | 写进仓库的 `AGENTS.md` |
| Cursor | 写进 `.cursor/rules`，或在对话中添加文档 |
| GitHub Copilot | 写进 `.github/copilot-instructions.md` |

## 相关

- [For Agents](./agents) · [MCP Server](./mcp)
