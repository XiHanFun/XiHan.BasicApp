<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatUserProfile } from '../types'
import type { ChatCallMedia } from '~/contracts'
import { XhButton, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { getChatApi } from '../api-contract'
import { callMediaFailureText } from '../call/call-helpers'
import { ChatCallMediaError } from '../call/call-media'
import { useChatCallStore } from '../call/call-store'
import { CHAT_PERMISSIONS } from '../constants'
import { useChatStore } from '../store'
import { chatErrorText } from './chat-helpers'
import ChatConversationAvatar from './ChatConversationAvatar.vue'
import ChatPresenceBadge from './ChatPresenceBadge.vue'

/**
 * 成员资料卡：聊天里看得到的那些（名字、头像、主部门、在线状态）与两个人都在的群
 * 从这里能直接发消息、打语音或视频，也能点进共同的群
 */
defineOptions({ name: 'ChatUserCardDialog' })

const props = defineProps<{
  userId: null | string
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const chatStore = useChatStore()
const callStore = useChatCallStore()
const userStore = useUserStore()

const profile = ref<ChatUserProfile | null>(null)
const loading = ref(false)
const busy = ref(false)
let loadSeq = 0

const isSelf = computed(() => props.userId !== null && props.userId === userStore.userInfo?.basicId)
const online = computed(() => chatStore.isUserOnline(props.userId))
const canCall = computed(() => !isSelf.value && userStore.hasPermission(CHAT_PERMISSIONS.call))
const callBusy = computed(() => callStore.call !== null || callStore.working)

function notifyFailure(error: unknown) {
  toast.danger(error instanceof ChatCallMediaError
    ? callMediaFailureText(t, error.failure)
    : chatErrorText(error) || t('common.messages.operation_failed'))
}

watch([
  show,
  () => props.userId,
], async ([visible, userId]) => {
  if (!visible || !userId) {
    return
  }
  const seq = ++loadSeq
  profile.value = null
  loading.value = true
  try {
    const loaded = await getChatApi().userProfile(userId)
    if (seq !== loadSeq) {
      return
    }
    profile.value = loaded
    // 在线状态并进聊天 store：之后的上下线推送也改在那里
    chatStore.notePresence([loaded])
  }
  catch (error) {
    if (seq === loadSeq) {
      notifyFailure(error)
      show.value = false
    }
  }
  finally {
    if (seq === loadSeq) {
      loading.value = false
    }
  }
}, { immediate: true })

/** 打开与他的单聊；成了才收起卡片 */
async function openSingle(): Promise<null | string> {
  const userId = props.userId
  if (!userId || busy.value) {
    return null
  }
  busy.value = true
  try {
    return (await chatStore.startSingleConversation(userId)).conversationId
  }
  catch (error) {
    notifyFailure(error)
    return null
  }
  finally {
    busy.value = false
  }
}

async function handleMessage() {
  if (await openSingle()) {
    show.value = false
  }
}

async function handleCall(media: ChatCallMedia) {
  const conversationId = await openSingle()
  if (!conversationId) {
    return
  }
  show.value = false
  try {
    await callStore.start(conversationId, media)
  }
  catch (error) {
    notifyFailure(error)
  }
}

function handleOpenGroup(conversationId: string) {
  show.value = false
  void chatStore.openConversation(conversationId)
}
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: calc(var(--xh-space-8) * 12)">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('chat.user_card.title') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <div v-if="loading || !profile" class="flex justify-center py-8">
          <XhSpinner />
        </div>
        <div v-else class="flex flex-col gap-4">
          <div class="flex items-center gap-3">
            <ChatPresenceBadge :user-id="profile.userId">
              <XUserAvatar :avatar="profile.avatarFileId ?? null" :name="profile.userName" :size="56" />
            </ChatPresenceBadge>
            <div class="min-w-0 flex-1">
              <div class="truncate text-sm font-semibold text-foreground">
                {{ profile.userName }}
              </div>
              <div v-if="profile.displayName && profile.displayName !== profile.userName" class="truncate text-xs text-muted-foreground">
                {{ profile.displayName }}
              </div>
              <div class="mt-0.5 truncate text-xs text-muted-foreground">
                {{ online ? t('chat.presence.online') : t('chat.presence.offline') }}
                <template v-if="profile.departmentName">
                  · {{ profile.departmentName }}
                </template>
              </div>
            </div>
          </div>

          <div v-if="!isSelf" class="flex flex-wrap gap-2">
            <XhButton variant="subtle" size="sm" tone="brand" :loading="busy" @click="handleMessage">
              <Icon icon="lucide:message-circle" />
              {{ t('chat.member_menu.send_message') }}
            </XhButton>
            <template v-if="canCall">
              <XhButton variant="subtle" size="sm" :disabled="busy || callBusy || !online" @click="handleCall('Voice')">
                <Icon icon="lucide:phone" />
                {{ t('chat.call.voice') }}
              </XhButton>
              <XhButton variant="subtle" size="sm" :disabled="busy || callBusy || !online" @click="handleCall('Video')">
                <Icon icon="lucide:video" />
                {{ t('chat.call.video') }}
              </XhButton>
            </template>
          </div>

          <section v-if="profile.commonGroups.length">
            <h3 class="m-0 mb-1 text-xs font-medium text-muted-foreground">
              {{ t('chat.user_card.common_groups', { n: profile.commonGroups.length }) }}
            </h3>
            <ul class="chat-user-card__groups xh-scroll-area m-0 list-none p-0">
              <li v-for="group in profile.commonGroups" :key="group.conversationId">
                <button type="button" class="chat-user-card__group" @click="handleOpenGroup(group.conversationId)">
                  <ChatConversationAvatar
                    :conversation-id="group.conversationId"
                    :conversation-type="group.conversationType"
                    :avatar="group.avatar ?? null"
                    :name="group.conversationName"
                    :size="28"
                  />
                  <span class="min-w-0 flex-1 truncate text-left text-[13px] text-foreground">{{ group.conversationName }}</span>
                  <span class="shrink-0 text-[11px] text-muted-foreground">{{ t('chat.members.count', { n: group.memberCount }) }}</span>
                </button>
              </li>
            </ul>
          </section>
        </div>
      </XhDialogBody>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 共同的群多了在卡片里滚 */
.chat-user-card__groups {
  max-block-size: var(--xh-viewport-h-sm);
}

.chat-user-card__group {
  display: flex;
  gap: var(--xh-space-2_5);
  align-items: center;
  inline-size: 100%;
  padding: var(--xh-space-1_5) var(--xh-space-2);
  border: none;
  border-radius: var(--xh-shape-control);
  background: transparent;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-user-card__group:hover {
  background: var(--xh-bg-subtle-hover);
}

.chat-user-card__group:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}
</style>
