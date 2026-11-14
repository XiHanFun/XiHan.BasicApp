<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { IslandAccessory, IslandAction, IslandTask } from '~/composables/useDynamicIsland'
import { useOnline } from '@vueuse/core'
import { XhButton } from '@xihan-ui/vue'
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { toast } from '~/composables'
import { configureDynamicIsland, islandStatus, useDynamicIsland } from '~/composables/useDynamicIsland'
import { configureNotificationSound } from '~/composables/useNotificationSound'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'
import { defaultLeading, defaultTrailing, usesElapsed } from './dynamic-island-view'
import DynamicIslandAccessory from './DynamicIslandAccessory.vue'
import DynamicIslandActivity from './DynamicIslandActivity.vue'

defineOptions({ name: 'DynamicIsland' })

const { t } = useI18n()
const router = useRouter()

// 偏好开关：关闭则灵动岛不呈现（可在偏好设置中切换）
const appStore = useAppStore()
const enabled = computed(() => appStore.widgetDynamicIsland)

// 关闭灵动岛时，终态（成功/失败/信息）由轻提示接管；开启时不接管，全程走灵动岛
configureDynamicIsland({
  isEnabled: () => appStore.widgetDynamicIsland,
  message: (state, content) => {
    if (state === 'success') {
      toast.success(content)
    }
    else if (state === 'error') {
      toast.danger(content)
    }
    else {
      toast.info(content)
    }
  },
})

// 提示音与灵动岛开关相互独立：岛关掉了也可能只想要声音
configureNotificationSound(() => appStore.notifySound)

const {
  current,
  activeTasks,
  expanded,
  loadingCount,
  pendingTasks,
  pendingCount,
  hasPanel,
  expand,
  collapse,
  dismissTask,
} = useDynamicIsland()

/** 壳体是否可见：折叠态需有当前任务，展开态需有面板内容 */
const shellVisible = computed(() => enabled.value && (expanded.value
  ? hasPanel.value
  : !!current.value))

// ── 折叠态形态 ──────────────────────────────────────────────────
// 单任务＝第一形态：中~宽自适应胶囊，显示任务文案
// 多任务＝第二形态：固定「宽」档胶囊，常驻聚合文案「N 个任务进行中」+ 计数 + 展开箭头
// 不再有更小的极简档（图标+计数），宽度只在「中」「宽」之间
// 聚合文案只在「岛上显示的确实是进行中任务」时生效：队首是消息时要显示消息本身
const isMulti = computed(() => loadingCount.value > 1 && current.value?.state === 'loading')
const displayLabel = computed(() => {
  if (!current.value) {
    return ''
  }
  if (isMulti.value) {
    return t('island.tasks_ongoing', { n: loadingCount.value })
  }
  return current.value.label
})

/** 胶囊左侧：多任务聚合或调用方没给时是状态指示器 */
const pillLeading = computed<IslandAccessory | null>(() => {
  const task = current.value
  if (!task) {
    return null
  }
  return isMulti.value || !task.leading
    ? defaultLeading(task)
    : task.leading
})
/** 左侧是不是状态指示器：只有它带状态色与完成态圆底 */
const pillLeadingIsState = computed(() => !!current.value && (isMulti.value || !current.value.leading))
/** 胶囊右侧：多任务聚合时只给百分比 */
const pillTrailing = computed<IslandAccessory | undefined>(() => {
  const task = current.value
  if (!task) {
    return undefined
  }
  return isMulti.value
    ? defaultTrailing(task)
    : task.trailing ?? defaultTrailing(task)
})

function onActionClick(task: IslandTask, action: IslandAction): void {
  action.handler()
  if (!action.keepOpen) {
    dismissTask(task.id)
  }
}

function isTaskClickable(task: IslandTask): boolean {
  return !!task.onClick || !!task.link
}

function onTaskClick(task: IslandTask): void {
  if (task.onClick) {
    task.onClick()
    collapse()
  }
  else if (task.link) {
    void router.push(task.link)
    collapse()
  }
}

