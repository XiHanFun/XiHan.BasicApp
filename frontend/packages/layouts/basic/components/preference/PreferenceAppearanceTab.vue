<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NColorPicker, NIcon, NInput, NInputNumber, NRadioGroup, NSlider, NSwitch } from 'naive-ui'
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { LOADER_CURVES } from '~/components/common/math-curve-loaders'
import PageLoader from '~/components/common/PageLoader.vue'
import { ALL_THEME_COLORS, DEFAULT_THEME_COLOR, THEME_COLOR_GROUPS } from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceAppearanceTab' })

const props = defineProps<PreferenceAppearanceTabProps>()
const emit = defineEmits<{
  themeModeChange: [value: 'light' | 'dark' | 'auto', origin?: { clientX: number, clientY: number }]
}>()

const appStore = props.appStore
const { t } = useI18n()
const { isDark } = useTheme()

const isNoSidebar = computed(() => ['top', 'full'].includes(appStore.layoutMode))
const isDualColumn = computed(() => ['side-mixed', 'header-mix'].includes(appStore.layoutMode))

const sidebarDarkDisabled = computed(() => isDark.value || isNoSidebar.value)
const sidebarSubDarkDisabled = computed(
  () => isDark.value || !isDualColumn.value || !appStore.sidebarDark,
)
const headerDarkDisabled = computed(() => isDark.value)

watch(
  () => appStore.sidebarDark,
  (val) => {
    if (!val)
      appStore.sidebarSubDark = false
  },
)

interface PreferenceAppearanceTabProps {
  appStore: ReturnType<typeof useAppStore>
  themeMode: string
}

const themeModes = [
  { value: 'light', labelKey: 'preference.appearance.mode.light', icon: 'lucide:sun' },
  { value: 'dark', labelKey: 'preference.appearance.mode.dark', icon: 'lucide:moon' },
  { value: 'auto', labelKey: 'preference.appearance.mode.auto', icon: 'lucide:monitor' },
] as const

const themeColorGroups = THEME_COLOR_GROUPS
const allPresetColors = ALL_THEME_COLORS

const localizedModes = computed(() => themeModes.map(m => ({ ...m, label: t(m.labelKey) })))

/**
 * 模式切换：change 事件不带坐标，取被点中的模式卡片矩形中心作为扩散起点，
 * 否则 useTheme 会回退到视口中心，动画看起来「从上方扩散」而非从点击处。
 */
function handleModeChange(value: 'light' | 'dark' | 'auto', event: Event) {
  const card = (event.currentTarget as HTMLElement | null)
    ?.closest('.mode-item')
    ?.querySelector('.theme-mode-card')
  if (!card) {
    emit('themeModeChange', value)
    return
  }
  const rect = card.getBoundingClientRect()
  emit('themeModeChange', value, {
    clientX: rect.left + rect.width / 2,
    clientY: rect.top + rect.height / 2,
  })
}

const transitionItems = computed(() => [
  { value: 'scale-up', label: t('preference.general.animation.scale_up') },
  { value: 'scale-down', label: t('preference.general.animation.scale_down') },
  { value: 'slide-left', label: t('preference.general.animation.slide_left') },
  { value: 'slide-right', label: t('preference.general.animation.slide_right') },
  { value: 'slide-up', label: t('preference.general.animation.slide_up') },
  { value: 'slide-down', label: t('preference.general.animation.slide_down') },
  { value: 'skew-slide', label: t('preference.general.animation.skew_slide') },
  { value: 'fade', label: t('preference.general.animation.fade') },
  { value: 'zoom-fade', label: t('preference.general.animation.zoom_fade') },
  { value: 'blur-fade', label: t('preference.general.animation.blur_fade') },
  { value: 'rotate-fade', label: t('preference.general.animation.rotate_fade') },
  { value: 'flip-fade', label: t('preference.general.animation.flip_fade') },
])

