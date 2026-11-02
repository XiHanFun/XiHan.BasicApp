<script setup lang="ts">
import type { ChatLocalMessage } from '../store'
import { XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XInput from '~/components/common/XInput.vue'
import { playNotificationSound, toast } from '~/composables'
import { Icon } from '~/iconify'
import { getChatApi } from '../api-contract'
import { useChatStore } from '../store'
import { chatErrorText } from './chat-helpers'
import ChatConversationAvatar from './ChatConversationAvatar.vue'

defineOptions({ name: 'ChatForwardDialog' })

const props = defineProps<{
  /** 待转发的消息（服务端原样复制类型、正文与附件到目标会话，不带「转发自」） */
  message: ChatLocalMessage | null
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const chatStore = useChatStore()

const keyword = ref('')
const sendingTo = ref<null | string>(null)

const candidates = computed(() => {
  const key = keyword.value.trim().toLowerCase()
  return chatStore.conversations.filter(c =>
    // 不能转发回原会话（无意义），其余全部可选
    c.conversationId !== props.message?.conversationId
    && (!key || c.displayName.toLowerCase().includes(key)))
})

watch(show, (visible) => {
  if (visible) {
    keyword.value = ''
    sendingTo.value = null
  }
})

async function handleForward(conversationId: string) {
  const target = props.message
  if (!target || sendingTo.value) {
    return
  }
  sendingTo.value = conversationId
  try {
    // 6.0 转发是服务端操作（方案 H1）：附件须是发送人自己上传的，前端原样重发转不了别人的附件；
    // 转出的消息经推送回到各会话（含本人的其他端）
    await getChatApi().forwardMessage(target.messageId, [conversationId])
    // 4.x 转发即发送，发出去了响一声；6.0 照旧
    playNotificationSound('send')
    show.value = false
    toast.success(t('chat.forward.sent'))
  }
  catch (error) {
    toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
  }
  finally {
    sendingTo.value = null
  }
}
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: 380px">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('chat.forward.title') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <XInput
          v-model:value="keyword"
          size="sm"
          clearable
          :placeholder="t('chat.list.search_placeholder')"
        >
          <template #prefix>
            <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
          </template>
        </XInput>

        <div v-if="!candidates.length" class="py-8">
          <XhEmptyStateRoot size="sm">
            <XhEmptyStateIndicator>
              <Icon :icon="keyword.trim() ? 'lucide:search-x' : 'lucide:inbox'" width="24" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ keyword.trim() ? t('common.no_result') : t('common.empty') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('chat.list.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </div>
        <div v-else class="xh-scroll-area mt-2" style="max-height: 320px">
          <button
            v-for="conv in candidates"
            :key="conv.conversationId"
            type="button"
            class="chat-forward-item"
            :disabled="sendingTo !== null"
            @click="handleForward(conv.conversationId)"
          >
            <ChatConversationAvatar :conversation-id="conv.conversationId" :conversation-type="conv.conversationType" :avatar="conv.avatar" :name="conv.displayName" :size="32" />
            <span class="min-w-0 flex-1 truncate text-left text-[13px] text-foreground">{{ conv.displayName }}</span>
            <Icon
              v-if="sendingTo === conv.conversationId"
              icon="lucide:loader-circle"
              width="14"
              height="14"
              class="animate-spin text-primary"
            />
          </button>
        </div>
      </XhDialogBody>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.chat-forward-item {
  display: flex;
  gap: 10px;
  align-items: center;
  width: 100%;
  padding: 7px 8px;
  border: none;
  border-radius: 8px;
  background: transparent;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-forward-item:hover:not(:disabled) {
  background: hsl(var(--accent));
}

.chat-forward-item:disabled {
  cursor: not-allowed;
  opacity: 0.7;
}
</style>
