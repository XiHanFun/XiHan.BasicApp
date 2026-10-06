<script setup lang="ts">
import {
  XhButton,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerRoot,
  XhDrawerTitle,
  XhScrollAreaContent,
  XhScrollAreaRoot,
  XhScrollAreaScrollbar,
  XhScrollAreaThumb,
  XhScrollAreaTrack,
  XhScrollAreaViewport,
  XhTabsContent,
  XhTabsIndicator,
  XhTabsList,
  XhTabsRoot,
  XhTabsTrigger,
} from '@xihan-ui/vue'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import SyncStatusBadge from '~/components/common/SyncStatusBadge.vue'
import { toast } from '~/composables'
import {
  LAYOUT_EVENT_OPEN_PREFERENCE_DRAWER,
} from '~/constants'
import { useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppStore, useAuthStore, useLayoutBridgeStore } from '~/stores'
import PreferenceAppearanceTab from './preference/PreferenceAppearanceTab.vue'
import PreferenceGeneralTab from './preference/PreferenceGeneralTab.vue'
import PreferenceLayoutTab from './preference/PreferenceLayoutTab.vue'
import PreferenceShortcutTab from './preference/PreferenceShortcutTab.vue'

defineOptions({ name: 'AppPreferenceDrawer' })

const appStore = useAppStore()
const authStore = useAuthStore()
const layoutBridgeStore = useLayoutBridgeStore()
const { t } = useI18n()
const visible = ref(false)
const activeTab = ref('appearance')
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
    toast.success(t('preference.drawer.copy_success'))
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('preference.drawer.copy_failed'))
  }
}

function resetPreferences() {
  // 草稿模式下仅把内存值恢复默认（实时预览），需点击「保存」才落地；关闭不保存则还原
  appStore.resetPreferences()
  toast.success(t('preference.drawer.reset_success'))
}

/** 保存草稿：把当前预览的偏好提交落地（本地 + 按开关上行后端） */
function savePreferences() {
  appStore.commitPreferenceDraft()
  toast.success(t('preference.drawer.save_success'))
}

function openDrawer() {
  visible.value = true
}

function handleOpenPreferenceDrawer() {
  layoutBridgeStore.requestOpenPreferenceDrawer()
}

function handleThemeModeChange(
  value: 'light' | 'dark' | 'auto',
  origin?: { clientX: number, clientY: number },
) {
  // 三种模式统一走扩散动画：auto 由 animateThemeTransition 内部按系统主题决定方向并落地为跟随系统
  // origin 为被点中的模式卡片中心，缺省才回退到视口中心
  animateThemeTransition(value, origin)
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
  // 兜底：组件卸载（如登出）时结束草稿，避免残留暂停态影响后续正常落地
  appStore.discardPreferenceDraft()
})

watch(
  () => layoutBridgeStore.preferenceDrawerVersion,
  () => {
    openDrawer()
  },
)

// 偏好草稿生命周期：打开抽屉进入草稿（仅预览）；关闭若未保存则还原到打开/最近保存时的值
watch(visible, (open, was) => {
  if (open && !was) {
    appStore.beginPreferenceDraft()
  }
  else if (!open && was) {
    appStore.discardPreferenceDraft()
  }
})
</script>

