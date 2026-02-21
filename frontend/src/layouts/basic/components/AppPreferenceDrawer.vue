<script setup lang="ts">
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCard,
  NColorPicker,
  NDrawer,
  NDrawerContent,
  NInput,
  NInputNumber,
  NRadioButton,
  NRadioGroup,
  NSlider,
  NSpace,
  NSwitch,
  NTabPane,
  NTabs,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useAuthStore } from '@/store/auth'
import { STORAGE_PREFIX } from '~/constants'
import { useAppStore } from '~/stores'

defineOptions({ name: 'AppPreferenceDrawer' })

const appStore = useAppStore()
const authStore = useAuthStore()
const message = useMessage()
const visible = ref(false)

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

const themeMode = computed({ get: () => appStore.themeMode, set: (v) => appStore.setTheme(v) })
const layoutMode = computed({
  get: () => appStore.layoutMode,
  set: (v) => appStore.setLayoutMode(v),
})
const themeColor = computed({
  get: () => appStore.themeColor,
  set: (v) => appStore.setThemeColor(v),
})
const uiRadius = computed({ get: () => appStore.uiRadius, set: (v) => appStore.setUiRadius(v) })
const fontSize = computed({ get: () => appStore.fontSize, set: (v) => appStore.setFontSize(v) })
const contentMode = computed({
  get: () => (appStore.contentCompact ? 'fixed' : 'fluid'),
  set: (value: 'fixed' | 'fluid') => appStore.setContentCompact(value === 'fixed'),
})

const layoutPresets = [
  { key: 'side', label: '垂直', icon: 'ph:layout-bold' },
  { key: 'side-mixed', label: '双列菜单', icon: 'ph:columns-plus-left-bold' },
  { key: 'top', label: '水平', icon: 'ph:rows-bold' },
  { key: 'header-sidebar', label: '侧边导航', icon: 'ph:split-horizontal-bold' },
  { key: 'mix', label: '混合垂直', icon: 'ph:layout-bold' },
  { key: 'header-mix', label: '混合双列', icon: 'ph:columns-bold' },
  { key: 'full', label: '内容全屏', icon: 'ph:arrows-out-simple-bold' },
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
  } catch {
    message.error('复制失败')
  }
}

function resetPreferences() {
  const keys = Object.keys(localStorage).filter((key) => key.startsWith(STORAGE_PREFIX))
  for (const key of keys) {
    localStorage.removeItem(key)
  }
  message.success('偏好设置已重置，正在刷新')
  window.location.reload()
}

function openDrawer() {
  visible.value = true
}

function handleOpenPreferenceDrawer() {
  openDrawer()
}

onMounted(() => {
  window.addEventListener('xihan-open-preference-drawer', handleOpenPreferenceDrawer)
})

onUnmounted(() => {
  window.removeEventListener('xihan-open-preference-drawer', handleOpenPreferenceDrawer)
})
</script>

