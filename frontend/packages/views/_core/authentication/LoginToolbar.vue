<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhButton, XhPopoverContent, XhPopoverPositioner, XhPopoverRoot, XhPopoverTrigger } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import LayoutAlignSwitcher from '~/components/common/LayoutAlignSwitcher.vue'
import LocaleSwitcher from '~/components/common/LocaleSwitcher.vue'
import { DEFAULT_THEME_COLOR, THEME_COLOR_PRESETS } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppStore } from '~/stores'

export type LoginFormAlign = 'left' | 'center' | 'right'

defineOptions({ name: 'LoginToolbar' })

const emit = defineEmits<{
  layoutChange: [align: LoginFormAlign]
}>()

const appStore = useAppStore()
const { isDark, toggleThemeWithTransition, setThemeColor } = useTheme()
const { t } = useI18n()

const colorPresets = computed(() => {
  const presets = [
    {
      color: DEFAULT_THEME_COLOR,
      label: t('preference.appearance.color.default'),
    },
  ]
  for (const item of THEME_COLOR_PRESETS) {
    presets.push({
      color: item.color,
      label: t(item.nameKey),
    })
  }
  return presets
})
const showColorPicker = ref(false)

// ---- 布局 ----
const currentAlign = ref<LoginFormAlign>('right')

function getLayoutIcon(align: LoginFormAlign) {
  if (align === 'left')
    return 'lucide:panel-left'
  if (align === 'center')
    return 'lucide:layout-panel-top'
  return 'lucide:panel-right'
}

function onAlignChange(align: LoginFormAlign) {
  currentAlign.value = align
  emit('layoutChange', align)
}
</script>

<template>
  <div class="login-toolbar app-glass">
    <!-- 颜色 -->
    <XhPopoverRoot v-model:open="showColorPicker" placement="bottom-end">
      <!-- 触发器本身就是那颗按钮：浮层触发器渲染成 button，不能再往里套一颗 -->
      <XhPopoverTrigger class="toolbar-btn xh-icon-btn" :aria-label="t('preference.appearance.color.title')">
        <Icon width="16" height="16" icon="lucide:palette" />
      </XhPopoverTrigger>
      <XhPopoverPositioner>
        <XhPopoverContent :aria-label="t('preference.appearance.color.title')">
          <div class="color-grid">
            <button
              v-for="preset in colorPresets"
              :key="preset.color"
              type="button"
              class="color-dot"
              :class="{ 'is-active': appStore.themeColor === preset.color }"
              :style="{ backgroundColor: preset.color }"
              :title="preset.label"
              :aria-label="preset.label"
              :aria-pressed="appStore.themeColor === preset.color"
              @click="
                () => {
                  setThemeColor(preset.color)
                  showColorPicker = false
                }
              "
            />
          </div>
        </XhPopoverContent>
      </XhPopoverPositioner>
    </XhPopoverRoot>

    <!-- 布局位置（封装组件，当前项高亮） -->
    <LayoutAlignSwitcher :value="currentAlign" placement="bottom-end" @change="onAlignChange">
      <XhButton variant="ghost" size="sm" icon-only class="toolbar-btn xh-icon-btn" :aria-label="t('preference.layout.title')">
        <Icon width="16" height="16" :icon="getLayoutIcon(currentAlign)" />
      </XhButton>
    </LayoutAlignSwitcher>

    <!-- 语言 -->
    <LocaleSwitcher
      v-if="appStore.widgetLanguagePlacement !== 'hidden'"
      variant="dropdown"
      apply
      placement="bottom-end"
    >
      <XhButton variant="ghost" size="sm" icon-only class="toolbar-btn xh-icon-btn" :aria-label="t('header.toolbar.switch_language')">
        <Icon width="16" height="16" icon="lucide:languages" />
      </XhButton>
    </LocaleSwitcher>

    <!-- 主题 -->
    <XhButton
      v-if="appStore.widgetThemePlacement !== 'hidden'"
      variant="ghost"
      size="sm"
      icon-only
      class="toolbar-btn"
      :aria-label="isDark ? t('header.toolbar.theme_to_light') : t('header.toolbar.theme_to_dark')"
      @click="(e: MouseEvent) => toggleThemeWithTransition(e)"
    >
      <Icon width="16" height="16" :icon="isDark ? 'lucide:sun' : 'lucide:moon'" />
    </XhButton>
  </div>
</template>

<style scoped>
.login-toolbar {
  position: absolute;
  top: 16px;
  inset-inline-end: 16px;
  z-index: 10;
  display: flex;
  align-items: center;
  gap: var(--xh-space-0_5);
  padding: var(--xh-space-1) var(--xh-space-2);
  border-radius: var(--xh-shape-pill);
}

.toolbar-btn {
  color: var(--xh-fg-default);
}

.toolbar-btn:hover {
  background: var(--xh-bg-subtle-hover);
}

/* 颜色色板 */
/* 默认色加七个预设，排成两行四个 */
.color-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--xh-space-1_5);
  padding: var(--xh-space-1);
}

.color-dot {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 2px solid transparent;
  cursor: pointer;
  transition:
    transform var(--xh-motion-duration-nudge) var(--xh-motion-ease-enter),
    box-shadow var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
  flex-shrink: 0;
}

.color-dot:hover {
  transform: scale(1.18);
  box-shadow: 0 2px 6px hsl(0 0% 0% / 0.25);
}

.color-dot.is-active {
  border-color: var(--xh-bg-surface);
  box-shadow:
    0 0 0 2px currentColor,
    0 2px 6px hsl(0 0% 0% / 0.2);
}
</style>
