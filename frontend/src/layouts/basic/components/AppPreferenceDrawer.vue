<script setup lang="ts">
import { computed, ref } from 'vue'
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
} from 'naive-ui'
import { LAYOUT_MODE_OPTIONS } from '~/constants'
import { useAuthStore } from '@/store/auth'
import { useAppStore } from '~/stores'

defineOptions({ name: 'AppPreferenceDrawer' })

const appStore = useAppStore()
const authStore = useAuthStore()
const visible = ref(false)

const themePresets = ['#18a058', '#1677ff', '#6366f1', '#ec4899', '#f59e0b', '#4f46e5', '#10b981', '#334155', '#c2410c', '#b91c1c', '#3f3f46', '#1f2937']

const themeMode = computed({ get: () => appStore.themeMode, set: (v) => appStore.setTheme(v) })
const layoutMode = computed({ get: () => appStore.layoutMode, set: (v) => appStore.setLayoutMode(v) })
const themeColor = computed({ get: () => appStore.themeColor, set: (v) => appStore.setThemeColor(v) })
const uiRadius = computed({ get: () => appStore.uiRadius, set: (v) => appStore.setUiRadius(v) })
const fontSize = computed({ get: () => appStore.fontSize, set: (v) => appStore.setFontSize(v) })

function clearAndLogout() {
  localStorage.clear()
  sessionStorage.clear()
  authStore.logout()
}
</script>

