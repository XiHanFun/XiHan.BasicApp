<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { Icon } from '@iconify/vue'
import { NCard, NColorPicker, NIcon, NInputNumber, NRadioGroup, NSwitch } from 'naive-ui'
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { DEFAULT_THEME_COLOR } from '~/constants'
import { useTheme } from '~/hooks'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceAppearanceTab' })

const props = defineProps<PreferenceAppearanceTabProps>()
const emit = defineEmits<{
  themeModeChange: [value: 'light' | 'dark' | 'auto']
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

watch(() => appStore.sidebarDark, (val) => {
  if (!val) appStore.sidebarSubDark = false
})

interface ThemePresetItem {
  color: string
  nameKey: string
}

interface ThemeColorGroup {
  familyKey: string
  items: ThemePresetItem[]
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

const themeColorGroups: ThemeColorGroup[] = [
  {
    familyKey: 'preference.appearance.color.family.red',
    items: [
      { color: '#C0446A', nameKey: 'preference.appearance.color.preset.yan_zhi_hong' },
      { color: '#C0392B', nameKey: 'preference.appearance.color.preset.zhu_sha_hong' },
      { color: '#8B1A3A', nameKey: 'preference.appearance.color.preset.jiang_zi_hong' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.orange',
    items: [
      { color: '#A0522D', nameKey: 'preference.appearance.color.preset.zhe_shi_zong' },
      { color: '#C45C26', nameKey: 'preference.appearance.color.preset.zhuan_wa_cheng' },
      { color: '#D4751A', nameKey: 'preference.appearance.color.preset.hu_po_cheng' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.yellow',
    items: [
      { color: '#D4A017', nameKey: 'preference.appearance.color.preset.jiang_huang_cheng' },
      { color: '#E8C97E', nameKey: 'preference.appearance.color.preset.xiang_ye_huang' },
      { color: '#F0C040', nameKey: 'preference.appearance.color.preset.teng_huang_se' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.green',
    items: [
      { color: '#7AB648', nameKey: 'preference.appearance.color.preset.song_hua_lv' },
      { color: '#5C8A6F', nameKey: 'preference.appearance.color.preset.zhu_qing_lv' },
      { color: '#2E8B57', nameKey: 'preference.appearance.color.preset.bi_yu_lv' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.cyan',
    items: [
      { color: '#48C0A3', nameKey: 'preference.appearance.color.preset.bi_bo_qing' },
      { color: '#1A6B56', nameKey: 'preference.appearance.color.preset.shi_qing_se' },
      { color: '#3DAA8A', nameKey: 'preference.appearance.color.preset.fei_cui_qing' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.blue',
    items: [
      { color: '#3A5A8C', nameKey: 'preference.appearance.color.preset.cang_qing_lan' },
      { color: '#5A7FA0', nameKey: 'preference.appearance.color.preset.shi_ban_lan' },
      { color: '#2A5CAA', nameKey: 'preference.appearance.color.preset.ji_lan_se' },
    ],
  },
  {
    familyKey: 'preference.appearance.color.family.purple',
    items: [
      { color: '#8B7BA8', nameKey: 'preference.appearance.color.preset.ding_xiang_zi' },
      { color: '#9C7B9A', nameKey: 'preference.appearance.color.preset.ou_he_zi' },
      { color: '#6A4C8C', nameKey: 'preference.appearance.color.preset.qing_lian_zi' },
    ],
  },
]

/** 所有预设色值（含默认色），用于判断当前色是否为自定义 */
const allPresetColors = [
  DEFAULT_THEME_COLOR,
  ...themeColorGroups.flatMap(g => g.items.map(i => i.color)),
]

const localizedModes = computed(() => themeModes.map(m => ({ ...m, label: t(m.labelKey) })))
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
            class="mode-item"
          >
            <input
              type="radio"
              :value="mode.value"
              class="sr-only"
              :checked="props.themeMode === mode.value"
              @change="emit('themeModeChange', mode.value)"
            >
            <div
              class="theme-mode-card"
              :class="{ 'is-active': props.themeMode === mode.value }"
            >
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
        <!-- 自定义分组（默认色 + 取色器），置于所有色系上方 -->
        <div class="color-group-row">
          <span class="color-group-label">{{ t('preference.appearance.color.custom') }}</span>
          <div class="color-group-swatches w-full">
            <!-- 默认色 -->
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
            <!-- 取色器 -->
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
                <NColorPicker
                  :value="appStore.themeColor"
                  :modes="['hex']"
                  :show-alpha="false"
                  :actions="['confirm']"
                  class="custom-color-overlay"
                  @update:value="value => appStore.setThemeColor(value)"
                >
                  <template #label>
                    <span />
                  </template>
                </NColorPicker>
              </div>
              <span class="theme-color-label">{{ t('preference.appearance.color.picker') }}</span>
            </div>
          </div>
        </div>

        <!-- 每行一个色系 + 三个色块 -->
        <div
          v-for="group in themeColorGroups"
          :key="group.familyKey"
          class="color-group-row"
        >
          <span class="color-group-label">{{ t(group.familyKey) }}</span>
          <div class="color-group-swatches w-full">
            <div
              v-for="item in group.items"
              :key="item.color"
              class="color-item"
            >
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
          <span :class="{ 'text-[hsl(var(--muted-foreground))]': sidebarDarkDisabled }">
            {{ t('preference.appearance.navigation.sidebar_dark') }}
          </span>
          <PrefTip :content="t('preference.appearance.navigation.sidebar_dark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.sidebarDark" :disabled="sidebarDarkDisabled" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span :class="{ 'text-[hsl(var(--muted-foreground))]': sidebarSubDarkDisabled }">
            {{ t('preference.appearance.navigation.sidebar_sub_dark') }}
          </span>
          <PrefTip :content="t('preference.appearance.navigation.sidebar_sub_dark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.sidebarSubDark" :disabled="sidebarSubDarkDisabled" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
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
            @update:value="value => value !== null && appStore.setFontSize(value)"
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
</style>
