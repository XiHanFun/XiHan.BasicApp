# 灵动岛

本页说明灵动岛能呈现什么、各区放什么由谁决定，以及调用方怎样把一件事放上岛，适用于前端开发者。

灵动岛是屏幕顶部正中常驻的深色胶囊，呈现进行中的任务、刚完成的结果和需要一直看得见的状态（网络或实时连接断开、模仿登录）。组件 `DynamicIsland` 挂在 `App.vue`，调用方只经 `~/composables` 的函数往岛上放活动，不碰组件本身。偏好设置里能关掉灵动岛：关掉后进行中与常驻状态不再出现，成功、失败与信息改由轻提示呈现。

## 三种呈现

一件事在岛上叫一条活动。分区参照 iOS 的实时活动，同一条活动有三种呈现：

| 呈现 | 何时出现 | 分区 |
| --- | --- | --- |
| 胶囊 | 一直在：最新的消息，没有消息时是最近的进行中或常驻状态 | 左侧 `leading`、中间标题、右侧 `trailing`；右端还有岛自己的计数与展开箭头 |
| 卡片 | 有新内容时自动短暂展开，几秒后收回胶囊 | 首行 `leading`、标题与 `trailing`；其下 `detail`（最多两行）、内容块 `blocks`、进度与操作 `actions` 铺满整宽 |
| 面板 | 点开胶囊后列出全部活动 | 每条与卡片同一套分区，`detail` 完整显示，进行中的显示已用时，非常驻的悬停可移除 |

多条消息同时到达时在胶囊后方叠起来，最新的在最前；同时存活的消息最多 5 条，再来就丢最旧的。面板展开期间消息不倒计时，收起后重新计时。

## 放上岛

| 函数 | 用途 |
| --- | --- |
| `islandStart(id, label, init?)` | 开始一条活动，缺省进行中；返回句柄推进到终态 |
| `islandStatus(id, label, init?)` | 常驻状态，不自动消失，缺省信息态；经句柄的 `success` / `info` / `dismiss` 收尾 |
| `applyServerTaskProgress(payload)` | 服务端 `TaskProgress` 推送的后台任务进度，由实时通信接好，页面不用调 |

同一个 `id` 复用同一条：再调一次即更新这一条，不新增。句柄的方法：

| 方法 | 说明 |
| --- | --- |
| `update(label)` | 只换标题，保持进行中 |
| `patch(fields)` | 合并更新任意字段，省略的字段沿用原值 |
| `setProgress(n)` | 设进度 0-100 |
| `success(label?, fields?)` | 成功，停留 1.6 秒后消失 |
| `error(label?, fields?)` | 失败，停留 3.2 秒后消失 |
| `info(label?, fields?)` | 信息，停留 2.4 秒后消失；带 `persistent: true` 则常驻 |
| `dismiss()` | 立即移除 |

```ts
import { islandStart } from '~/composables'

const task = islandStart('report:export', t('report.exporting'), { icon: 'lucide:download', progress: 0 })
task.setProgress(40)
task.success(t('report.exported', { n: total }), {
  actions: [{ key: 'open', label: t('report.open'), icon: 'lucide:folder-open', handler: openFolder }],
})
```

## 字段

| 字段 | 说明 |
| --- | --- |
| `state` | `loading` / `success` / `error` / `info` |
| `detail` | 副文本：卡片里最多两行，面板里完整显示 |
| `icon` | 缺省指示器上的图标：进行中画在进度环中心，终态替换状态图标 |
| `progress` | 进度 0-100；省略即不确定态（旋转弧环） |
| `leading` | 胶囊左侧与卡片、面板首行标题前的小件，缺省是状态指示器 |
| `trailing` | 胶囊右侧与卡片、面板首行末尾的小件，缺省在进行中且有进度时显示百分比（只在胶囊上） |
| `blocks` | 正文下方的内容块，自上而下 |
| `actions` | 操作按钮：`{ key, label, icon?, tone?, handler, keepOpen? }`，`tone` 取 `neutral` / `brand` / `danger`，点击后缺省移除这一条，`keepOpen: true` 则保留 |
| `actionLayout` | `inline` 一行带字的按钮（缺省），`bar` 底部等宽的图标栏（读屏念 `label`） |
| `alert` | 有新内容时是否自动展开成卡片，缺省带 `detail` 或 `blocks` 时展开 |
| `elapsed` | 进行中时面板里是否显示已用时，缺省显示 |
| `dismissible` | 面板里能否手动移除，缺省非常驻的都能 |
| `duration` | 终态停留的毫秒数，改写上面的缺省值 |
| `link` / `onClick` | 点卡片或面板里这一条时跳转的应用内路径，或执行的回调 |
| `persistent` | 常驻，不自动消失；`islandStatus` 自动带上 |

