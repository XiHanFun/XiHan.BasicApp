<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatMemberItem } from '../types'
import type { ChatCallMedia } from '~/contracts'
import {
  XhButton,
  XhButtonIndicator,
  XhButtonLabel,
  XhDialogBody,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogFooter,
  XhDialogHeader,
  XhDialogRoot,
  XhDialogTitle,
  XhListboxContent,
  XhListboxEmpty,
  XhListboxItem,
  XhListboxItemIndicator,
  XhListboxItemPrefix,
  XhListboxItemSuffix,
  XhListboxItemText,
  XhListboxLoading,
  XhListboxRoot,
} from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XInput from '~/components/common/XInput.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { getChatApi } from '../api-contract'
import { callMediaFailureText } from '../call/call-helpers'
import { ChatCallMediaError } from '../call/call-media'
import { useChatCallStore } from '../call/call-store'
import { useChatStore } from '../store'
import { chatErrorText } from './chat-helpers'
import ChatPresenceBadge from './ChatPresenceBadge.vue'

/**
 * 在群聊或部门群里发起通话：从会话成员里点名邀请
 * 不在线的列出但选不了（响不了铃）；连自己在内不超过这种通话的人数上限，选满了别的就选不了
 */
defineOptions({ name: 'ChatCallInviteDialog' })