// 加载动画列表（名称按 locale 国际化）
const loaderItems = computed(() =>
  LOADER_CURVES.map(curve => ({
    value: curve.id,
    label: t(`preference.general.animation.loaders.${curve.id}`),
  })),
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
          <label v-for="mode in localizedModes" :key="mode.value" class="mode-item">
            <input
              type="radio"
              :value="mode.value"
              class="sr-only"
              :checked="props.themeMode === mode.value"
              @change="(e: Event) => handleModeChange(mode.value, e)"
            >
            <div class="theme-mode-card" :class="{ 'is-active': props.themeMode === mode.value }">
              <NIcon size="20">
                <Icon :icon="mode.icon" />
              </NIcon>
            </div>
            <span class="mode-label">{{ mode.label }}</span>
          </label>
        </div>
      </NRadioGroup>
    </NCard>

    <!-- 颜色 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.color.title') }}
      </div>
      <div class="space-y-1.5">
        <!-- 默认或自定义 -->
        <div class="color-group-row">
          <span class="color-group-label">
            {{ t('preference.appearance.color.default_or_custom') }}
          </span>
          <div class="w-full color-group-swatches">
            <div class="color-item">
              <button
                type="button"
                class="theme-color-card"
                :class="{ 'is-active': appStore.themeColor === DEFAULT_THEME_COLOR }"
                :title="t('preference.appearance.color.default')"
                @click="appStore.setThemeColor(DEFAULT_THEME_COLOR)"
              >
                <div class="theme-color-dot" :style="{ backgroundColor: DEFAULT_THEME_COLOR }" />
              </button>
              <span class="theme-color-label">{{ t('preference.appearance.color.default') }}</span>
            </div>
            <div class="color-item">
              <div
                class="theme-color-card custom-color-card"
                :class="{ 'is-active': !allPresetColors.includes(appStore.themeColor) }"
              >
                <div class="theme-color-dot custom-dot">
                  <NIcon size="16">
                    <Icon icon="lucide:pipette" />
                  </NIcon>
                </div>
                <!-- 包裹 div 承载定位 class：NColorPicker 根为 VBinder(teleport)，class 无法直接挂载 -->
                <div class="custom-color-overlay">
                  <NColorPicker
                    :value="appStore.themeColor"
                    :modes="['hex']"
                    :show-alpha="false"
                    :actions="['confirm']"
                    @update:value="(value) => appStore.setThemeColor(value)"
                  >
                    <template #label>
                      <span />
                    </template>
                  </NColorPicker>
                </div>
              </div>
              <span class="theme-color-label">{{ t('preference.appearance.color.custom') }}</span>
            </div>
          </div>
        </div>

        <!-- 每行一个色系 + 三个色块 -->
        <div v-for="group in themeColorGroups" :key="group.familyKey" class="color-group-row">
          <span class="color-group-label">{{ t(group.familyKey) }}</span>
          <div class="w-full color-group-swatches">
            <div v-for="item in group.items" :key="item.color" class="color-item">
              <button
                type="button"
                class="theme-color-card"
                :class="{ 'is-active': appStore.themeColor === item.color }"
                :title="t(item.nameKey)"
                @click="appStore.setThemeColor(item.color)"
              >
                <div class="theme-color-dot" :style="{ backgroundColor: item.color }" />
              </button>
              <span class="theme-color-label">{{ t(item.nameKey) }}</span>
            </div>
          </div>
        </div>
      </div>
      <div class="pref-row mt-2">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.appearance.color.dynamic') }}</span>
          <PrefTip :content="t('preference.appearance.color.dynamic_tip')" />
        </div>
        <NSwitch v-model:value="appStore.themeDynamicColor" />
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
        <div class="flex gap-1 items-center">
          <span :class="{ 'text-[hsl(var(--muted-foreground))]': sidebarDarkDisabled }">
            {{ t('preference.appearance.navigation.sidebar_dark') }}
          </span>
          <PrefTip :content="t('preference.appearance.navigation.sidebar_dark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.sidebarDark" :disabled="sidebarDarkDisabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span :class="{ 'text-[hsl(var(--muted-foreground))]': sidebarSubDarkDisabled }">
            {{ t('preference.appearance.navigation.sidebar_sub_dark') }}
          </span>
          <PrefTip :content="t('preference.appearance.navigation.sidebar_sub_dark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.sidebarSubDark" :disabled="sidebarSubDarkDisabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span :class="{ 'text-[hsl(var(--muted-foreground))]': headerDarkDisabled }">
            {{ t('preference.appearance.navigation.header_dark') }}
          </span>
          <PrefTip :content="t('preference.appearance.navigation.header_dark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.headerDark" :disabled="headerDarkDisabled" />
      </div>
    </NCard>

    <!-- 字体 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.font.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.appearance.font.size') }}</span>
        <div class="flex gap-1.5 items-center">
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

    <!-- 动画 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.general.animation.title') }}
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.animation.transition_progress') }}</span>
          <PrefTip :content="t('preference.general.animation.transition_progress_tip')" />
        </div>
        <NSwitch v-model:value="appStore.transitionProgress" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.animation.transition_loading') }}</span>
          <PrefTip :content="t('preference.general.animation.transition_loading_tip')" />
        </div>
        <NSwitch v-model:value="appStore.transitionLoading" />
      </div>
      <div
        class="transition-grid"
        :class="{ 'pointer-events-none opacity-40': !appStore.transitionLoading }"
      >
        <div
          v-for="item in loaderItems"
          :key="item.value"
          class="transition-item"
          :class="{ 'is-active': appStore.loadingName === item.value }"
          @click="appStore.transitionLoading && (appStore.loadingName = item.value)"
        >
          <div class="transition-preview">
            <PageLoader :name="item.value" :size="40" preview :fixed-color="appStore.loadingFixedColor" />
          </div>
          <span class="item-label" :title="item.label">{{ item.label }}</span>
        </div>
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.animation.loading_fixed_color') }}</span>
          <PrefTip :content="t('preference.general.animation.loading_fixed_color_tip')" />
        </div>
        <NSwitch
          v-model:value="appStore.loadingFixedColor"
          :disabled="!appStore.transitionLoading"
        />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.animation.theme_animation') }}</span>
          <PrefTip :content="t('preference.general.animation.theme_animation_tip')" />
        </div>
        <NSwitch v-model:value="appStore.themeAnimationEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.animation.transition_enable') }}</span>
          <PrefTip :content="t('preference.general.animation.transition_enable_tip')" />
        </div>
        <NSwitch v-model:value="appStore.transitionEnable" />
      </div>
      <div
        class="transition-grid"
        :class="{ 'pointer-events-none opacity-40': !appStore.transitionEnable }"
      >
        <div
          v-for="item in transitionItems"
          :key="item.value"
          class="transition-item"
          :class="{ 'is-active': appStore.transitionName === item.value }"
          @click="appStore.transitionEnable && (appStore.transitionName = item.value)"
        >
          <div class="transition-preview">
            <div class="preview-block" :class="`anim-${item.value}`" />
          </div>
          <span class="item-label">{{ item.label }}</span>
        </div>
      </div>
    </NCard>

    <!-- 交互 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.interaction.title') }}
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.dynamic_island') }}</span>
          <PrefTip :content="t('preference.general.dynamic_island_tip')" />
        </div>
        <NSwitch v-model:value="appStore.widgetDynamicIsland" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.table_row_peek') }}</span>
          <PrefTip :content="t('preference.general.table_row_peek_tip')" />
        </div>
        <NSwitch v-model:value="appStore.tableRowPeek" />
      </div>
    </NCard>

    <!-- 效果 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.effects.title') }}
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.frosted_glass') }}</span>
          <PrefTip :content="t('preference.general.frosted_glass_tip')" />
        </div>
        <NSwitch v-model:value="appStore.frostedGlassEnabled" />
      </div>
      <div v-if="appStore.frostedGlassEnabled" class="pref-row">
        <span>{{ t('preference.general.frosted_glass_intensity') }}</span>
        <NSlider
          v-model:value="appStore.frostedGlassIntensity"
          :min="1"
          :max="100"
          :step="1"
          :tooltip="true"
          style="width: 150px"
        />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.watermark') }}</span>
          <PrefTip :content="t('preference.general.watermark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.watermarkEnabled" />
      </div>
      <div v-if="appStore.watermarkEnabled" class="mb-2 pref-row">
        <span>{{ t('preference.general.watermark_content') }}</span>
        <NInput
          v-model:value="appStore.watermarkText"
          size="small"
          style="width: 150px"
          :placeholder="t('preference.general.watermark_text')"
        />
      </div>
    </NCard>

    <!-- 其它 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.appearance.other.title') }}
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.appearance.other.color_weakness') }}</span>
          <PrefTip :content="t('preference.appearance.other.color_weakness_tip')" />
        </div>
        <NSwitch v-model:value="appStore.colorWeaknessEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.appearance.other.grayscale') }}</span>
          <PrefTip :content="t('preference.appearance.other.grayscale_tip')" />
        </div>
        <NSwitch v-model:value="appStore.grayscaleEnabled" />
      </div>
    </NCard>
  </div>
</template>

<style scoped>
/* 模式选项外层容器 */
.mode-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  cursor: pointer;
}