// ── 计时（进行中的已用时、两侧或内容块里的计时）：用到时才每秒走一次 ────────
const nowTick = ref(Date.now())
let tickTimer: ReturnType<typeof setInterval> | null = null
const needsClock = computed(() => activeTasks.value.some(task =>
  (task.state === 'loading' && task.elapsed !== false) || usesElapsed(task)))
watch(needsClock, (needed) => {
  if (needed && !tickTimer) {
    nowTick.value = Date.now()
    tickTimer = setInterval(() => {
      nowTick.value = Date.now()
    }, 1000)
  }
  else if (!needed && tickTimer) {
    clearInterval(tickTimer)
    tickTimer = null
  }
}, { immediate: true })

// ── 单壳体形变：胶囊 ⇄ 面板共用一个容器，宽/高/圆角平滑过渡 ────────
const PILL_HEIGHT = 34
// 折叠态两档宽度：「中」(下限 200) 与「宽」(上限 320)。单任务在二者间按内容自适应，多任务固定「宽」档
// 中档下限刻意取得较饱满，避免「登录成功」等短文案缩成小胶囊，与「宽」档形成清晰的中/宽两级
const PILL_MIN_WIDTH = 200
const PILL_MAX_WIDTH = 320

const pillInnerRef = ref<HTMLElement | null>(null)
const panelLayerRef = ref<HTMLElement | null>(null)
const pillWidth = ref<number>(PILL_MIN_WIDTH)
const panelHeight = ref<number>(120)
const cardLayerRef = ref<HTMLElement | null>(null)
const cardHeight = ref<number>(72)

function panelWidth(): number {
  return Math.min(window.innerWidth * 0.86, 340)
}

async function measurePill(): Promise<void> {
  await nextTick()
  const inner = pillInnerRef.value
  if (!inner) {
    return
  }
  // 量取内容固有宽度：临时切到 max-content（同帧内改回，不产生闪烁）
  // 注意不能直接用 scrollWidth——inner 为 width:100% 时它不小于容器宽，会逐次放大
  const previousWidth = inner.style.width
  inner.style.width = 'max-content'
  const intrinsic = inner.getBoundingClientRect().width
  inner.style.width = previousWidth
  // 宽度只收敛为两档：「中」(PILL_MIN_WIDTH 下限) ~「宽」(PILL_MAX_WIDTH 上限)，不再有更小的极简档
  // 第二形态（多任务）固定「宽」档承载聚合文案；第一形态（单任务）在中~宽间按内容自适应，超「宽」自然截断
  const cap = Math.min(window.innerWidth * 0.8, PILL_MAX_WIDTH)
  pillWidth.value = isMulti.value
    ? cap
    : Math.min(Math.max(Math.ceil(intrinsic), Math.min(PILL_MIN_WIDTH, cap)), cap)
}

// ── 卡片态：参照 iOS 灵动岛「重要更新自动短暂展开、随后自行收回」──────
// 由任务的 alert 决定，缺省取「带正文（detail）或内容块」：聊天消息一类有内容的才值得抢占视线，
// 纯状态胶囊（任务进行中）不打断。用户手动展开时立刻让位给内容态
const CARD_LINGER = 2600
const cardTaskId = ref<string | null>(null)
let cardTimer: ReturnType<typeof setTimeout> | null = null

const cardTask = computed<IslandTask | null>(() =>
  (cardTaskId.value
    ? activeTasks.value.find(item => item.id === cardTaskId.value) ?? null
    : null))

function clearCardTimer(): void {
  if (cardTimer) {
    clearTimeout(cardTimer)
    cardTimer = null
  }
}

function dismissCard(): void {
  clearCardTimer()
  cardTaskId.value = null
}

function presentCard(task: IslandTask): void {
  cardTaskId.value = task.id
  clearCardTimer()
  cardTimer = setTimeout(dismissCard, CARD_LINGER)
}

