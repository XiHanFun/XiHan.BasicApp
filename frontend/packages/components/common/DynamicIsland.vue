<script lang="ts" setup>
import type { IslandAction, IslandState, IslandTask } from '~/composables/useDynamicIsland'
import { useOnline } from '@vueuse/core'
import { useMessage } from 'naive-ui'
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { configureDynamicIsland, islandStatus, useDynamicIsland } from '~/composables/useDynamicIsland'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'

defineOptions({ name: 'DynamicIsland' })

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

function onTaskClick(task: IslandTask): void {
  if (task.onClick) {
    task.onClick()
    collapse()
  }
}

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
    netHandle = islandStatus('sys:network', '网络已断开，等待重连…', {
      state: 'error',
      icon: 'lucide:wifi-off',
      detail: '当前更改可能无法同步，网络恢复后会自动重试。',
    })
  }
  else if (was === false) {
    netHandle?.success('网络已恢复')
    netHandle = null
  }
}, { immediate: true })

onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocPointer, true)
  document.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <Teleport to="body">
    <div ref="rootRef" class="di-root">
      <!-- 折叠态：胶囊 -->
      <Transition name="island">
        <button
          v-if="enabled && current && !expanded"
          type="button"
          class="di-pill"
          :class="`is-${current.state}`"
          :aria-label="current.label"
          @click="expand"
        >
          <span
            class="di-indicator"
            :class="{ 'di-spin': current.state === 'loading' && current.progress == null }"
          >
            <Icon :icon="current.icon || stateIcon(current.state)" width="15" height="15" />
          </span>
          <Transition name="di-text" mode="out-in">
            <span :key="`${current.state}:${current.label}`" class="di-label">{{ current.label }}</span>
          </Transition>
          <span v-if="loadingCount > 1" class="di-count">{{ loadingCount }}</span>
          <Icon v-if="hasPanel" icon="lucide:chevron-down" width="13" height="13" class="di-chevron" />
          <span
            v-if="current.progress != null"
            class="di-pill-progress"
            :style="{ width: `${current.progress}%` }"
          />
        </button>
      </Transition>

      <!-- 展开态：活动面板 -->
      <Transition name="panel">
        <div v-if="enabled && expanded" class="di-panel">
          <div class="di-panel__head">
            <span class="di-panel__title">动态</span>
            <button type="button" class="di-panel__close" aria-label="收起" @click="collapse">
              <Icon icon="lucide:chevron-up" width="16" height="16" />
            </button>
          </div>

          <div class="di-panel__body">
            <!-- 活动中 -->
            <template v-if="activeTasks.length">
              <div
                v-for="t in activeTasks"
                :key="t.id"
                class="di-item"
                :class="[`is-${t.state}`, { 'is-clickable': !!t.onClick }]"
                @click="onTaskClick(t)"
              >
                <span
                  class="di-indicator"
                  :class="{ 'di-spin': t.state === 'loading' && t.progress == null }"
                >
                  <Icon :icon="t.icon || stateIcon(t.state)" width="15" height="15" />
                </span>
                <div class="di-item__body">
                  <div class="di-item__label">
                    {{ t.label }}
                  </div>
                  <div v-if="t.detail" class="di-item__detail">
                    {{ t.detail }}
                  </div>
                  <div v-if="t.progress != null" class="di-progress">
                    <span class="di-progress__track">
                      <span class="di-progress__bar" :style="{ width: `${t.progress}%` }" />
                    </span>
                    <span class="di-progress__pct">{{ Math.round(t.progress) }}%</span>
                  </div>
                  <div v-if="t.actions?.length" class="di-actions">
                    <button
                      v-for="a in t.actions"
                      :key="a.key"
                      type="button"
                      class="di-action"
                      :class="`tone-${a.tone || 'default'}`"
                      @click.stop="onActionClick(t, a)"
                    >
                      <Icon v-if="a.icon" :icon="a.icon" width="13" height="13" />
                      {{ a.label }}
                    </button>
                  </div>
                </div>
              </div>
            </template>

            <!-- 最近历史 -->
            <div v-if="history.length" class="di-history">
              <div class="di-history__head">
                <span>最近</span>
                <button type="button" class="di-history__clear" @click="clearHistory">
                  清空
                </button>
              </div>
              <div
                v-for="h in history"
                :key="h.id"
                class="di-history-item"
                :class="`is-${h.state}`"
              >
                <span class="di-indicator">
                  <Icon :icon="stateIcon(h.state)" width="13" height="13" />
                </span>
                <span class="di-history-item__label">{{ h.label }}</span>
              </div>
            </div>

            <div v-if="!activeTasks.length && !history.length" class="di-empty">
              暂无动态
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
}

/* 标志性"灵动岛"恒为深色，与明暗主题解耦 */
.di-pill,
.di-panel {
  pointer-events: auto;
  color: rgb(255 255 255 / 95%);
  background: rgb(18 18 20 / 94%);
  box-shadow:
    0 10px 30px rgb(0 0 0 / 30%),
    inset 0 0 0 1px rgb(255 255 255 / 8%);
  backdrop-filter: blur(14px);
}

/* ============ 折叠态胶囊 ============ */
.di-pill {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  max-width: min(80vw, 440px);
  height: 34px;
  padding: 0 12px 0 14px;
  border: none;
  border-radius: 9999px;
  font-size: 13px;
  font-weight: 500;
  line-height: 1;
  white-space: nowrap;
  cursor: pointer;
  overflow: hidden;
}

.di-indicator {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: rgb(255 255 255 / 85%);
}

.is-success .di-indicator {
  color: #34d399;
}

.is-error .di-indicator {
  color: #f87171;
}

.is-info .di-indicator {
  color: #60a5fa;
}

.di-spin {
  animation: di-spin 0.8s linear infinite;
}

@keyframes di-spin {
  to {
    transform: rotate(360deg);
  }
}

.di-label {
  overflow: hidden;
  text-overflow: ellipsis;
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
  flex-shrink: 0;
  opacity: 0.5;
}

/* 折叠态底部进度条 */
.di-pill-progress {
  position: absolute;
  left: 0;
  bottom: 0;
  height: 2px;
  background: #60a5fa;
  border-radius: 9999px;
  transition: width 0.25s ease;
}

/* ============ 展开态面板 ============ */
.di-panel {
  width: min(86vw, 340px);
  border-radius: 16px;
  transform-origin: top center;
  overflow: hidden;
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
  display: flex;
  gap: 10px;
  padding: 8px;
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

/* 进度条 */
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
  transition: width 0.25s ease;
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
  opacity: 0.8;
}

.di-history-item__label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.di-empty {
  padding: 16px;
  text-align: center;
  font-size: 12px;
  color: rgb(255 255 255 / 45%);
}

/* ============ 过渡 ============ */
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

.panel-enter-active {
  transition:
    opacity 0.26s ease,
    transform 0.36s cubic-bezier(0.22, 1.2, 0.36, 1);
}

.panel-leave-active {
  transition:
    opacity 0.18s ease,
    transform 0.2s cubic-bezier(0.4, 0, 1, 1);
}

.panel-enter-from,
.panel-leave-to {
  opacity: 0;
  transform: translateY(-12px) scale(0.92);
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
