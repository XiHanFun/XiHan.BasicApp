<script lang="ts" setup>
import type { IslandAction, IslandState, IslandTask } from '~/composables/useDynamicIsland'
import { useOnline } from '@vueuse/core'
import { useMessage } from 'naive-ui'
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { configureDynamicIsland, islandStatus, useDynamicIsland } from '~/composables/useDynamicIsland'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'

defineOptions({ name: 'DynamicIsland' })

const { t } = useI18n()
const router = useRouter()

// 偏好开关：关闭则灵动岛不呈现（可在偏好设置中切换）
const appStore = useAppStore()
const enabled = computed(() => appStore.widgetDynamicIsland)

// 关闭灵动岛时，终态（成功/失败/信息）由 Naive Message 接管；开启时不接管，全程走灵动岛
const message = useMessage()
configureDynamicIsland({
  isEnabled: () => appStore.widgetDynamicIsland,
  message: (state, content) => {
    if (state === 'success') {
      message.success(content)
    }
    else if (state === 'error') {
      message.error(content)
    }
    else {
      message.info(content)
    }
  },
})

const {
  current,
  activeTasks,
  history,
  expanded,
  loadingCount,
  hasPanel,
  expand,
  collapse,
  dismissTask,
  clearHistory,
} = useDynamicIsland()

/** 壳体是否可见：折叠态需有当前任务，展开态需有面板内容 */
const shellVisible = computed(() => enabled.value && (expanded.value ? hasPanel.value : !!current.value))

function stateIcon(state?: IslandState): string {
  switch (state) {
    case 'success':
      return 'lucide:check'
    case 'error':
      return 'lucide:triangle-alert'
    case 'info':
      return 'lucide:info'
    default:
      return 'lucide:loader-2'
  }
}

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

// ── 进度环（确定态）参数 ─────────────────────────────────────────
const RING_RADIUS = 8
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS

function ringDashOffset(progress: number): number {
  const clamped = Math.max(0, Math.min(100, progress))
  return RING_CIRCUMFERENCE * (1 - clamped / 100)
}

// ── 耗时计时（任一 loading 任务存在时每秒刷新） ───────────────────
const nowTick = ref(Date.now())
let tickTimer: ReturnType<typeof setInterval> | null = null
watch(loadingCount, (count) => {
  if (count > 0 && !tickTimer) {
    tickTimer = setInterval(() => {
      nowTick.value = Date.now()
    }, 1000)
  }
  else if (count === 0 && tickTimer) {
    clearInterval(tickTimer)
    tickTimer = null
  }
}, { immediate: true })

function elapsedText(task: IslandTask): string {
  const seconds = Math.max(0, Math.floor((nowTick.value - task.startedAt) / 1000))
  if (seconds < 60) {
    return `${t('island.elapsed_prefix')} ${seconds}s`
  }
  return `${t('island.elapsed_prefix')} ${Math.floor(seconds / 60)}m ${seconds % 60}s`
}

function relativeTime(time: number): string {
  const diff = Date.now() - time
  if (diff < 60_000) {
    return t('island.just_now')
  }
  if (diff < 3_600_000) {
    return t('island.minutes_ago', { n: Math.floor(diff / 60_000) })
  }
  return t('island.hours_ago', { n: Math.floor(diff / 3_600_000) })
}

// ── 单壳体形变：胶囊 ⇄ 面板共用一个容器，宽/高/圆角平滑过渡 ────────
const PILL_HEIGHT = 34
const PILL_H_PADDING = 26 // 左 14 + 右 12，与样式对齐

const pillInnerRef = ref<HTMLElement | null>(null)
const panelLayerRef = ref<HTMLElement | null>(null)
const pillWidth = ref<number>(180)
const panelHeight = ref<number>(120)

function panelWidth(): number {
  return Math.min(window.innerWidth * 0.86, 340)
}

