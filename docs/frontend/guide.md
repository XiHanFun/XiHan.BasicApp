# 页面引导

本页说明界面与页面引导何时出现、由哪几章组成、怎样给业务页写自己的一章，适用于前端开发者。

页面引导用 XiHan.UI 的 Tour 组件逐步高亮界面上的区域与按钮：遮罩盖住页面，高亮框落在目标上，旁边的气泡讲它做什么，底部有上一步、下一步与跳过。播放器 `AppGuide` 挂在基础布局里；播什么、什么时候播由 `~/stores` 的 `useGuideStore` 决定，壳层、列表页与业务页只管登记自己的章节。

## 三章

| 章节 | 记录键 | 内容 | 登记位置 |
| --- | --- | --- | --- |
| 界面导览 | `shell` | 菜单、顶栏、标签栏、内容区、页脚，以及顶栏和标签栏上的各个按钮 | `packages/layouts/basic/components/guide/shell-guide.ts` |
| 列表页 | `schema` | 搜索区（查询、重置、搜索设置、条件预设、高级搜索）、操作区（刷新、导入、导出、表格设置、全屏）、表格（排序、勾选与批量、悬停速览、行操作、分页） | `packages/components/schema/guide.ts`，SchemaPage 自动登记 |
| 业务页 | `page:<页面码>` | 本页自己的列与操作 | 页面的 `PageSchema.guide` |

步骤只指向锚点：元素上的 `data-guide` 属性。开播前把此刻不在页面上的步骤去掉（被偏好藏起的按钮、挪进悬浮组的工具、这个布局里没有的菜单、没有权限的导入导出、没有数据时的行操作），一个目标都指不到的章节整章不播。

## 何时出现

| 情形 | 行为 |
| --- | --- |
| 进入壳层，界面导览没看过 | 自动播放 |
| 第一次进入某个列表页 | 列表页那一章没看过就播，接着播本页没看过的业务一章；界面导览也没看过时排在最前，连成一串 |
| 看完、跳过、按 Esc 或关掉 | 这一串都记成看过（用户设置，偏好场景、键 `guide`，记每一章看过的版本），换设备登录不再出现；跳过后到离开这一页之前不再自动播放 |
| 中途换了页面 | 收起，不记成看过，下次进来再播 |
| 欢迎层开着、登录后的公告没处理完、锁屏期间 | 等它们结束再判断 |
| 模仿登录 | 不自动播放，也不读记录 |
| 读不到记录 | 这一次不自动播放，下次进入再判断 |

页面在首批数据回来、表格按同步后的设置摆好之后才算就绪，自动播放等它，再等页面进场动画播完才量目标。分屏副侧的页面不登记引导，入口与自动播放只认当前页。

章节的 `version` 是内容版本：步骤有实质变化时加一，看过旧版的人会再看到一次。

## 重看入口

- 标签栏右侧的「页面引导」：当前页登记了引导才出现，重播本页的列表页一章与业务一章。
- 命令面板里的「页面引导」与「界面导览」。
- 帮助中心的项目概览页上的「界面导览」。

重看不论看过没有都从头播。构建时把环境变量 `VITE_APP_GUIDE` 设为 `false` 即整个关掉：不自动播放、不读记录，入口也都收起。端到端测试的那份构建就是这样关掉的，免得遮罩挡住用例的点击。

## 给业务页写一章

列表页在 `PageSchema.guide` 里写步骤，排在列表页通用的一章之后：

```ts
import { schemaGuideTarget } from '~/components'

const schema = computed<PageSchema>(() => ({
  pageCode: 'identity.users',
  // …
  guide: {
    version: 1,
    steps: [
      {
        id: 'create',
        target: schemaGuideTarget.action('create'),
        titleKey: 'identity.users.guide.create.title',
        descriptionKey: 'identity.users.guide.create.description',
        placement: 'bottom-start',
      },
      {
        id: 'status',
        target: schemaGuideTarget.column('status'),
        titleKey: 'identity.users.guide.status.title',
        descriptionKey: 'identity.users.guide.status.description',
      },
    ],
  },
}))
```

| 字段 | 说明 |
| --- | --- |
| `version` | 内容版本，步骤有实质变化时加一 |
| `steps[].id` | 同一章里唯一 |
| `steps[].target` | 高亮的目标；不给即气泡居中（开场、收尾） |
| `steps[].titleKey`、`descriptionKey` | 标题与说明的文案键，写进模块的十个区域文件 |
| `steps[].placement` | 气泡的首选方位，缺省在目标下方，放不下时组件库自动翻到另一侧 |
| `steps[].when` | 另外的出现条件（如偏好关掉了对应功能） |

`schemaGuideTarget` 取列表页里的元素，都只在本页的根里找：

| 写法 | 指向 |
| --- | --- |
| `column(key)` | 字段 `key` 那一列的列头 |
| `cell(key)` | 第一行里字段 `key` 那一格 |
| `action(key)` | 页面级操作按钮（`actions` 里 `scope` 为 `page` 的那一项） |
| `rowActions()` | 第一行的行操作（「更多」菜单的触发钮） |

不是 SchemaPage 的页面用 `~/composables` 的 `usePageGuide` 登记：

```ts
import { usePageGuide } from '~/composables'

const rootRef = ref<HTMLElement | null>(null)
usePageGuide({
  root: () => rootRef.value,
  ready: () => loaded.value,
  chapters: () => [{
    key: 'page:workflow.designer',
    version: 1,
    title: () => t('workflow.designer.title'),
    steps: () => [
      { id: 'canvas', target: 'workflow.canvas', titleKey: 'workflow.designer.guide.canvas.title', descriptionKey: 'workflow.designer.guide.canvas.description' },
    ],
  }],
})
```

字符串目标是锚点名，在页面根里找 `data-guide` 等于它的元素；也可以写函数 `(root) => Element | null` 自己取。

## 写法建议

- 一步只讲一件事：标题说是什么，说明讲它做什么、什么时候用，两三句以内。
- 通用一章已经讲过的（搜索、导出、表格设置、行操作的概念）业务一章不再重复，只讲本页特有的列、标记与操作。
- 锚点改名或删掉时这一步会悄悄消失。壳层与列表页的锚点有单测核对，业务页的步骤要在浏览器里看一遍。
- 文案里不写 `@`（vue-i18n 会当成链接引用）。
