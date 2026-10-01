<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { XhSwitch, XhToggleGroupItem, XhToggleGroupRoot } from '@xihan-ui/vue'
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XInput, XNumberInput, XSelect } from '~/components'
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
  tip?: string
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
  { label: t('preference.layout.tabbar.style_card'), value: 'card' },
  { label: t('preference.layout.tabbar.style_brisk'), value: 'brisk' },
])

/** 偏好设置入口的位置：入口不能没有，故不给「隐藏」 */
const preferencePositionOptions = computed(() => [
  { label: t('preference.layout.widget.placement_auto'), value: 'auto' },
  { label: t('preference.layout.widget.placement_header'), value: 'header' },
  { label: t('preference.layout.widget.placement_floating'), value: 'floating' },
])

/** 顶栏工具的位置：与偏好设置入口同一套，另可隐藏 */
const widgetPlacementOptions = computed(() => [
  ...preferencePositionOptions.value,
  { label: t('preference.layout.widget.placement_hidden'), value: 'hidden' },
])

const layout = computed(() => appStore.layoutMode)
const isFullContent = computed(() => layout.value === 'full')
const isNoSidebar = computed(() => ['top', 'full'].includes(layout.value))
const isMixedNav = computed(() => layout.value === 'mix')

const sidebarDisabled = computed(() => isNoSidebar.value)
const sidebarItemDisabled = computed(() => sidebarDisabled.value || !appStore.sidebarShow)
const sidebarExpandOnHoverDisabled = computed(
  () => sidebarItemDisabled.value || !appStore.sidebarCollapsed,
)
const sidebarCollapsedShowTitleDisabled = computed(
  () => sidebarItemDisabled.value || !appStore.sidebarCollapsed,
)
const sidebarAutoActivateChildDisabled = computed(
  () => sidebarItemDisabled.value || !['side-mixed', 'mix', 'header-mix'].includes(layout.value),
)

const headerDisabled = computed(() => isFullContent.value)
const headerItemDisabled = computed(() => headerDisabled.value || !appStore.headerShow)

const navDisabled = computed(() => isFullContent.value)
const navSplitDisabled = computed(() => navDisabled.value || !isMixedNav.value)

const breadcrumbDisabled = computed(() => {
  if (isFullContent.value || !appStore.headerShow)
    return true
  return !['side', 'side-mixed', 'header-sidebar'].includes(layout.value)
})
const breadcrumbItemDisabled = computed(() => breadcrumbDisabled.value || !appStore.breadcrumbEnabled)
const breadcrumbShowHomeDisabled = computed(
  () => breadcrumbItemDisabled.value || !appStore.breadcrumbShowIcon,
)

const copyrightDisabled = computed(() => !appStore.footerEnable)
const copyrightItemDisabled = computed(() => copyrightDisabled.value || !appStore.copyrightEnable)

watch(() => appStore.sidebarShow, (val) => {
  if (!val) {
    appStore.sidebarCollapsed = false
    appStore.sidebarExpandOnHover = false
  }
})

watch(() => appStore.sidebarCollapsed, (val) => {
  if (!val)
    appStore.sidebarExpandOnHover = false
})
</script>

