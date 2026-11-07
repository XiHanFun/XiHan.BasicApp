# backend-next

XiHan.BasicApp 6.0 后端（方案第 4、5 节）：基于 XiHan.Framework 5（`next` 分支）从零实现，能力包 + 操作契约，一份契约同时服务 HTTP / gRPC / MCP。

## 布局

```text
backend-next/
├─ XiHan.BasicApp.slnx
├─ Directory.Build.props            版本 6.0.0-preview.1、Nullable、警告即错误、文档文件
├─ Directory.Packages.props         中央包版本
├─ Directory.Build.targets          XiHanFrameworkReference / XiHanFrameworkAnalyzer 项 → ProjectReference 或 PackageReference
├─ props/framework.props            缺省 ProjectReference ../../XiHan.Framework/next/src，锁定提交 XiHanFrameworkCommit；UseXiHanFrameworkSource=false 切 NuGet 包
├─ src/
│  ├─ XiHan.BasicApp.Contracts      Tier 0：实体协议与基类、分页 / 批量结果、权限描述
│  ├─ XiHan.BasicApp.Runtime        Tier 0：SqlSugar 回调（库类型、连接串、操作人、snake_case）、租户作用域查询、操作人中间件
│  ├─ XiHan.BasicApp.Identity       Tier 1：N0 最小切片（SysUser、SysUserStatistics；users.create / get / list）
│  └─ XiHan.BasicApp.Host           Full Profile + SqlSugar + JWT + 三个数据库 Provider + 发件箱；HTTP / OpenAPI / Scalar / MCP / gRPC
└─ test/
   ├─ XiHan.BasicApp.Architecture.Tests   层级依赖、Expose 显式、权限码文法、匿名白名单、目录与文案、框架提交锁定
   └─ XiHan.BasicApp.Integration.Tests    TestServer + SQLite：HTTP / MCP / gRPC 契约、事务；Smoke/ 是方案 9.1 的四项冒烟（两库冒烟需 XIHAN_TEST_POSTGRES）
```

## 命令（仓库根）

```bash
dotnet restore backend-next/XiHan.BasicApp.slnx
dotnet build backend-next/XiHan.BasicApp.slnx -c Release --no-restore
XIHAN_TEST_POSTGRES='Host=127.0.0.1;Port=5432;Username=postgres;Password=postgres' dotnet test --solution backend-next/XiHan.BasicApp.slnx -c Release --no-build
dotnet run --project backend-next/src/XiHan.BasicApp.Host --launch-profile Development
dotnet run --project backend-next/src/XiHan.BasicApp.Host -- contracts export --out frontend-next/contracts
```

框架以源码引用：同级目录需要 `XiHan.Framework` 检出在 `props/framework.props` 锁定的提交（`Architecture.Tests` 核对 HEAD）。工作区解决方案 `E:\Repository\XiHanFun\XiHanFun.Next.Local.slnx` 登记本目录全部工程与框架 `next/src`。

## 规则

- 每个公开方法一个 `[Operation("<资源>.<动词>")]`，显式写 `Expose`（架构测试强制）；权限码 `<包>.<资源>.<动作>`，资源段与操作 ID 同词；匿名操作登记白名单。
- 平台库实体不实现 `IMultiTenant`；租户库实体用 `TenantScoped*` 基类。分页、投影、联表经 `TenantScopedQuery.For<T>` 起手。
- 请求 DTO 有缺省值的属性用 `set`，不用 `init`（源生成序列化会把 init 当构造参数，缺席成员变 null）。
- 文案不进代码：每包 `<包>Messages` + `Localization/<区域>.json` × 10，键集与常量一一对应（架构测试守护）。