<template>
  <NButton quaternary circle size="small" @click="openDrawer">
    <template #icon>
      <Icon icon="lucide:settings-2" width="18" />
    </template>
  </NButton>

  <NDrawer v-model:show="visible" :width="396" placement="right">
    <NDrawerContent
      class="preference-drawer-content"
      title="偏好设置"
      closable
      :body-content-style="{ paddingTop: '0px' }"
    >
      <NTabs class="preference-tabs" type="segment" animated>
        <NTabPane name="appearance" tab="外观">
          <div class="space-y-5">
            <NCard size="small">
              <div class="mb-2 font-medium">主题</div>
              <NRadioGroup v-model:value="themeMode">
                <NSpace>
                  <NRadioButton value="light">浅色</NRadioButton>
                  <NRadioButton value="dark">深色</NRadioButton>
                  <NRadioButton value="auto">跟随系统</NRadioButton>
                </NSpace>
              </NRadioGroup>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">内置主题</div>
              <div class="grid grid-cols-4 gap-2">
                <button
                  v-for="color in themePresets"
                  :key="color"
                  type="button"
                  class="relative h-10 rounded border"
                  :class="
                    themeColor === color
                      ? 'border-blue-500 ring-2 ring-blue-200'
                      : 'border-gray-200'
                  "
                  :style="{ backgroundColor: color }"
                  @click="themeColor = color"
                >
                  <Icon
                    v-if="themeColor === color"
                    icon="lucide:check"
                    width="14"
                    class="absolute right-1 top-1 text-white"
                  />
                </button>
              </div>
              <div class="mt-2">
                <NColorPicker v-model:value="themeColor" :modes="['hex']" />
              </div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">圆角</div>
              <NRadioGroup v-model:value="uiRadius">
                <NSpace>
                  <NRadioButton :value="0">0</NRadioButton>
                  <NRadioButton :value="0.25">0.25</NRadioButton>
                  <NRadioButton :value="0.5">0.5</NRadioButton>
                  <NRadioButton :value="0.75">0.75</NRadioButton>
                  <NRadioButton :value="1">1</NRadioButton>
                </NSpace>
              </NRadioGroup>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">字体大小</div>
              <NInputNumber v-model:value="fontSize" :min="12" :max="20" />
            </NCard>
          </div>
        </NTabPane>

        <NTabPane name="layout" tab="布局">
          <div class="space-y-4">
            <NCard size="small">
              <div class="mb-2 font-medium">布局</div>
              <div class="grid grid-cols-3 gap-3">
                <button
                  v-for="item in layoutPresets"
                  :key="item.key"
                  type="button"
                  class="layout-preset-card"
                  :class="layoutMode === item.key ? 'is-active' : ''"
                  @click="layoutMode = item.key"
                >
                  <div class="layout-preset-preview">
                    <Icon :icon="item.icon" width="26" />
                  </div>
                  <div class="mt-2 text-xs">{{ item.label }}</div>
                </button>
              </div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">内容</div>
              <div class="mb-3 grid grid-cols-2 gap-3">
                <button
                  type="button"
                  class="layout-preset-card"
                  :class="contentMode === 'fluid' ? 'is-active' : ''"
                  @click="contentMode = 'fluid'"
                >
                  <div class="layout-preset-preview">
                    <Icon icon="ph:arrows-out-cardinal-bold" width="24" />
                  </div>
                  <div class="mt-2 text-xs">流式</div>
                </button>
                <button
                  type="button"
                  class="layout-preset-card"
                  :class="contentMode === 'fixed' ? 'is-active' : ''"
                  @click="contentMode = 'fixed'"
                >
                  <div class="layout-preset-preview">
                    <Icon icon="ph:bounding-box-bold" width="24" />
                  </div>
                  <div class="mt-2 text-xs">定宽</div>
                </button>
              </div>
              <div class="text-xs text-gray-500">内容最大宽度</div>
              <NSlider v-model:value="appStore.contentMaxWidth" :min="960" :max="1600" :step="10" />
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">顶栏</div>
              <div class="mb-2 flex items-center justify-between">
                <span>模式</span>
                <NRadioGroup v-model:value="appStore.headerMode">
                  <NSpace>
                    <NRadioButton value="fixed">固定</NRadioButton>
                    <NRadioButton value="static">静态</NRadioButton>
                  </NSpace>
                </NRadioGroup>
              </div>
              <div class="flex items-center justify-between">
                <span>菜单位置</span>
                <NRadioGroup v-model:value="appStore.headerMenuAlign">
                  <NSpace>
                    <NRadioButton value="left">左侧</NRadioButton>
                    <NRadioButton value="center">居中</NRadioButton>
                    <NRadioButton value="right">右侧</NRadioButton>
                  </NSpace>
                </NRadioGroup>
              </div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">导航菜单</div>
              <div class="mb-2 flex items-center justify-between">
                <span>导航菜单风格</span>
                <NRadioGroup v-model:value="appStore.navigationStyle">
                  <NSpace>
                    <NRadioButton value="rounded">圆润</NRadioButton>
                    <NRadioButton value="plain">朴素</NRadioButton>
                  </NSpace>
                </NRadioGroup>
              </div>
              <div class="flex items-center justify-between">
                <span>导航菜单分离</span>
                <NSwitch v-model:value="appStore.navigationSplit" />
              </div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">侧边栏</div>
              <div class="mb-2 flex items-center justify-between">
                <span>显示侧边栏</span>
                <NSwitch v-model:value="appStore.sidebarShow" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>折叠菜单</span>
                <NSwitch v-model:value="appStore.sidebarCollapsed" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>鼠标悬停展开</span>
                <NSwitch v-model:value="appStore.sidebarExpandOnHover" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>自动激活子菜单</span>
                <NSwitch v-model:value="appStore.sidebarAutoActivateChild" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>显示折叠按钮</span>
                <NSwitch v-model:value="appStore.sidebarCollapseButton" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>折叠显示菜单名</span>
                <NSwitch v-model:value="appStore.sidebarCollapsedShowTitle" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>固定按钮</span>
                <NSwitch v-model:value="appStore.sidebarFixedButton" />
              </div>
              <div class="text-xs text-gray-500">宽度</div>
              <NInputNumber v-model:value="appStore.sidebarWidth" :min="180" :max="320" />
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">面包屑导航</div>
              <div class="mb-2 flex items-center justify-between">
                <span>开启面包屑</span>
                <NSwitch v-model:value="appStore.breadcrumbEnabled" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>仅有一个时隐藏</span>
                <NSwitch v-model:value="appStore.breadcrumbHideOnlyOne" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>显示面包屑图标</span>
                <NSwitch v-model:value="appStore.breadcrumbShowIcon" />
              </div>
              <div class="flex items-center justify-between">
                <span>显示首页按钮</span>
                <NSwitch v-model:value="appStore.breadcrumbShowHome" />
              </div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">标签栏</div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用标签栏</span>
                <NSwitch v-model:value="appStore.tabbarEnabled" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>持久化标签页</span>
                <NSwitch v-model:value="appStore.tabbarPersist" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>访问历史记录</span>
                <NSwitch v-model:value="appStore.tabbarVisitHistory" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>最大标签数</span>
                <NInputNumber v-model:value="appStore.tabbarMaxCount" :min="0" :max="30" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用拖拽排序</span>
                <NSwitch v-model:value="appStore.tabbarDraggable" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>显示更多按钮</span>
                <NSwitch v-model:value="appStore.tabbarShowMore" />
              </div>
              <div class="flex items-center justify-between">
                <span>显示最大化按钮</span>
                <NSwitch v-model:value="appStore.tabbarShowMaximize" />
              </div>
            </NCard>
          </div>
        </NTabPane>

        <NTabPane name="shortcut" tab="快捷键">
          <NCard size="small">
            <div class="mb-2 font-medium">全局快捷键</div>
            <div class="mb-2 flex items-center justify-between">
              <span>启用快捷键</span>
              <NSwitch v-model:value="appStore.shortcutEnable" />
            </div>
            <div class="mb-2 flex items-center justify-between">
              <span>全局搜索 (Ctrl+K)</span>
              <NSwitch v-model:value="appStore.shortcutSearch" />
            </div>
            <div class="mb-2 flex items-center justify-between">
              <span>快速登出 (Ctrl+Shift+Q)</span>
              <NSwitch v-model:value="appStore.shortcutLogout" />
            </div>
            <div class="flex items-center justify-between">
              <span>快速锁屏 (Ctrl+L)</span>
              <NSwitch v-model:value="appStore.shortcutLock" />
            </div>
          </NCard>
        </NTabPane>

        <NTabPane name="general" tab="通用">
          <div class="space-y-4">
            <NCard size="small">
              <div class="mb-2 font-medium">通用</div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用全局搜索</span>
                <NSwitch v-model:value="appStore.searchEnabled" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>动态标题</span>
                <NSwitch v-model:value="appStore.dynamicTitle" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>水印</span>
                <NSwitch v-model:value="appStore.watermarkEnabled" />
              </div>
              <NInput
                v-if="appStore.watermarkEnabled"
                v-model:value="appStore.watermarkText"
                class="mb-2"
                placeholder="水印文案"
              />
              <div class="mb-2 flex items-center justify-between">
                <span>定时检查更新</span>
                <NSwitch v-model:value="appStore.enableCheckUpdates" />
              </div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">动画</div>
              <div class="mb-2 flex items-center justify-between">
                <span>页面切换动画</span>
                <NSwitch v-model:value="appStore.transitionEnable" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>主题切换动画</span>
                <NSwitch v-model:value="appStore.themeAnimationEnabled" />
              </div>
              <NRadioGroup v-model:value="appStore.transitionName">
                <NSpace>
                  <NRadioButton value="fade">淡入淡出</NRadioButton>
                  <NRadioButton value="slide-left">左右滑动</NRadioButton>
                  <NRadioButton value="zoom">缩放</NRadioButton>
                </NSpace>
              </NRadioGroup>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">小部件</div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用主题切换</span>
                <NSwitch v-model:value="appStore.widgetThemeToggle" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用语言切换</span>
                <NSwitch v-model:value="appStore.widgetLanguageToggle" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用全屏</span>
                <NSwitch v-model:value="appStore.widgetFullscreen" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用通知</span>
                <NSwitch v-model:value="appStore.widgetNotification" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用刷新</span>
                <NSwitch v-model:value="appStore.widgetRefresh" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用侧栏切换</span>
                <NSwitch v-model:value="appStore.widgetSidebarToggle" />
              </div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">底栏 & 版权</div>
              <div class="mb-2 flex items-center justify-between">
                <span>显示底栏</span>
                <NSwitch v-model:value="appStore.footerEnable" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>固定在底部</span>
                <NSwitch v-model:value="appStore.footerFixed" />
              </div>
              <div class="mb-2 flex items-center justify-between">
                <span>启用版权</span>
                <NSwitch v-model:value="appStore.copyrightEnable" />
              </div>
              <NInput v-model:value="appStore.copyrightCompany" class="mb-2" placeholder="公司名" />
              <NInput v-model:value="appStore.copyrightSite" placeholder="公司主页" />
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">辅助模式</div>
              <div class="mb-2 flex items-center justify-between">
                <span>灰色模式</span>
                <NSwitch v-model:value="appStore.grayscaleEnabled" />
              </div>
              <div class="flex items-center justify-between">
                <span>色弱模式</span>
                <NSwitch v-model:value="appStore.colorWeaknessEnabled" />
              </div>
            </NCard>
          </div>
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

.layout-preset-card {
  border: 1px solid rgb(229 231 235);
  border-radius: 10px;
  background: rgb(249 250 251);
  padding: 8px;
  text-align: center;
  transition: all 0.2s ease;
}

.layout-preset-card:hover {
  transform: translateY(-1px);
  border-color: rgb(59 130 246 / 45%);
  background: rgb(243 244 246);
}

.layout-preset-card.is-active {
  border-color: rgb(59 130 246);
  box-shadow: 0 0 0 2px rgb(191 219 254 / 70%);
}

.layout-preset-preview {
  display: flex;
  height: 54px;
  align-items: center;
  justify-content: center;
  border-radius: 8px;
  background: white;
  color: rgb(59 130 246);
}

:global(.dark) .layout-preset-card {
  border-color: rgb(55 65 81);
  background: rgb(31 41 55);
}

:global(.dark) .layout-preset-card:hover {
  border-color: rgb(59 130 246 / 60%);
  background: rgb(17 24 39);
}

:global(.dark) .layout-preset-preview {
  background: rgb(17 24 39);
}
</style>
