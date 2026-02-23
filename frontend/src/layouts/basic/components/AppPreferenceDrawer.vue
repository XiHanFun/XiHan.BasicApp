<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { NButton, NDrawer, NDrawerContent, NIcon, NSpace, NTabPane, NTabs, useMessage } from 'naive-ui'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '@/store/auth'
import { STORAGE_PREFIX } from '~/constants'
import { useTheme } from '~/hooks'
import { useAppStore } from '~/stores'
import PreferenceAppearanceTab from './preference/PreferenceAppearanceTab.vue'
import PreferenceFab from './preference/PreferenceFab.vue'
import PreferenceGeneralTab from './preference/PreferenceGeneralTab.vue'
import PreferenceLayoutTab from './preference/PreferenceLayoutTab.vue'
import PreferenceShortcutTab from './preference/PreferenceShortcutTab.vue'

defineOptions({ name: 'AppPreferenceDrawer' })

const appStore = useAppStore()
const authStore = useAuthStore()
const message = useMessage()
const { t } = useI18n()
const visible = ref(false)
const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
const isNarrowScreen = computed(() => viewportWidth.value < 960)
const contentMaximized = ref(false)
const showFloatingFab = computed(
  () => isNarrowScreen.value || contentMaximized.value || !appStore.headerShow,
)
const { animateThemeTransition, followSystem } = useTheme()

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
  { key: 'side', label: t('preference.layout.preset.side') },
  { key: 'side-mixed', label: t('preference.layout.preset.side_mixed') },
  { key: 'top', label: t('preference.layout.preset.top') },
  { key: 'mix', label: t('preference.layout.preset.mix') },
  { key: 'header-mix', label: t('preference.layout.preset.header_mix') },
  { key: 'header-sidebar', label: t('preference.layout.preset.header_sidebar') },
  { key: 'full', label: t('preference.layout.preset.full') },
])

function clearAndLogout() {
  localStorage.clear()
  sessionStorage.clear()
  authStore.logout()
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
  const keys = Object.keys(localStorage).filter(key => key.startsWith(STORAGE_PREFIX))
  for (const key of keys) {
    localStorage.removeItem(key)
  }
  message.success(t('preference.drawer.reset_success'))
  window.location.reload()
}

function openDrawer() {
  visible.value = true
}

function handleFabClick() {
  openDrawer()
}

function handleOpenPreferenceDrawer() {
  openDrawer()
}

function handleThemeModeChange(value: 'light' | 'dark' | 'auto') {
  if (value === 'auto') {
    followSystem()
    return
  }
  animateThemeTransition(value)
}

function handleLayoutModeChange(value: string) {
  layoutMode.value = value
}

function handleContentModeChange(value: 'fixed' | 'fluid') {
  contentMode.value = value
}

function handleViewportResize() {
  viewportWidth.value = window.innerWidth
}

function handleContentMaximizedChange(event: Event) {
  const customEvent = event as CustomEvent<boolean>
  contentMaximized.value = Boolean(customEvent.detail)
}

onMounted(() => {
  handleViewportResize()
  window.addEventListener('resize', handleViewportResize)
  window.addEventListener('xihan-content-maximized-change', handleContentMaximizedChange)
  window.addEventListener('xihan-open-preference-drawer', handleOpenPreferenceDrawer)
  window.dispatchEvent(new CustomEvent('xihan-sync-content-maximize-state'))
})

onUnmounted(() => {
  window.removeEventListener('resize', handleViewportResize)
  window.removeEventListener('xihan-content-maximized-change', handleContentMaximizedChange)
  window.removeEventListener('xihan-open-preference-drawer', handleOpenPreferenceDrawer)
})
</script>

<template>
  <PreferenceFab :show="showFloatingFab" @click="handleFabClick" />

  <NDrawer v-model:show="visible" :width="396" placement="right">
    <NDrawerContent
      class="preference-drawer-content"
      :body-content-style="{ paddingTop: '0px' }"
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
      <template #footer>
        <NSpace justify="end">
          <NButton circle type="primary" secondary :title="t('preference.drawer.copy')" @click="copyPreferences">
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
:deep(.preference-drawer-content .n-drawer-body-content-wrapper) {
  padding-top: 0 !important;
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