async function measurePill(): Promise<void> {
  await nextTick()
  const inner = pillInnerRef.value
  if (inner) {
    pillWidth.value = Math.min(inner.scrollWidth + PILL_H_PADDING, Math.min(window.innerWidth * 0.8, 440))
  }
}

// 面板内容尺寸随任务/历史变化：ResizeObserver 持续驱动壳体高度过渡（惰性创建，规避挂载时序）
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
  () => [current.value?.label, current.value?.state, current.value?.progress != null, loadingCount.value] as const,
  () => {
    void measurePill()
  },
  { immediate: true },
)

const shellStyle = computed(() => {
  if (expanded.value) {
    return {
      width: `${panelWidth()}px`,
      height: `${panelHeight.value}px`,
      borderRadius: '16px',
    }
  }
  return {
    width: `${pillWidth.value}px`,
    height: `${PILL_HEIGHT}px`,
    borderRadius: '17px',
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
  if (tickTimer) {
    clearInterval(tickTimer)
    tickTimer = null
  }
})
</script>

<template>
  <Teleport to="body">
    <div ref="rootRef" class="di-root">
      <Transition name="island">
        <div
          v-if="shellVisible"
          class="di-shell"
          :class="[expanded ? 'is-open' : `is-${current?.state ?? 'info'}`]"
          :style="shellStyle"
        >
          <!-- 折叠层：胶囊（图标贴最左，按钮贴最右） -->
          <button
            type="button"
            class="di-layer di-pill"
            :class="{ 'is-active': !expanded }"
            :aria-label="current?.label"
            :tabindex="expanded ? -1 : 0"
            @click="expand"
          >
            <span ref="pillInnerRef" class="di-pill__inner">
              <!-- 指示器：确定态进度环 / 不确定态旋转弧环 / 终态状态图标 -->
              <span class="di-indicator">
                <template v-if="current?.state === 'loading'">
                  <svg class="di-ring" :class="{ 'di-ring--spin': current.progress == null }" viewBox="0 0 20 20">
                    <circle class="di-ring__track" cx="10" cy="10" :r="RING_RADIUS" />
                    <circle
                      class="di-ring__bar"
                      cx="10"
                      cy="10"
                      :r="RING_RADIUS"
                      :stroke-dasharray="RING_CIRCUMFERENCE"
                      :stroke-dashoffset="current.progress == null ? RING_CIRCUMFERENCE * 0.72 : ringDashOffset(current.progress)"
                    />
                  </svg>
                  <Icon v-if="current.icon" :icon="current.icon" width="9" height="9" class="di-ring__icon" />
                </template>
                <Icon v-else-if="current" :icon="current.icon || stateIcon(current.state)" width="15" height="15" />
              </span>
              <Transition name="di-text" mode="out-in">
                <span :key="`${current?.state}:${current?.label}`" class="di-label">{{ current?.label }}</span>
              </Transition>
              <span class="di-trailing">
                <span v-if="current?.state === 'loading' && current?.progress != null" class="di-pct">
                  {{ Math.round(current.progress) }}%
                </span>
                <span v-if="loadingCount > 1" class="di-count">{{ loadingCount }}</span>
                <Icon v-if="hasPanel" icon="lucide:chevron-down" width="13" height="13" class="di-chevron" />
              </span>
            </span>
          </button>

          <!-- 展开层：活动面板 -->
          <div ref="panelLayerRef" class="di-layer di-panel" :class="{ 'is-active': expanded }">
            <div class="di-panel__head">
              <span class="di-panel__title">{{ t('island.title') }}</span>
              <button type="button" class="di-panel__close" :aria-label="t('island.collapse')" :tabindex="expanded ? 0 : -1" @click.stop="collapse">
                <Icon icon="lucide:chevron-up" width="16" height="16" />
              </button>
            </div>

            <div class="di-panel__body">
              <!-- 活动中 -->
              <template v-if="activeTasks.length">
                <div
                  v-for="task in activeTasks"
                  :key="task.id"
                  class="di-item"
                  :class="[`is-${task.state}`, { 'is-clickable': isTaskClickable(task) }]"
                  @click="onTaskClick(task)"
                >
                  <span class="di-indicator">
                    <template v-if="task.state === 'loading'">
                      <svg class="di-ring" :class="{ 'di-ring--spin': task.progress == null }" viewBox="0 0 20 20">
                        <circle class="di-ring__track" cx="10" cy="10" :r="RING_RADIUS" />
                        <circle
                          class="di-ring__bar"
                          cx="10"
                          cy="10"
                          :r="RING_RADIUS"
                          :stroke-dasharray="RING_CIRCUMFERENCE"
                          :stroke-dashoffset="task.progress == null ? RING_CIRCUMFERENCE * 0.72 : ringDashOffset(task.progress)"
                        />
                      </svg>
                      <Icon v-if="task.icon" :icon="task.icon" width="9" height="9" class="di-ring__icon" />
                    </template>
                    <Icon v-else :icon="task.icon || stateIcon(task.state)" width="15" height="15" />
                  </span>
                  <div class="di-item__body">
                    <div class="di-item__label">
                      {{ task.label }}
                    </div>
                    <div v-if="task.detail" class="di-item__detail">
                      {{ task.detail }}
                    </div>
                    <div v-if="task.progress != null && task.state === 'loading'" class="di-progress">
                      <span class="di-progress__track">
                        <span class="di-progress__bar" :style="{ width: `${task.progress}%` }" />
                      </span>
                      <span class="di-progress__pct">{{ Math.round(task.progress) }}%</span>
                    </div>
                    <div v-if="task.state === 'loading'" class="di-item__meta">
                      {{ elapsedText(task) }}
                    </div>
                    <div v-if="task.actions?.length" class="di-actions">
                      <button
                        v-for="action in task.actions"
                        :key="action.key"
                        type="button"
                        class="di-action"
                        :class="`tone-${action.tone || 'default'}`"
                        @click.stop="onActionClick(task, action)"
                      >
                        <Icon v-if="action.icon" :icon="action.icon" width="13" height="13" />
                        {{ action.label }}
                      </button>
                    </div>
                  </div>
                  <button
                    v-if="!task.persistent"
                    type="button"
                    class="di-item__dismiss"
                    :aria-label="t('island.dismiss')"
                    @click.stop="dismissTask(task.id)"
                  >
                    <Icon icon="lucide:x" width="12" height="12" />
                  </button>
                </div>
              </template>

              <!-- 最近历史 -->
              <div v-if="history.length" class="di-history">
                <div class="di-history__head">
                  <span>{{ t('island.recent') }}</span>
                  <button type="button" class="di-history__clear" @click="clearHistory">
                    {{ t('island.clear') }}
                  </button>
                </div>
                <div
                  v-for="item in history"
                  :key="item.id + item.order"
                  class="di-history-item"
                  :class="`is-${item.state}`"
                >
                  <span class="di-indicator">
                    <Icon :icon="stateIcon(item.state)" width="13" height="13" />
                  </span>
                  <span class="di-history-item__label">{{ item.label }}</span>
                  <span class="di-history-item__time">{{ relativeTime(item.time) }}</span>
                </div>
              </div>

              <div v-if="!activeTasks.length && !history.length" class="di-empty">
                {{ t('island.empty') }}
              </div>
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
  left: 50%;
  z-index: 3000;
  transform: translateX(-50%);
  pointer-events: none;
  display: flex;
  justify-content: center;
}

