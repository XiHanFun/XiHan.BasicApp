# 契约、安全与运维规范

## 前后端契约清单

改变 Dynamic API 前逐项核对：

1. 服务名、方法名、HTTP 语义和 route value。
2. 请求/响应 DTO 字段、nullable、枚举、ID、日期和分页。
3. 后端 Mapper 与前端 `*.types.ts`。
4. API facade、页面、store、测试和文档调用点。
5. 权限码、权限种子、角色授权与 UI 可见性。
6. 后端菜单 Component 路径和前端动态路由映射。

不同时保留两套字段或两条 API 来“平滑”未经要求的变更。确需兼容时必须有明确期限、迁移方和删除条件。

## 安全边界

- 认证、授权、租户和数据范围必须在后端执行。
- 每个 Dynamic API 方法显式具有权限、匿名或经审查的例外状态。
- 客户端传入 TenantId、UserId、角色或数据范围不能直接作为信任事实。
- DTO 不返回密码、密钥、刷新令牌、内部连接信息或无关个人数据。
- 日志不得记录凭据、完整令牌、敏感请求体或跨租户数据。
- 文件、模板、脚本和 AI 输入必须沿用现有验证、大小限制与权限策略。

## Framework 引用模式

- 单独构建 `backend/XiHan.BasicApp.slnx` 默认使用 NuGet 包，保证仓库可独立克隆和构建。
- 仓库根 XiHanFun 工作区解决方案可以使用同级 Framework 源码。
- 需要强制模式时显式设置 `-p:UseXiHanFrameworkSource=true|false`。
- 不通过探测同级目录自动切换，不在提交中混入本地路径特例。

## 配置和健康检查

- 数据库是应用启动硬依赖；连接失败应清晰暴露。
- Redis 可选行为、Qdrant 可用性和各健康检查等级以当前代码与配置为准。
- 不为让健康页变绿而吞掉真实依赖失败或篡改状态。
- 本地访问地址从 `launchSettings.json`、`appsettings*.json`、环境变量或启动日志读取；脚本不要硬编码端口。
- 示例配置使用占位符，绝不提交生产密码、连接串或 Token。

## 数据库升级

- PostgreSQL 脚本放在 `backend/src/main/XiHan.BasicApp.WebHost/UpdateScripts/<version>/<version>.sql`。
- 脚本可审阅、尽量幂等，并显式处理已有数据、索引和回滚风险。
- 应用不自动执行版本脚本；数据库变更由明确的升级流程触发。
- 未经用户明确授权，不连接生产库、不执行迁移、不修改外部数据。

## 运行与发布

- 启动 WebHost 时指向 `XiHan.BasicApp.WebHost.csproj` 和明确 launch profile。
- 修改配置、健康检查、SignalR、后台任务或分布式存储时，验证启动、关闭、重连、超时和资源释放。
- 部署、容器推送、数据库升级、外部消息和发布会改变外部状态，只在用户明确要求时执行。
- 诊断 OOM 时分别收集宿主内存压力、进程指标、对象/集合增长和连接数量，不能把长连接存在直接等同于内存泄漏。