/* 主题模式卡片（仅图标，无文字） */
.theme-mode-card {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 10px 6px;
  width: 100%;
  border: 1.5px solid hsl(var(--border));
  border-radius: var(--radius);
  background: hsl(var(--card));
  color: hsl(var(--muted-foreground));
  transition: all 0.18s ease;
}

.mode-item:hover .theme-mode-card {
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

/* 模式文字标签 */
.mode-label {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  text-align: center;
  line-height: 1.2;
  transition: color 0.15s ease;
}

.mode-item:hover .mode-label {
  color: hsl(var(--foreground));
}

/* 色系分组行 */
.color-group-row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

/* 色系名标签 */
.color-group-label {
  font-size: 11px;
  font-weight: 700;
  color: hsl(var(--muted-foreground));
  line-height: 1;
}

/* 一行三个色块 */
.color-group-swatches {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 6px;
}

/* 每个色块 + 标签的包裹层 */
.color-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

/* 内置主题颜色卡片 */
.theme-color-card {
  display: flex;
  align-items: stretch;
  justify-content: center;
  padding: 0;
  width: 100%;
  min-height: 42px;
  overflow: hidden;
  border: 1.5px solid hsl(var(--border));
  border-radius: var(--radius-card);
  background: hsl(var(--card));
  cursor: pointer;
  transition: all 0.16s ease;
}

.theme-color-card:hover {
  border-color: hsl(var(--primary) / 0.55);
}

.theme-color-card.is-active {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 2px hsl(var(--primary) / 0.22);
}

/* 色块充满整个卡片 */
.theme-color-dot {
  width: 100%;
  min-height: 42px;
  flex-shrink: 0;
  position: relative;
}

/* 粗粒底层：低频 fractalNoise + overlay，模拟大颗粒涂料 */
.theme-color-dot:not(.custom-dot)::before {
  content: '';
  position: absolute;
  inset: 0;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='c'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.38' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23c)'/%3E%3C/svg%3E");
  background-size: 120px 120px;
  mix-blend-mode: overlay;
  opacity: 0.55;
  pointer-events: none;
}

/* 细粒表层：高频 turbulence + soft-light，叠加细腻砂砾感 */
.theme-color-dot:not(.custom-dot)::after {
  content: '';
  position: absolute;
  inset: 0;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='f'%3E%3CfeTurbulence type='turbulence' baseFrequency='0.85' numOctaves='6' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23f)'/%3E%3C/svg%3E");
  background-size: 72px 72px;
  mix-blend-mode: soft-light;
  opacity: 0.62;
  pointer-events: none;
}

