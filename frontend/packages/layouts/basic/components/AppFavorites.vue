<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhButton, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhPopoverContent, XhPopoverPositioner, XhPopoverRoot, XhPopoverTitle, XhPopoverTrigger, XhSeparator, XhSortableItem, XhSortableLiveRegion, XhSortableRoot } from '@xihan-ui/vue'
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import SyncStatusBadge from '~/components/common/SyncStatusBadge.vue'
import { Icon } from '~/iconify'
import { useAppStore, useFavoritesStore } from '~/stores'
import { registerFavoritesAnchor, useFavoritesPulse } from '../composables/use-favorites-fly'
import AppIconButton from './AppIconButton.vue'

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
  return te(title)
    ? t(title)
    : title
}

function resolveIcon(icon?: string): string {
  if (!icon) {
    return 'lucide:bookmark'
  }
  return icon.includes(':')
    ? icon
    : `lucide:${icon}`
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

function onSort(details: { from: number, to: number }): void {
  favoritesStore.move(details.from, details.to)
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
  <XhPopoverRoot v-model:open="showPanel" placement="bottom-start">
    <!-- 触发器合到顶栏图标钮上（as-child），不往触发器渲染出的 button 里再套一颗
         anchorRef 仍留在外层 span 上——它是灵动岛的落位参照，不能跟着浮层开合动 -->
    <span ref="anchorRef" class="mr-1 inline-flex">
      <XhPopoverTrigger as-child>
        <AppIconButton
          :active="showPanel"
          :class="{ 'fav-btn--pulse': pulsing }"
          :tooltip="t('header.favorites.title')"
        >
          <!-- 数字、99+、「零则收起」与贴角定位都归组件库算
             开着收藏夹同步时等后端那份回来再出数：本地那份可能是别的设备改动之前的旧值 -->
          <XhBadge
            size="sm"
            tone="brand"
            :count="favoritesStore.ready ? favoritesStore.count : 0"
            :label="t('header.favorites.count_label', { n: favoritesStore.count })"
          >
            <Icon icon="lucide:star" width="16" height="16" />
          </XhBadge>
        </AppIconButton>
      </XhPopoverTrigger>
    </span>

    <XhPopoverPositioner>
      <XhPopoverContent class="fav-popover">
        <div class="fav-panel flex flex-col gap-2">
          <!-- 头部（与表格设置/搜索设置统一样式） -->
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <!-- 标题部件给浮层起名（content 的 aria-labelledby 指向它） -->
              <XhPopoverTitle class="text-base font-semibold text-foreground">
                {{ t('header.favorites.title') }}
              </XhPopoverTitle>
              <SyncStatusBadge :synced="appStore.favoritesSyncEnabled" />
            </div>
          </div>

          <XhSeparator class="my-1" />

          <!-- 空态（收藏未就绪时空态与列表都不摆） -->
          <XhEmptyStateRoot v-if="favoritesStore.ready && items.length === 0" class="fav-empty" size="sm">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('header.favorites.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>

          <!-- 收藏药丸（可拖拽排序，点击导航，× 移除） -->
          <XhSortableRoot
            v-else-if="favoritesStore.ready"
            :ids="items.map(i => i.path)"
            orientation="both"
            class="fav-list flex flex-wrap gap-2"
            style="--xh-sortable-gap: 0"
            @sort="onSort"
          >
            <XhSortableItem
              v-for="item in items"
              :key="item.path"
              :item-id="item.path"
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
              <XhButton
                variant="ghost"
                size="sm"
                icon-only
                class="x-icon-btn fav-chip__close"
                :aria-label="t('header.favorites.remove')"
                @click="(e: MouseEvent) => handleRemove(item.path, e)"
                @keydown.enter.stop
              >
                <Icon icon="lucide:x" />
              </XhButton>
            </XhSortableItem>
            <XhSortableLiveRegion />
          </XhSortableRoot>

          <div v-if="items.length > 0" class="fav-footer">
            <XhSeparator class="my-1" />
            <span class="text-xs text-foreground/40">{{ t('header.favorites.hint') }}</span>
          </div>
        </div>
      </XhPopoverContent>
    </XhPopoverPositioner>
  </XhPopoverRoot>
</template>

<style scoped>
/* 收藏面板定宽，与表格设置/搜索设置浮层一致 */
.fav-popover {
  inline-size: 340px;
}

/* 飞入命中脉冲 */
.fav-btn--pulse {
  animation: fav-pulse var(--xh-motion-duration-slide) var(--xh-motion-ease-enter-strong);
}

@keyframes fav-pulse {
  0% {
    transform: scale(1);
  }

  35% {
    transform: scale(1.32);
    color: var(--xh-fg-brand);
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
  padding: var(--xh-space-6) 0;
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
  gap: var(--xh-space-1_5);
  max-width: 100%;
  padding: var(--xh-space-1) var(--xh-space-1_5) var(--xh-space-1) var(--xh-space-2_5);
  border-radius: var(--xh-shape-pill);
  background: var(--xh-bg-muted);
  color: color-mix(in srgb, var(--xh-fg-default) 85%, transparent);
  font-size: var(--xh-font-size-sm);
  line-height: 20px;
  cursor: pointer;
  user-select: none;
  transition:
    background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    box-shadow var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.fav-chip:hover {
  background: color-mix(in srgb, var(--xh-bg-brand) 14%, var(--xh-bg-muted));
  color: var(--xh-fg-default);
}

.fav-chip--current {
  background: color-mix(in srgb, var(--xh-bg-brand) 18%, var(--xh-bg-surface));
  color: var(--xh-fg-brand);
  box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--xh-fg-brand) 30%, transparent);
}

.fav-chip__label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 140px;
}

/* 拖拽中的药丸（sortable 在被拖那一项上写 data-dragging） */
.fav-chip[data-dragging] {
  opacity: 0.5;
  box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--xh-fg-brand) 40%, transparent);
  cursor: grabbing;
}

/* 标签上的移除钮：组件库纯图标钮压成 sm 字形档的小圆，前景随标签字色 */
.fav-chip__close {
  --xh-button-h: var(--xh-glyph-size-sm);
  --xh-button-icon-size: var(--xh-glyph-size-xs);
  --xh-button-fg: currentcolor;
}
</style>