/** 当前形态：内容态优先于卡片态，卡片态优先于胶囊 */
const mode = computed<'pill' | 'card' | 'panel'>(() => {
  if (expanded.value) {
    return 'panel'
  }
  return cardTask.value ? 'card' : 'pill'
})

/** 卡片点击：有跳转目标就直达，否则展开成内容态 */
function onCardClick(): void {
  const task = cardTask.value
  dismissCard()
  if (task && (task.onClick || task.link)) {
    onTaskClick(task)
    return
  }
  expand()
}

// ── 入栈动效：岛已在屏上时，新消息自顶缘落下压到最前 ─────────────────
// 只认「真的更新的那条」：最前一条到期后浮上来的是更旧的一条，此时再落一次
// 就成了「又来新消息」的假象
const sliding = ref(false)
const lastTopOrder = ref(0)
let slideTimer: ReturnType<typeof setTimeout> | null = null

function triggerSlideIn(): void {
  sliding.value = false
  requestAnimationFrame(() => {
    sliding.value = true
    if (slideTimer) {
      clearTimeout(slideTimer)
    }
    slideTimer = setTimeout(() => {
      sliding.value = false
      slideTimer = null
    }, 520)
  })
}

watch(
  // 键里带 detail：同一条目内容被更新（如同一任务推来新正文）也要重新呈现卡片，
  // 否则内容变了却因 id/状态/标题未变而静默停留在胶囊
  () => (current.value
    ? `${current.value.id}:${current.value.state}:${current.value.label}:${current.value.detail ?? ''}`
    : null),
  (next, prev) => {
    const task = current.value
    if (!next || !task) {
      return
    }
    const isNewer = task.order > lastTopOrder.value
    lastTopOrder.value = task.order
    if (expanded.value) {
      return
    }
    if (!isNewer) {
      // 前一条到期后浮上来的是更旧的一条，它自己的计时也快到了，
      // 这时再展成卡片会一闪而过，维持胶囊即可
      return
    }
    // 仅「岛已可见」时才落下；首次出现交给入场动效
    if (prev && next !== prev) {
      triggerSlideIn()
    }
    if (task.alert ?? Boolean(task.detail || task.blocks?.length)) {
      presentCard(task)
    }
  },
)

// 手动展开内容态时卡片让位；任务被移除时卡片同步撤下
watch(expanded, (open) => {
  if (open) {
    dismissCard()
  }
})
watch(cardTask, (task) => {
  if (!task) {
    dismissCard()
  }
})

// 卡片高度随正文行数变化，同样以 ResizeObserver 驱动壳体过渡
let cardObserver: ResizeObserver | null = null
watch(cardLayerRef, (layer, _old, onCleanup) => {
  if (!layer) {
    return
  }
  cardObserver ??= new ResizeObserver(() => {
    if (cardLayerRef.value) {
      cardHeight.value = cardLayerRef.value.offsetHeight
    }
  })
  cardObserver.observe(layer)
  cardHeight.value = layer.offsetHeight
  onCleanup(() => cardObserver?.unobserve(layer))
}, { flush: 'post' })

// 面板内容尺寸随任务变化：ResizeObserver 持续驱动壳体高度过渡（惰性创建，规避挂载时序）
let panelObserver: ResizeObserver | null = null
watch(panelLayerRef, (layer, _old, onCleanup) => {
  if (!layer) {
    return
  }
  panelObserver ??= new ResizeObserver(() => {
    if (panelLayerRef.value) {
      panelHeight.value = panelLayerRef.value.offsetHeight
    }
  })
  panelObserver.observe(layer)
  panelHeight.value = layer.offsetHeight
  onCleanup(() => panelObserver?.unobserve(layer))
}, { flush: 'post' })

watch(
  () => [
    displayLabel.value,
    current.value?.state,
    pillLeading.value?.type,
    pillTrailing.value?.type === 'text'
      ? pillTrailing.value.text
      : pillTrailing.value?.type,
    loadingCount.value,
    pendingCount.value,
  ] as const,
  () => {
    void measurePill()
  },
  { immediate: true },
)

