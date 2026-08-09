# 开发流程

从零加一个前端页面的完整流程。**前端页面的事实源在后端**——菜单、路由、组件路径、权限码、i18n 键都在后端 `PageRegistry` 登记，前端只补落盘文件。所以流程是「后端先行、前端跟进」。

## 五步

```text
① 后端 PageRegistry 登记页面与按钮（建菜单即绑权限）
② 后端加分页/详情/写入接口（分页记得标 [HttpPost]）
③ 前端加 API 模块（src/api/modules/{域}/）
④ 前端加视图文件（src/views/{域}/{页}/index.vue）
⑤ 补 i18n 文案（menu.ts + 模块文案）
```

前两步属于后端，见 [后端手册 · 二次开发](../backend/development)。下面只讲前端三步。

## ③ API 模块

按域建两个文件：

```text
src/api/modules/organization/
├── position.ts          # API 定义
└── position.types.ts    # DTO 类型
```

标准 CRUD 用资源工厂：

```ts
import { defineResource } from '../../factory'

export const positionApi = defineResource<
  PositionListItemDto, PositionDetailDto,
  PositionCreateDto, PositionUpdateDto, PositionPageQueryDto
>({
  query: 'PositionQuery',
  command: 'Position',
})
```

有自定义动作时改用低层客户端逐个拼（`position.ts` 就是这种写法）：

```ts
// position.ts
import { createDynamicApiClient, createReadApi } from '../../base'

const positionQueryApi = createDynamicApiClient('PositionQuery')
const positionCommandApi = createDynamicApiClient('Position')
const positionReadApi = createReadApi<PositionListItemDto, PositionDetailDto, PositionPageQueryDto>(
  'PositionQuery',
  'Position',
)

export const positionApi = {
  detail: (id: ApiId) => positionReadApi.detail(id),
  page: (input: PositionPageQueryDto) => positionQueryApi.post('PositionPage', input),
  updateStatus: (input: PositionStatusUpdateDto) => positionCommandApi.put('PositionStatus', input),
}
```

别忘了在该域的 `index.ts` 里导出。动作名推导规则与分页协议见 [服务端交互](./request)。

## ④ 视图文件

**路径由后端 `PageDescriptor.Component` 决定**，约定是 `Path` 去前导斜杠 + `/index`：

| 后端 `Path` | 后端 `Component` | 前端文件 |
| --- | --- | --- |
| `/identity/position` | `identity/position/index` | `src/views/identity/position/index.vue` |

列表页直接照 [Schema 驱动页面](./schema-page) 的骨架写，三段：字段声明 → 查询构建 → 页面 Schema。

::: warning `_core` 页面要额外登记
不落在 `src/views` 的页面（个人中心、关于页等，源码在 `packages/views/_core/`），`Component` 写 `_core/xxx/index`，**必须同时在 `packages/router/dynamic.ts` 的 `coreComponentMap` 登记**，否则菜单有、点进去 404。
:::

## ⑤ i18n 文案

两处：

| 文件 | 键 | 说明 |
| --- | --- | --- |
| `packages/locales/langs/{zh-CN,en-US}/menu.ts` | `identity_position` | 菜单标题。键 = 后端 `I18nKey` 去掉 `menu.` 前缀 |
| `src/locales/langs/{zh-CN,en-US}/{模块}.ts` | `identity.position.position_name` 等 | 页面内文案，约定 `模块.实体.字段或动作`。新增模块文件要在同级 `zh-CN.ts` / `en-US.ts` 里 import 并导出 |

::: danger 裸 `@` 会白屏
文案里出现裸 `@`（如 `联系 @admin`）会触发 vue-i18n 的 linked message 语法而抛错、整页白屏。必须转义成 <code v-pre>{'@'}</code>。见 [国际化](./i18n)。
:::

## 自检清单

写完对照一遍，漏哪项就是哪种表现：

| 项 | 漏了会怎样 |
| --- | --- |
| 后端 `PageRegistry` 登记 | 侧边栏没有这个菜单 |
| 后端权限定义落库 | 菜单种子 fail-closed 跳过，菜单还是不出现 |
| 后端分页方法标 `[HttpPost]` | 前端发的 body 后端收不到 |
| 视图文件路径与 `Component` 一致 | 菜单有、点进去 404 |
| `_core` 页面登记 `coreComponentMap` | 同上 |
| `menu.ts` 文案 | 侧边栏显示 `menu.xxx` 原文 |
| 字段 `permission` / `exportPermission` | 无权用户也看得到列/按钮（**服务端仍会拦，但体验差**） |

## 质量门禁

```bash
pnpm type-check
```

```bash
npx eslint src/views/identity/position/index.vue --fix
```

::: warning 别跑全仓 lint
`pnpm run lint:fix` 展开是 `cross-env CI=true oxlint --fix && cross-env CI=true eslint . --fix`，会扫全仓并改动与你无关的文件。**只对本次改动的文件跑**；误跑了用 `git checkout HEAD -- <非本次任务的文件>` 还原。
:::

前端改动由开发者自行在浏览器验证，不走自动化预览。

## 相关页面

- [后端手册 · 二次开发](../backend/development)：后端纵切片的完整接线清单
- [路由与菜单](./routing)：路由怎么从后端菜单生成
- [Schema 驱动页面](./schema-page)：列表页开发手册
- [目录结构与代码地图](../project-structure#前端)：我要改 X 去哪个文件