/* ============ 单壳体：胶囊 ⇄ 面板形变容器 ============ */
/* 标志性"灵动岛"恒为深色，与明暗主题解耦 */
.di-shell {
  position: relative;
  pointer-events: auto;
  color: rgb(255 255 255 / 95%);
  background: rgb(18 18 20 / 94%);
  box-shadow:
    0 10px 30px rgb(0 0 0 / 30%),
    inset 0 0 0 1px rgb(255 255 255 / 8%);
  backdrop-filter: blur(14px);
  overflow: hidden;
  /* 宽/高/圆角统一缓动：展开收起为同一容器的形变，不再上下抖动 */
  transition:
    width 0.45s cubic-bezier(0.3, 1.15, 0.35, 1),
    height 0.45s cubic-bezier(0.3, 1.15, 0.35, 1),
    border-radius 0.45s cubic-bezier(0.3, 1.15, 0.35, 1);
}

/* 两层内容：淡入淡出交叉过渡，非活动层不可交互 */
.di-layer {
  position: absolute;
  top: 0;
  left: 0;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.22s ease;
}

.di-layer.is-active {
  opacity: 1;
  pointer-events: auto;
  transition: opacity 0.26s ease 0.1s;
}

/* ============ 折叠层：胶囊 ============ */
.di-pill {
  width: 100%;
  height: 34px;
  padding: 0;
  border: none;
  background: transparent;
  color: inherit;
  font-size: 13px;
  font-weight: 500;
  line-height: 1;
  white-space: nowrap;
  cursor: pointer;
}