/* 取色器占位 */
.custom-dot {
  display: flex;
  align-items: center;
  justify-content: center;
  background: hsl(var(--muted));
  color: hsl(var(--muted-foreground));
}

.theme-color-label {
  font-size: 10px;
  color: hsl(var(--muted-foreground));
  line-height: 1;
  white-space: nowrap;
}

/* 自定义卡片：relative 以便遮罩定位；虚线边框区分"非预设" */
.custom-color-card {
  position: relative;
  border-style: dashed;
}

/* NColorPicker 透明遮罩：绝对覆盖整张卡片，点击即弹出取色器 */
/* 透明触发层：绝对覆盖整卡（包裹 div，class 挂在此处而非 NColorPicker 上） */
.custom-color-overlay {
  position: absolute;
  inset: 0;
  z-index: 1;
  opacity: 0;
  cursor: pointer;
}

/* 内部 NColorPicker 触发块充满整层，确保任意位置点击都能弹出取色器 */
.custom-color-overlay :deep(.n-color-picker),
.custom-color-overlay :deep(.n-color-picker__fill) {
  width: 100%;
  height: 100%;
}

/* 圆角按钮 */
.radius-btn {
  padding: 6px 4px;
  border: 1.5px solid hsl(var(--border));
  border-radius: var(--radius);
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

/* 过渡动画预览选择器 */
.transition-grid {
  display: grid;
  /* minmax(0,1fr) 允许列收缩，避免长标签（如加载动画英文名）撑破栅格导致横向溢出 */
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 8px;
  margin-top: 4px;
}

.transition-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  cursor: pointer;
}

.transition-preview {
  width: 100%;
  aspect-ratio: 4 / 3;
  border-radius: var(--radius);
  border: 2px solid hsl(var(--border));
  background: hsl(var(--muted));
  overflow: visible;
  clip-path: inset(-7px round calc(var(--radius) + 5px));
  position: relative;
  /* 居中容器内的加载器（绝对定位的 .preview-block 不受影响） */
  display: grid;
  place-items: center;
  transition: border-color 0.18s ease;
}

.transition-item.is-active .transition-preview {
  border-color: hsl(var(--primary));
}

