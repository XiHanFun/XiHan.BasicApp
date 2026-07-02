<script setup lang="ts">
import type { ChatStartMode } from './ChatStartDialog.vue'
import { computed, onMounted, ref } from 'vue'
import { useChatStore } from '~/stores'
import ChatConversationList from './ChatConversationList.vue'
import ChatMembersDrawer from './ChatMembersDrawer.vue'
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

const startMode = ref<ChatStartMode>('single')
const showStartDialog = ref(false)
const showMembersDrawer = ref(false)

const isDrawerMode = computed(() => props.mode === 'drawer')
// 抽屉窄布局：有活跃会话时显示消息流，否则显示列表
const drawerShowThread = computed(() => isDrawerMode.value && Boolean(chatStore.activeConversationId))

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
    <!-- 双栏：列表 -->
    <template v-if="!isDrawerMode">
      <div class="w-72 shrink-0 border-r border-border">
        <ChatConversationList @select="handleSelect" @start="handleStart" />
      </div>
      <div class="min-w-0 flex-1">
        <ChatMessageThread @members="showMembersDrawer = true" />
      </div>
    </template>

    <!-- 窄单栏：列表 ↔ 消息流 -->
    <template v-else>
      <div class="min-w-0 flex-1">
        <ChatMessageThread
          v-if="drawerShowThread"
          show-back
          @back="handleBack"
          @members="showMembersDrawer = true"
        />
        <ChatConversationList v-else @select="handleSelect" @start="handleStart" />
      </div>
    </template>

    <ChatStartDialog v-model:show="showStartDialog" :mode="startMode" />
    <ChatMembersDrawer v-model:show="showMembersDrawer" :conversation="chatStore.activeConversation" />
  </div>
</template>
