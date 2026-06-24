<script setup lang="ts">
import { NButton, NIcon, NPopover } from 'naive-ui'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import LayoutAlignSwitcher from '~/components/common/LayoutAlignSwitcher.vue'
import LocaleSwitcher from '~/components/common/LocaleSwitcher.vue'
import { DEFAULT_THEME_COLOR, THEME_COLOR_GROUPS } from '~/constants'
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
  const presets = [{ color: DEFAULT_THEME_COLOR, label: t('preference.appearance.color.default') }]
  for (const group of THEME_COLOR_GROUPS) {
    for (const item of group.items) {
      presets.push({ color: item.color, label: t(item.nameKey) })
    }
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
  <div class="login-toolbar">
    <!-- 颜色 -->
    <NPopover
      v-model:show="showColorPicker"
      trigger="click"
      placement="bottom-end"
      :show-arrow="false"
    >
      <template #trigger>
        <NButton quaternary circle size="small" class="toolbar-btn">
          <template #icon>
            <NIcon size="16">
              <Icon icon="lucide:palette" />
            </NIcon>
          </template>
        </NButton>
      </template>
      <div class="color-grid">
        <button
          v-for="preset in colorPresets"
          :key="preset.color"
          type="button"
          class="color-dot"
          :class="{ 'is-active': appStore.themeColor === preset.color }"
          :style="{ backgroundColor: preset.color }"
          :title="preset.label"
          @click="
            () => {
              setThemeColor(preset.color)
              showColorPicker = false
            }
          "
        />
      </div>
    </NPopover>

    <!-- 布局位置（封装组件，当前项高亮） -->
    <LayoutAlignSwitcher :value="currentAlign" placement="bottom-end" @change="onAlignChange">
      <NButton quaternary circle size="small" class="toolbar-btn">
        <template #icon>
          <NIcon size="16">
            <Icon :icon="getLayoutIcon(currentAlign)" />
          </NIcon>
        </template>
      </NButton>
    </LayoutAlignSwitcher>

    <!-- 语言 -->
    <LocaleSwitcher
      v-if="appStore.widgetLanguageToggle"
      variant="dropdown"
      apply
      placement="bottom-end"
    >
      <NButton quaternary circle size="small" class="toolbar-btn">
        <template #icon>
          <NIcon size="16">
            <Icon icon="lucide:languages" />
          </NIcon>
        </template>
      </NButton>
    </LocaleSwitcher>

    <!-- 主题 -->
    <NButton
      v-if="appStore.widgetThemeToggle"
      quaternary
      circle
      size="small"
      class="toolbar-btn"
      @click="(e: MouseEvent) => toggleThemeWithTransition(e)"
    >
      <template #icon>
        <NIcon size="16">
          <Icon :icon="isDark ? 'lucide:sun' : 'lucide:moon'" />
        </NIcon>
      </template>
    </NButton>
  </div>
</template>

<style scoped>
.login-toolbar {
  position: absolute;
  top: 16px;
  right: 16px;
  z-index: 10;
  display: flex;
  align-items: center;
  gap: 2px;
  padding: 4px 8px;
  border-radius: 999px;
  background: hsl(var(--background) / 0.65);
  backdrop-filter: blur(8px);
  -webkit-backdrop-filter: blur(8px);
  border: 1px solid hsl(var(--border) / 0.5);
}

.toolbar-btn {
  color: hsl(var(--foreground)) !important;
}

.toolbar-btn:hover {
  background: hsl(var(--accent)) !important;
}

/* 颜色色板 */
.color-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 6px;
  padding: 4px;
}

.color-dot {
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 2px solid transparent;
  cursor: pointer;
  transition:
    transform 0.15s ease,
    box-shadow 0.15s ease;
  flex-shrink: 0;
}

.color-dot:hover {
  transform: scale(1.18);
  box-shadow: 0 2px 6px hsl(0 0% 0% / 0.25);
}

.color-dot.is-active {
  border-color: hsl(var(--background));
  box-shadow:
    0 0 0 2px currentColor,
    0 2px 6px hsl(0 0% 0% / 0.2);
}
</style>
