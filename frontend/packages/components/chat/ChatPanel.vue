<script setup lang="ts">
import type { ChatStartMode } from './ChatStartDialog.vue'
import { computed, onMounted, ref } from 'vue'
import { useIsMobile } from '~/composables'
import { useChatStore } from '~/stores'
import ChatConversationList from './ChatConversationList.vue'
import ChatMembersDialog from './ChatMembersDialog.vue'
import ChatMessageThread from './ChatMessageThread.vue'
import ChatStartDialog from './ChatStartDialog.vue'

defineOptions({ name: 'ChatPanel' })

const props = withDefaults(defineProps<{
  /** page：双栏（列表+消息流）；drawer：窄单栏（列表 ↔ 消息流切换） */
  mode?: 'drawer' | 'page'
}>(), {
  mode: 'page',
})

const chatStore = useChatStore()
const { isMobile } = useIsMobile()

const startMode = ref<ChatStartMode>('single')
const showStartDialog = ref(false)
const showMembersDialog = ref(false)

const isDrawerMode = computed(() => props.mode === 'drawer')
// 窄单栏：抽屉模式恒定；page 模式在小屏（<768）自动收敛为列表 ↔ 消息流切换
const singlePane = computed(() => isDrawerMode.value || isMobile.value)
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
