<script setup lang="ts">
import type { DragEndEvent } from '@dnd-kit/vue'
import { DragDropProvider } from '@dnd-kit/vue'
import { NEmpty, NPopover } from 'naive-ui'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { resolveSortMove } from '~/components/common/sortable'
import SortableItem from '~/components/common/SortableItem.vue'
import { Icon } from '~/iconify'
import { useFavoritesStore } from '~/stores'
import { registerFavoritesAnchor, useFavoritesPulse } from '../composables/use-favorites-fly'

defineOptions({ name: 'AppFavorites' })

const route = useRoute()
const router = useRouter()
const { t, te } = useI18n()
const favoritesStore = useFavoritesStore()

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
          <span v-if="favoritesStore.count > 0" class="fav-btn__badge">{{ favoritesStore.count }}</span>
        </button>
      </span>
    </template>

    <div class="flex flex-col gap-2">
      <!-- 头部 -->
      <div class="flex items-center justify-between">
        <span class="text-sm font-semibold text-foreground">收藏夹</span>
        <span class="text-xs text-foreground/40">可拖拽排序</span>
      </div>

      <!-- 空态 -->
      <NEmpty
        v-if="items.length === 0"
        size="small"
        description="暂无收藏，右键标签页选择「收藏」即可添加"
        class="py-3"
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

/* 收藏药丸 */
.fav-list {
  max-height: 320px;
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