// ── 折叠态堆叠：最新一条在最前完整展示，其余在下方露边 ─────────────────
// 露两层（含最前共三条可见）与 Sonner 的 visibleToasts、AntD 的堆叠阈值一致；
// 再多的不再露层，改由文案右侧的 +N 兜底，读全部走展开面板
const STACK_MAX_LAYERS = 2
const hovering = ref(false)

/** 露在后方的消息：栈顶之下由新到旧取前几条 */
const stackTasks = computed(() => (expanded.value
  ? []
  : pendingTasks.value.slice(0, STACK_MAX_LAYERS)))

/**
 * 叠层几何：与最前一条等大，逐层下移并缩小，从下缘露边
 * 朝下叠：岛在屏幕顶部，堆叠一律朝屏幕内侧延伸
 */
function layerStyle(depth: number): Record<string, string> {
  const isCard = mode.value === 'card'
  const lift = (hovering.value ? 18 : 12) * depth
  return {
    width: `${isCard
      ? panelWidth()
      : pillWidth.value}px`,
    height: `${isCard ? cardHeight.value : PILL_HEIGHT}px`,
    borderRadius: isCard ? '20px' : '18px',
    transform: `translateX(-50%) translateY(${lift}px) scale(${1 - 0.05 * depth})`,
    opacity: `${0.92 - (depth - 1) * 0.2}`,
    // 越旧压得越深；负值让整叠都落在壳体之后（.di-root 自成层叠上下文，不会漏到页面底下）
    zIndex: `${-depth}`,
  }
}

/**
 * 叠层按「消息」取 key 而不是层号：按层号取的话，持续来新消息时层数恒为上限，
 * key 集合不变，元素只是原地改样式，进出场动画一次都不会触发
 * DOM 顺序保持由新到旧固定不变（前后靠 z-index 定），避免 TransitionGroup 走 FLIP 与行内 transform 打架
 */
const stackLayers = computed(() =>
  stackTasks.value.map((task, index) => ({
    key: task.id,
    style: layerStyle(index + 1),
  })))

const shellStyle = computed(() => {
  if (expanded.value) {
    return {
      width: `${panelWidth()}px`,
      height: `${panelHeight.value}px`,
      borderRadius: '16px',
    }
  }
  // 卡片态：与内容态同宽，高度随正文行数；圆角介于胶囊与面板之间
  if (mode.value === 'card') {
    return {
      width: `${panelWidth()}px`,
      height: `${cardHeight.value}px`,
      borderRadius: '20px',
    }
  }
  // 折叠态恒为胶囊圆角：参照 iOS 灵动岛——完成态不改变形状，仅内容从进度环切换为状态图标
  return {
    width: `${pillWidth.value}px`,
    height: `${PILL_HEIGHT}px`,
    borderRadius: '18px',
  }
})

// ── 点击外部 / Esc 收起 ──────────────────────────────────────────
const rootRef = ref<HTMLElement | null>(null)
function onDocPointer(e: PointerEvent): void {
  if (expanded.value && rootRef.value && !rootRef.value.contains(e.target as Node)) {
    collapse()
  }
}
function onKeydown(e: KeyboardEvent): void {
  if (e.key === 'Escape') {
    collapse()
  }
}
watch(expanded, (open) => {
  if (open) {
    document.addEventListener('pointerdown', onDocPointer, true)
    document.addEventListener('keydown', onKeydown)
  }
  else {
    document.removeEventListener('pointerdown', onDocPointer, true)
    document.removeEventListener('keydown', onKeydown)
    void measurePill()
  }
})
// 不在场的层是 inert 的：键盘展开时把焦点送进面板，收起时还给胶囊，不让焦点掉回页面开头
const pillRef = ref<HTMLElement | null>(null)
watch(expanded, async (open) => {
  if (!rootRef.value?.contains(document.activeElement)) {
    return
  }
  await nextTick()
  if (open) {
    panelLayerRef.value?.querySelector<HTMLElement>('button:not([disabled])')?.focus()
  }
  else {
    pillRef.value?.focus()
  }
}, { flush: 'pre' })

