<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NCard, NColorPicker, NIcon, NInputNumber, NRadioGroup, NSwitch } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceAppearanceTab' })

const props = defineProps<PreferenceAppearanceTabProps>()
const emit = defineEmits<{
  themeModeChange: [value: 'light' | 'dark' | 'auto']
}>()

const appStore = props.appStore
const { t } = useI18n()

interface ThemePresetItem {
  color: string
  name: string
}

interface PreferenceAppearanceTabProps {
  appStore: ReturnType<typeof useAppStore>
  themeMode: string
}

const themeModes = [
  { value: 'light', labelKey: 'preference.appearance.mode.light', icon: 'lucide:sun' },
  { value: 'dark', labelKey: 'preference.appearance.mode.dark', icon: 'lucide:moon' },
  { value: 'auto', labelKey: 'preference.appearance.mode.auto', icon: 'lucide:monitor' },
] as const

const themePresetItems: ThemePresetItem[] = [
  { color: '#feba07', name: '琥珀黄' },
  { color: '#5bae23', name: '鹦鹉绿' },
  { color: '#20a162', name: '孔雀绿' },
  { color: '#2c9678', name: '青矾绿' },
  { color: '#12aa9c', name: '美蝶绿' },
  { color: '#0eb0c9', name: '孔雀蓝' },
  { color: '#1772b4', name: '景泰蓝' },
  { color: '#61649f', name: '山梗紫' },
  { color: '#813c85', name: '桔梗紫' },
  { color: '#8b2671', name: '青莲' },
  { color: '#d2357d', name: '玫瑰红' },
  { color: '#f04a3a', name: '珊瑚红' },
  { color: '#c21f30', name: '枫叶红' },
  { color: '#f86b1d', name: '燕颔红' },
  { color: '#142334', name: '燕颔蓝' },
]

const localizedModes = computed(() =>
  themeModes.map(m => ({ ...m, label: t(m.labelKey) })),
)
</script>

<template>
  <div class="space-y-4">
    <!-- 模式 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.mode.title') }}
      </div>
      <NRadioGroup
        :value="props.themeMode"
        class="w-full"
        @update:value="(value) => emit('themeModeChange', value as 'light' | 'dark' | 'auto')"
      >
        <div class="grid grid-cols-3 gap-2">
          <label
            v-for="mode in localizedModes"
            :key="mode.value"
            class="theme-mode-card"
            :class="{ 'is-active': props.themeMode === mode.value }"
          >
            <input
              type="radio"
              :value="mode.value"
              class="sr-only"
              :checked="props.themeMode === mode.value"
              @change="emit('themeModeChange', mode.value)"
            >
            <NIcon size="20" class="mb-1"><Icon :icon="mode.icon" /></NIcon>
            <span class="text-xs">{{ mode.label }}</span>
          </label>
        </div>
      </NRadioGroup>
    </NCard>

    <!-- 颜色 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.color.title') }}
      </div>
      <div class="grid grid-cols-3 gap-x-2 gap-y-3">
        <!-- 预设颜色 -->
        <div v-for="preset in themePresetItems" :key="preset.color" class="color-item">
          <button
            type="button"
            class="theme-color-card"
            :class="{ 'is-active': appStore.themeColor === preset.color }"
            @click="appStore.setThemeColor(preset.color)"
          >
            <div class="theme-color-dot" :style="{ backgroundColor: preset.color }" />
          </button>
          <span class="theme-color-label">{{ preset.name }}</span>
        </div>

        <!-- 自定义颜色 -->
        <div class="color-item">
          <div
            class="theme-color-card custom-color-card"
            :class="{ 'is-active': !themePresetItems.some((p) => p.color === appStore.themeColor) }"
          >
            <div class="theme-color-dot custom-dot">
              <NIcon size="16">
                <Icon icon="lucide:pipette" />
              </NIcon>
            </div>
            <NColorPicker
              :value="appStore.themeColor"
              :modes="['hex']"
              :show-alpha="false"
              :actions="['confirm']"
              class="custom-color-overlay"
              @update:value="(value) => appStore.setThemeColor(value)"
            >
              <template #label>
                <span />
              </template>
            </NColorPicker>
          </div>
          <span class="theme-color-label">{{ t('preference.appearance.color.custom') }}</span>
        </div>
      </div>
    </NCard>

    <!-- 圆角 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.radius.title') }}
      </div>
      <div class="grid grid-cols-5 gap-1.5">
        <button
          v-for="r in [0, 0.25, 0.5, 0.75, 1]"
          :key="r"
          type="button"
          class="radius-btn"
          :class="{ 'is-active': appStore.uiRadius === r }"
          @click="appStore.setUiRadius(r)"
        >
          {{ r }}
        </button>
      </div>
    </NCard>

    <!-- 导航 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.navigation.title') }}
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.appearance.navigation.sidebar_dark') }}</span>
          <PrefTip :content="t('preference.appearance.navigation.sidebar_dark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.sidebarDark" />
      </div>
      <div class="pref-row">
        <span :class="{ 'text-[hsl(var(--muted-foreground))]': !appStore.sidebarDark }">
          {{ t('preference.appearance.navigation.sidebar_sub_dark') }}
        </span>
        <NSwitch v-model:value="appStore.sidebarSubDark" :disabled="!appStore.sidebarDark" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.appearance.navigation.header_dark') }}</span>
          <PrefTip :content="t('preference.appearance.navigation.header_dark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.headerDark" />
      </div>
    </NCard>

    <!-- 字体 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.font.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.appearance.font.size') }}</span>
        <div class="flex items-center gap-1.5">
          <NInputNumber
            :value="appStore.fontSize"
            :min="12"
            :max="20"
            :step="1"
            size="small"
            button-placement="both"
            :input-props="{ style: 'text-align: center' }"
            style="width: 130px"
            @update:value="(value) => value !== null && appStore.setFontSize(value)"
          />
          <span class="unit-label">px</span>
        </div>
      </div>
    </NCard>

    <!-- 其它 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.other.title') }}
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.appearance.other.color_weakness') }}</span>
          <PrefTip :content="t('preference.appearance.other.color_weakness_tip')" />
        </div>
        <NSwitch v-model:value="appStore.colorWeaknessEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.appearance.other.grayscale') }}</span>
          <PrefTip :content="t('preference.appearance.other.grayscale_tip')" />
        </div>
        <NSwitch v-model:value="appStore.grayscaleEnabled" />
      </div>
    </NCard>
  </div>
