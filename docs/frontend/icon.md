# 字体图标

图标走 Iconify，**运行在离线模式**——这带来一条必须记住的限制。

## 用法

```vue
<script setup lang="ts">
import { Icon } from '~/iconify'
</script>

<template>
  <Icon icon="lucide:eye" />
  <Icon icon="lucide:trash-2" class="text-red-500" />
</template>
```

`Icon` 不是全局组件，用前须从 `~/iconify`（或转出它的 `~/components`）导入。

图标名格式是 `{集合}:{图标}`，可在 [Iconify 官网](https://icon-sets.iconify.design/) 搜索。

菜单图标在**后端** `PageRegistry` 的 `PageDescriptor.Icon` 字段里写同样的格式：

```csharp
new("identity.position", "岗位管理", "menu.identity_position", MenuType.Menu,
    "/identity/position", "IdentityPosition", "identity/position/index",
    "identity", SaasPermissionCodes.Position.Read, "lucide:briefcase", 135),
//                                                 ↑ 图标
```

## 离线模式的限制

::: danger 只有预加载的图标集能直接渲染
配置在 `~/iconify/offline.ts`。运行期直接渲染**只保证这四个已预加载集**：

- **`lucide`** —— 主力图标集，优先用它
- **`tabler`**
- **`mdi`**
- **`simple-icons`** —— 第三方品牌 logo（如 Gitee 用 `simple-icons:gitee`）

未预加载的集（`carbon` / `ep` / `heroicons` 等）在页面里直接用会**渲染为空白**，且不报错——因为离线 `Icon` 对已挂载组件不会因后加载而重渲染。

以仓库的 `PRELOAD_ICON_PACKAGES` 配置为准。
:::

品牌图标优先用 `simple-icons:*` 或 `tabler:brand-*`。

## `IconPicker` 是例外

图标选择器组件可以**按需懒加载** carbon / ep / heroicons 供用户挑选——那是「用户在弹窗里选图标」的场景，与「页面里直接渲染」不同，不受上面限制。

但要注意：**用户选了未预加载集的图标存进菜单后，页面渲染时仍然会空白**。如果允许用户自选图标，要么把可选集限制在预加载的四个里，要么把用户可能选的集也加入预加载。

## 为什么用离线模式

在线模式下 Iconify 会在运行时向 CDN 请求图标数据。离线模式把图标数据打进产物，代价是体积和「只能用预加载集」，换来的是：

- **内网/断网环境可用**——这是企业中后台的常见部署场景；
- 不依赖第三方 CDN 可用性；
- 首屏图标不闪烁。

## 加一个图标集

1. 确认对应的 `@iconify-json/{集合}` 依赖已安装；
2. 在 `~/iconify/offline.ts` 的 `ICON_SET_META` 与 `PACKAGE_LOADERS` 里登记该集（carbon / ep / heroicons 已登记，可跳过）；
3. 把该集加入同文件的 `PRELOAD_ICON_PACKAGES`；
4. 重新构建。

::: warning 加集会增大产物
每个图标集都是完整数据包。只在确实需要时加，优先从已有四个集里找替代图标。
:::

## 排查

| 现象 | 原因 |
| --- | --- |
| 图标位置空白 | 用了未预加载的图标集 |
| 菜单图标空白 | 后端 `Icon` 字段写了未预加载集的图标名 |
| 图标名写对了还是不显示 | 图标名拼错（大小写、连字符）；去 Iconify 官网核对 |
| `IconPicker` 里能看到、放到页面就没了 | 正是上面那条限制——选择器懒加载，页面渲染不懒加载 |

## 相关页面

- [布局与主题](./theme)：外观定制
- [路由与菜单](./routing)：菜单图标从哪来
- [常用组件](./components)：`IconPicker` 等公共组件
