<script setup lang="ts">
import type { ChatStartMode } from './ChatStartDialog.vue'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useIsMobile } from '~/composables'
import { useChatStore } from '../store'
import ChatConversationList from './ChatConversationList.vue'
import ChatMembersDialog from './ChatMembersDialog.vue'
import ChatMessageThread from './ChatMessageThread.vue'
import ChatStartDialog from './ChatStartDialog.vue'

defineOptions({ name: 'ChatPanel' })

const props = withDefaults(defineProps<{
  /** page：双栏（列表+消息流）；panel：浮动面板里的窄单栏（列表 ↔ 消息流切换） */
  mode?: 'panel' | 'page'
}>(), {
  mode: 'page',
})

const chatStore = useChatStore()
const { isMobile } = useIsMobile()

const startMode = ref<ChatStartMode>('single')
const showStartDialog = ref(false)
const showMembersDialog = ref(false)

const isPanelMode = computed(() => props.mode === 'panel')
// 窄单栏：浮动面板模式恒定；page 模式在小屏（<768）自动收敛为列表 ↔ 消息流切换
const singlePane = computed(() => isPanelMode.value || isMobile.value)
const singlePaneShowThread = computed(() => singlePane.value && Boolean(chatStore.activeConversationId))

function handleSelect(conversationId: string) {
  void chatStore.openConversation(conversationId)
}

function handleStart(mode: ChatStartMode) {
  startMode.value = mode
  showStartDialog.value = true
}

function handleBack() {
  chatStore.closeActiveConversation()
}

onMounted(() => {
  chatStore.ensureConversations().catch(() => {})
})

// 聊天界面卸载即释放「当前会话」：否则离开聊天页后 activeConversationId 仍指着它，
// 该会话的新消息会被判为「正在看」而直接标已读，顶栏未读角标永远涨不起来。
onUnmounted(() => {
  chatStore.closeActiveConversation()
})
</script>

<template>
  <div class="flex h-full min-h-0 overflow-hidden rounded-lg border border-border bg-card">
    <!-- 双栏：列表 + 消息流 -->
    <template v-if="!singlePane">
      <div class="w-80 shrink-0 border-r border-border xl:w-[360px]">
        <ChatConversationList @select="handleSelect" @start="handleStart" />
      </div>
      <div class="min-w-0 flex-1">
        <ChatMessageThread @members="showMembersDialog = true" />
      </div>
    </template>

    <!-- 窄单栏：列表 ↔ 消息流 -->
    <template v-else>
      <div class="min-w-0 flex-1">
        <ChatMessageThread
          v-if="singlePaneShowThread"
          show-back
          @back="handleBack"
          @members="showMembersDialog = true"
        />
        <ChatConversationList v-else @select="handleSelect" @start="handleStart" />
      </div>
    </template>

    <ChatStartDialog v-model:show="showStartDialog" :mode="startMode" />
    <ChatMembersDialog v-model:show="showMembersDialog" :conversation="chatStore.activeConversation" />
  </div>
</template>
