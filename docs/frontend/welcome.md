# 新版欢迎页

本页说明新版欢迎页何时出现、怎样给它登记一屏亮点、怎样关掉，适用于前端开发者。

新版欢迎页是登录后首次进入一条新的版本线时出现一次的全屏欢迎层：各国问候由沙粒依次聚成又吹散，接着是欢迎主画面（标志与版本号由沙粒循环聚散）、亮点导览和收尾，操作都在页面底部。组件 `AppWelcome` 挂在基础布局里；出不出现、有哪几屏由 `~/stores` 的 `useWelcomeStore` 决定，壳层与各能力包只管登记自己的亮点。

## 何时出现

版本线是应用版本的「主版本.次版本」。欢迎页取登记过亮点、又不高于当前版本的最新一条版本线，账号还没看过它才出现。

| 情形 | 行为 |
| --- | --- |
| 账号没有记录，或记下的版本线更早 | 进入壳层后出现 |
| 跳过、按 Esc、看完后点「开始使用」 | 关闭并把这条版本线记到账号上（用户设置，偏好场景、键 `welcome`），换设备登录不再出现 |
| 模仿登录 | 不出现，也不读记录 |
| 锁屏期间（如首次登录强制改密） | 解锁后再判断 |
| 读不到记录 | 这一次不出现，下次进入再判断 |
| 没有登记过不高于当前版本的亮点 | 不出现，也不发请求 |

出现期间背后的壳层不接操作，强制阅读的通知与登录后弹窗等它关掉再出现。看过之后可以在「关于」页点「新版亮点」重看，重看不读也不改账号上的记录。

构建时把环境变量 `VITE_APP_WELCOME` 设为 `false` 即整个关掉：不出现、不读记录，「关于」页的入口也收起。

## 登记一屏亮点

亮点经 `registerWelcomeHighlights` 登记，各能力包在自己的 `setup.ts` 里调用，壳层的几屏在 `packages/layouts/basic/components/welcome/highlights.ts`。同一条版本线里 `key` 相同的后登记覆盖先登记。

```ts
import { registerWelcomeHighlights } from '~/stores'

registerWelcomeHighlights({
  release: '<主版本>.<次版本>',
  key: 'report',
  order: 70,
  icon: 'lucide:chart-column',
  tone: 'info',
  categoryKey: 'report.welcome.category',
  titleKey: 'report.welcome.title',
  descriptionKey: 'report.welcome.description',
  pointKeys: ['report.welcome.point_export', 'report.welcome.point_share'],
  demo: () => import('./components/WelcomeDemo.vue'),
  page: 'report.dashboards',
  action: { labelKey: 'report.welcome.action', to: '/report/dashboards' },
})
```

| 字段 | 说明 |
| --- | --- |
| `release` | 归属的版本线；到了这个版本才会出现 |
| `key`、`order` | 同一条版本线里的标识与排序，小的在前；各包的排序号与壳层的穿插 |
| `icon`、`tone` | 类目图标与语气（`brand` / `info` / `success` / `warning` / `danger`），背景光斑与类目标签跟着语气取色 |
| `categoryKey`、`titleKey`、`descriptionKey`、`pointKeys` | 类目、标题、说明与两三条要点的文案键，十种区域都要有 |
| `demo` | 右侧的演示组件，按需加载 |
| `page` | 所属页面的编码：后端没有给当前用户下发这个页面（可选模块关掉、套餐不含或没有权限）时这一屏不出现 |
| `permission` | 权限码：没有这个权限时不出现 |
| `when` | 另外的出现条件，可以是异步的；不成立或判断出错时这一屏不出现，其余照常 |
| `action` | 这一屏末尾的按钮：`to` 是应用内路由，`run` 是别的动作（如打开命令面板），二选一；点了先退场再执行 |

只介绍使用者感知得到的变化；一屏只讲一件事。一屏亮点都没有的用户看到的是欢迎主画面接收尾。

## 演示组件

演示放在 `XArtboard`（`~/components`）里：按固定的设计尺寸排版，再随舞台整体缩放，窄屏上不会重排走样。演示只是画面，不接指针与键盘，文字用模块自己的文案键。

要循环播放的演示用 `~/composables` 的 `useSequence`：

```ts
import { useSequence } from '~/composables'

const loop = useSequence(async (wait) => {
  step.value = 'typing'
  await wait(1200)
  step.value = 'done'
  await wait(2400)
})

onMounted(() => loop.start())
```

一遍跑完自动从头再来；`wait(Number.POSITIVE_INFINITY)` 让序列停在最后一步；组件卸载时自动停下。颜色、间距、圆角与动效只用 XiHan.UI 的语义令牌，数字滚动、快捷键等直接用组件库的组件。

## 画面与无障碍

- 沙粒是 `@xihan-ui/backgrounds` 的 `sand` 效果；文字的配色与背景光斑取自应用的标志（偏好里的品牌标志，缺省 `VITE_APP_LOGO`），换了标志跟着换。
- 标志读不了像素（跨源且没有 CORS 头）时主画面放一张静止的标志，文字改取语义令牌的颜色；浏览器不支持 WebGL2 时同样放静止的标志，不演问候。
- 减少动态效果时不演问候，直接到欢迎主画面，标志聚成即止，关闭没有退场动画。
- 键盘：`Enter` 与 `→` 前进，`←` 后退，`Esc` 关闭；每一幕的焦点落在主操作上，关闭后焦点回到打开它的入口。
