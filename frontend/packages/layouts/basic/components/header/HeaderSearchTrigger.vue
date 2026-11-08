<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhKbd } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { GLOBAL_HOTKEYS } from '~/composables/useGlobalShortcuts'
import { Icon } from '~/iconify'
import { useAppStore, useLayoutBridgeStore } from '~/stores'

/**
 * 顶栏的命令面板入口：只是一颗按钮，面板本身由布局层的 AppGlobalSearch 常驻承载，
 * 入口放到悬浮组或隐藏时，快捷键照样能打开面板
 */
defineOptions({ name: 'HeaderSearchTrigger' })

const props = withDefaults(defineProps<{
  /** 收成图标钮：顶栏空间要先让给横向菜单时由外部打开，不再随断点自动展开 */
  compact?: boolean
}>(), { compact: false })

const { t } = useI18n()
const appStore = useAppStore()
const layoutBridgeStore = useLayoutBridgeStore()

// 仅在快捷键启用时展示触发按钮上的 ⌘K/Ctrl+K 徽标
const showShortcut = computed(() => appStore.shortcutEnable && appStore.shortcutSearch)
</script>

<template>
  <div>
    <!-- 宽屏铺开完整的命令面板入口；收紧时（compact）只留图标钮，把宽度让给横向菜单 -->
    <div v-if="!props.compact" class="hidden sm:block">
      <button type="button" class="search-trigger" @click="layoutBridgeStore.requestOpenGlobalSearch()">
        <span class="shrink-0 text-[hsl(var(--muted-foreground))]" style="display: inline-flex; font-size: var(--xh-glyph-size-text)">
          <Icon icon="lucide:search" />
        </span>
        <span class="search-trigger-text">{{ t('header.search.placeholder') }}</span>
        <XhKbd v-if="showShortcut" class="search-kbd" :keys="[...GLOBAL_HOTKEYS.search]" />
      </button>
    </div>
    <div :class="props.compact ? undefined : 'sm:hidden'">
      <button type="button" class="search-trigger-icon" :aria-label="t('header.search.placeholder')" @click="layoutBridgeStore.requestOpenGlobalSearch()">
        <Icon width="16" height="16" icon="lucide:search" />
      </button>
    </div>
  </div>
</template>

<style scoped>
.search-trigger {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1_5);
  height: 32px;
  padding: 0 var(--xh-space-2_5);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-pill);
  background: color-mix(in srgb, var(--xh-bg-muted) 40%, transparent);
  cursor: pointer;
  transition:
    background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    border-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
  outline: none;
}

.search-trigger:hover {
  background: color-mix(in srgb, var(--xh-bg-muted) 80%, transparent);
}

.search-trigger-text {
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-muted);
  white-space: nowrap;
  user-select: none;
}

/* 键帽画在触发按钮里，点它等于点按钮 */
.search-kbd {
  pointer-events: none;
}

.search-trigger-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 50%;
  background: transparent;
  cursor: pointer;
  color: var(--xh-fg-default);
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
  outline: none;
}

.search-trigger-icon:hover {
  background: var(--xh-bg-subtle-hover);
}
</style>
