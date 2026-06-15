<script setup lang="ts">
import type { DragEndEvent } from '@dnd-kit/vue'
import { DragDropProvider } from '@dnd-kit/vue'
import { NDivider, NEmpty, NNumberAnimation, NPopover } from 'naive-ui'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { resolveSortMove } from '~/components/common/sortable'
import SortableItem from '~/components/common/SortableItem.vue'
import SyncStatusBadge from '~/components/common/SyncStatusBadge.vue'
import { Icon } from '~/iconify'
import { useAppStore, useFavoritesStore } from '~/stores'
import { registerFavoritesAnchor, useFavoritesPulse } from '../composables/use-favorites-fly'

defineOptions({ name: 'AppFavorites' })

const route = useRoute()
const router = useRouter()
const { t, te } = useI18n()
const favoritesStore = useFavoritesStore()
const appStore = useAppStore()

const showPanel = ref(false)
const anchorRef = ref<HTMLElement | null>(null)
const pulsing = ref(false)

/** 标题可能是 i18n key（如 'menu.workspace'），渲染时翻译；非 key 原样展示 */
function display(title: string): string {
  return te(title) ? t(title) : title
}

function resolveIcon(icon?: string): string {
  if (!icon) {
    return 'lucide:bookmark'
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
}

const items = computed(() => favoritesStore.favorites)

function handleNavigate(path: string): void {
  showPanel.value = false
  if (route.fullPath !== path) {
    void router.push(path)
  }
}

function handleRemove(path: string, e: MouseEvent): void {
  e.stopPropagation()
  favoritesStore.remove(path)
}

function onDragEnd(event: DragEndEvent): void {
  const move = resolveSortMove(event, items.value.map(item => item.path))
  if (move) {
    favoritesStore.move(move.from, move.to)
  }
}

// ── 按钮脉冲（飞入命中时抖动一下） ──────────────────────────────
const pulseTick = useFavoritesPulse()
let pulseTimer: ReturnType<typeof setTimeout> | null = null
watch(pulseTick, () => {
  pulsing.value = false
  // 强制下一帧重置，确保连续命中也能重放动画
  requestAnimationFrame(() => {
    pulsing.value = true
    if (pulseTimer) {
      clearTimeout(pulseTimer)
    }
    pulseTimer = setTimeout(() => {
      pulsing.value = false
    }, 600)
  })
})

onMounted(() => {
  registerFavoritesAnchor(anchorRef.value)
  // 跨端同步：拉取后端收藏夹覆盖本地（尽力而为）
  void favoritesStore.hydrate()
})

onBeforeUnmount(() => {
  registerFavoritesAnchor(null)
  if (pulseTimer) {
    clearTimeout(pulseTimer)
  }
})
</script>

<template>
  <NPopover
    v-model:show="showPanel"
    trigger="click"
    placement="bottom-start"
    :width="320"
    display-directive="show"
    :show-arrow="false"
  >
    <template #trigger>
      <span ref="anchorRef" class="mr-1 inline-flex">
        <button
          type="button"
          class="fav-btn"
          :class="{ 'fav-btn--active': showPanel, 'fav-btn--pulse': pulsing }"
          title="收藏夹"
          aria-label="收藏夹"
        >
          <Icon icon="lucide:star" width="18" height="18" />
          <span v-if="favoritesStore.count > 0" class="fav-btn__badge">
            <NNumberAnimation :to="Math.min(favoritesStore.count, 99)" :duration="500" :precision="0" />
            <span v-if="favoritesStore.count > 99">+</span>
          </span>
        </button>
      </span>
    </template>

    <div class="fav-panel flex flex-col gap-2">
      <!-- 头部（与表格设置/搜索设置统一样式） -->
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="text-base font-semibold text-foreground">收藏夹</span>
          <SyncStatusBadge :synced="appStore.favoritesSyncEnabled" />
        </div>
      </div>

      <NDivider class="!my-1" />

      <!-- 空态 -->
      <NEmpty
        v-if="items.length === 0"
        size="small"
        description="暂无收藏，右键标签页选择「收藏」即可添加"
        class="fav-empty"
      />

      <!-- 收藏药丸（可拖拽排序，点击导航，× 移除） -->
      <DragDropProvider v-else @drag-end="onDragEnd">
        <div class="fav-list flex flex-wrap gap-2">
          <SortableItem
            v-for="(item, index) in items"
            :id="item.path"
            :key="item.path"
            :index="index"
            class="fav-chip"
            :class="{ 'fav-chip--current': route.fullPath === item.path }"
            role="button"
            tabindex="0"
            :title="display(item.title)"
            @click="handleNavigate(item.path)"
            @keydown.enter.prevent="handleNavigate(item.path)"
          >
            <Icon :icon="resolveIcon(item.icon)" width="14" height="14" class="shrink-0 opacity-70" />
            <span class="fav-chip__label">{{ display(item.title) }}</span>
            <button
              type="button"
              class="fav-chip__close"
              aria-label="移除收藏"
              @click="(e) => handleRemove(item.path, e)"
              @keydown.enter.stop
            >
              <Icon icon="lucide:x" width="12" height="12" />
            </button>
          </SortableItem>
        </div>
      </DragDropProvider>

      <div v-if="items.length > 0" class="fav-footer">
        <NDivider class="!my-1" />
        <span class="text-xs text-foreground/40">点击导航到对应页面；拖拽可排序；× 移除收藏</span>
      </div>
    </div>
  </NPopover>
</template>

<style scoped>
/* 收藏夹触发按钮（与 XihanIconButton 视觉一致） */
.fav-btn {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--foreground) / 65%);
  cursor: pointer;
  outline: none;
  flex-shrink: 0;
  transition:
    background 0.15s ease,
    color 0.15s ease;
}