// 无可展开内容时自动收起
watch(hasPanel, (has) => {
  if (!has) {
    collapse()
  }
})

// ── 网络状态（常驻） ─────────────────────────────────────────────
const online = useOnline()
let netHandle: ReturnType<typeof islandStatus> | null = null
watch(online, (isOnline, was) => {
  if (!isOnline) {
    netHandle = islandStatus('sys:network', t('island.network_lost'), {
      state: 'error',
      icon: 'lucide:wifi-off',
      detail: t('island.network_lost_detail'),
    })
  }
  else if (was === false) {
    netHandle?.success(t('island.network_restored'))
    netHandle = null
  }
}, { immediate: true })

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocPointer, true)
  document.removeEventListener('keydown', onKeydown)
  panelObserver?.disconnect()
  panelObserver = null
  cardObserver?.disconnect()
  cardObserver = null
  clearCardTimer()
  if (tickTimer) {
    clearInterval(tickTimer)
    tickTimer = null
  }
  if (slideTimer) {
    clearTimeout(slideTimer)
    slideTimer = null
  }
})
</script>

<template>
  <Teleport to="body">
    <div ref="rootRef" class="di-root">
      <!-- 后方叠层：先于壳体入 DOM 以保证被壳体压住，仅折叠态且队列里还有待展示时出现 -->
      <TransitionGroup name="di-stack">
        <div
          v-for="layer in stackLayers"
          v-show="shellVisible"
          :key="layer.key"
          class="di-stack"
          :style="layer.style"
        />
      </TransitionGroup>
      <Transition name="island" appear>
        <div
          v-if="shellVisible"
          class="di-shell"
          :class="[expanded ? 'is-open' : `is-${current?.state ?? 'info'}`, { 'di-slide': sliding, 'is-persistent': !expanded && current?.persistent }]"
          :style="shellStyle"
          data-xh-ink="light"
          @pointerenter="hovering = true"
          @pointerleave="hovering = false"
        >
          <!-- 折叠层：胶囊，左侧 leading、中间文案、右侧 trailing 与岛自己的计数和展开箭头 -->
          <button
            ref="pillRef"
            type="button"
            class="di-layer di-pill"
            :class="{ 'is-active': mode === 'pill' }"
            :aria-label="current?.label"
            :inert="mode !== 'pill'"
            @click="expand"
          >
            <span ref="pillInnerRef" class="di-pill__inner">
              <span v-if="pillLeading && current" class="di-indicator" :class="{ 'is-state': pillLeadingIsState }">
                <DynamicIslandAccessory :accessory="pillLeading" :started-at="current.startedAt" :now="nowTick" />
              </span>
              <Transition name="di-text" mode="out-in">
                <span v-if="displayLabel" :key="displayLabel" class="di-label">{{ displayLabel }}</span>
              </Transition>
              <span class="di-trailing">
                <DynamicIslandAccessory v-if="pillTrailing && current" :accessory="pillTrailing" :started-at="current.startedAt" :now="nowTick" />
                <span v-if="pendingCount > STACK_MAX_LAYERS" class="di-count">+{{ pendingCount }}</span>
                <Icon v-if="hasPanel" icon="lucide:chevron-down" width="13" height="13" class="di-chevron" />
              </span>
            </span>
          </button>

          <!-- 卡片层：有新内容时自动短暂呈现这一条的展开视图，点击进入目标或展开面板
               三层叠着做交叉淡变，不在场的层 inert：读屏与 Tab 都跳过它 -->
          <div ref="cardLayerRef" class="di-layer di-card" :class="{ 'is-active': mode === 'card' }" :inert="mode !== 'card'">
            <DynamicIslandActivity
              v-if="cardTask"
              :task="cardTask"
              variant="card"
              clickable
              :now="nowTick"
              @open="onCardClick"
              @action="action => cardTask && onActionClick(cardTask, action)"
            />
          </div>

          <!-- 展开层：活动面板 -->
          <div ref="panelLayerRef" class="di-layer di-panel" :class="{ 'is-active': expanded }" :inert="!expanded">
            <div class="di-panel__head">
              <span class="di-panel__title">{{ t('island.title') }}</span>
              <XhButton variant="ghost" size="sm" icon-only class="x-icon-btn" :aria-label="t('island.collapse')" @click.stop="collapse">
                <Icon icon="lucide:chevron-up" />
              </XhButton>
            </div>

            <div class="di-panel__body">
              <DynamicIslandActivity
                v-for="task in activeTasks"
                :key="task.id"
                :task="task"
                variant="row"
                :clickable="isTaskClickable(task)"
                :now="nowTick"
                @open="onTaskClick(task)"
                @action="action => onActionClick(task, action)"
                @dismiss="dismissTask(task.id)"
              />
            </div>
          </div>
        </div>
      </Transition>
    </div>
  </Teleport>