<template>
  <div class="space-y-2">
    <!-- 布局 -->
    <section class="pref-card">
      <div class="section-title">
        {{ t('preference.layout.title') }}
      </div>
      <div class="grid grid-cols-3 gap-2">
        <div
          v-for="item in props.layoutPresets"
          :key="item.key"
          class="preset-item"
        >
          <button
            type="button"
            class="layout-preset-card"
            :class="props.layoutMode === item.key ? 'is-active' : ''"
            @click="emit('layoutModeChange', item.key)"
          >
            <div class="layout-preset-preview">
              <LayoutPreviewSvg :type="item.key" />
            </div>
          </button>
          <div class="preset-label">
            {{ item.label }}
            <PrefTip v-if="item.tip" :content="item.tip" />
          </div>
        </div>
      </div>
    </section>

    <!-- 内容 -->
    <section class="pref-card">
      <div class="section-title">
        {{ t('preference.layout.content.title') }}
      </div>
      <div class="grid grid-cols-3 gap-2">
        <div class="preset-item">
          <button
            type="button"
            class="layout-preset-card"
            :class="props.contentMode === 'fluid' ? 'is-active' : ''"
            @click="emit('contentModeChange', 'fluid')"
          >
            <div class="layout-preset-preview">
              <LayoutPreviewSvg type="content-fluid" />
            </div>
          </button>
          <div class="preset-label">
            {{ t('preference.layout.content.fluid') }}
            <PrefTip :content="t('preference.layout.content.fluid_tip')" />
          </div>
        </div>
        <div class="preset-item">
          <button
            type="button"
            class="layout-preset-card"
            :class="props.contentMode === 'fixed' ? 'is-active' : ''"
            @click="emit('contentModeChange', 'fixed')"
          >
            <div class="layout-preset-preview">
              <LayoutPreviewSvg type="content-fixed" />
            </div>
          </button>
          <div class="preset-label">
            {{ t('preference.layout.content.fixed') }}
            <PrefTip :content="t('preference.layout.content.fixed_tip')" />
          </div>
        </div>
      </div>
    </section>

    <!-- 侧边栏 -->
    <section class="pref-card" :class="{ 'opacity-60': sidebarDisabled }">
      <div class="section-title">
        {{ t('preference.layout.sidebar.title') }}
      </div>
      <div class="pref-row" :class="{ 'opacity-50': sidebarDisabled }">
        <span>{{ t('preference.layout.sidebar.show') }}</span>
        <XhSwitch v-model:checked="appStore.sidebarShow" :disabled="sidebarDisabled" :aria-label="t('preference.layout.sidebar.show')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': sidebarItemDisabled }">
        <span>{{ t('preference.layout.sidebar.collapse') }}</span>
        <XhSwitch v-model:checked="appStore.sidebarCollapsed" :disabled="sidebarItemDisabled" :aria-label="t('preference.layout.sidebar.collapse')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': sidebarExpandOnHoverDisabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.sidebar.hover_expand') }}</span>
          <PrefTip :content="t('preference.layout.sidebar.hover_expand_tip')" />
        </div>
        <XhSwitch
          v-model:checked="appStore.sidebarExpandOnHover"
          :disabled="sidebarExpandOnHoverDisabled"
          :aria-label="t('preference.layout.sidebar.hover_expand')"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': sidebarCollapsedShowTitleDisabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.sidebar.collapsed_show_title') }}</span>
          <PrefTip :content="t('preference.layout.sidebar.collapsed_show_title_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.sidebarCollapsedShowTitle" :disabled="sidebarCollapsedShowTitleDisabled" :aria-label="t('preference.layout.sidebar.collapsed_show_title')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': sidebarAutoActivateChildDisabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.sidebar.auto_activate_child') }}</span>
          <PrefTip :content="t('preference.layout.sidebar.auto_activate_child_tip')" />
        </div>
        <XhSwitch
          v-model:checked="appStore.sidebarAutoActivateChild"
          :disabled="sidebarAutoActivateChildDisabled"
          :aria-label="t('preference.layout.sidebar.auto_activate_child')"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': sidebarItemDisabled }">
        <span>{{ t('preference.layout.sidebar.show_buttons') }}</span>
        <div class="flex gap-1">
          <button
            type="button"
            class="btn-toggle"
            :class="{ 'is-active': appStore.sidebarCollapseButton && !sidebarItemDisabled }"
            :aria-pressed="appStore.sidebarCollapseButton"
            :disabled="sidebarItemDisabled"
            @click="!sidebarItemDisabled && (appStore.sidebarCollapseButton = !appStore.sidebarCollapseButton)"
          >
            {{ t('preference.layout.sidebar.collapse_button') }}
          </button>
          <button
            type="button"
            class="btn-toggle"
            :class="{ 'is-active': appStore.sidebarFixedButton && !sidebarItemDisabled }"
            :aria-pressed="appStore.sidebarFixedButton"
            :disabled="sidebarItemDisabled"
            @click="!sidebarItemDisabled && (appStore.sidebarFixedButton = !appStore.sidebarFixedButton)"
          >
            {{ t('preference.layout.sidebar.fixed_button') }}
          </button>
        </div>
      </div>
      <div class="pref-row" :class="{ 'opacity-50': sidebarItemDisabled }">
        <span>{{ t('preference.layout.sidebar.width') }}</span>
        <div class="flex items-center gap-1.5">
          <XNumberInput
            v-model:value="appStore.sidebarWidth"
            :min="180"
            :max="320"
            size="sm"
            class="pref-num pref-num--center"
            style="width: 130px"
            :disabled="sidebarItemDisabled"
            :aria-label="t('preference.layout.sidebar.width')"
          />
          <span class="unit-label">px</span>
        </div>
      </div>
    </section>

    <!-- 顶栏 -->
    <section class="pref-card" :class="{ 'opacity-60': headerDisabled }">
      <div class="section-title">
        {{ t('preference.layout.header.title') }}
      </div>
      <div class="pref-row" :class="{ 'opacity-50': headerDisabled }">
        <span>{{ t('preference.layout.header.show') }}</span>
        <XhSwitch v-model:checked="appStore.headerShow" :disabled="headerDisabled" :aria-label="t('preference.layout.header.show')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': headerItemDisabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.header.mode') }}</span>
          <PrefTip :content="t('preference.layout.header.mode_tip')" />
        </div>
        <XhToggleGroupRoot v-model:value="appStore.headerMode" :disabled="headerItemDisabled" disallow-empty size="sm" :aria-label="t('preference.layout.header.mode')">
          <XhToggleGroupItem value="fixed">
            {{ t('preference.layout.header.mode_fixed') }}
          </XhToggleGroupItem>
          <XhToggleGroupItem value="static">
            {{ t('preference.layout.header.mode_static') }}
          </XhToggleGroupItem>
        </XhToggleGroupRoot>
      </div>
      <div class="pref-row" :class="{ 'opacity-50': headerItemDisabled }">
        <span>{{ t('preference.layout.header.menu_align') }}</span>
        <XhToggleGroupRoot v-model:value="appStore.headerMenuAlign" :disabled="headerItemDisabled" disallow-empty size="sm" :aria-label="t('preference.layout.header.menu_align')">
          <XhToggleGroupItem value="start">
            {{ t('preference.layout.header.menu_align_left') }}
          </XhToggleGroupItem>
          <XhToggleGroupItem value="center">
            {{ t('preference.layout.header.menu_align_center') }}
          </XhToggleGroupItem>
          <XhToggleGroupItem value="end">
            {{ t('preference.layout.header.menu_align_right') }}
          </XhToggleGroupItem>
        </XhToggleGroupRoot>
      </div>
    </section>

    <!-- 导航菜单 -->
    <section class="pref-card" :class="{ 'opacity-60': navDisabled }">
      <div class="section-title">
        {{ t('preference.layout.navigation.title') }}
      </div>
      <div class="pref-row" :class="{ 'opacity-50': navDisabled }">
        <span>{{ t('preference.layout.navigation.style') }}</span>
        <XhToggleGroupRoot v-model:value="appStore.navigationStyle" :disabled="navDisabled" disallow-empty size="sm" :aria-label="t('preference.layout.navigation.style')">
          <XhToggleGroupItem value="rounded">
            {{ t('preference.layout.navigation.style_rounded') }}
          </XhToggleGroupItem>
          <XhToggleGroupItem value="plain">
            {{ t('preference.layout.navigation.style_plain') }}
          </XhToggleGroupItem>
        </XhToggleGroupRoot>
      </div>
      <div class="pref-row" :class="{ 'opacity-50': navSplitDisabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.navigation.split') }}</span>
          <PrefTip :content="t('preference.layout.navigation.split_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.navigationSplit" :disabled="navSplitDisabled" :aria-label="t('preference.layout.navigation.split')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': navDisabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.navigation.accordion') }}</span>
          <PrefTip :content="t('preference.layout.navigation.accordion_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.navigationAccordion" :disabled="navDisabled" :aria-label="t('preference.layout.navigation.accordion')" />
      </div>
    </section>

    <!-- 面包屑导航 -->
    <section class="pref-card" :class="{ 'opacity-60': breadcrumbDisabled }">
      <div class="section-title">
        {{ t('preference.layout.breadcrumb.title') }}
      </div>
      <div class="pref-row" :class="{ 'opacity-50': breadcrumbDisabled }">
        <span>{{ t('preference.layout.breadcrumb.enabled') }}</span>
        <XhSwitch v-model:checked="appStore.breadcrumbEnabled" :disabled="breadcrumbDisabled" :aria-label="t('preference.layout.breadcrumb.enabled')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': breadcrumbItemDisabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.breadcrumb.hide_only_one') }}</span>
          <PrefTip :content="t('preference.layout.breadcrumb.hide_only_one_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.breadcrumbHideOnlyOne" :disabled="breadcrumbItemDisabled" :aria-label="t('preference.layout.breadcrumb.hide_only_one')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': breadcrumbItemDisabled }">
        <span>{{ t('preference.layout.breadcrumb.show_icon') }}</span>
        <XhSwitch v-model:checked="appStore.breadcrumbShowIcon" :disabled="breadcrumbItemDisabled" :aria-label="t('preference.layout.breadcrumb.show_icon')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': breadcrumbShowHomeDisabled }">
        <span>{{ t('preference.layout.breadcrumb.show_home') }}</span>
        <XhSwitch v-model:checked="appStore.breadcrumbShowHome" :disabled="breadcrumbShowHomeDisabled" :aria-label="t('preference.layout.breadcrumb.show_home')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': breadcrumbItemDisabled }">
        <span>{{ t('preference.layout.breadcrumb.style') }}</span>
        <XhToggleGroupRoot v-model:value="appStore.breadcrumbStyle" :disabled="breadcrumbItemDisabled" disallow-empty size="sm" :aria-label="t('preference.layout.breadcrumb.style')">
          <XhToggleGroupItem value="normal">
            {{ t('preference.layout.breadcrumb.style_normal') }}
          </XhToggleGroupItem>
          <XhToggleGroupItem value="background">
            {{ t('preference.layout.breadcrumb.style_background') }}
          </XhToggleGroupItem>
        </XhToggleGroupRoot>
      </div>
    </section>

    <!-- 标签栏 -->
    <section class="pref-card">
      <div class="section-title">
        {{ t('preference.layout.tabbar.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.tabbar.enabled') }}</span>
        <XhSwitch v-model:checked="appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.enabled')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.persist') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.persist_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.tabbarPersist" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.persist')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.visit_history') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.visit_history_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.tabbarVisitHistory" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.visit_history')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.max_count') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.max_count_tip')" />
        </div>
        <div class="flex items-center gap-1.5">
          <XNumberInput
            v-model:value="appStore.tabbarMaxCount"
            :min="0"
            :max="30"
            size="sm"
            class="pref-num pref-num--center"
            style="width: 120px"
            :disabled="!appStore.tabbarEnabled"
            :aria-label="t('preference.layout.tabbar.max_count')"
          />
          <span class="unit-label">{{ t('preference.layout.tabbar.max_count_unit') }}</span>
        </div>
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.draggable') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.draggable_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.tabbarDraggable" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.draggable')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.scroll_response') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.scroll_response_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.tabbarScrollResponse" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.scroll_response')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.tabbar.middle_click_close') }}</span>
          <PrefTip :content="t('preference.layout.tabbar.middle_click_close_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.tabbarMiddleClickClose" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.middle_click_close')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.show_icon') }}</span>
        <XhSwitch v-model:checked="appStore.tabbarShowIcon" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.show_icon')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.show_more') }}</span>
        <XhSwitch v-model:checked="appStore.tabbarShowMore" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.show_more')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.show_overview') }}</span>
        <XhSwitch v-model:checked="appStore.tabbarShowOverview" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.show_overview')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.show_maximize') }}</span>
        <XhSwitch v-model:checked="appStore.tabbarShowMaximize" :disabled="!appStore.tabbarEnabled" :aria-label="t('preference.layout.tabbar.show_maximize')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.tabbarEnabled }">
        <span>{{ t('preference.layout.tabbar.style') }}</span>
        <XSelect
          v-model:value="appStore.tabbarStyle"
          :options="tabbarStyleOptions"
          size="sm"
          style="width: 100px"
          :aria-label="t('preference.layout.tabbar.style')"
          :disabled="!appStore.tabbarEnabled"
        />
      </div>
    </section>

    <!-- 工具栏 -->
    <section class="pref-card">
      <div class="section-title">
        {{ t('preference.layout.widget.title') }}
      </div>
      <div class="pref-row">
        <div class="flex items-center gap-1">
          <span>{{ t('preference.layout.breadcrumb.nav_buttons') }}</span>
          <PrefTip :content="t('preference.layout.breadcrumb.nav_buttons_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.breadcrumbNavButtons" :aria-label="t('preference.layout.breadcrumb.nav_buttons')" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.refresh') }}</span>
        <XhSwitch v-model:checked="appStore.widgetRefresh" :aria-label="t('preference.layout.widget.refresh')" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.favorites') }}</span>
        <XhSwitch v-model:checked="appStore.widgetFavorites" :aria-label="t('preference.layout.widget.favorites')" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.sidebar_toggle') }}</span>
        <XhSwitch v-model:checked="appStore.widgetSidebarToggle" :aria-label="t('preference.layout.widget.sidebar_toggle')" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.global_search') }}</span>
        <XSelect
          v-model:value="appStore.widgetSearchPlacement"
          :options="widgetPlacementOptions"
          size="sm"
          style="width: 110px"
          :aria-label="t('preference.layout.widget.global_search')"
        />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.language_toggle') }}</span>
        <XSelect
          v-model:value="appStore.widgetLanguagePlacement"
          :options="widgetPlacementOptions"
          size="sm"
          style="width: 110px"
          :aria-label="t('preference.layout.widget.language_toggle')"
        />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.timezone_toggle') }}</span>
        <XSelect
          v-model:value="appStore.widgetTimezonePlacement"
          :options="widgetPlacementOptions"
          size="sm"
          style="width: 110px"
          :aria-label="t('preference.layout.widget.timezone_toggle')"
        />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.theme_toggle') }}</span>
        <XSelect
          v-model:value="appStore.widgetThemePlacement"
          :options="widgetPlacementOptions"
          size="sm"
          style="width: 110px"
          :aria-label="t('preference.layout.widget.theme_toggle')"
        />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.fullscreen') }}</span>
        <XSelect
          v-model:value="appStore.widgetFullscreenPlacement"
          :options="widgetPlacementOptions"
          size="sm"
          style="width: 110px"
          :aria-label="t('preference.layout.widget.fullscreen')"
        />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.preference_position') }}</span>
        <XSelect
          v-model:value="appStore.widgetPreferencePosition"
          :options="preferencePositionOptions"
          size="sm"
          style="width: 110px"
          :aria-label="t('preference.layout.widget.preference_position')"
        />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.notification') }}</span>
        <XhSwitch v-model:checked="appStore.widgetNotification" :aria-label="t('preference.layout.widget.notification')" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.widget.lock_screen') }}</span>
        <XhSwitch v-model:checked="appStore.widgetLockScreen" :aria-label="t('preference.layout.widget.lock_screen')" />
      </div>
    </section>

    <!-- 底栏 -->
    <section class="pref-card">
      <div class="section-title">
        {{ t('preference.layout.footer.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.layout.footer.show') }}</span>
        <XhSwitch v-model:checked="appStore.footerEnable" :aria-label="t('preference.layout.footer.show')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.footerEnable }">
        <span>{{ t('preference.layout.footer.fixed') }}</span>
        <XhSwitch v-model:checked="appStore.footerFixed" :disabled="!appStore.footerEnable" :aria-label="t('preference.layout.footer.fixed')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': !appStore.footerEnable }">
        <span>{{ t('preference.layout.footer.show_dev_info') }}</span>
        <XhSwitch v-model:checked="appStore.footerShowDevInfo" :disabled="!appStore.footerEnable" :aria-label="t('preference.layout.footer.show_dev_info')" />
      </div>
    </section>

    <!-- 版权 -->
    <section class="pref-card" :class="{ 'opacity-60': copyrightDisabled }">
      <div class="section-title">
        {{ t('preference.layout.copyright.title') }}
      </div>
      <div class="pref-row" :class="{ 'opacity-50': copyrightDisabled }">
        <span>{{ t('preference.layout.copyright.enabled') }}</span>
        <XhSwitch v-model:checked="appStore.copyrightEnable" :disabled="copyrightDisabled" :aria-label="t('preference.layout.copyright.enabled')" />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': copyrightItemDisabled }">
        <span>{{ t('preference.layout.copyright.name') }}</span>
        <XInput
          v-model:value="appStore.copyrightName"
          size="sm"
          style="width: 150px"
          class="pref-num pref-num--right"
          :disabled="copyrightItemDisabled"
          :aria-label="t('preference.layout.copyright.name')"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': copyrightItemDisabled }">
        <span>{{ t('preference.layout.copyright.site') }}</span>
        <XInput
          v-model:value="appStore.copyrightSite"
          size="sm"
          style="width: 150px"
          class="pref-num pref-num--right"
          :disabled="copyrightItemDisabled"
          :aria-label="t('preference.layout.copyright.site')"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': copyrightItemDisabled }">
        <span>{{ t('preference.layout.copyright.start_date') }}</span>
        <XInput
          v-model:value="appStore.copyrightDate"
          size="sm"
          style="width: 90px"
          class="pref-num pref-num--right"
          placeholder="2016"
          :disabled="copyrightItemDisabled"
          :aria-label="t('preference.layout.copyright.start_date')"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': copyrightItemDisabled }">
        <span>{{ t('preference.layout.copyright.icp') }}</span>
        <XInput
          v-model:value="appStore.copyrightIcp"
          size="sm"
          style="width: 150px"
          class="pref-num pref-num--right"
          :placeholder="t('preference.layout.copyright.optional')"
          :disabled="copyrightItemDisabled"
          :aria-label="t('preference.layout.copyright.icp')"
        />
      </div>
      <div class="pref-row" :class="{ 'opacity-50': copyrightItemDisabled }">
        <span>{{ t('preference.layout.copyright.icp_url') }}</span>
        <XInput
          v-model:value="appStore.copyrightIcpUrl"
          size="sm"
          style="width: 150px"
          class="pref-num pref-num--right"
          :placeholder="t('preference.layout.copyright.optional')"
          :disabled="copyrightItemDisabled"
          :aria-label="t('preference.layout.copyright.icp_url')"
        />
      </div>
    </section>
  </div>
