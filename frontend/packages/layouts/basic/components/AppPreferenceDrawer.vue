<script setup lang="ts">
import {
  NButton,
  NDrawer,
  NDrawerContent,
  NIcon,
  NScrollbar,
  NSpace,
  NTabPane,
  NTabs,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  LAYOUT_EVENT_OPEN_PREFERENCE_DRAWER,
} from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppStore, useAuthStore, useLayoutBridgeStore } from '~/stores'
import { usePreferenceEntry } from '../composables'
import PreferenceAppearanceTab from './preference/PreferenceAppearanceTab.vue'
import PreferenceFab from './preference/PreferenceFab.vue'
import PreferenceGeneralTab from './preference/PreferenceGeneralTab.vue'
import PreferenceLayoutTab from './preference/PreferenceLayoutTab.vue'
import PreferenceShortcutTab from './preference/PreferenceShortcutTab.vue'

defineOptions({ name: 'AppPreferenceDrawer' })

const appStore = useAppStore()
const authStore = useAuthStore()
const layoutBridgeStore = useLayoutBridgeStore()
const message = useMessage()
const { t, locale: i18nLocale } = useI18n()
const visible = ref(false)
// 偏好设置入口：头部按钮与悬浮 FAB 互斥，统一由 usePreferenceEntry 判定（auto 模式窄屏走 FAB）
const { showFab: showFloatingFab } = usePreferenceEntry()
const { animateThemeTransition } = useTheme()

const themeMode = computed(() => appStore.themeMode)
const layoutMode = computed({
  get: () => appStore.layoutMode,
  set: v => appStore.setLayoutMode(v),
})
const contentMode = computed({
  get: () => (appStore.contentCompact ? 'fixed' : 'fluid'),
  set: (value: 'fixed' | 'fluid') => appStore.setContentCompact(value === 'fixed'),
})

const layoutPresets = computed(() => [
  {
    key: 'side',
    label: t('preference.layout.preset.side'),
    tip: t('preference.layout.preset_tip.side'),
  },
  {
    key: 'side-mixed',
    label: t('preference.layout.preset.side_mixed'),
    tip: t('preference.layout.preset_tip.side_mixed'),
  },
  {
    key: 'top',
    label: t('preference.layout.preset.top'),
    tip: t('preference.layout.preset_tip.top'),
  },
  {
    key: 'mix',
    label: t('preference.layout.preset.mix'),
    tip: t('preference.layout.preset_tip.mix'),
  },
  {
    key: 'header-mix',
    label: t('preference.layout.preset.header_mix'),
    tip: t('preference.layout.preset_tip.header_mix'),
  },
  {
    key: 'header-sidebar',
    label: t('preference.layout.preset.header_sidebar'),
    tip: t('preference.layout.preset_tip.header_sidebar'),
  },
  {
    key: 'full',
    label: t('preference.layout.preset.full'),
    tip: t('preference.layout.preset_tip.full'),
  },
])

async function clearAndLogout() {
  localStorage.clear()
  sessionStorage.clear()
  await authStore.logout()
}

async function copyPreferences() {
  try {
    await navigator.clipboard.writeText(JSON.stringify(appStore.$state, null, 2))
    message.success(t('preference.drawer.copy_success'))
  }
  catch {
    message.error(t('preference.drawer.copy_failed'))
  }
}

function resetPreferences() {
  // 仅重置「偏好设置」为默认值（内存级，经各偏好 watch 落地本地并同步后端）；
  // 不清理 token / 用户信息 / 收藏 / 标签，也不刷新整页，因此不会登出
  appStore.resetPreferences()
  // 语言为「动作型」偏好（vue-i18n 不响应式跟随 store），重置后手动同步当前界面语言
  i18nLocale.value = appStore.locale
  message.success(t('preference.drawer.reset_success'))
}

function openDrawer() {
  visible.value = true
}

function handleFabClick() {
  layoutBridgeStore.requestOpenPreferenceDrawer()
}

function handleOpenPreferenceDrawer() {
  layoutBridgeStore.requestOpenPreferenceDrawer()
}

function handleThemeModeChange(value: 'light' | 'dark' | 'auto') {
  // 三种模式统一走扩散动画：auto 由 animateThemeTransition 内部按系统主题决定方向并落地为跟随系统
  animateThemeTransition(value)
}

