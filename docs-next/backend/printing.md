# 打印模板

本页说明打印模板的作用域、数据源登记、操作与权限、演示数据和模块开关，适用于接入单据打印的后端与前端开发者。

Printing 是 Tier 3 的可选模块：后端只存模板的设计 JSON、按编码解析模板、提供数据源目录；设计、样例数据、预览与打印都在浏览器里完成，本包没有作业与普通端点。模型与规则沿用 5.x，写法换成 6.0 的。

## 作用域

模板 `SysPrintTemplate` 是平台库实体（实现 `IOwnedByTenant`）：`ownerTenantId` 为 `platform` 的是平台全局模板，否则是该租户的私有模板。独立库的租户也读写平台库里的这张表。

| 规则 | 说明 |
| --- | --- |
| 编码 | 不含空白、不超过 100 个字符，所属一侧内唯一（部分唯一索引，删了可以重建），创建后不可改 |
| 数据源 | 可空：空即自由模板；非空时必须已登记（`PRINTING_VALIDATION_003`） |
| 模板 JSON | 只做最小结构校验：根是对象、`schemaVersion` 是数字 1、`pages` 是非空的对象数组（`PRINTING_VALIDATION_002`），其余原样保存；完整校验由设计器做 |
| 开放给租户 | 只对全局模板有意义，租户模板恒为否 |
| 删除 | 只能删已停用的（`PRINTING_CONFLICT_002`），软删除 |
| 并发 | 修改与启停要交回读到的 `concurrencyStamp`，与当前的不同即 409 `PERSISTENCE_CONFLICT_*` |

按编码解析（`print-templates.resolve`）的作用域：

| 请求一侧 | `Auto` | `Tenant` | `Global` |
| --- | --- | --- | --- |
| 平台 | 启用的全局模板，不要求开放 | 400 `PRINTING_VALIDATION_004` | 同 `Auto` |
| 租户 | 先取本租户启用的，没有（或已停用）再取启用且开放的全局模板 | 只取本租户启用的，不回退 | 只取启用且开放的全局模板 |

解析不到即 404 `PRINTING_NOTFOUND_002`。各侧「某编码的启用模板」进共享缓存（键 `printing:templates:<侧>:<编码>`，30 分钟，「没有」也缓存），模板写入后在事务提交之后失效对应的那一项。

## 数据源

数据源只是元数据：字段契约与静态样例，后端不取业务数据，真实打印数据由前端调用方传给打印服务。业务包在自己的 `Configure` 里经 `Contracts` 登记：

```csharp
context.Services.AddPrintDataSource(new PrintDataSourceDefinition(
    "sales.order",
    "销售订单",
    [
        new("orderNo", "订单号"),
        new("qr", "订单二维码", PrintFieldKinds.QrCode),
        new("items", "明细", PrintFieldKinds.Table, [new("sku", "物料"), new("quantity", "数量", 60, "number")]),
    ],
    """{ "orderNo": "SO-0001", "qr": "SO-0001", "items": [] }"""));
```

Printing 的注册表在启动时收下全部登记并校验，编码重复或契约不合法即启动失败：

| 项 | 规则 |
| --- | --- |
| 编码 / 字段键 | 不超过 100 个字符、不含空白，字段键在本数据源里唯一 |
| 样例 | 根是对象或数组的 JSON |
| 字段种类 | `text`、`image`、`barcode`、`qrcode`、`table`；只有 `table` 能带列，且至少一列、列不重复 |
| 样例控件 | `boolean`、`date`、`datetime`、`number`、`text`、`textarea`，不写即按样例值推断 |

内置数据源 `system.print-demo`（全部字段种类的样板）总会登记。Printing 关掉时登记照样无害，没有消费方。

## 操作与权限

| 操作 | 路由 | 权限 |
| --- | --- | --- |
| `print-templates.list` | `POST /api/print-templates:list` | `read` |
| `print-templates.get` | `GET /api/print-templates/{id}` | `read` |
| `print-templates.resolve` | `GET /api/print-templates:resolve?templateCode=&scope=` | `use` |
| `print-templates.create` | `POST /api/print-templates` | `create` |
| `print-templates.update` | `PUT /api/print-templates/{id}` | `update` |
| `print-templates.set-status` | `POST /api/print-templates/{id}:set-status` | `status` |
| `print-templates.delete` | `DELETE /api/print-templates?ids=` | `delete` |
| `print-data-sources.list` | `GET /api/print-data-sources` | `read` 或 `use` 其一（策略 `printing.data-source-catalog`） |

权限码 `printing.print-templates.{read,create,update,status,delete,use}` 两侧都有，`printing.print-templates.global-manage` 只在平台一侧：平台维护全局模板要同时持有对应的写权限与全局管理（`PRINTING_DENIED_001`）。写入一律写在调用方这一侧。

列表的作用域用筛选 `scope`（`eq`）：平台只有全局模板（全部状态）；租户缺省看本租户的，`Global` 看启用且开放的全局模板。租户读不开放或已停用的全局模板详情即 403 `PRINTING_DENIED_002`，别的租户的模板当作不存在。可筛选 `templateCode`、`templateName`、`dataSourceCode`、`status`、`allowTenantUse`，排序 `templateCode`、`templateName`、`dataSourceCode`、`status`、`order`、`createdAt`。模板实体登记了字段安全。

## 演示数据

`BasicApp:Seed:EnableDemoData` 打开时：

- 登记 5 个演示数据源：`demo.pickup-receipt`、`demo.delivery-note`、`demo.formats`、`demo.inspection`、`demo.outage-notice`；
- 种子 `printing.demo`（号段 950）写 6 张平台全局模板，向租户开放、启用：上面 5 个各一张，外加绑 `system.print-demo` 的「打印元素总览」。按编码先查后写（含已删除的），已有的不改、删掉的不补回。

演示 JSON 与 5.x 同一批文件，前端单测读同一批文件核对每张模板都能通过设计器的完整校验、按样例排版不出错误级诊断。

## 前端

- 页面 `printing.print-templates`（路由 `/printing/print-templates`）在 `src/modules/printing`：平台管全局模板，租户在「本租户模板」与「可用全局模板」之间切换；新增与编辑元数据在弹窗里，版面在全屏设计器里改，另有样例数据预览与直打。
- `setup.ts` 调 `configurePrinting`：`resolveTemplate` 接 `print-templates.resolve`，`listDataSources` 接 `print-data-sources.list`。业务页面打印单据用 `~/printing` 的 `previewPrintByCode(code, data)` 或 `directPrintByCode`。
- 设计器 `@xihan-print/*`（文档见 [print.docs.xihanfun.com](https://print.docs.xihanfun.com)）经 `packages/printing` 接入，设计器共用的接线（跟随应用语言、属性编辑器、宿主命令图标、菜单栏标题与状态栏）在 `packages/designers`，流程设计器随 Workflow 接到同一处。
- 静默直打依赖每台操作机装 XiHan 打印客户端（本机 WebSocket），连不上时不退回浏览器打印；预览只代表打印对话框已打开，不知道用户最终是打印还是取消。

## 开关

```json
{ "BasicApp": { "Modules": { "Printing": { "Enabled": false } } } }
```

关掉后宿主不装 Printing：权限目录、页面登记与操作都不在，菜单种子删掉打印模板页，前端模块随菜单不再出现；其他包登记的数据源没有消费方。

## 下一步

- [架构总览](./introduction) · [功能清单](../features) · [配置参考](../configuration#basicapp-modules)