</template>

<style scoped>
/* 每个预设的外层容器（触发区） */
.preset-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  cursor: pointer;
}

/* 标签文字在边框外部 */
.preset-label {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 3px;
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  text-align: center;
  line-height: 1.2;
  transition: color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.preset-item:hover .preset-label {
  color: hsl(var(--foreground));
}

.layout-preset-card {
  border: 1.5px solid hsl(var(--border));
  border-radius: var(--radius-card);
  background: hsl(var(--card));
  color: hsl(var(--foreground));
  padding: 0;
  width: 100%;
  overflow: hidden;
  text-align: center;
  transition:
    border-color var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    box-shadow var(--xh-motion-duration-enter) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-move) var(--xh-motion-ease-enter);
}

.layout-preset-card:hover {
  transform: translateY(-1px);
  border-color: color-mix(in srgb, hsl(var(--primary)) 45%, hsl(var(--border)));
}

.layout-preset-card.is-active {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 2px color-mix(in srgb, hsl(var(--primary)) 30%, transparent);
}

.layout-preset-preview {
  display: flex;
  height: 64px;
  width: 100%;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  background: hsl(var(--background));
}

/* 折叠/固定按钮的多选 toggle */
.btn-toggle {
  padding: 3px 10px;
  font-size: 12px;
  border-radius: var(--radius);
  border: 1.5px solid hsl(var(--border));
  background: hsl(var(--card));
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  transition: all var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.btn-toggle:hover {
  border-color: hsl(var(--primary) / 0.5);
  color: hsl(var(--foreground));
}

.btn-toggle.is-active {
  border-color: var(--xh-color-brand-600);
  background: var(--xh-color-brand-600);
  color: hsl(var(--primary-foreground));
  font-weight: 500;
}

/* 输入框里的文字对齐：数字框减钮在前、加钮在后，数字居中夹在两钮之间；input 由组件库渲染，只能经 :deep 够到 */
.pref-num--center :deep([data-scope='number-field'][data-part='input']) {
  text-align: center;
}

.pref-num--right :deep([data-scope='number-field'][data-part='input']),
.pref-num--right :deep([data-scope='text-field'][data-part='input']) {
  text-align: right;
}
</style>
