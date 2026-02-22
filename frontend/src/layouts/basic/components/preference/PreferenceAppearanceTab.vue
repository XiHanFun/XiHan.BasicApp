<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NCard, NColorPicker, NInputNumber, NRadioGroup, NSwitch } from 'naive-ui'

defineOptions({ name: 'PreferenceAppearanceTab' })

const props = defineProps<PreferenceAppearanceTabProps>()
const emit = defineEmits<{
  themeModeChange: [value: 'light' | 'dark' | 'auto']
}>()

const appStore = props.appStore

interface ThemePresetItem {
  color: string
  label: string
}

interface PreferenceAppearanceTabProps {
  appStore: ReturnType<typeof useAppStore>
  themeMode: string
  themePresets: string[]
}

const themeModes = [
  { value: 'light', label: '浅色', icon: 'lucide:sun' },
  { value: 'dark', label: '深色', icon: 'lucide:moon' },
  { value: 'auto', label: '跟随系统', icon: 'lucide:monitor' },
] as const

const themePresetItems: ThemePresetItem[] = [
  { color: '#4080FF', label: '品蓝' },
  { color: '#7C3AED', label: '青莲' },
  { color: '#E91E8C', label: '胭脂' },
  { color: '#F59E0B', label: '杏黄' },
  { color: '#0EA5E9', label: '天青' },
  { color: '#10B981', label: '翡翠' },
  { color: '#6B7280', label: '苍灰' },
  { color: '#059669', label: '松绿' },
  { color: '#1D4ED8', label: '宝蓝' },
  { color: '#EA580C', label: '橘红' },
  { color: '#DC2626', label: '朱红' },
  { color: '#374151', label: '玄灰' },
  { color: '#334155', label: '青黛' },
  { color: '#4B5563', label: '素灰' },
]
</script>

<template>
  <div class="space-y-4">
    <!-- 模式 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">模式</div>
      <NRadioGroup
        :value="props.themeMode"
        class="w-full"
        @update:value="(value) => emit('themeModeChange', value as 'light' | 'dark' | 'auto')"
      >
        <div class="grid grid-cols-3 gap-2">
          <label
            v-for="mode in themeModes"
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
            />
            <Icon :icon="mode.icon" width="20" class="mb-1" />
            <span class="text-xs">{{ mode.label }}</span>
          </label>
        </div>
      </NRadioGroup>
    </NCard>

    <!-- 颜色 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">颜色</div>
      <div class="grid grid-cols-3 gap-x-2 gap-y-3">
        <!-- 预设颜色：标签在卡片外面 -->
        <div v-for="preset in themePresetItems" :key="preset.color" class="color-item">
          <button
            type="button"
            class="theme-color-card"
            :class="{ 'is-active': appStore.themeColor === preset.color }"
            @click="appStore.setThemeColor(preset.color)"
          >
            <div class="theme-color-dot" :style="{ backgroundColor: preset.color }" />
          </button>
          <span class="theme-color-label">{{ preset.label }}</span>
        </div>

        <!-- 自定义颜色 -->
        <div class="color-item">
          <div
            class="theme-color-card custom-color-card"
            :class="{ 'is-active': !themePresetItems.some((p) => p.color === appStore.themeColor) }"
          >
            <div class="theme-color-dot custom-dot">
              <Icon icon="lucide:pipette" width="16" />
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
          <span class="theme-color-label">自定义</span>
        </div>
      </div>
    </NCard>

    <!-- 圆角 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">圆角</div>
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
      <div class="section-title">导航</div>
      <div class="mb-2 flex items-center justify-between">
        <span>深色侧边栏</span>
        <NSwitch v-model:value="appStore.sidebarDark" />
      </div>
      <div class="mb-2 flex items-center justify-between">
        <span :class="{ 'text-[hsl(var(--muted-foreground))]': !appStore.sidebarDark }">
          深色侧边栏子栏
        </span>
        <NSwitch v-model:value="appStore.sidebarSubDark" :disabled="!appStore.sidebarDark" />
      </div>
      <div class="flex items-center justify-between">
        <span>深色顶栏</span>
        <NSwitch v-model:value="appStore.headerDark" />
      </div>
    </NCard>

    <!-- 字体 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">字体</div>
      <div class="flex items-center justify-between">
        <span>大小</span>
        <div class="flex items-center gap-1.5">
          <NInputNumber
            :value="appStore.fontSize"
            :min="12"
            :max="20"
            :step="1"
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
      <div class="section-title">其它</div>
      <div class="mb-2 flex items-center justify-between">
        <span>色弱模式</span>
        <NSwitch v-model:value="appStore.colorWeaknessEnabled" />
      </div>
      <div class="flex items-center justify-between">
        <span>灰色模式</span>
        <NSwitch v-model:value="appStore.grayscaleEnabled" />
      </div>
    </NCard>
  </div>
</template>

<style scoped>
.section-title {
  margin-bottom: 10px;
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

/* 内置主题颜色卡片（白底卡片 + 小圆角色块，标签在外面） */
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