.fav-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.fav-btn--active {
  background: hsl(var(--accent));
  color: hsl(var(--primary));
}

.fav-btn__badge {
  position: absolute;
  top: -1px;
  right: -1px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 14px;
  height: 14px;
  padding: 0 3px;
  border-radius: 9999px;
  background: hsl(var(--primary));
  color: hsl(var(--primary-foreground));
  font-size: 9px;
  font-weight: 600;
  line-height: 14px;
  text-align: center;
}

/* 飞入命中脉冲 */
.fav-btn--pulse {
  animation: fav-pulse 0.6s cubic-bezier(0.22, 1, 0.36, 1);
}

@keyframes fav-pulse {
  0% {
    transform: scale(1);
  }

  35% {
    transform: scale(1.32);
    color: hsl(var(--primary));
  }

  60% {
    transform: scale(0.92);
  }

  100% {
    transform: scale(1);
  }
}

/* 面板内容区：保底高度，避免空态/少量收藏时面板过矮 */
.fav-panel {
  min-height: 200px;
}

/* 空态居中填满保底高度 */
.fav-empty {
  display: flex;
  flex: 1;
  flex-direction: column;
  justify-content: center;
  padding: 24px 0;
}

/* 页脚（分割线 + 提示语）：钉在弹层底部 */
.fav-footer {
  margin-top: auto;
}

/* 收藏药丸 */
.fav-list {
  max-height: 480px;
  overflow-y: auto;
}

.fav-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  max-width: 100%;
  padding: 4px 6px 4px 10px;
  border-radius: 9999px;
  background: hsl(var(--muted));
  color: hsl(var(--foreground) / 85%);
  font-size: 13px;
  line-height: 20px;
  cursor: pointer;
  user-select: none;
  transition:
    background 0.15s ease,
    color 0.15s ease,
    box-shadow 0.15s ease;
}

.fav-chip:hover {
  background: color-mix(in srgb, hsl(var(--primary)) 14%, hsl(var(--muted)));
  color: hsl(var(--foreground));
}

.fav-chip--current {
  background: color-mix(in srgb, hsl(var(--primary)) 18%, hsl(var(--background)));
  color: hsl(var(--primary));
  box-shadow: inset 0 0 0 1px hsl(var(--primary) / 30%);
}

.fav-chip__label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 140px;
}

.fav-chip__close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  border: 0;
  padding: 0;
  border-radius: 9999px;
  background: transparent;
  color: currentcolor;
  opacity: 0.55;
  cursor: pointer;
  transition:
    background 0.15s ease,
    opacity 0.15s ease;
}

.fav-chip__close:hover {
  opacity: 1;
  background: hsl(var(--primary) / 20%);
}

/* 拖拽中的药丸（dnd-kit 在 SortableItem 根节点写入 data-dragging） */
.fav-chip[data-dragging] {
  opacity: 0.5;
  box-shadow: inset 0 0 0 1px hsl(var(--primary) / 40%);
  cursor: grabbing;
}
</style>
