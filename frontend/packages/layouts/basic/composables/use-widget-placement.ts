import type { Ref } from 'vue'
import type { PreferenceEntryPlacement, WidgetPlacement } from '~/types'
import { useResizeObserver } from '@vueuse/core'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useIsMobile } from '~/composables'
import { useContentMaximize } from '~/hooks'
import { useAppStore } from '~/stores'
import { useEffectiveLayoutMode } from './use-effective-layout-mode'

/** 一个工具此刻落在哪：顶栏、悬浮组，或不显示 */
export type ResolvedWidgetPlacement = 'header' | 'floating' | 'hidden'

/** 顶栏上从左到右的次序：挤不下时按它从左往右一个个让到悬浮组，放得下了从右往左放回 */
const WIDGET_KEYS = ['search', 'language', 'timezone', 'theme', 'fullscreen', 'preference'] as const
type WidgetKey = typeof WIDGET_KEYS[number]

// 模块级单例：顶栏量出来的挤压程度，顶栏、悬浮组与偏好抽屉读的是同一份
/**
 * 挤压档：0 不挤；1 命令面板收成图标钮；再往上每一档从左往右多让一个「自动」的工具到悬浮组。
 * 面包屑排在最后让：前面几档都用完了才轮到它截断
 */
const squeezeLevel = ref(0)
/** 「自动」且本该在顶栏的工具里，从左数已经让到悬浮组的个数 */
const squeezedCount = computed(() => Math.max(0, squeezeLevel.value - 1))

/**
 * 顶栏工具与偏好设置入口的落位，顶栏与悬浮组（AppFloatToolbar）共用这一处判定，二者互斥：
 * - hidden：不显示；floating：进悬浮组
 * - header：放顶栏；但顶栏不显示、或小屏（< 768）顶栏放不下时回落悬浮，免得挤掉右上角的账号入口
 * - auto：内容最大化 / 顶栏隐藏 / 全屏内容布局时悬浮；否则放顶栏，顶栏挤不下时从左往右依次让到悬浮组
 *   （挤压程度由 AppHeader 里的 useHeaderSqueeze 量；命令面板在让出之前先收成图标钮，见 searchSqueezed）
 */
export function useWidgetPlacement() {
  const appStore = useAppStore()
  const { contentIsMaximize } = useContentMaximize()
  const effectiveLayoutMode = useEffectiveLayoutMode()
  const { isMobile } = useIsMobile()

  const settings = computed<Record<WidgetKey, WidgetPlacement | PreferenceEntryPlacement>>(() => ({
    search: appStore.widgetSearchPlacement,
    language: appStore.widgetLanguagePlacement,
    timezone: appStore.widgetTimezonePlacement,
    theme: appStore.widgetThemePlacement,
    fullscreen: appStore.widgetFullscreenPlacement,
    preference: appStore.widgetPreferencePosition,
  }))

  const headerUnavailable = computed(() => !appStore.headerShow || isMobile.value)
  const autoInHeader = computed(() =>
    !contentIsMaximize.value
    && appStore.headerShow
    && effectiveLayoutMode.value !== 'full')

  /** 会被挤压的工具：设成自动、此刻本该在顶栏的，按顶栏上的次序排 */
  const squeezable = computed(() => autoInHeader.value ? WIDGET_KEYS.filter(key => settings.value[key] === 'auto') : [])

  function resolve(key: WidgetKey): ResolvedWidgetPlacement {
    switch (settings.value[key]) {
      case 'hidden':
        return 'hidden'
      case 'floating':
        return 'floating'
      case 'header':
        return headerUnavailable.value ? 'floating' : 'header'
      default:
        if (!autoInHeader.value) {
          return 'floating'
        }
        return squeezable.value.indexOf(key) < squeezedCount.value ? 'floating' : 'header'
    }
  }

  const search = computed(() => resolve('search'))
  const language = computed(() => resolve('language'))
  const timezone = computed(() => resolve('timezone'))
  const theme = computed(() => resolve('theme'))
  const fullscreen = computed(() => resolve('fullscreen'))
  const preference = computed(() => resolve('preference'))

  /** 悬浮组里是否有东西（没有就不渲染悬浮按钮） */
  const hasFloating = computed(() =>
    [search, language, timezone, theme, fullscreen, preference].some(item => item.value === 'floating'))

  /** 顶栏挤压到第一档：命令面板收成图标钮 */
  const searchSqueezed = computed(() => squeezeLevel.value >= 1)

  return { search, language, timezone, theme, fullscreen, preference, hasFloating, squeezable, searchSqueezed }
}