/* 图标贴最左、操作贴最右：label 弹性占中，两端由内边距锚定 */
.di-pill__inner {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  height: 100%;
  padding: 0 12px 0 14px;
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
  gap: 6px;
  flex-shrink: 0;
  margin-left: auto;
}

.di-indicator {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 20px;
  height: 20px;
  color: rgb(255 255 255 / 85%);
}

.is-success .di-indicator,
.di-item.is-success .di-indicator {
  color: #34d399;
}

.is-error .di-indicator,
.di-item.is-error .di-indicator {
  color: #f87171;
}

.is-info .di-indicator,
.di-item.is-info .di-indicator {
  color: #60a5fa;
}

/* ============ 进度环 ============ */
.di-ring {
  width: 20px;
  height: 20px;
  transform: rotate(-90deg);
}

.di-ring__track {
  fill: none;
  stroke: rgb(255 255 255 / 16%);
  stroke-width: 2.5;
}

.di-ring__bar {
  fill: none;
  stroke: #60a5fa;
  stroke-width: 2.5;
  stroke-linecap: round;
  transition: stroke-dashoffset 0.4s cubic-bezier(0.25, 1, 0.35, 1);
}

/* 不确定态：固定弧长旋转 */
.di-ring--spin {
  animation: di-ring-rotate 1s linear infinite;
}

.di-ring--spin .di-ring__bar {
  transition: none;
}

@keyframes di-ring-rotate {
  to {
    transform: rotate(270deg);
  }
}

.di-ring__icon {
  position: absolute;
  inset: 0;
  margin: auto;
  color: rgb(255 255 255 / 80%);
}

.di-pct {
  font-size: 11px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: #93c5fd;
}

.di-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 9999px;
  background: rgb(255 255 255 / 16%);
  font-size: 10px;
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
  padding: 10px 12px 6px 16px;
}

.di-panel__title {
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.02em;
}

.di-panel__close {
  display: inline-flex;
  padding: 2px;
  border: 0;
  border-radius: 6px;
  background: transparent;
  color: rgb(255 255 255 / 60%);
  cursor: pointer;
}

.di-panel__close:hover {
  background: rgb(255 255 255 / 10%);
  color: rgb(255 255 255 / 95%);
}

.di-panel__body {
  max-height: min(60vh, 460px);
  padding: 4px 8px 10px;
  overflow-y: auto;
}

/* 活动项 */
.di-item {
  position: relative;
  display: flex;
  gap: 10px;
  padding: 8px 26px 8px 8px;
  border-radius: 10px;
}

.di-item.is-clickable {
  cursor: pointer;
}

.di-item.is-clickable:hover {
  background: rgb(255 255 255 / 6%);
}

.di-item .di-indicator {
  margin-top: 1px;
}

.di-item__body {
  min-width: 0;
  flex: 1;
}

.di-item__label {
  font-size: 13px;
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
}

.di-item__detail {
  margin-top: 2px;
  font-size: 12px;
  color: rgb(255 255 255 / 55%);
  line-height: 1.45;
}

