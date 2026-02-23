<script setup lang="ts">
import type { useAppStore } from '~/stores'
import {
  NCard,
  NInput,
  NInputNumber,
  NRadioButton,
  NRadioGroup,
  NSelect,
  NSpace,
  NSwitch,
} from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import LayoutPreviewSvg from './LayoutPreviewSvg.vue'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceLayoutTab' })

const props = defineProps<PreferenceLayoutTabProps>()

const emit = defineEmits<{
  layoutModeChange: [value: string]
  contentModeChange: [value: 'fixed' | 'fluid']
}>()

interface LayoutPreset {
  key: string
  label: string
}

interface PreferenceLayoutTabProps {
  appStore: ReturnType<typeof useAppStore>
  layoutMode: string
  contentMode: 'fixed' | 'fluid'
  layoutPresets: ReadonlyArray<LayoutPreset> | LayoutPreset[]
}

const appStore = props.appStore
const { t } = useI18n()

const tabbarStyleOptions = computed(() => [
  { label: t('preference.layout.tabbar.style_chrome'), value: 'chrome' },
  { label: t('preference.layout.tabbar.style_plain'), value: 'plain' },
  { label: t('preference.layout.tabbar.style_rounded'), value: 'rounded' },
])

const preferencePositionOptions = computed(() => [
  { label: t('preference.layout.widget.preference_position_auto'), value: 'auto' },
  { label: t('preference.layout.widget.preference_position_fixed_left'), value: 'fixed-left' },
  { label: t('preference.layout.widget.preference_position_fixed_right'), value: 'fixed-right' },
])
</script>

