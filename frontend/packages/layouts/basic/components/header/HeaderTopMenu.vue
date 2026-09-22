<script setup lang="ts">
import type { NavigationMenuNode } from '@xihan-ui/headless'
import type { AppMenuOption } from '~/types'
import { useDebounceFn } from '@vueuse/core'
import { resolveMotionPreference } from '@xihan-ui/motion'
import {
  XhButton,
  XhNavigationMenuContent,
  XhNavigationMenuItem,
  XhNavigationMenuLink,
  XhNavigationMenuList,
  XhNavigationMenuRoot,
  XhNavigationMenuTrigger,
  XhNavigationMenuViewport,
} from '@xihan-ui/vue'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { VNodeRender } from '~/components'
import { Icon } from '~/iconify'
import HeaderTopMenuPanel from './HeaderTopMenuPanel.vue'

/**
 * 顶栏横向菜单。
 *
 * 顶级入口由导航菜单负责（键盘横向遍历、浮层落位、指示条），但整套结构是手摆的而不是喂
 * collection 让它自动铺：collection 里的 label 只能是纯串，而顶栏标签要带角标与外链图标。
 * 入口之下的整棵子树走面板自绘——组件库的 menu / menubar 都只支持两级，路由菜单可以更深。
 *
 * 入口一行排不下时横向滚动（与标签栏同款：箭头 + 滚轮），而不是折行把顶栏撑高。
 * 面板因此不能再留在各自那一项里——列表成了滚动容器会把它裁掉——改放进组件库的共享外壳
 * （viewport）：它挂在根上、在滚动容器之外，水平落位由下面按当前入口算好写进 --panel-inset-start。
 */
defineOptions({ name: 'HeaderTopMenu' })

const props = defineProps<{
  options: AppMenuOption[]
  /** 当前选中项 */
  activeKey?: string
}>()

const emit = defineEmits<{ select: [key: string] }>()

/** 面板与视口右缘至少留这么多间距，贴边的入口才不会把面板顶出屏幕 */
const PANEL_EDGE_GAP = 8
/** 点一次箭头滚过的距离：一屏减去这一段，留出重叠便于对照 */
const SCROLL_OVERLAP = 120

/**
 * collection 仍要给：它是入口身份、禁用与键盘序列的事实源。
 * 无子级的入口给 href 占位，点它由 click 拦下走路由，不让浏览器整页跳走。
 */
const entries = computed<NavigationMenuNode[]>(() =>
  props.options.map<NavigationMenuNode>(option => ({
    value: option.key,
    label: typeof option.label === 'string' ? option.label : option.key,
    ...(option.disabled ? { disabled: true } : {}),
    ...(option.children?.length ? {} : { href: '#' }),
    ...(option.key === props.activeKey ? { current: true } : {}),
  })),
)

/** 带子级的入口：面板统一铺在共享外壳里，顺序与入口一致 */
const panelOptions = computed(() => props.options.filter(option => option.children?.length))

function onLinkClick(event: MouseEvent, key: string): void {
  event.preventDefault()
  emit('select', key)
}

// ── 横向滚动（与标签栏同款：箭头 + 滚轮，滚动条本身藏起来） ──────────
const scrollViewportRef = ref<HTMLElement | null>(null)
const showScrollBtn = ref(false)
const scrollAtStart = ref(true)
const scrollAtEnd = ref(false)

function scrollBehavior(): ScrollBehavior {
  return resolveMotionPreference() === 'reduce' ? 'instant' : 'smooth'
}

function calcShowScrollBtn() {
  const vp = scrollViewportRef.value
  if (!vp) {
    return
  }
  showScrollBtn.value = vp.scrollWidth > vp.clientWidth + 1
}

function updateScrollEdge() {
  const vp = scrollViewportRef.value
  if (!vp) {
    return
  }
  // RTL 下 scrollLeft 为负值（现代引擎口径），取绝对值即可当作「离起点多远」
  const offset = Math.abs(vp.scrollLeft)
  scrollAtStart.value = offset <= 1
  scrollAtEnd.value = offset + vp.clientWidth >= vp.scrollWidth - 1
}

function scrollDirection(dir: 'start' | 'end') {
  const vp = scrollViewportRef.value
  if (!vp) {
    return
  }
  const isRtl = getComputedStyle(vp).direction === 'rtl'
  const step = Math.max(vp.clientWidth - SCROLL_OVERLAP, SCROLL_OVERLAP)
  const toEnd = dir === 'end' ? 1 : -1
  vp.scrollBy({ behavior: scrollBehavior(), left: (isRtl ? -toEnd : toEnd) * step })
}