function handleLayoutModeChange(value: string) {
  layoutMode.value = value
}

function handleContentModeChange(value: 'fixed' | 'fluid') {
  contentMode.value = value
}

onMounted(() => {
  window.addEventListener(LAYOUT_EVENT_OPEN_PREFERENCE_DRAWER, handleOpenPreferenceDrawer)
})

onUnmounted(() => {
  window.removeEventListener(LAYOUT_EVENT_OPEN_PREFERENCE_DRAWER, handleOpenPreferenceDrawer)
})

watch(
  () => layoutBridgeStore.preferenceDrawerVersion,
  () => {
    openDrawer()
  },
)
</script>

<template>
  <PreferenceFab :show="showFloatingFab" @click="handleFabClick" />

  <NDrawer v-model:show="visible" :width="396" placement="right">
    <NDrawerContent
      class="preference-drawer-content"
      :body-content-style="{ padding: 0, overflow: 'hidden', height: '100%' }"
    >
      <template #header>
        <div class="drawer-header">
          <span class="drawer-title">{{ t('preference.drawer.title') }}</span>
          <button
            tabindex="-1"
            class="close-btn"
            :aria-label="t('common.close')"
            @click="visible = false"
          >
            <NIcon size="16">
              <Icon icon="lucide:x" />
            </NIcon>
          </button>
        </div>
      </template>
      <NScrollbar class="preference-scrollbar">
        <NTabs class="preference-tabs" type="segment" animated>
          <NTabPane name="appearance" :tab="t('preference.drawer.tab.appearance')">
            <PreferenceAppearanceTab
              :app-store="appStore"
              :theme-mode="themeMode"
              @theme-mode-change="handleThemeModeChange"
            />
          </NTabPane>

          <NTabPane name="layout" :tab="t('preference.drawer.tab.layout')">
            <PreferenceLayoutTab
              :app-store="appStore"
              :layout-mode="layoutMode"
              :content-mode="contentMode"
              :layout-presets="layoutPresets"
              @layout-mode-change="handleLayoutModeChange"
              @content-mode-change="handleContentModeChange"
            />
          </NTabPane>

          <NTabPane name="shortcut" :tab="t('preference.drawer.tab.shortcut')">
            <PreferenceShortcutTab :app-store="appStore" />
          </NTabPane>

          <NTabPane name="general" :tab="t('preference.drawer.tab.general')">
            <PreferenceGeneralTab :app-store="appStore" />
          </NTabPane>
        </NTabs>
      </NScrollbar>
      <template #footer>
        <NSpace justify="end">
          <NButton
            circle
            type="primary"
            secondary
            :title="t('preference.drawer.copy')"
            @click="copyPreferences"
          >
            <template #icon>
              <Icon icon="lucide:copy" width="16" />
            </template>
          </NButton>
          <NButton circle :title="t('preference.drawer.reset')" @click="resetPreferences">
            <template #icon>
              <Icon icon="lucide:rotate-ccw" width="16" />
            </template>
          </NButton>
          <NButton circle :title="t('preference.drawer.clear_cache')" @click="clearAndLogout">
            <template #icon>
              <Icon icon="lucide:trash-2" width="16" />
            </template>
          </NButton>
        </NSpace>
      </template>
    </NDrawerContent>
  </NDrawer>
</template>

<style scoped>
.preference-drawer-content {
  font-size: 14px;
}

:deep(.n-drawer-footer) {
  padding: 8px 16px !important;
}

:deep(.preference-scrollbar) {
  height: 100%;
}

:deep(.preference-scrollbar .n-scrollbar-content) {
  padding: 0 16px 16px;
}

:deep(.preference-tabs > .n-tabs-nav) {
  position: sticky;
  top: 0;
  z-index: 10;
  padding-top: 12px;
  padding-bottom: 4px;
  background: var(--n-color);
}

:deep(.preference-tabs > .n-tabs-nav .n-tabs-rail) {
  margin-top: 0;
}

/* 自定义头部 */
.drawer-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

.drawer-title {
  font-size: 16px;
  font-weight: 600;
  color: hsl(var(--foreground));
}

/* 自定义关闭按钮 */
.close-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  transition:
    background 0.15s ease,
    color 0.15s ease;
  outline: none;
}

.close-btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.close-btn:active {
  background: hsl(var(--accent) / 0.7);
}
</style>
