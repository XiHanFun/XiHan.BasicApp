# 字体图标

本页说明管理端的图标写法、离线图标集与图标选择器，适用于前端开发者。

图标统一用 [Iconify](https://iconify.design) 的 `集合:名称` 写法，经 `@iconify/vue` 渲染；后端页面登记里的图标也是这种字符串（如 `lucide:users`、`lucide:shield`）。

## 离线图标集

`packages/iconify` 在启动时离线预加载运行期常用的图标集，不依赖 Iconify 在线 API：

| 图标集 | 用途 |
| --- | --- |
| `lucide` | 通用：菜单、按钮、操作 |
| `tabler` | 部分页面 |
| `mdi` | 品牌等图标 |
| `simple-icons` | 第三方登录的品牌 logo（登录页、绑定页离线渲染） |

预加载是为了避免离线模式下图标空白：离线 Icon 对已挂载的组件不会因为后加载而重渲染。

另有 Carbon、Element Plus、Heroicons 三个图标集按需加载，只在图标选择器里用到时才拉取。

## 使用

```vue
<script setup lang="ts">
import { Icon } from '~/components'
</script>

<template>
  <Icon icon="lucide:sliders-horizontal" />
</template>
```

SchemaPage 的操作、菜单节点、通知、工作台小组件都直接写图标字符串。

## 图标选择器

`packages/iconify/IconPicker.vue`（从 5.x 搬入）：按图标集分页签、按名搜索、按需加载、可清除。菜单管理与通知公告的「图标」字段用它。

## 约定

- 新增页面的图标在后端 `<包>Pages.cs` 里登记，菜单管理里可改；
- 优先用 `lucide`，保持风格一致；
- 不在组件里内联 SVG 字符串，也不复制第三方图标库的运行时。

## 下一步

- [常用组件](./components) · [布局与主题](./theme)