/** 当前页所在的入口滚进可视区：切路由后不必自己找 */
async function scrollToActive() {
  const vp = scrollViewportRef.value
  if (!vp || !props.activeKey) {
    return
  }
  await nextTick()
  if (vp.clientWidth >= vp.scrollWidth) {
    return
  }
  const active = vp.querySelector<HTMLElement>(`[data-value="${CSS.escape(props.activeKey)}"]`)
  active?.scrollIntoView({ behavior: scrollBehavior(), inline: 'nearest', block: 'nearest' })
}

function handleWheel(event: WheelEvent) {
  const vp = scrollViewportRef.value
  if (!vp || vp.scrollWidth <= vp.clientWidth) {
    return
  }
  event.preventDefault()
  vp.scrollBy({ left: event.deltaY !== 0 ? event.deltaY * 3 : event.deltaX * 3 })
}

// ── 面板落位：共享外壳挂在根上，水平位置跟着当前入口走 ────────────────
const openValue = ref<string | null>(null)
const panelInsetStart = ref(0)

function rootElement(): HTMLElement | null {
  return scrollViewportRef.value?.closest<HTMLElement>('[data-scope="navigation-menu"][data-part="root"]') ?? null
}

/**
 * 把共享外壳挪到当前入口下方：量的是入口与根的内联起点之差，
 * 再按视口右缘回拉，贴边的入口不会把面板顶出屏幕。
 */
async function syncPanelInset() {
  const value = openValue.value
  const root = rootElement()
  const vp = scrollViewportRef.value
  if (!value || !root || !vp) {
    return
  }
  await nextTick()
  const trigger = vp.querySelector<HTMLElement>(`[data-part="trigger"][data-value="${CSS.escape(value)}"]`)
  const panel = root.querySelector<HTMLElement>('[data-scope="navigation-menu"][data-part="viewport"]')
  if (!trigger || !panel) {
    return
  }
  const rootRect = root.getBoundingClientRect()
  const triggerRect = trigger.getBoundingClientRect()
  const panelWidth = panel.offsetWidth
  const isRtl = getComputedStyle(root).direction === 'rtl'
  // 内联起点：LTR 量左缘到左缘，RTL 量右缘到右缘
  const raw = isRtl ? rootRect.right - triggerRect.right : triggerRect.left - rootRect.left
  const rootStart = isRtl ? window.innerWidth - rootRect.right : rootRect.left
  const maxInset = window.innerWidth - rootStart - panelWidth - PANEL_EDGE_GAP
  panelInsetStart.value = Math.max(0, Math.min(raw, Math.max(maxInset, 0)))
}

function onValueChange(details: { value: string | null }) {
  openValue.value = details.value
  void syncPanelInset()
}

const debouncedCalc = useDebounceFn(calcShowScrollBtn, 80)
const debouncedEdge = useDebounceFn(updateScrollEdge, 80)

/** 滚动时面板跟着入口走：不跟的话它会停在原处，与入口对不上 */
function onViewportScroll() {
  debouncedEdge()
  void syncPanelInset()
}

let resizeObserver: ResizeObserver | null = null

onMounted(async () => {
  await nextTick()
  const vp = scrollViewportRef.value
  calcShowScrollBtn()
  updateScrollEdge()
  void scrollToActive()
  if (vp) {
    resizeObserver = new ResizeObserver(() => {
      debouncedCalc()
      debouncedEdge()
    })
    resizeObserver.observe(vp)
    vp.addEventListener('scroll', onViewportScroll, { passive: true })
    vp.addEventListener('wheel', handleWheel, { passive: false })
  }
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  const vp = scrollViewportRef.value
  vp?.removeEventListener('scroll', onViewportScroll)
  vp?.removeEventListener('wheel', handleWheel)
})

// 菜单项增减（切租户 / 权限变化）后重算箭头；当前页变化时把它滚进可视区
watch(() => props.options.length, () => {
  void nextTick(() => {
    calcShowScrollBtn()
    updateScrollEdge()
  })
})

watch(() => props.activeKey, () => {
  void scrollToActive()
})
</script>