<template>
  <div class="space-y-4">
    <!-- 布局 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.title') }}
      </div>
      <div class="grid grid-cols-3 gap-2">
        <button
          v-for="item in props.layoutPresets"
          :key="item.key"
          type="button"
          class="layout-preset-card"
          :class="props.layoutMode === item.key ? 'is-active' : ''"
          @click="emit('layoutModeChange', item.key)"
        >
          <div class="layout-preset-preview">
            <LayoutPreviewSvg :type="item.key" />
          </div>
          <div class="mt-1.5 text-xs">
            {{ item.label }}
          </div>
        </button>
      </div>
    </NCard>

    <!-- 内容 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.content.title') }}
      </div>
      <div class="grid grid-cols-2 gap-2">
        <button
          type="button"
          class="layout-preset-card"
          :class="props.contentMode === 'fluid' ? 'is-active' : ''"
          @click="emit('contentModeChange', 'fluid')"
        >
          <div class="layout-preset-preview">
            <LayoutPreviewSvg type="content-fluid" />
          </div>
          <div class="mt-1.5 text-xs">
            {{ t('preference.layout.content.fluid') }}
          </div>
        </button>
        <button
          type="button"
          class="layout-preset-card"
          :class="props.contentMode === 'fixed' ? 'is-active' : ''"
          @click="emit('contentModeChange', 'fixed')"
        >
          <div class="layout-preset-preview">
            <LayoutPreviewSvg type="content-fixed" />
          </div>
          <div class="mt-1.5 text-xs">
            {{ t('preference.layout.content.fixed') }}
          </div>
        </button>
      </div>
    </NCard>

    <!-- 侧边栏 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.sidebar.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.sidebar.show') }}</span>
        <NSwitch v-model:value="appStore.sidebarShow" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.sidebar.collapse') }}</span>
        <NSwitch v-model:value="appStore.sidebarCollapsed" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1" :class="{ 'text-[hsl(var(--muted-foreground))]': !appStore.sidebarCollapsed }">
          <span>{{ t('preference.layout.sidebar.hover_expand') }}</span>
          <PrefTip :content="t('preference.layout.sidebar.hover_expand_tip')" />
        </div>
        <NSwitch
          v-model:value="appStore.sidebarExpandOnHover"
          :disabled="!appStore.sidebarCollapsed"
        />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.sidebar.collapsed_show_title') }}</span>
          <PrefTip :content="t('preference.layout.sidebar.collapsed_show_title_tip')" />
        </div>
        <NSwitch v-model:value="appStore.sidebarCollapsedShowTitle" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1" :class="{ 'text-[hsl(var(--muted-foreground))]': !appStore.sidebarCollapsed }">
          <span>{{ t('preference.layout.sidebar.auto_activate_child') }}</span>
          <PrefTip :content="t('preference.layout.sidebar.auto_activate_child_tip')" />
        </div>
        <NSwitch
          v-model:value="appStore.sidebarAutoActivateChild"
          :disabled="!appStore.sidebarCollapsed"
        />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.sidebar.show_buttons') }}</span>
        <div class="flex gap-1">
          <button
            type="button"
            class="btn-toggle"
            :class="{ 'is-active': appStore.sidebarCollapseButton }"
            @click="appStore.sidebarCollapseButton = !appStore.sidebarCollapseButton"
          >
            {{ t('preference.layout.sidebar.collapse_button') }}
          </button>
          <button
            type="button"
            class="btn-toggle"
            :class="{ 'is-active': appStore.sidebarFixedButton }"
            @click="appStore.sidebarFixedButton = !appStore.sidebarFixedButton"
          >
            {{ t('preference.layout.sidebar.fixed_button') }}
          </button>
        </div>
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.sidebar.width') }}</span>
        <div class="flex items-center gap-1.5">
          <NInputNumber
            v-model:value="appStore.sidebarWidth"
            :min="180"
            :max="320"
            size="small"
            button-placement="both"
            :input-props="{ style: 'text-align: center' }"
            style="width: 130px"
          />
          <span class="unit-label">px</span>
        </div>
      </div>
    </NCard>

    <!-- 顶栏 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.header.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.header.show') }}</span>
        <NSwitch v-model:value="appStore.headerShow" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.headerShow }">
        <span>{{ t('preference.layout.header.mode') }}</span>
        <NRadioGroup v-model:value="appStore.headerMode" size="small" :disabled="!appStore.headerShow">
          <NSpace :size="0">
            <NRadioButton value="fixed">
              {{ t('preference.layout.header.mode_fixed') }}
            </NRadioButton>
            <NRadioButton value="static">
              {{ t('preference.layout.header.mode_static') }}
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.headerShow }">
        <span>{{ t('preference.layout.header.menu_align') }}</span>
        <NRadioGroup v-model:value="appStore.headerMenuAlign" size="small" :disabled="!appStore.headerShow">
          <NSpace :size="0">
            <NRadioButton value="left">
              {{ t('preference.layout.header.menu_align_left') }}
            </NRadioButton>
            <NRadioButton value="center">
              {{ t('preference.layout.header.menu_align_center') }}
            </NRadioButton>
            <NRadioButton value="right">
              {{ t('preference.layout.header.menu_align_right') }}
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
    </NCard>

    <!-- 导航菜单 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.navigation.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.navigation.style') }}</span>
        <NRadioGroup v-model:value="appStore.navigationStyle" size="small">
          <NSpace :size="0">
            <NRadioButton value="rounded">
              {{ t('preference.layout.navigation.style_rounded') }}
            </NRadioButton>
            <NRadioButton value="plain">
              {{ t('preference.layout.navigation.style_plain') }}
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.navigation.split') }}</span>
          <PrefTip :content="t('preference.layout.navigation.split_tip')" />
        </div>
        <NSwitch v-model:value="appStore.navigationSplit" />
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.navigation.accordion') }}</span>
          <PrefTip :content="t('preference.layout.navigation.accordion_tip')" />
        </div>
        <NSwitch v-model:value="appStore.navigationAccordion" />
      </div>
    </NCard>

    <!-- 面包屑导航 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.breadcrumb.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.breadcrumb.enabled') }}</span>
        <NSwitch v-model:value="appStore.breadcrumbEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.breadcrumbEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.breadcrumb.hide_only_one') }}</span>
          <PrefTip :content="t('preference.layout.breadcrumb.hide_only_one_tip')" />
        </div>
        <NSwitch v-model:value="appStore.breadcrumbHideOnlyOne" :disabled="!appStore.breadcrumbEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.breadcrumbEnabled }">
        <span>{{ t('preference.layout.breadcrumb.show_icon') }}</span>
        <NSwitch v-model:value="appStore.breadcrumbShowIcon" :disabled="!appStore.breadcrumbEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.breadcrumbEnabled }">
        <span>{{ t('preference.layout.breadcrumb.show_home') }}</span>
        <NSwitch v-model:value="appStore.breadcrumbShowHome" :disabled="!appStore.breadcrumbEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.breadcrumbEnabled }">
        <span>{{ t('preference.layout.breadcrumb.style') }}</span>
        <NRadioGroup v-model:value="appStore.breadcrumbStyle" size="small" :disabled="!appStore.breadcrumbEnabled">
          <NSpace :size="0">
            <NRadioButton value="normal">
              {{ t('preference.layout.breadcrumb.style_normal') }}
            </NRadioButton>
            <NRadioButton value="background">
              {{ t('preference.layout.breadcrumb.style_background') }}
            </NRadioButton>
          </NSpace>
        </NRadioGroup>
      </div>
    </NCard>

    <!-- 标签栏 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.tabbar.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.tabbar.enabled') }}</span>
        <NSwitch v-model:value="appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.persist') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.persist_tip')" />
        </div>
        <NSwitch v-model:value="appStore.tabbarPersist" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.visit_history') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.visit_history_tip')" />
        </div>
        <NSwitch v-model:value="appStore.tabbarVisitHistory" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.max_count') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.max_count_tip')" />
        </div>
        <div class="flex items-center gap-1.5">
          <NInputNumber
            v-model:value="appStore.tabbarMaxCount"
            :min="0"
            :max="30"
            size="small"
            button-placement="both"
            :input-props="{ style: 'text-align: center' }"
            style="width: 120px"
            :disabled="!appStore.tabbarEnabled"
          />
          <span class="unit-label">个</span>
        </div>
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.draggable') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.draggable_tip')" />
        </div>
        <NSwitch v-model:value="appStore.tabbarDraggable" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.scroll_response') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.scroll_response_tip')" />
        </div>
        <NSwitch v-model:value="appStore.tabbarScrollResponse" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.middle_click_close') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.middle_click_close_tip')" />
        </div>
        <NSwitch v-model:value="appStore.tabbarMiddleClickClose" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.show_icon') }}</span>
        <NSwitch v-model:value="appStore.tabbarShowIcon" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.show_more') }}</span>
        <NSwitch v-model:value="appStore.tabbarShowMore" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.show_maximize') }}</span>
        <NSwitch v-model:value="appStore.tabbarShowMaximize" :disabled="!appStore.tabbarEnabled" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.style') }}</span>
        <NSelect
          v-model:value="appStore.tabbarStyle"
          :options="tabbarStyleOptions"
          size="small"
          style="width: 100px"
          :disabled="!appStore.tabbarEnabled"
        />
      </div>
    </NCard>

    <!-- 小部件 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.widget.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.global_search') }}</span>
        <NSwitch v-model:value="appStore.searchEnabled" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.theme_toggle') }}</span>
        <NSwitch v-model:value="appStore.widgetThemeToggle" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.language_toggle') }}</span>
        <NSwitch v-model:value="appStore.widgetLanguageToggle" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.fullscreen') }}</span>
        <NSwitch v-model:value="appStore.widgetFullscreen" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.notification') }}</span>
        <NSwitch v-model:value="appStore.widgetNotification" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.lock_screen') }}</span>
        <NSwitch v-model:value="appStore.widgetLockScreen" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.sidebar_toggle') }}</span>
        <NSwitch v-model:value="appStore.widgetSidebarToggle" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.refresh') }}</span>
        <NSwitch v-model:value="appStore.widgetRefresh" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.preference_position') }}</span>
        <NSelect
          v-model:value="appStore.widgetPreferencePosition"
          :options="preferencePositionOptions"
          size="small"
          style="width: 110px"
        />
      </div>
    </NCard>

    <!-- 底栏 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.footer.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.footer.show') }}</span>
        <NSwitch v-model:value="appStore.footerEnable" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.footerEnable }">
        <span>{{ t('preference.layout.footer.fixed') }}</span>
        <NSwitch v-model:value="appStore.footerFixed" :disabled="!appStore.footerEnable" />
      </div>
    </NCard>

    <!-- 版权 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.layout.copyright.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.copyright.enabled') }}</span>
        <NSwitch v-model:value="appStore.copyrightEnable" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>{{ t('preference.layout.copyright.name') }}</span>
        <NInput
          v-model:value="appStore.copyrightName"
          size="small"
          style="width: 150px"
          :input-props="{ style: 'text-align: right' }"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>{{ t('preference.layout.copyright.site') }}</span>
        <NInput
          v-model:value="appStore.copyrightSite"
          size="small"
          style="width: 150px"
          :input-props="{ style: 'text-align: right' }"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>{{ t('preference.layout.copyright.date') }}</span>
        <NInput
          v-model:value="appStore.copyrightDate"
          size="small"
          style="width: 90px"
          :input-props="{ style: 'text-align: right' }"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>{{ t('preference.layout.copyright.icp') }}</span>
        <NInput
          v-model:value="appStore.copyrightIcp"
          size="small"
          style="width: 150px"
          :input-props="{ style: 'text-align: right' }"
          :placeholder="t('preference.layout.copyright.optional')"
          :disabled="!appStore.copyrightEnable"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.copyrightEnable }">
        <span>{{ t('preference.layout.copyright.icp_url') }}</span>
        <NInput
          v-model:value="appStore.copyrightIcpUrl"
          size="small"
          style="width: 150px"
          :input-props="{ style: 'text-align: right' }"
          :placeholder="t('preference.layout.copyright.optional')"
          :disabled="!appStore.copyrightEnable"
        />
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