</template>

<style scoped>
.di-root {
  position: fixed;
  top: 10px;
  inset-inline-start: 50%;
  z-index: 3000;
  transform: translateX(-50%);
  pointer-events: none;
  display: flex;
  justify-content: center;
}

/* ============ 后方叠层：多条活动消息的「还有更多」暗示 ============ */
/* 与壳体同色（压低亮度会糊成灰块，看不出是卡片），靠缩放与左右挪出体现前后
   不接收指针事件：点击落在壳体上，行为与单条时一致 */
.di-stack {
  position: absolute;
  top: 0;
  inset-inline-start: 50%;
  pointer-events: none;
  background: color-mix(in srgb, var(--xh-color-neutral-950) 94%, transparent);
  box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--xh-fg-on-media-chrome) 7%, transparent);
  /* 底色恒为深色、与明暗主题解耦，只取统一的玻璃模糊（ui.css 的 --app-glass-backdrop） */
  -webkit-backdrop-filter: var(--app-glass-backdrop);
  backdrop-filter: var(--app-glass-backdrop);
  transition:
    width var(--xh-motion-duration-slide) var(--xh-motion-ease-settle),
    height var(--xh-motion-duration-slide) var(--xh-motion-ease-settle),
    border-radius var(--xh-motion-duration-slide) var(--xh-motion-ease-settle),
    transform var(--xh-motion-duration-slide) var(--xh-motion-ease-settle),
    opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

/* 入栈动效：新叠层从「最前那条的原位、原尺寸」沉到后方，看着就是上一条被压下去
   关键帧只写起始态，结束态隐式取元素自身的行内 transform（各层位移不同，写死不了）；
   动画期间关键帧优先级高于行内样式，动画结束自然落回行内位置 */
.di-stack-enter-active {
  /* 关掉基础 transition：入场只跑关键帧，避免与之并存时 Vue 把过渡类型判成 transition */
  transition: none;
  animation: di-stack-push var(--xh-motion-duration-slide) var(--xh-motion-ease-settle);
}

@keyframes di-stack-push {
  from {
    transform: translateX(-50%) scale(1);
    opacity: 0.95;
  }
}

/* 出栈动效：最旧的一层顺着同一方向继续下沉并淡出
   与入场对称——关键帧只写结束态，起始态隐式取元素自身的行内 transform */
.di-stack-leave-active {
  transition: none;
  animation: di-stack-drop var(--xh-motion-duration-slide) var(--xh-motion-ease-exit) forwards;
}

@keyframes di-stack-drop {
  to {
    transform: translateX(-50%) translateY(34px) scale(0.8);
    opacity: 0;
  }
}

/* 关掉 TransitionGroup 的 FLIP：叠层位置全靠行内 transform，而 FLIP 会直接改写
   el.style.transform 做位移补偿，两者互相覆盖。层间推移交给 .di-stack 自带的
   transform 过渡即可（深度变了行内值就变，自然滑过去） */
.di-stack-move {
  transition: none;
}

