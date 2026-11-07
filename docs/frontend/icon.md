# 字体图标

本页说明前端的图标写法、离线图标集与图标选择器，适用于前端开发者。

图标统一用 [Iconify](https://iconify.design) 的 `集合:名称` 写法，经 `@iconify/vue/offline` 离线渲染；后端页面登记里的图标也是这种字符串（如 `lucide:users`、`lucide:shield`）。

## 图标集

`packages/iconify` 只用本地安装的图标集（`@iconify-json/*`），不访问 Iconify 在线 API：

| 图标集 | 加载 | 用途 |
| --- | --- | --- |
| `lucide` | 启动时预载 | 通用：菜单、按钮、操作 |
| `tabler` | 启动时预载 | 部分页面 |
| `mdi` | 启动时预载 | 品牌等图标 |
| `simple-icons` | 启动时预载 | 第三方登录的品牌 logo（登录页、绑定页） |
| `carbon`、`ep`（Element Plus）、`heroicons` | 图标选择器里按需加载 | 仅供挑选 |

`main.ts` 在挂载前装好预载的四个图标集：离线 `Icon` 对已挂载的组件不会因为图标集后到而重渲染。按需加载的三个图标集只有打开图标选择器、切到对应页签时才装进来，刷新后不会预载，菜单、按钮等常驻位置不要用它们。

## 使用

```vue
<script setup lang="ts">
import { Icon } from '~/components'
</script>

<template>
  <Icon icon="lucide:sliders-horizontal" />
</template>
```

`Icon` 也可以从 `~/iconify` 引入。SchemaPage 的操作、菜单节点、通知、工作台小组件都直接写图标字符串。

## 图标选择器

`IconPicker`（`~/iconify`）：按图标集分页签、按名搜索、按需加载、可清除，`v-model` 绑定 `集合:名称` 字符串；不带集合前缀的值按 `lucide` 处理。菜单管理与通知公告的「图标」字段用它。

## 约定

- 新增页面的图标在后端 `<包>Pages.cs` 的页面登记里写，菜单管理里可改；
- 优先用 `lucide`，保持风格一致；
- 不在组件里内联 SVG 字符串，也不复制第三方图标库的运行时；
- 只有图标的操作要有可访问名称或提示。

## 下一步

- [常用组件](./components) · [布局与主题](./theme)
