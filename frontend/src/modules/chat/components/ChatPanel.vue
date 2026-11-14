<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatStartMode } from './ChatStartDialog.vue'
import { useMediaQuery } from '@vueuse/core'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { useIsMobile } from '~/composables'
import { useChatStore } from '../store'
import { provideChatUserCard } from './chat-user-card'
import ChatConversationDetails from './ChatConversationDetails.vue'
import ChatConversationList from './ChatConversationList.vue'
import ChatMessageThread from './ChatMessageThread.vue'
import ChatStartDialog from './ChatStartDialog.vue'
import ChatUserCardDialog from './ChatUserCardDialog.vue'

defineOptions({ name: 'ChatPanel' })

const props = withDefaults(defineProps<{
  /** page：多栏（列表 + 消息流，宽屏再加详情）；panel：浮动面板里的窄单栏（列表 ↔ 消息流 ↔ 详情切换） */
  mode?: 'panel' | 'page'
}>(), {
  mode: 'page',
})

const chatStore = useChatStore()
const { isMobile } = useIsMobile()
// 三栏并排要的宽度：列表与详情各一栏之外，消息流还得留得下气泡
const isWide = useMediaQuery('(min-width: 1280px)')

const startMode = ref<ChatStartMode>('single')
const showStartDialog = ref(false)
const showDetails = ref(false)

const isPanelMode = computed(() => props.mode === 'panel')
// 窄单栏：浮动面板模式恒定；page 模式在小屏（<768）自动收敛为列表 ↔ 消息流切换
const singlePane = computed(() => isPanelMode.value || isMobile.value)
const singlePaneShowThread = computed(() => singlePane.value && Boolean(chatStore.activeConversationId))
// 详情的两种摆法：宽屏嵌在消息流旁边当第三栏，放不下时顶替消息流
const detailsInline = computed(() => !singlePane.value && isWide.value)
const detailsVisible = computed(() => showDetails.value && Boolean(chatStore.activeConversation))
const detailsCovering = computed(() => detailsVisible.value && !detailsInline.value)

// 成员资料卡只放一张：消息流里点头像、会话详情里点成员都开它
const { show: showUserCard, userId: userCardUserId } = provideChatUserCard()

function handleSelect(conversationId: string) {
  void chatStore.openConversation(conversationId)
}

/** 搜索命中：打开它所在的会话并定位到那条消息 */
async function handleLocate(conversationId: string, messageId: string) {
  await chatStore.openConversation(conversationId)
  await chatStore.jumpToMessage(conversationId, messageId).catch(() => {})
}

function handleStart(mode: ChatStartMode) {
  startMode.value = mode
  showStartDialog.value = true
}

function handleBack() {
  chatStore.closeActiveConversation()
}

// 换了会话：第三栏跟着换内容；顶替着消息流的详情收起，让新会话的消息露出来
watch(() => chatStore.activeConversationId, (id) => {
  if (!id || !detailsInline.value) {
    showDetails.value = false
  }
})

onMounted(() => {
  chatStore.ensureConversations().catch(() => {})
})

// 聊天界面卸载即释放「当前会话」：否则离开聊天页后 activeConversationId 仍指着它，
// 该会话的新消息会被判为「正在看」而直接标已读，顶栏未读角标永远涨不起来
onUnmounted(() => {
  chatStore.closeActiveConversation()
})
</script>

<template>
  <div class="flex h-full min-h-0 overflow-hidden rounded-lg border border-border bg-card">
    <!-- 多栏：列表 + 消息流（+ 详情） -->
    <template v-if="!singlePane">
      <div class="w-80 shrink-0 border-r border-border xl:w-[360px]">
        <ChatConversationList @select="handleSelect" @start="handleStart" @locate="handleLocate" />
      </div>
      <div class="min-w-0 flex-1">
        <!-- 详情顶替消息流时消息流只藏不卸：滚动位置、搜索与输入中的内容回来还在 -->
        <ChatMessageThread v-show="!detailsCovering" :details-open="detailsVisible" @details="showDetails = !showDetails" />
        <ChatConversationDetails
          v-if="detailsCovering && chatStore.activeConversation"
          class="app-fade"
          :conversation="chatStore.activeConversation"
          @close="showDetails = false"
        />
      </div>
      <div v-if="detailsVisible && detailsInline && chatStore.activeConversation" class="app-fade w-80 shrink-0 border-l border-border">
        <ChatConversationDetails inline :conversation="chatStore.activeConversation" @close="showDetails = false" />
      </div>
    </template>

    <!-- 窄单栏：列表 ↔ 消息流 ↔ 详情 -->
    <template v-else>
      <div class="min-w-0 flex-1">
        <template v-if="singlePaneShowThread">
          <ChatMessageThread
            v-show="!detailsCovering"
            show-back
            :details-open="detailsVisible"
            @back="handleBack"
            @details="showDetails = !showDetails"
          />
          <ChatConversationDetails
            v-if="detailsCovering && chatStore.activeConversation"
            class="app-fade"
            :conversation="chatStore.activeConversation"
            @close="showDetails = false"
          />
        </template>
        <ChatConversationList v-else @select="handleSelect" @start="handleStart" @locate="handleLocate" />
      </div>
    </template>

    <ChatStartDialog v-model:show="showStartDialog" :mode="startMode" />
    <ChatUserCardDialog v-model:show="showUserCard" :user-id="userCardUserId" />
  </div>
</template>