.di-item__meta {
  margin-top: 4px;
  font-size: 11px;
  font-variant-numeric: tabular-nums;
  color: rgb(255 255 255 / 40%);
}

.di-item__dismiss {
  position: absolute;
  top: 8px;
  right: 8px;
  display: inline-flex;
  padding: 2px;
  border: 0;
  border-radius: 5px;
  background: transparent;
  color: rgb(255 255 255 / 35%);
  cursor: pointer;
  opacity: 0;
  transition: opacity 0.15s ease, color 0.15s ease;
}

.di-item:hover .di-item__dismiss {
  opacity: 1;
}

.di-item__dismiss:hover {
  color: rgb(255 255 255 / 85%);
  background: rgb(255 255 255 / 10%);
}

/* 进度条（面板内与环互补，长文案下更易读） */
.di-progress {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 7px;
}

.di-progress__track {
  position: relative;
  flex: 1;
  height: 5px;
  background: rgb(255 255 255 / 12%);
  border-radius: 9999px;
  overflow: hidden;
}

.di-progress__bar {
  position: absolute;
  left: 0;
  top: 0;
  height: 100%;
  background: linear-gradient(90deg, #60a5fa, #818cf8);
  border-radius: 9999px;
  transition: width 0.35s cubic-bezier(0.25, 1, 0.35, 1);
}

.di-progress__pct {
  font-size: 11px;
  font-variant-numeric: tabular-nums;
  color: rgb(255 255 255 / 70%);
}

/* 操作按钮 */
.di-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 9px;
}

.di-action {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  height: 26px;
  padding: 0 10px;
  border: 0;
  border-radius: 7px;
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  background: rgb(255 255 255 / 12%);
  color: rgb(255 255 255 / 92%);
  transition: background 0.15s ease;
}

.di-action:hover {
  background: rgb(255 255 255 / 20%);
}

.di-action.tone-primary {
  background: #3b82f6;
  color: #fff;
}

.di-action.tone-primary:hover {
  background: #2f74e8;
}

.di-action.tone-danger {
  background: rgb(248 113 113 / 22%);
  color: #fca5a5;
}

.di-action.tone-danger:hover {
  background: rgb(248 113 113 / 32%);
}

/* 历史 */
.di-history {
  margin-top: 4px;
  padding-top: 6px;
  border-top: 1px solid rgb(255 255 255 / 8%);
}

.di-history__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 4px 8px;
  font-size: 11px;
  color: rgb(255 255 255 / 45%);
}

.di-history__clear {
  border: 0;
  background: transparent;
  color: rgb(255 255 255 / 45%);
  font-size: 11px;
  cursor: pointer;
}

.di-history__clear:hover {
  color: rgb(255 255 255 / 80%);
}

.di-history-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 5px 8px;
  font-size: 12px;
  color: rgb(255 255 255 / 65%);
}

.di-history-item .di-indicator {
  width: auto;
  height: auto;
  opacity: 0.8;
}

.di-history-item__label {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.di-history-item__time {
  flex-shrink: 0;
  font-size: 11px;
  color: rgb(255 255 255 / 35%);
}

.di-empty {
  padding: 16px;
  text-align: center;
  font-size: 12px;
  color: rgb(255 255 255 / 45%);
}

/* ============ 壳体出现/消失过渡 ============ */
.island-enter-active {
  transition:
    opacity 0.28s ease,
    transform 0.42s cubic-bezier(0.22, 1.4, 0.36, 1);
}

.island-leave-active {
  transition:
    opacity 0.18s ease,
    transform 0.22s cubic-bezier(0.4, 0, 1, 1);
}

.island-enter-from,
.island-leave-to {
  opacity: 0;
  transform: translateY(-14px) scale(0.8);
}

.di-text-enter-active,
.di-text-leave-active {
  transition:
    opacity 0.18s ease,
    transform 0.18s ease;
}

.di-text-enter-from {
  opacity: 0;
  transform: translateY(6px);
}

.di-text-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>