/**
 * 顶栏挤压的测量端，只在 AppHeader 挂一份。
 * 顶栏这一行挤（内容溢出，或面包屑被截断）时逐档加挤：先把命令面板收成图标钮，再把「自动」的工具从左往右
 * 一个个让到悬浮组，每一档记下它从工具区腾出的宽度；这一行的空档（中间弹性区的宽度）够放回上一档腾出的宽度时
 * 逐档放回，放回后又挤（工具自身宽度变了）就退回并改记。
 * 腾出的宽度只取决于那颗工具自己，与面包屑长短、面包屑是否显示、行宽都无关，路由与断点怎么变都不会记过时。
 * 加挤与放回都在同一轮微任务里做完，浏览器只画最后排定的那一版，不会一档档闪。
 *
 * @param row 顶栏这一行：溢出按它的 scrollWidth 与 clientWidth 比
 * @param toolbar 右侧工具区：腾出的宽度按它的宽度差量；账号名、租户名变宽也会挤，要盯它的尺寸
 * @param breadcrumb 面包屑：能压缩、会截断，截掉的宽度也算挤；它排在最后让
 * @param spacer 中间的弹性区：放得下时多出来的宽度都在它身上，就是这一行的空档
 */
export function useHeaderSqueeze(
  row: Readonly<Ref<HTMLElement | null | undefined>>,
  toolbar: Readonly<Ref<HTMLElement | null | undefined>>,
  breadcrumb: Readonly<Ref<HTMLElement | null | undefined>>,
  spacer: Readonly<Ref<HTMLElement | null | undefined>>,
) {
  const appStore = useAppStore()
  const { squeezable } = useWidgetPlacement()
  /** freed[k]：从第 k 档加到第 k + 1 档时工具区腾出的宽度 */
  const freed: number[] = []
  let settling = false
  const maxLevel = computed(() => 1 + squeezable.value.length)

  /** 面包屑被截掉的宽度：各段文字自己裁切（overflow 非 visible），截掉多少就是它们的 scrollWidth 超出多少 */
  function truncated() {
    const el = breadcrumb.value
    if (!el) {
      return 0
    }
    let width = 0
    for (const node of el.querySelectorAll<HTMLElement>('*')) {
      if (node.scrollWidth > node.clientWidth && getComputedStyle(node).overflowX !== 'visible') {
        width += node.scrollWidth - node.clientWidth
      }
    }
    return width
  }

  /** 挤了多少：这一行溢出的宽度加面包屑被截掉的宽度 */
  function pressure(el: HTMLElement) {
    return el.scrollWidth - el.clientWidth + truncated()
  }

  function toolbarWidth() {
    return toolbar.value?.getBoundingClientRect().width ?? 0
  }

  /** 加一档，记下这一档从工具区腾出的宽度 */
  async function squeezeOnce() {
    const before = toolbarWidth()
    squeezeLevel.value++
    await nextTick()
    freed[squeezeLevel.value - 1] = before - toolbarWidth()
  }

  async function settle() {
    const el = row.value
    if (!el || settling) {
      return
    }
    settling = true
    try {
      while (pressure(el) > 0 && squeezeLevel.value < maxLevel.value) {
        await squeezeOnce()
      }
      while (
        squeezeLevel.value > 0
        && pressure(el) <= 0
        && (spacer.value?.clientWidth ?? 0) >= (freed[squeezeLevel.value - 1] ?? Number.POSITIVE_INFINITY)
      ) {
        squeezeLevel.value--
        await nextTick()
        if (pressure(el) > 0) {
          await squeezeOnce()
          break
        }
      }
    }
    finally {
      settling = false
    }
  }

  /** 从头量：参与挤压的工具换了一批（改了位置设置、切了布局），或工具自身宽度变了（换语言），旧的挤压档与宽度都不作数 */
  async function remeasure() {
    squeezeLevel.value = 0
    freed.length = 0
    await nextTick()
    await settle()
  }

  // 尺寸变化在 ResizeObserver 回调里只记一笔、下一帧再量：回调里当场加挤会改动被观察元素的尺寸，
  // 浏览器这一轮会报「ResizeObserver loop completed with undelivered notifications」。下一帧的量在绘制之前，照样不闪
  let frame = 0
  function scheduleSettle() {
    if (frame) {
      return
    }
    frame = requestAnimationFrame(() => {
      frame = 0
      settle()
    })
  }

  useResizeObserver(row, scheduleSettle)
  useResizeObserver(toolbar, scheduleSettle)
  useResizeObserver(breadcrumb, scheduleSettle)
  onBeforeUnmount(() => cancelAnimationFrame(frame))
  watch(() => [squeezable.value.join(), appStore.locale], () => remeasure())
  onMounted(() => remeasure())
}