/* ============ 单壳体：胶囊 ⇄ 面板形变容器 ============ */
/* 标志性"灵动岛"恒为深色，与明暗主题解耦 */
.di-shell {
  position: relative;
  pointer-events: auto;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 95%, transparent);
  background: color-mix(in srgb, var(--xh-color-neutral-950) 94%, transparent);
  box-shadow:
    0 10px 30px color-mix(in srgb, var(--xh-color-neutral-950) 30%, transparent),
    inset 0 0 0 1px color-mix(in srgb, var(--xh-fg-on-media-chrome) 8%, transparent);
  -webkit-backdrop-filter: var(--app-glass-backdrop);
  backdrop-filter: var(--app-glass-backdrop);
  overflow: hidden;
  /* 宽/高/圆角统一缓动：展开收起为同一容器的形变，不再上下抖动 */
  transition:
    width var(--xh-motion-duration-slide) var(--xh-motion-ease-settle),
    height var(--xh-motion-duration-slide) var(--xh-motion-ease-settle),
    border-radius var(--xh-motion-duration-slide) var(--xh-motion-ease-settle);
}

/* 两层内容：淡入淡出交叉过渡，非活动层不可交互 */
.di-layer {
  position: absolute;
  top: 0;
  inset-inline-start: 0;
  opacity: 0;
  pointer-events: none;
  transition: opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.di-layer.is-active {
  opacity: 1;
  pointer-events: auto;
  transition: opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter)
    calc(2 * var(--xh-motion-stagger-step));
}

/* 键盘聚焦给与组件库同一道环：2px 画在层自己的盒内。不写的话落回浏览器缺省环，画在盒外、被岛壳的 overflow 裁掉 */
.di-layer:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

/* ============ 折叠层：胶囊 ============ */
.di-pill {
  width: 100%;
  height: 34px;
  padding: 0;
  border: none;
  background: transparent;
  color: inherit;
  font-size: var(--xh-font-size-sm);
  font-weight: 500;
  line-height: 1;
  white-space: nowrap;
  cursor: pointer;
}

/* 图标贴最左、操作贴最右：label 弹性占中，两端由内边距锚定 */
.di-pill__inner {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  width: 100%;
  height: 100%;
  padding: 0 var(--xh-space-3) 0 var(--xh-space-3);
}

.di-label {
  flex: 1 1 auto;
  min-width: 0;
  text-align: left;
  overflow: hidden;
  text-overflow: ellipsis;
}

.di-trailing {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1_5);
  flex-shrink: 0;
  margin-inline-start: auto;
}

.di-indicator {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 20px;
  height: 20px;
  color: color-mix(in srgb, var(--xh-fg-on-media-chrome) 85%, transparent);
}

/* 调用方给的小件按自身宽度排（短文本、头像），定宽的圆只给状态指示器 */
.di-indicator:not(.is-state) {
  width: auto;
  min-width: var(--xh-glyph-size-lg);
}

.is-success .di-indicator.is-state {
  color: color-mix(in srgb, var(--xh-fg-success) 75%, var(--xh-fg-on-media-chrome));
}

.is-error .di-indicator.is-state {
  color: color-mix(in srgb, var(--xh-fg-danger) 85%, var(--xh-fg-on-media-chrome));
}

.is-info .di-indicator.is-state {
  color: color-mix(in srgb, var(--xh-fg-brand) 70%, var(--xh-fg-on-media-chrome));
}

/* ============ 终态：折叠态指示器用状态色实心圆底 + 深色图标 ============ */
/* 形状仍是胶囊（同 iOS 完成态不变形），靠状态色圆底图标 + 弹入动效与「进度环」区分
   常驻状态（模仿登录、网络或实时连接断开）不是完成反馈，不画圆底、不弹入，与面板里同一个图标 */
