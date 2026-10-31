# MCP Server

本页说明 6.0 自带的 MCP 端点与开发时可用的 MCP 服务端，适用于把 BasicApp 接入 AI 客户端或用 AI 工具开发 BasicApp 的开发者。

与 5.x 不同，6.0 的宿主本身提供 MCP 服务：宿主映射 `/mcp`，声明了 `Expose.Mcp` 的操作自动成为 MCP 工具，与 HTTP 接口共用同一份操作契约，认证、租户、权限与审计规则相同。编写前端代码时，还可以接入 XiHan.UI 的 MCP 服务端查询组件 API。

## BasicApp 的 MCP 端点

### 协议

| 项 | 说明 |
| --- | --- |
| 端点 | `POST /mcp`（开发态 `http://localhost:9710/mcp`，前端开发服务器也代理了 `/mcp`） |
| 协议 | MCP `2025-06-18`，JSON-RPC 2.0，Streamable HTTP；一条消息一个答复，不开 SSE、不建会话 |
| 认证 | 请求头 `Authorization: Bearer <访问令牌>`，与 HTTP 接口同一个令牌；凭据无效即 401 |
| 区域 | `Accept-Language` 决定工具标题、说明与错误文案的语言 |
| 服务名 | `XiHan:Protocols:Mcp:ServerName`，缺省 `XiHan.BasicApp` |

每个 `tools/call` 都是一次独立的内核调用：以令牌的主体与租户执行，经过权限判定、数据范围、字段安全与审计，和管理端点按钮没有区别。

### 工具

工具名由操作 ID 推导：`<资源>_<动词>`（资源段里的连字符保留，如 `dict-items_list`）。目前发布 12 个只读目录类操作：

| 工具 | 操作 | 权限码 |
| --- | --- | --- |
| `users_get` / `users_list` | `users.get` / `users.list` | `identity.users.read` |
| `departments_get` / `departments_tree` | `departments.get` / `departments.tree` | `organization.departments.read` |
| `positions_get` / `positions_list` | `positions.get` / `positions.list` | `organization.positions.read` |
| `memberships_list` | `memberships.list` | `organization.memberships.read` |
| `permissions_list` | `permissions.list` | `access.permissions.read` |
| `dicts_get` / `dicts_list` / `dicts_options` | `dicts.get` / `dicts.list` / `dicts.options` | `settings.dicts.read` / 登录即可 |
| `dict-items_list` | `dict-items.list` | `settings.dict-items.read` |

- `inputSchema` / `outputSchema` 就是操作的请求 / 响应 Schema；
- `title` / `description` 是操作的本地化文案；
- 只读工具带 `readOnlyHint`；
- 失败的调用返回 `isError: true`，文本是本地化的错误说明，错误码在结果里。

管理类写操作不发布到 MCP。新增 MCP 工具：操作必须只读（`get` / `list` 或声明 `ReadOnly = true`），在 `Expose` 里加 `Expose.Mcp | Expose.Ai`，并登记到架构测试 `OperationExposureTests`。

### 接入客户端

以 Claude Code 为例，在项目根的 `.mcp.json`：

```json
{
  "mcpServers": {
    "xihan-basicapp": {
      "type": "http",
      "url": "http://localhost:9710/mcp",
      "headers": { "Authorization": "Bearer <访问令牌>" }
    }
  }
}
```

访问令牌用 `auth.login` 取得（缺省 120 分钟有效）。令牌代表一个真实用户：给 AI 用的账号按最小权限授予，并留意工具调用同样进访问日志。

### 调试

```bash
curl -X POST http://localhost:9710/mcp \
  -H 'Authorization: Bearer <访问令牌>' -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

## 开发用的 MCP 服务端

| 服务端 | 用途 | 6.0 现状 |
| --- | --- | --- |
| `xihan-ui`（npm `@xihan-ui/cli`） | 组件 API、部件、示例与设计令牌 | 可用，见 [XiHan.UI 的 MCP Server](https://ui.docs.xihanfun.com/ai/mcp)；版本取 `frontend-next/pnpm-workspace.yaml` catalog 里的 `@xihan-ui/vue` |
| `xihan-framework` | 查框架包、模块与配置键 | 5.x 的 `XiHan.Framework.Cli` 面向框架 4.x；框架 5 的 CLI 目前只有打包、迁移等命令，没有 MCP。写后端时直接读框架仓库 `next/` 的源码、`spec/` 与各包 README |

## 相关

- [For Agents](./agents) · [LLMs.txt](./llms) · [AI 能力（规划中）](../backend/ai)