.layout-preset-card {
  border: 1.5px solid hsl(var(--border));
  border-radius: 10px;
  background: hsl(var(--card));
  color: hsl(var(--foreground));
  padding: 8px;
  text-align: center;
  transition: all 0.2s ease;
}

.layout-preset-card:hover {
  transform: translateY(-1px);
  border-color: color-mix(in srgb, hsl(var(--primary)) 45%, hsl(var(--border)));
  background: hsl(var(--accent));
}

.layout-preset-card.is-active {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 2px color-mix(in srgb, hsl(var(--primary)) 30%, transparent);
}

.layout-preset-preview {
  display: flex;
  height: 64px;
  align-items: center;
  justify-content: center;
  border-radius: 6px;
  overflow: hidden;
  background: hsl(var(--background));
}

/* 折叠/固定按钮的多选 toggle */
.btn-toggle {
  padding: 3px 10px;
  font-size: 12px;
  border-radius: 4px;
  border: 1.5px solid hsl(var(--border));
  background: hsl(var(--card));
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  transition: all 0.15s ease;
}

.btn-toggle:hover {
  border-color: hsl(var(--primary) / 0.5);
  color: hsl(var(--foreground));
}

.btn-toggle.is-active {
  border-color: hsl(var(--primary));
  background: hsl(var(--primary));
  color: hsl(var(--primary-foreground));
  font-weight: 500;
}
</style>