.di-shell.is-success:not(.is-persistent) .di-pill__inner .di-indicator.is-state,
.di-shell.is-error:not(.is-persistent) .di-pill__inner .di-indicator.is-state,
.di-shell.is-info:not(.is-persistent) .di-pill__inner .di-indicator.is-state {
  width: 18px;
  height: 18px;
  border-radius: var(--xh-shape-pill);
  color: color-mix(in srgb, var(--xh-color-neutral-950) 92%, transparent);
  /* 完成反馈：状态图标弹现，呼应 iOS 灵动岛完成态的图标弹入 */
  animation: di-badge-pop var(--xh-motion-duration-slide) var(--xh-motion-ease-settle);
}

@keyframes di-badge-pop {
  0% {
    transform: scale(0.2);
    opacity: 0;
  }

  55% {
    transform: scale(1.12);
    opacity: 1;
  }

  100% {
    transform: scale(1);
  }
}

.di-shell.is-success:not(.is-persistent) .di-pill__inner .di-indicator.is-state {
  background: color-mix(in srgb, var(--xh-fg-success) 75%, var(--xh-fg-on-media-chrome));
}

.di-shell.is-error:not(.is-persistent) .di-pill__inner .di-indicator.is-state {
  background: color-mix(in srgb, var(--xh-fg-danger) 85%, var(--xh-fg-on-media-chrome));
}

.di-shell.is-info:not(.is-persistent) .di-pill__inner .di-indicator.is-state {
  background: color-mix(in srgb, var(--xh-fg-brand) 70%, var(--xh-fg-on-media-chrome));
}

.di-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  height: 16px;
  padding: 0 var(--xh-space-1);
  border-radius: var(--xh-shape-pill);
  background: color-mix(in srgb, var(--xh-fg-on-media-chrome) 16%, transparent);
  font-size: var(--xh-font-size-xs);
  font-weight: 600;
}

.di-chevron {
  opacity: 0.5;
}

/* ============ 展开层：活动面板 ============ */
/* 固定目标宽度（与壳体展开宽度一致）：折叠态下测得的高度即最终高度，形变期间内容不回流 */
.di-panel {
  width: min(86vw, 340px);
}

.di-panel__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--xh-space-2_5) var(--xh-space-3) var(--xh-space-1_5) var(--xh-space-4);
}

.di-panel__title {
  font-size: var(--xh-font-size-sm);
  font-weight: 600;
  letter-spacing: 0.02em;
}

.di-panel__body {
  max-height: min(60vh, 460px);
  padding: var(--xh-space-1) var(--xh-space-2) var(--xh-space-2_5);
  overflow-y: auto;
}

/* ============ 壳体出现/消失过渡：从视口顶缘弹出 / 收回 ============ */
/* 入场：自顶部滑入并轻微回弹（appear 保证刷新恢复任务时的初始挂载同样有动效） */
.island-enter-active {
  transition:
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-slide) var(--xh-motion-ease-settle);
}

/* 离场：加速收回顶部 */
.island-leave-active {
  transition:
    opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-exit) var(--xh-motion-stagger-step),
    transform var(--xh-motion-duration-slide) var(--xh-motion-ease-exit);
}

.island-enter-from,
.island-leave-to {
  opacity: 0;
  /* -100% 按壳体自身高度计算，再加顶部留白，确保完全移出视口上缘 */
  transform: translateY(calc(-100% - 14px)) scale(0.88);
}

/* 入栈：新消息自顶缘落下压到最前（岛已在屏上、不重新入场时）
   方向与堆叠一致——堆朝下延伸，新的就从上方来 */
.di-slide {
  animation: di-slide-in var(--xh-motion-duration-slide) var(--xh-motion-ease-settle);
}

@keyframes di-slide-in {
  0% {
    transform: translateY(-16px) scale(0.94);
    opacity: 0.3;
  }

  100% {
    transform: translateY(0) scale(1);
    opacity: 1;
  }
}

.di-text-enter-active,
.di-text-leave-active {
  transition:
    opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-move) var(--xh-motion-ease-enter);
}

.di-text-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.di-text-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* ── 卡片态：介于胶囊与内容面板之间的中间形态，承载这一条的展开视图 ── */
.di-card {
  inline-size: 100%;
}
</style>