<template>
  <!-- unmount-on-exit=false：第一次打开才挂内容，之后收起只隐藏、不卸载；
       偏好会被反复开合，每次打开都把整页表单重挂一遍会让滑入那一帧卡住 -->
  <XhDrawerRoot v-model:open="visible" side="right" :unmount-on-exit="false">
    <XhDrawerContent class="preference-drawer-content">
      <div class="drawer-header">
        <div class="flex items-center gap-2">
          <XhDrawerTitle class="drawer-title">
            {{ t('preference.drawer.title') }}
          </XhDrawerTitle>
          <SyncStatusBadge :synced="appStore.preferenceSyncEnabled" />
        </div>
        <!-- 关闭钮用组件库部件：键盘可达，可及名取全局文案，悬停、按压与聚焦环随家族配方 -->
        <XhDrawerCloseTrigger class="drawer-close" />
      </div>

      <!-- 面板内容各不相同，标签与面板手摆而不喂 collection。
           滚动区只包面板，标签行留在外面：它不随内容滚，也就不会和滚动条压在一起。
           lazy-mount：四页一次全挂是三千多个节点、六十来个提示浮层，抽屉滑入那一帧整块卡住；
           只挂当前页，其余等第一次切过去再挂，挂过的留着不卸，来回切不重建 -->
      <XhTabsRoot v-model:value="activeTab" class="preference-tabs" variant="segment" lazy-mount>
        <XhTabsList>
          <XhTabsTrigger value="appearance">
            {{ t('preference.drawer.tab.appearance') }}
          </XhTabsTrigger>
          <XhTabsTrigger value="layout">
            {{ t('preference.drawer.tab.layout') }}
          </XhTabsTrigger>
          <XhTabsTrigger value="shortcut">
            {{ t('preference.drawer.tab.shortcut') }}
          </XhTabsTrigger>
          <XhTabsTrigger value="general">
            {{ t('preference.drawer.tab.general') }}
          </XhTabsTrigger>
          <XhTabsIndicator />
        </XhTabsList>

        <XhScrollAreaRoot class="preference-scrollbar">
          <XhScrollAreaViewport>
            <XhScrollAreaContent>
              <XhTabsContent value="appearance">
                <PreferenceAppearanceTab
                  :app-store="appStore"
                  :theme-mode="themeMode"
                  @theme-mode-change="handleThemeModeChange"
                />
              </XhTabsContent>

              <XhTabsContent value="layout">
                <PreferenceLayoutTab
                  :app-store="appStore"
                  :layout-mode="layoutMode"
                  :content-mode="contentMode"
                  :layout-presets="layoutPresets"
                  @layout-mode-change="handleLayoutModeChange"
                  @content-mode-change="handleContentModeChange"
                />
              </XhTabsContent>

              <XhTabsContent value="shortcut">
                <PreferenceShortcutTab :app-store="appStore" />
              </XhTabsContent>

              <XhTabsContent value="general">
                <PreferenceGeneralTab :app-store="appStore" />
              </XhTabsContent>
            </XhScrollAreaContent>
          </XhScrollAreaViewport>
          <!-- 滑块的行程按轨道节点量：少了 Track 这层，拖滑块与点轨道都会变成空操作 -->
          <XhScrollAreaScrollbar orientation="vertical">
            <XhScrollAreaTrack>
              <XhScrollAreaThumb />
            </XhScrollAreaTrack>
          </XhScrollAreaScrollbar>
        </XhScrollAreaRoot>
      </XhTabsRoot>

      <div class="drawer-footer">
        <div class="flex gap-2">
          <XhButton
            variant="subtle"
            tone="brand"
            icon-only
            class="footer-round"
            :title="t('preference.drawer.copy')"
            :aria-label="t('preference.drawer.copy')"
            @click="copyPreferences"
          >
            <Icon icon="lucide:copy" width="16" />
          </XhButton>
          <XhButton variant="outline" icon-only class="footer-round" :title="t('preference.drawer.reset')" :aria-label="t('preference.drawer.reset')" @click="resetPreferences">
            <Icon icon="lucide:rotate-ccw" width="16" />
          </XhButton>
          <XhButton variant="outline" icon-only class="footer-round" :title="t('preference.drawer.clear_cache')" :aria-label="t('preference.drawer.clear_cache')" @click="clearAndLogout">
            <Icon icon="lucide:trash-2" width="16" />
          </XhButton>
        </div>
        <XhButton
          variant="solid"
          class="footer-save"
          :disabled="!appStore.preferenceDraftDirty"
          @click="savePreferences"
        >
          {{ t('preference.drawer.save') }}
        </XhButton>
      </div>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
/* 底部三颗图标钮走圆形。面板自身的宽度与字号在 src/styles/admin-page.css：
   content 被 portal 到 body，scopeId 不跟过去，scoped 规则选不中它 */
.footer-round {
  inline-size: 32px;
  padding-inline: 0;
  border-radius: var(--xh-radius-full);
}

.drawer-footer {
  display: flex;
  gap: 10px;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

/* 工具按钮（复制/重置/清空）居左；保存按钮居右、宽度自适应（较窄） */
.footer-save {
  flex: none;
}

/* 这几个目标元素都写在本组件模板里、自带 scopeId；
   套上 :deep() 反而要求祖先带 scopeId，而它们的祖先在 portal 之后一个都没有 */
/* 标签行固定、滚动区吃掉面板剩下的高度 */
.preference-tabs {
  flex: 1;
  min-height: 0;

  --xh-tabs-gap: 12px;
  --xh-tabs-content-py: 8px;
}

.preference-scrollbar {
  flex: 1;
  min-height: 0;
}

.preference-scrollbar [data-scope='scroll-area'][data-part='content'] {
  padding: 0 16px 16px;
}

/* 标签行在滚动区之外，左右缩进自己写，与面板内容的 16px 对齐 */
.preference-tabs > [data-scope='tabs'][data-part='list'] {
  margin-inline: 16px;
}

/* 四段等分铺满：组件库不定各段宽度，由使用者按容器决定 */
.preference-tabs > [data-scope='tabs'][data-part='list'] > [data-scope='tabs'][data-part='trigger'] {
  flex: 1;
}

/* 自定义头部 */
.drawer-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
}

/* 皮肤给角落关闭钮让出的标题右内衬：关闭钮在头部行内，撤掉免得把同步徽标推远 */
.drawer-title {
  padding-inline-end: 0;
  font-size: 16px;
  font-weight: 600;
  color: hsl(var(--foreground));
}

/* 关闭钮留在头部行尾、与标题同行居中，不走皮肤的角落绝对定位 */
.drawer-close {
  position: static;
}
</style>