.item-label {
  font-size: 11px;
  line-height: 1.3;
  color: hsl(var(--muted-foreground));
  text-align: center;
  /* 长名换行并最多 2 行截断，保证卡片宽度统一、不溢出 */
  display: -webkit-box;
  overflow: hidden;
  word-break: break-word;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.transition-item.is-active .item-label {
  color: hsl(var(--primary));
  font-weight: 500;
}

.preview-block {
  position: absolute;
  inset: 5px;
  border-radius: calc(var(--radius) - 1px);
  background: hsl(var(--primary) / 0.72);
}

@keyframes anim-fade {
  0%,
  100% {
    opacity: 0;
  }
  35%,
  65% {
    opacity: 1;
  }
}
.anim-fade {
  animation: anim-fade 2s ease-in-out infinite;
}

@keyframes anim-slide-left {
  0% {
    transform: translateX(115%);
    opacity: 0;
  }
  25% {
    opacity: 1;
  }
  75% {
    opacity: 1;
  }
  100% {
    transform: translateX(-115%);
    opacity: 0;
  }
}
.anim-slide-left {
  animation: anim-slide-left 2.2s ease-in-out infinite;
}

@keyframes anim-slide-up {
  0% {
    transform: translateY(115%);
    opacity: 0;
  }
  25% {
    opacity: 1;
  }
  75% {
    opacity: 1;
  }
  100% {
    transform: translateY(-115%);
    opacity: 0;
  }
}
.anim-slide-up {
  animation: anim-slide-up 2.2s ease-in-out infinite;
}

@keyframes anim-slide-down {
  0% {
    transform: translateY(-115%);
    opacity: 0;
  }
  25% {
    opacity: 1;
  }
  75% {
    opacity: 1;
  }
  100% {
    transform: translateY(115%);
    opacity: 0;
  }
}
.anim-slide-down {
  animation: anim-slide-down 2.2s ease-in-out infinite;
}

@keyframes anim-slide-right {
  0% {
    transform: translateX(-115%);
    opacity: 0;
  }
  25% {
    opacity: 1;
  }
  75% {
    opacity: 1;
  }
  100% {
    transform: translateX(115%);
    opacity: 0;
  }
}
.anim-slide-right {
  animation: anim-slide-right 2.2s ease-in-out infinite;
}

@keyframes anim-zoom-fade {
  0%,
  100% {
    transform: scale(0.86);
    opacity: 0;
  }
  35%,
  65% {
    transform: scale(1);
    opacity: 1;
  }
}
.anim-zoom-fade {
  animation: anim-zoom-fade 2s cubic-bezier(0.2, 0.8, 0.2, 1) infinite;
}

@keyframes anim-flip-fade {
  0%,
  100% {
    transform: perspective(300px) rotateY(-22deg) scale(0.95);
    opacity: 0;
  }
  35%,
  65% {
    transform: perspective(300px) rotateY(0deg) scale(1);
    opacity: 1;
  }
}
.anim-flip-fade {
  transform-origin: center;
  animation: anim-flip-fade 2.1s ease-in-out infinite;
}

@keyframes anim-scale-up {
  0%,
  100% {
    transform: scale(0.8);
    opacity: 0;
  }
  35%,
  65% {
    transform: scale(1);
    opacity: 1;
  }
}
.anim-scale-up {
  animation: anim-scale-up 2s ease-in-out infinite;
}

@keyframes anim-scale-down {
  0%,
  100% {
    transform: scale(1.18);
    opacity: 0;
  }
  35%,
  65% {
    transform: scale(1);
    opacity: 1;
  }
}
.anim-scale-down {
  animation: anim-scale-down 2.1s ease-in-out infinite;
}

@keyframes anim-blur-fade {
  0%,
  100% {
    filter: blur(5px);
    opacity: 0;
  }
  35%,
  65% {
    filter: blur(0);
    opacity: 1;
  }
}
.anim-blur-fade {
  animation: anim-blur-fade 2s ease-in-out infinite;
}

@keyframes anim-rotate-fade {
  0%,
  100% {
    transform: rotate(-10deg) scale(0.92);
    opacity: 0;
  }
  35%,
  65% {
    transform: rotate(0deg) scale(1);
    opacity: 1;
  }
}
.anim-rotate-fade {
  transform-origin: center;
  animation: anim-rotate-fade 2s ease-in-out infinite;
}

@keyframes anim-skew-slide {
  0% {
    transform: translateX(115%) skewX(-12deg);
    opacity: 0;
  }
  25% {
    opacity: 1;
  }
  75% {
    opacity: 1;
  }
  100% {
    transform: translateX(-115%) skewX(12deg);
    opacity: 0;
  }
}
.anim-skew-slide {
  animation: anim-skew-slide 2.2s ease-in-out infinite;
}
</style>
