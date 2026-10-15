来源：https://basicapp.docs.xihanfun.com/ai/mcp

# MCP Server

BasicApp 没有自己的 MCP 服务端：后端与前端分别接入 XiHan.Framework 与 XiHan.UI 的服务端，AI 写后端代码时查框架的包、模块与配置键，写前端代码时查组件的 API、示例与令牌。两者都随包离线发布，按项目引用的版本钉住。

| 服务端 | 由谁提供 | 管什么 | 文档 |
| --- | --- | --- | --- |
| `xihan-framework` | .NET 工具 `XiHan.Framework.Cli` | 包与模块类、`[DependsOn]`、包依赖、配置节的属性与缺省值、框架文档 | [框架的 MCP Server](https://framework.docs.xihanfun.com/ai/mcp) |
| `xihan-ui` | npm 包 `@xihan-ui/cli` | 组件 API、部件与样式钩子、三种写法的示例、设计令牌、design.md | [组件库的 MCP Server](https://ui.docs.xihanfun.com/ai/mcp) |

## 用 setup 写入

在仓库根运行。两条命令分别扫描 `backend/` 的项目文件与 `frontend/` 的依赖读出版本，写进仓库根同一个配置文件，各自只改自己那一项：

```bash
dnx XiHan.Framework.Cli --yes -- setup --client claude    # 写 xihan-framework，并写入框架技能
npx -y @xihan-ui/cli setup --client claude                # 写 xihan-ui，并写入组件库技能
```

`--client` 可换成 `cursor`（`.cursor/mcp.json`）、`vscode`（`.vscode/mcp.json`）或 `codex`（打印 `~/.codex/config.toml` 片段）。只要 MCP 配置不要技能时加 `--mode mcp`；加 `--dry-run` 只打印不写文件，加 `--check` 核对配置是否就绪。需要 .NET 10 SDK（`dnx`）与 Node ≥ 18（`npx`）。

## 手写配置

仓库根的 `.mcp.json`（Claude Code）或 `.cursor/mcp.json`（Cursor）：

```json
{
  "mcpServers": {
    "xihan-framework": {
      "command": "dnx",
      "args": ["XiHan.Framework.Cli@<框架版本>", "--yes", "--", "mcp"]
    },
    "xihan-ui": {
      "command": "npx",
      "args": ["-y", "@xihan-ui/cli@<组件库版本>", "mcp"]
    }
  }
}
```

两个版本号分别取自：

| 版本 | 从哪里看 |
| --- | --- |
| 框架版本 | `backend/` 下各项目文件里 `XiHan.Framework.*` 的 `Version` |
| 组件库版本 | `frontend/pnpm-workspace.yaml` 的 catalog 里 `@xihan-ui/vue` 的版本 |

Windows（非 WSL）上 Claude Code 需要经 `cmd /c` 调用 `dnx` 与 `npx`：把 `command` 写成 `cmd`，`args` 前面加上 `"/c"` 与原来的命令名，`setup --client claude` 会自动这样写。VS Code 的 `.vscode/mcp.json` 顶层键是 `servers`，每一项再加 `"type": "stdio"`。

## 相关

- [For Agents](./agents) · [LLMs.txt](./llms)
