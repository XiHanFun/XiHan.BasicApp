<script setup lang="ts">
import type { FloatingPanelPosition, FloatingPanelResizeEdge, FloatingPanelSize, FloatingPanelWindowState } from '@xihan-ui/headless'
import {
  XhButton,
  XhFloatingPanelBody,
  XhFloatingPanelCloseTrigger,
  XhFloatingPanelContent,
  XhFloatingPanelDragTrigger,
  XhFloatingPanelHeader,
  XhFloatingPanelPositioner,
  XhFloatingPanelResizeTrigger,
  XhFloatingPanelRoot,
  XhFloatingPanelTitle,
  XhFloatingPanelWindowStateTrigger,
} from '@xihan-ui/vue'
import { nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useIsMobile } from '~/composables'
import { Icon } from '~/iconify'
import ChatPanel from '../components/ChatPanel.vue'
import { CHAT_PAGE_PATH } from '../constants'
import { useChatStore } from '../store'

/**
 * 在线聊天浮动面板：非模态，边聊边看页面；可拖动、改尺寸、收拢成标题栏或铺满视口。
 *
 * 顶栏聊天按钮经 store 的版本计数器请求打开（按钮在壳层另一处，不是本面板的 trigger 部件），
 * 所以开合、位置、尺寸与形态都由这里受控持有。
 */
defineOptions({ name: 'AppChatPanel' })

/** 面板与视口四边、与触发按钮之间的留白（px） */
const GAP = 12
/** 首次打开的尺寸：窄单栏放得下会话列表与消息流 */
const PANEL_SIZE: FloatingPanelSize = { width: 420, height: 640 }
/** 尺寸下限：再小会话列表与输入框就挤不下 */
const MIN_SIZE: FloatingPanelSize = { width: 320, height: 360 }
const RESIZE_EDGES: FloatingPanelResizeEdge[] = ['n', 'e', 's', 'w', 'ne', 'nw', 'se', 'sw']

const { t } = useI18n()
const router = useRouter()
const chatStore = useChatStore()
const { isMobile } = useIsMobile()

const open = ref(false)
const windowState = ref<FloatingPanelWindowState>('default')
// 位置与尺寸从一开始就受控，首次打开时才按触发按钮落位；之后保留用户拖放的结果
const position = ref<FloatingPanelPosition>({ x: GAP, y: GAP })
const dimensions = ref<FloatingPanelSize>({ ...PANEL_SIZE })
let placed = false

/** 首次落位：贴在发起按钮下方、靠视口右缘 */
function placeUnder(anchor: HTMLElement): void {
  const top = anchor.getBoundingClientRect().bottom + GAP
  const width = Math.max(MIN_SIZE.width, Math.min(PANEL_SIZE.width, window.innerWidth - GAP * 2))
  const height = Math.max(MIN_SIZE.height, Math.min(PANEL_SIZE.height, window.innerHeight - top - GAP))
  dimensions.value = { width, height }
  position.value = { x: window.innerWidth - width - GAP, y: top }
}

/**
 * 再次打开时收回视口：组件不夹取位置，窗口缩小或面板被拖到屏外后，
 * 再点按钮也要让标题栏够得着（受控写回是组件库给的做法）
 */
function keepInViewport(): void {
  const { width, height } = dimensions.value
  position.value = {
    x: Math.min(Math.max(position.value.x, GAP), Math.max(window.innerWidth - width - GAP, GAP)),
    y: Math.min(Math.max(position.value.y, GAP), Math.max(window.innerHeight - height - GAP, GAP)),
  }
}

watch(() => chatStore.chatPanelVersion, () => {
  if (placed) {
    keepInViewport()
  }
  else if (chatStore.chatPanelOrigin) {
    placeUnder(chatStore.chatPanelOrigin)
    placed = true
  }
  // 小屏直接铺满；收拢着再点按钮则展开回常规
  if (isMobile.value)
    windowState.value = 'maximized'
  else if (windowState.value === 'minimized')
    windowState.value = 'default'
  open.value = true
  chatStore.ensureConversations().catch(() => {})
})

// 浮动面板不接管焦点归还：关闭后把焦点送回发起打开的按钮
watch(open, (value) => {
  const origin = chatStore.chatPanelOrigin
  if (!value && origin?.isConnected)
    void nextTick(() => origin.focus())
})

function handleOpenFullPage() {
  open.value = false
  void router.push(CHAT_PAGE_PATH)
}
</script>

<template>
  <XhFloatingPanelRoot
    v-model:open="open"
    v-model:position="position"
    v-model:dimensions="dimensions"
    v-model:window-state="windowState"
    :min-size="MIN_SIZE"
  >
    <XhFloatingPanelPositioner>
      <XhFloatingPanelContent>
        <XhFloatingPanelHeader>
          <XhFloatingPanelTitle>{{ t('chat.panel.title') }}</XhFloatingPanelTitle>
          <XhFloatingPanelDragTrigger />
          <!-- 与标题栏自带的形态、关闭钮同一档：icon、sm、ghost -->
          <XhButton
            icon-only
            size="sm"
            variant="ghost"
            :aria-label="t('chat.panel.open_page')"
            :title="t('chat.panel.open_page')"
            @click="handleOpenFullPage"
          >
            <Icon icon="lucide:external-link" />
          </XhButton>
          <XhFloatingPanelWindowStateTrigger window-state="minimized" />
          <XhFloatingPanelWindowStateTrigger window-state="maximized" />
          <XhFloatingPanelCloseTrigger />
        </XhFloatingPanelHeader>
        <XhFloatingPanelBody class="app-chat-panel__body">
          <ChatPanel mode="panel" class="min-h-0 flex-1" />
        </XhFloatingPanelBody>
        <XhFloatingPanelResizeTrigger v-for="edge in RESIZE_EDGES" :key="edge" :edge="edge" />
      </XhFloatingPanelContent>
    </XhFloatingPanelPositioner>
  </XhFloatingPanelRoot>
</template>

<style scoped>
/* 正文交给聊天面板自己滚：会话列表与消息流各有滚动区，外层不再出第二根滚动条。
   收拢时组件库给正文打 hidden，这里避开它，免得本条压过组件库的隐藏 */
.app-chat-panel__body:not([hidden]) {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
</style>
