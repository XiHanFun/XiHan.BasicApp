<script setup lang="ts">
import { Icon } from '@iconify/vue'
import {
  NButton,
  NDrawer,
  NDrawerContent,
  NSpace,
  NTabPane,
  NTabs,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, onUnmounted, ref } from 'vue'
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
const visible = ref(false)
const viewportWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1200)
const isNarrowScreen = computed(() => viewportWidth.value < 960)
const contentMaximized = ref(false)
const showFloatingFab = computed(() => isNarrowScreen.value || contentMaximized.value)
const { animateThemeTransition, followSystem } = useTheme()

const themePresets = [
  '#18a058',
  '#1677ff',
  '#6366f1',
  '#ec4899',
  '#f59e0b',
  '#4f46e5',
  '#10b981',
  '#334155',
  '#c2410c',
  '#b91c1c',
  '#3f3f46',
  '#1f2937',
]

const themeMode = computed(() => appStore.themeMode)
const layoutMode = computed({
  get: () => appStore.layoutMode,
  set: v => appStore.setLayoutMode(v),
})
const contentMode = computed({
  get: () => (appStore.contentCompact ? 'fixed' : 'fluid'),
  set: (value: 'fixed' | 'fluid') => appStore.setContentCompact(value === 'fixed'),
})

const layoutPresets = [
  { key: 'side', label: '垂直' },
  { key: 'side-mixed', label: '双列菜单' },
  { key: 'top', label: '水平' },
  { key: 'header-sidebar', label: '侧边导航' },
  { key: 'mix', label: '混合垂直' },
  { key: 'header-mix', label: '混合双列' },
  { key: 'full', label: '内容全屏' },
] as const

function clearAndLogout() {
  localStorage.clear()
  sessionStorage.clear()
  authStore.logout()
}

async function copyPreferences() {
  try {
    await navigator.clipboard.writeText(JSON.stringify(appStore.$state, null, 2))
    message.success('偏好设置已复制')
  }
  catch {
    message.error('复制失败')
  }
}

function resetPreferences() {
  const keys = Object.keys(localStorage).filter(key => key.startsWith(STORAGE_PREFIX))
  for (const key of keys) {
    localStorage.removeItem(key)
  }
  message.success('偏好设置已重置，正在刷新')
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
      title="偏好设置"
      closable
      :body-content-style="{ paddingTop: '0px' }"
    >
      <NTabs class="preference-tabs" type="segment" animated>
        <NTabPane name="appearance" tab="外观">
          <PreferenceAppearanceTab
            :app-store="appStore"
            :theme-mode="themeMode"
            :theme-presets="themePresets"
            @theme-mode-change="handleThemeModeChange"
          />
        </NTabPane>

        <NTabPane name="layout" tab="布局">
          <PreferenceLayoutTab
            :app-store="appStore"
            :layout-mode="layoutMode"
            :content-mode="contentMode"
            :layout-presets="layoutPresets"
            @layout-mode-change="handleLayoutModeChange"
            @content-mode-change="handleContentModeChange"
          />
        </NTabPane>

        <NTabPane name="shortcut" tab="快捷键">
          <PreferenceShortcutTab :app-store="appStore" />
        </NTabPane>

        <NTabPane name="general" tab="通用">
          <PreferenceGeneralTab :app-store="appStore" />
        </NTabPane>
      </NTabs>
      <template #footer>
        <NSpace justify="end">
          <NButton circle type="primary" secondary title="复制偏好设置" @click="copyPreferences">
            <template #icon>
              <Icon icon="lucide:copy" width="16" />
            </template>
          </NButton>
          <NButton circle title="重置偏好" @click="resetPreferences">
            <template #icon>
              <Icon icon="lucide:rotate-ccw" width="16" />
            </template>
          </NButton>
          <NButton circle title="清空缓存" @click="clearAndLogout">
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
  margin-top: 0;
  padding-top: 0;
  background: var(--n-color);
}

:deep(.preference-tabs > .n-tabs-nav .n-tabs-rail) {
  margin-top: 0;
}
</style>