<template>
  <NButton quaternary circle size="small" @click="visible = true">
    <template #icon><Icon icon="lucide:settings-2" width="18" /></template>
  </NButton>

  <NDrawer v-model:show="visible" :width="396" placement="right">
    <NDrawerContent title="偏好设置自定义偏好设置 & 实时预览" closable>
      <NTabs type="segment" animated>
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
                  :class="themeColor === color ? 'border-blue-500 ring-2 ring-blue-200' : 'border-gray-200'"
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
              <NRadioGroup v-model:value="layoutMode">
                <NSpace vertical>
                  <NRadioButton
                    v-for="item in LAYOUT_MODE_OPTIONS"
                    :key="item.value"
                    :value="item.value"
                  >
                    {{ item.label }}
                  </NRadioButton>
                </NSpace>
              </NRadioGroup>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">内容</div>
              <div class="mb-2 flex items-center justify-between"><span>流式</span><NSwitch v-model:value="appStore.contentCompact" /></div>
              <div class="text-xs text-gray-500">内容最大宽度</div>
              <NSlider v-model:value="appStore.contentMaxWidth" :min="960" :max="1600" :step="10" />
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">侧边栏</div>
              <div class="mb-2 flex items-center justify-between"><span>显示侧边栏</span><NSwitch v-model:value="appStore.sidebarShow" /></div>
              <div class="mb-2 flex items-center justify-between"><span>折叠菜单</span><NSwitch v-model:value="appStore.sidebarCollapsed" /></div>
              <div class="mb-2 flex items-center justify-between"><span>鼠标悬停展开</span><NSwitch v-model:value="appStore.sidebarExpandOnHover" /></div>
              <div class="mb-2 flex items-center justify-between"><span>自动激活子菜单</span><NSwitch v-model:value="appStore.sidebarAutoActivateChild" /></div>
              <div class="mb-2 flex items-center justify-between"><span>显示折叠按钮</span><NSwitch v-model:value="appStore.sidebarCollapseButton" /></div>
              <div class="text-xs text-gray-500">宽度</div>
              <NInputNumber v-model:value="appStore.sidebarWidth" :min="180" :max="320" />
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">面包屑导航</div>
              <div class="mb-2 flex items-center justify-between"><span>开启面包屑</span><NSwitch v-model:value="appStore.breadcrumbEnabled" /></div>
              <div class="mb-2 flex items-center justify-between"><span>仅有一个时隐藏</span><NSwitch v-model:value="appStore.breadcrumbHideOnlyOne" /></div>
              <div class="mb-2 flex items-center justify-between"><span>显示面包屑图标</span><NSwitch v-model:value="appStore.breadcrumbShowIcon" /></div>
              <div class="flex items-center justify-between"><span>显示首页按钮</span><NSwitch v-model:value="appStore.breadcrumbShowHome" /></div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">标签栏</div>
              <div class="mb-2 flex items-center justify-between"><span>启用标签栏</span><NSwitch v-model:value="appStore.tabbarEnabled" /></div>
              <div class="mb-2 flex items-center justify-between"><span>持久化标签页</span><NSwitch v-model:value="appStore.tabbarPersist" /></div>
              <div class="mb-2 flex items-center justify-between"><span>访问历史记录</span><NSwitch v-model:value="appStore.tabbarVisitHistory" /></div>
              <div class="mb-2 flex items-center justify-between"><span>最大标签数</span><NInputNumber v-model:value="appStore.tabbarMaxCount" :min="0" :max="30" /></div>
              <div class="mb-2 flex items-center justify-between"><span>启用拖拽排序</span><NSwitch v-model:value="appStore.tabbarDraggable" /></div>
              <div class="mb-2 flex items-center justify-between"><span>显示更多按钮</span><NSwitch v-model:value="appStore.tabbarShowMore" /></div>
              <div class="flex items-center justify-between"><span>显示最大化按钮</span><NSwitch v-model:value="appStore.tabbarShowMaximize" /></div>
            </NCard>
          </div>
        </NTabPane>

        <NTabPane name="shortcut" tab="快捷键">
          <NCard size="small">
            <div class="mb-2 font-medium">全局快捷键</div>
            <div class="mb-2 flex items-center justify-between"><span>启用快捷键</span><NSwitch v-model:value="appStore.shortcutEnable" /></div>
            <div class="mb-2 flex items-center justify-between"><span>全局搜索 (Ctrl+K)</span><NSwitch v-model:value="appStore.shortcutSearch" /></div>
            <div class="mb-2 flex items-center justify-between"><span>快速登出 (Ctrl+Shift+Q)</span><NSwitch v-model:value="appStore.shortcutLogout" /></div>
            <div class="flex items-center justify-between"><span>快速锁屏 (Ctrl+L)</span><NSwitch v-model:value="appStore.shortcutLock" /></div>
          </NCard>
        </NTabPane>

        <NTabPane name="general" tab="通用">
          <div class="space-y-4">
            <NCard size="small">
              <div class="mb-2 font-medium">通用</div>
              <div class="mb-2 flex items-center justify-between"><span>启用全局搜索</span><NSwitch v-model:value="appStore.searchEnabled" /></div>
              <div class="mb-2 flex items-center justify-between"><span>动态标题</span><NSwitch v-model:value="appStore.dynamicTitle" /></div>
              <div class="mb-2 flex items-center justify-between"><span>水印</span><NSwitch v-model:value="appStore.watermarkEnabled" /></div>
              <NInput v-if="appStore.watermarkEnabled" v-model:value="appStore.watermarkText" class="mb-2" placeholder="水印文案" />
              <div class="mb-2 flex items-center justify-between"><span>定时检查更新</span><NSwitch v-model:value="appStore.enableCheckUpdates" /></div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">动画</div>
              <div class="mb-2 flex items-center justify-between"><span>页面切换动画</span><NSwitch v-model:value="appStore.transitionEnable" /></div>
              <div class="mb-2 flex items-center justify-between"><span>主题切换动画</span><NSwitch v-model:value="appStore.themeAnimationEnabled" /></div>
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
              <div class="mb-2 flex items-center justify-between"><span>启用主题切换</span><NSwitch v-model:value="appStore.widgetThemeToggle" /></div>
              <div class="mb-2 flex items-center justify-between"><span>启用语言切换</span><NSwitch v-model:value="appStore.widgetLanguageToggle" /></div>
              <div class="mb-2 flex items-center justify-between"><span>启用全屏</span><NSwitch v-model:value="appStore.widgetFullscreen" /></div>
              <div class="mb-2 flex items-center justify-between"><span>启用通知</span><NSwitch v-model:value="appStore.widgetNotification" /></div>
              <div class="mb-2 flex items-center justify-between"><span>启用刷新</span><NSwitch v-model:value="appStore.widgetRefresh" /></div>
              <div class="mb-2 flex items-center justify-between"><span>启用侧栏切换</span><NSwitch v-model:value="appStore.widgetSidebarToggle" /></div>
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">底栏 & 版权</div>
              <div class="mb-2 flex items-center justify-between"><span>显示底栏</span><NSwitch v-model:value="appStore.footerEnable" /></div>
              <div class="mb-2 flex items-center justify-between"><span>固定在底部</span><NSwitch v-model:value="appStore.footerFixed" /></div>
              <div class="mb-2 flex items-center justify-between"><span>启用版权</span><NSwitch v-model:value="appStore.copyrightEnable" /></div>
              <NInput v-model:value="appStore.copyrightCompany" class="mb-2" placeholder="公司名" />
              <NInput v-model:value="appStore.copyrightSite" placeholder="公司主页" />
            </NCard>

            <NCard size="small">
              <div class="mb-2 font-medium">辅助模式</div>
              <div class="mb-2 flex items-center justify-between"><span>灰色模式</span><NSwitch v-model:value="appStore.grayscaleEnabled" /></div>
              <div class="flex items-center justify-between"><span>色弱模式</span><NSwitch v-model:value="appStore.colorWeaknessEnabled" /></div>
            </NCard>
          </div>
        </NTabPane>
      </NTabs>
      <template #footer>
        <NSpace justify="space-between">
          <NButton type="primary">复制偏好设置</NButton>
          <NButton @click="clearAndLogout">清空缓存 & 退出登录</NButton>
        </NSpace>
      </template>
    </NDrawerContent>
  </NDrawer>
</template>