</template>

<style scoped>
.section-title {
  margin-bottom: 4px;
  padding: 0 6px;
  font-weight: 600;
  font-size: 13px;
  color: hsl(var(--foreground));
}

.unit-label {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  flex-shrink: 0;
}

/* 主题模式卡片 */
.theme-mode-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  padding: 10px 6px;
  border: 1.5px solid hsl(var(--border));
  border-radius: 8px;
  background: hsl(var(--card));
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  transition: all 0.18s ease;
}

.theme-mode-card:hover {
  border-color: hsl(var(--primary) / 0.5);
  color: hsl(var(--foreground));
  background: hsl(var(--accent));
}

.theme-mode-card.is-active {
  border-color: hsl(var(--primary));
  color: hsl(var(--primary));
  background: hsl(var(--primary) / 0.08);
  box-shadow: 0 0 0 2px hsl(var(--primary) / 0.2);
}

/* 每个色块 + 标签的包裹层 */
.color-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
}

/* 内置主题颜色卡片 */
.theme-color-card {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 10px;
  width: 100%;
  border: 1.5px solid hsl(var(--border));
  border-radius: 10px;
  background: hsl(var(--card));
  cursor: pointer;
  transition: all 0.16s ease;
}

.theme-color-card:hover {
  border-color: hsl(var(--primary) / 0.45);
  background: hsl(var(--accent));
}

.theme-color-card.is-active {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 2px hsl(var(--primary) / 0.22);
}

.theme-color-dot {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  flex-shrink: 0;
}

.custom-dot {
  display: flex;
  align-items: center;
  justify-content: center;
  background: hsl(var(--muted));
  color: hsl(var(--muted-foreground));
  border: 1.5px dashed hsl(var(--border));
}

.theme-color-label {
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  line-height: 1;
}

/* 自定义卡片：relative 以便遮罩定位 */
.custom-color-card {
  position: relative;
}

/* NColorPicker 透明遮罩：绝对覆盖整张卡片，点击即弹出取色器 */
.custom-color-overlay {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}

/* 圆角按钮 */
.radius-btn {
  padding: 6px 4px;
  border: 1.5px solid hsl(var(--border));
  border-radius: 6px;
  background: hsl(var(--card));
  color: hsl(var(--muted-foreground));
  font-size: 12px;
  cursor: pointer;
  transition: all 0.15s ease;
  text-align: center;
}

.radius-btn:hover {
  border-color: hsl(var(--primary) / 0.5);
  color: hsl(var(--foreground));
}

.radius-btn.is-active {
  border-color: hsl(var(--primary));
  background: hsl(var(--primary) / 0.1);
  color: hsl(var(--primary));
  font-weight: 600;
}
</style>