const props = defineProps<{
  conversationId: null | string
  media: ChatCallMedia
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const chatStore = useChatStore()
const callStore = useChatCallStore()
const userStore = useUserStore()

const members = ref<ChatMemberItem[]>([])
const loading = ref(false)
const keyword = ref('')
const userIds = ref<string[]>([])
const starting = ref(false)
/** 最多能邀请几个人（人数上限减去自己）；设置没取到为 null */
const maxInvitees = ref<null | number>(null)

/** 与聊天里别处一致用用户名，没有才取显示名 */
function nameOf(member: ChatMemberItem): string {
  return member.userName?.trim() || member.displayName?.trim() || ''
}

function notifyFailure(error: unknown) {
  toast.danger(error instanceof ChatCallMediaError
    ? callMediaFailureText(t, error.failure)
    : chatErrorText(error) || t('common.messages.operation_failed'))
}

watch(show, async (visible) => {
  if (!visible) {
    return
  }
  keyword.value = ''
  userIds.value = []
  members.value = []
  maxInvitees.value = null
  const id = props.conversationId
  if (!id) {
    return
  }
  loading.value = true
  try {
    const [loaded, settings] = await Promise.all([
      getChatApi().members(id),
      callStore.loadSettings(),
    ])
    // 等的时候对话框关了或换了会话
    if (!show.value || props.conversationId !== id) {
      return
    }
    // 在线状态并进聊天 store：在线点一律从那里取，之后的上下线推送也改在那里
    chatStore.notePresence(loaded)
    const mine = userStore.userInfo?.basicId
    members.value = loaded.filter(member => member.userId !== mine)
    maxInvitees.value = (props.media === 'Video'
      ? settings.maxVideoParticipants
      : settings.maxVoiceParticipants) - 1
  }
  catch (error) {
    notifyFailure(error)
  }
  finally {
    loading.value = false
  }
})

const full = computed(() => maxInvitees.value !== null && userIds.value.length >= maxInvitees.value)

const candidates = computed(() => {
  const key = keyword.value.trim().toLowerCase()
  return members.value.filter(member => !key
    || nameOf(member).toLowerCase().includes(key)
    || (member.displayName ?? '').toLowerCase().includes(key))
})

function selectable(member: ChatMemberItem): boolean {
  return chatStore.isUserOnline(member.userId) && (!full.value || userIds.value.includes(member.userId))
}

const collection = computed(() => candidates.value.map(member => ({
  value: member.userId,
  label: nameOf(member),
  disabled: !selectable(member),
})))

/** 还能再选的在线成员：全选只选到上限为止 */
const selectableOnline = computed(() => members.value.filter(member => chatStore.isUserOnline(member.userId) && !userIds.value.includes(member.userId)))

function handleSelectOnline() {
  const room = (maxInvitees.value ?? 0) - userIds.value.length
  if (room > 0) {
    userIds.value = [
      ...userIds.value,
      ...selectableOnline.value.slice(0, room).map(member => member.userId),
    ]
  }
}

function handleValueChange(next: string[]) {
  // 候选换了一批（搜索）时留着别处选的；超出上限的不要
  const visible = new Set(candidates.value.map(member => member.userId))
  const kept = userIds.value.filter(id => !visible.has(id))
  const merged = [
    ...kept,
    ...next.filter(id => !kept.includes(id)),
  ]
  userIds.value = maxInvitees.value === null
    ? merged
    : merged.slice(0, maxInvitees.value)
}

async function handleConfirm() {
  const id = props.conversationId
  if (!id || !userIds.value.length || starting.value) {
    return
  }
  starting.value = true
  try {
    await callStore.start(id, props.media, userIds.value)
    show.value = false
  }
  catch (error) {
    notifyFailure(error)
  }
  finally {
    starting.value = false
  }
}
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: calc(var(--xh-space-8) * 14)">
      <XhDialogHeader>
        <XhDialogTitle>{{ props.media === 'Video' ? t('chat.call.invite_title_video') : t('chat.call.invite_title_voice') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <div class="flex min-w-0 flex-col gap-2">
          <XInput
            v-model:value="keyword"
            size="sm"
            clearable
            :placeholder="t('chat.call.invite_search')"
            :aria-label="t('chat.call.invite_search')"
          >
            <template #prefix>
              <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
            </template>
          </XInput>
          <XhListboxRoot
            class="chat-call-invite__list"
            :value="userIds"
            :collection="collection"
            selection-mode="multiple"
            :loading="loading"
            :aria-label="t('chat.call.invite_members')"
            @update:value="handleValueChange"
          >
            <XhListboxContent>
              <XhListboxItem v-for="member in candidates" :key="member.userId" :value="member.userId">
                <XhListboxItemPrefix>
                  <ChatPresenceBadge :user-id="member.userId">
                    <XUserAvatar :avatar="member.avatarFileId ?? null" :name="nameOf(member)" :size="28" />
                  </ChatPresenceBadge>
                </XhListboxItemPrefix>
                <XhListboxItemText>{{ nameOf(member) }}</XhListboxItemText>
                <XhListboxItemSuffix v-if="!chatStore.isUserOnline(member.userId)">
                  {{ t('chat.presence.offline') }}
                </XhListboxItemSuffix>
                <XhListboxItemIndicator />
              </XhListboxItem>
            </XhListboxContent>
            <XhListboxEmpty>{{ t('chat.call.invite_empty') }}</XhListboxEmpty>
            <XhListboxLoading>{{ t('chat.picker.loading') }}</XhListboxLoading>
          </XhListboxRoot>
          <div class="flex items-center justify-between gap-2">
            <span class="text-xs text-muted-foreground" aria-live="polite">
              {{ maxInvitees === null ? '' : t('chat.call.invite_picked', { n: userIds.length, max: maxInvitees }) }}
            </span>
            <XhButton
              variant="link"
              size="sm"
              class="xh-link-button"
              :disabled="full || selectableOnline.length === 0"
              @click="handleSelectOnline"
            >
              {{ t('chat.call.invite_select_online') }}
            </XhButton>
          </div>
        </div>
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton variant="subtle" size="sm" :disabled="starting" @click="show = false">
          {{ t('chat.start.cancel') }}
        </XhButton>
        <XhButton
          variant="subtle"
          size="sm"
          tone="brand"
          :loading="starting"
          :disabled="!userIds.length || callStore.call !== null"
          @click="handleConfirm"
        >
          <XhButtonIndicator />
          <XhButtonLabel>{{ t('chat.call.invite_start') }}</XhButtonLabel>
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 列表定高：搜索换一批候选时对话框不跳动 */
.chat-call-invite__list {
  --xh-listbox-content-max-h: var(--xh-viewport-h-md);

  block-size: var(--xh-viewport-h-md);
}
</style>
