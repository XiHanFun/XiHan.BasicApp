# 打印模板（规划中 · N3）

::: warning 尚未实现
6.0 还没有 Printing 包，菜单里没有打印模板，也没有相关操作与权限码。本页记录总体方案里的设计，落地时以代码为准。5.x 的打印文档见 [5.x 文档](https://basicapp.docs.xihanfun.com/backend/printing)。
:::

## 定位

Printing 是 Tier 3 可选模块，计划在 N3 落地，规模较小（方案估计约 1 天）。它沿用 5.x 的模型，换成 6.0 的写法。

## 设计要点

| 部分 | 方案 |
| --- | --- |
| 实体 | `PrintTemplate`（平台库实体，`IOwnedByTenant` 区分平台模板与租户模板） |
| 数据源 | `Contracts` 里的 `IPrintDataSource` 注册表：业务包登记「这种单据能打印哪些数据」，Printing 不引用业务包 |
| 作用域 | 按租户优先、平台兜底解析模板 |
| 资源 | `print-templates`（模板管理）、`print-data-sources`（可用数据源） |
| 权限码 | `printing.print-templates.*` 等，按 `<包>.<资源>.<动作>` 登记 |

## 前端

- 设计器沿用 `@xihan-print/*`（打印设计器文档见 [print.docs.xihanfun.com](https://print.docs.xihanfun.com)），接线沿用 `configurePrinting`；
- 页面进 `src/modules/printing`，与 `packages/printing` 的设计器接线配合。

## 下一步

- [架构总览](./introduction) · [功能清单](../features)
