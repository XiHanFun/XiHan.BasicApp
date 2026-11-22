<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatConversationListItem } from '../types'
import { XhFieldControl, XhFieldRoot, XhFormRoot } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XEditModal from '~/components/common/EditModal.vue'
import XFieldLabel from '~/components/common/XFieldLabel.vue'
import XInput from '~/components/common/XInput.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { getChatApi } from '../api-contract'
import { useChatStore } from '../store'
import { CHAT_MAX_GROUP_NAME_LENGTH } from '../types'
import { chatErrorText } from './chat-helpers'
import ChatConversationAvatar from './ChatConversationAvatar.vue'

/** 编辑群信息（群主与管理员）：头像、名称、公告与描述；部门群的名称随部门、不能改 */
defineOptions({ name: 'ChatGroupInfoDialog' })

const props = defineProps<{
  conversation: ChatConversationListItem | null
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const chatStore = useChatStore()
const formId = useId()

const name = ref('')
const announcement = ref('')
const description = ref('')
const avatar = ref('')
const saving = ref(false)
const avatarUploading = ref(false)
const avatarInputRef = ref<HTMLInputElement>()

const isDepartment = computed(() => props.conversation?.conversationType === 'Department')

watch(show, (visible) => {
  if (visible) {
    name.value = props.conversation?.displayName ?? ''
    announcement.value = props.conversation?.announcement ?? ''
    description.value = props.conversation?.description ?? ''
    avatar.value = props.conversation?.avatar ?? ''
  }
})

function notifyFailure(error: unknown) {
  toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
}

/**
 * 群头像走聊天附件链路（含秒传），存文件主键
 * 显示时凭会话换签名地址（ChatConversationAvatar）
 */
async function handleAvatarPicked(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || avatarUploading.value) {
    return
  }
  avatarUploading.value = true
  try {
    avatar.value = (await getChatApi().uploadAttachment(file)).fileId
  }
  catch (error) {
    notifyFailure(error)
  }
  finally {
    avatarUploading.value = false
  }
}

async function handleSubmit() {
  const id = props.conversation?.conversationId
  if (!id || saving.value) {
    return
  }
  saving.value = true
  try {
    await getChatApi().updateConversationInfo({
      conversationId: id,
      // 部门群名称随部门同步禁改（null 不改）
      conversationName: isDepartment.value
        ? null
        : name.value.trim() || null,
      announcement: announcement.value,
      description: description.value,
      avatar: avatar.value,
    })
    chatStore.loadConversations().catch(() => {})
    show.value = false
    toast.success(t('chat.members.info_saved'))
  }
  catch (error) {
    notifyFailure(error)
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <XEditModal
    v-model:show="show"
    :title="t('chat.details.edit_info')"
    :width="480"
    :loading="saving"
    :form-id="formId"
    :initial-focus="isDepartment ? 'textarea[name=announcement]' : 'input[name=conversationName]'"
  >
    <XhFormRoot :id="formId" class="flex flex-col gap-3" novalidate @submit="handleSubmit">
      <XhFieldRoot>
        <XFieldLabel>{{ t('chat.details.avatar') }}</XFieldLabel>
        <!-- 头像钮不放进字段控件：那是给单个输入框的壳，会把它画成一条输入框 -->
        <div class="flex">
          <button
            type="button"
            class="chat-avatar-upload"
            :aria-label="t('chat.members.info_avatar')"
            :title="t('chat.members.info_avatar')"
            :disabled="avatarUploading"
            @click="avatarInputRef?.click()"
          >
            <ChatConversationAvatar
              v-if="conversation"
              :conversation-id="conversation.conversationId"
              :conversation-type="conversation.conversationType"
              :avatar="avatar || null"
              :name="name"
              :size="52"
              :round="false"
            />
            <span class="chat-avatar-upload__mask">
              <Icon
                :icon="avatarUploading ? 'lucide:loader-circle' : 'lucide:camera'"
                width="16"
                height="16"
                :class="{ 'animate-spin': avatarUploading }"
              />
            </span>
          </button>
          <input ref="avatarInputRef" type="file" accept="image/*" class="hidden" @change="handleAvatarPicked">
        </div>
      </XhFieldRoot>

      <XhFieldRoot :required="!isDepartment">
        <XFieldLabel :help="isDepartment ? t('chat.details.name_locked_help') : null">
          {{ t('chat.members.info_name') }}
        </XFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="name"
            name="conversationName"
            :max-length="CHAT_MAX_GROUP_NAME_LENGTH"
            show-count
            :disabled="isDepartment"
          />
        </XhFieldControl>
      </XhFieldRoot>

      <XhFieldRoot>
        <XFieldLabel :help="t('chat.details.announcement_help')">
          {{ t('chat.members.info_announcement') }}
        </XFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="announcement"
            name="announcement"
            type="textarea"
            :autosize="{ minRows: 3, maxRows: 8 }"
            :max-length="2000"
            show-count
          />
        </XhFieldControl>
      </XhFieldRoot>

      <XhFieldRoot>
        <XFieldLabel>{{ t('chat.members.info_description') }}</XFieldLabel>
        <XhFieldControl>
          <XInput
            v-model:value="description"
            name="description"
            type="textarea"
            :autosize="{ minRows: 2, maxRows: 4 }"
            :max-length="500"
            show-count
          />
        </XhFieldControl>
      </XhFieldRoot>
    </XhFormRoot>
  </XEditModal>
</template>

<style scoped>
/* 群头像上传：悬浮或聚焦出相机遮罩。圆角与方形头像（XUserAvatar round=false）同取控件档，遮罩与图边对齐 */
.chat-avatar-upload {
  position: relative;
  padding: var(--xh-space-0);
  border: none;
  border-radius: var(--xh-shape-control);
  background: transparent;
  cursor: pointer;
  flex-shrink: 0;
  overflow: hidden;
}

/* 与组件库同一道聚焦环：画在盒内，底下是头像图，内侧衬一道底色间隔线 */
.chat-avatar-upload:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
  box-shadow: inset 0 0 0 calc(var(--xh-ring-width) + var(--xh-ring-gap-width)) var(--xh-ring-gap);
}

.chat-avatar-upload__mask {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--xh-shape-control);
  /* 压在头像图片上的上传遮罩：与媒体控件同一套暗色衬底 + 前景 */
  background: var(--xh-bg-media-chrome);
  color: var(--xh-fg-on-media-chrome);
  opacity: 0;
  transition: opacity var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-avatar-upload:hover .chat-avatar-upload__mask,
.chat-avatar-upload:focus-visible .chat-avatar-upload__mask,
.chat-avatar-upload:disabled .chat-avatar-upload__mask {
  opacity: 1;
}
</style>