## 两侧小件

`leading` 与 `trailing` 放得下一个图标、一枚头像、一个小环或一小段字，胶囊、卡片与面板三处用同一份：

| `type` | 字段 | 呈现 |
| --- | --- | --- |
| `icon` | `icon`、`tone?` | 图标 |
| `avatar` | `name`、`avatar?` | 用户头像（`avatar` 是文件主键或直链），没有图时显示名字的前两个字 |
| `ring` | `value?`、`max?`、`icon?`、`tone?` | 进度环，`value` 省略即旋转；环心可放图标 |
| `text` | `text`、`tone?` | 一小段字，如 `3/8`、`12%` |
| `elapsed` | — | 自活动开始起的计时，读作 `m:ss` |
| `custom` | `component`、`props?` | 调用方自己的组件 |

`tone` 取组件库的语气：`neutral`、`brand`、`success`、`warning`、`danger`、`info`。没写 `leading` 时左侧是状态指示器，带状态色，成功、失败与信息在胶囊上还有一枚实心圆底；常驻状态不画圆底。

## 内容块

`blocks` 自上而下排在卡片与面板里每一条的正文下方：

| `type` | 字段 | 呈现 |
| --- | --- | --- |
| `text` | `text` | 一段说明 |
| `metric` | `value`、`unit?`、`caption?`、`accessory?` | 大号数值与单位，下方说明，右侧可挂一枚小件（如 `ring` 画目标完成度） |
| `progress` | `value`、`max?`、`label?`、`valueText?`、`tone?` | 线形进度，上方左侧名称、右侧取值（缺省百分比） |
| `list` | `items: { key, label, leading?, value?, progress?, tone? }[]` | 列表：行首小件、名称、右侧取值，给了 `progress` 就在行下画一条进度 |
| `grid` | `items: { key, label, icon?, value }[]` | 等宽网格：每格自上而下是标题、图标、取值 |
| `media` | `src`、`alt` | 图片，按 16:9 裁切 |
| `custom` | `component`、`props?` | 调用方自己的组件 |

```ts
islandStatus('fleet:car', t('fleet.car.title'), {
  leading: { type: 'icon', icon: 'lucide:car' },
  trailing: { type: 'text', text: t('fleet.car.parked') },
  blocks: [
    { type: 'metric', value: 323, unit: 'km', caption: t('fleet.car.updated_at', { time }) },
    { type: 'progress', value: 76, label: t('fleet.car.battery'), tone: 'success' },
  ],
  actionLayout: 'bar',
  actions: [
    { key: 'lock', label: t('fleet.car.lock'), icon: 'lucide:lock', keepOpen: true, handler: lock },
    { key: 'charge', label: t('fleet.car.charge'), icon: 'lucide:zap', keepOpen: true, handler: charge },
  ],
})
```

## 约定

- 文案走 `t()`，岛不替调用方翻译；切换语言后要以同一个 `id` 再调一次更新文案。
- `custom` 组件在深色底上渲染，颜色取 `--xh-*` 语义令牌即随岛的深色墨域；它自己管交互，整条点击不覆盖它。胶囊本身是一枚按钮，胶囊上的 `custom` 只做展示。
- 调用方的组件放进来时被标成非响应式，`props` 变了要经 `patch` 重新给。
- 刷新页面后只恢复服务端进行中的任务（等推送续上），本地活动随页面结束。
- 只在岛上显示、不能丢的状态，关掉灵动岛后要有自己的退路：模仿登录在岛关掉时退回顶部横幅。

## 下一步

- [实时通信](./realtime) · [常用组件](./components) · [布局与主题](./theme)