<template>
  <XhNavigationMenuRoot
    class="header-top-menu"
    :collection="entries"
    @value-change="onValueChange"
  >
    <XhButton
      v-show="showScrollBtn"
      class="header-top-menu__arrow-btn"
      variant="ghost"
      size="sm"
      :disabled="scrollAtStart"
      :aria-label="$t('header.toolbar.menu_scroll_prev')"
      @click="scrollDirection('start')"
    >
      <Icon icon="lucide:chevrons-left" width="14" />
    </XhButton>

    <!-- 入口列表：一行排不下就横向滚动，滚动条藏起来交给箭头与滚轮 -->
    <div ref="scrollViewportRef" class="header-top-menu__viewport">
      <XhNavigationMenuList class="header-top-menu__list">
        <XhNavigationMenuItem v-for="option in options" :key="option.key">
          <!-- 有子级：入口是浮层触发器，面板铺在下面的共享外壳里 -->
          <XhNavigationMenuTrigger v-if="option.children?.length" :value="option.key">
            <span class="header-top-menu__entry">
              <VNodeRender v-if="typeof option.label === 'function'" :content="option.label()" />
              <template v-else>{{ option.label }}</template>
              <Icon icon="lucide:chevron-down" class="header-top-menu__arrow" />
            </span>
          </XhNavigationMenuTrigger>
          <!-- 无子级：入口即去处 -->
          <XhNavigationMenuLink
            v-else
            href="#"
            :current="option.key === activeKey"
            @click="(event: MouseEvent) => onLinkClick(event, option.key)"
          >
            <span class="header-top-menu__entry">
              <VNodeRender v-if="typeof option.label === 'function'" :content="option.label()" />
              <template v-else>{{ option.label }}</template>
            </span>
          </XhNavigationMenuLink>
        </XhNavigationMenuItem>
      </XhNavigationMenuList>
    </div>

    <XhButton
      v-show="showScrollBtn"
      class="header-top-menu__arrow-btn"
      variant="ghost"
      size="sm"
      :disabled="scrollAtEnd"
      :aria-label="$t('header.toolbar.menu_scroll_next')"
      @click="scrollDirection('end')"
    >
      <Icon icon="lucide:chevrons-right" width="14" />
    </XhButton>

    <!-- 共享面板外壳：挂在根上（滚动容器之外），横向落位跟着当前入口 -->
    <XhNavigationMenuViewport
      class="header-top-menu__panel-shell"
      :style="{ '--panel-inset-start': `${panelInsetStart}px` }"
    >
      <XhNavigationMenuContent
        v-for="option in panelOptions"
        :key="option.key"
        :value="option.key"
        class="header-top-menu__panel"
      >
        <HeaderTopMenuPanel
          :nodes="option.children!"
          :active-key="activeKey"
          @select="(key: string) => emit('select', key)"
        />
      </XhNavigationMenuContent>
    </XhNavigationMenuViewport>
  </XhNavigationMenuRoot>
</template>

<style scoped>
/* 顶级入口的观感对齐旧版顶栏：大档控件高（40px）、左右 10px、圆角 6px、正文字号。
   几何与各态配色走组件库的公开槽：展开中的入口套品牌淡底、指向当前页的链接染品牌字，
   悬停 / 按下 / 焦点环由 Collection Item 家族按状态给，这里不再直接盖 background / color。
   无子级的入口（link）与触发器同一副几何，取值直接引触发器那几支 */
.header-top-menu {
  --xh-navigation-menu-font-size: var(--xh-text-body-size);
  --xh-navigation-menu-trigger-h: var(--xh-control-h-lg);
  --xh-navigation-menu-trigger-px: 10px;
  --xh-navigation-menu-trigger-radius: 6px;
  --xh-navigation-menu-trigger-bg-active: hsl(var(--primary) / 15%);
  --xh-navigation-menu-link-font-size: var(--xh-navigation-menu-font-size);
  --xh-navigation-menu-link-px: var(--xh-navigation-menu-trigger-px);
  --xh-navigation-menu-link-py: 0;
  --xh-navigation-menu-link-radius: var(--xh-navigation-menu-trigger-radius);
  --xh-navigation-menu-link-fg-current: hsl(var(--primary));

  /* 根是箭头 + 滚动区一行排开；它同时是共享面板外壳的定位参照系（皮肤已给 relative） */
  display: flex;
  align-items: center;
  gap: var(--xh-space-1);
  min-width: 0;
}

/* 链接只有内衬槽没有高度槽，高度直接给 */
.header-top-menu :deep([data-scope='navigation-menu'][data-part='link']) {
  block-size: var(--xh-navigation-menu-trigger-h);
}

/* 滚动区：藏掉滚动条，滚动交给两端箭头与滚轮 */
.header-top-menu__viewport {
  flex: 1;
  min-width: 0;
  overflow-x: auto;
  overflow-y: hidden;
  scrollbar-width: none;
  overscroll-behavior-x: contain;
}

.header-top-menu__viewport::-webkit-scrollbar {
  display: none;
}

/* 入口不折行、不被压扁：排不下就交给滚动 */
.header-top-menu__list {
  flex-wrap: nowrap;
  width: max-content;
}

.header-top-menu__list :deep([data-scope='navigation-menu'][data-part='item']) {
  flex: none;
}

.header-top-menu__arrow-btn {
  flex: none;
}

/* 共享面板外壳：皮肤把它钉在根的内联起点，这里改接上面算好的偏移 */
.header-top-menu__panel-shell {
  inset-inline-start: var(--panel-inset-start, 0);
  padding: 6px;
}

.header-top-menu__panel {
  min-inline-size: 180px;
}

/* 有子级的入口带一枚下拉箭头 */
.header-top-menu__arrow {
  flex: none;
  opacity: 0.7;
}

.header-top-menu__entry {
  display: inline-flex;
  gap: 4px;
  align-items: center;
  min-inline-size: 0;
}
</style>
