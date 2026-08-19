<script setup lang="ts">
import type { ChatLocalMessage } from '../store'
import { XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XInput from '~/components/common/XInput.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { useChatStore } from '../store'

defineOptions({ name: 'ChatForwardDialog' })

const props = defineProps<{
  /** 待转发的消息（文本原样重发；图片/文件复用附件列表） */
  message: ChatLocalMessage | null
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const message$ = toast
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
    await chatStore.sendMessage({
      conversationId,
      messageType: target.messageType,
      content: target.content,
      attachments: target.attachments,
    })
    show.value = false
    message$.success(t('chat.forward.sent'))
  }
  catch {
    // 请求层已有统一错误提示
  }
  finally {
    sendingTo.value = null
  }
}
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: 380px">
      <XhDialogTitle>{{ t('chat.forward.title') }}</XhDialogTitle>
      <XhDialogCloseTrigger>✕</XhDialogCloseTrigger>
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
          <XhEmptyStateIcon>
            <Icon :icon="keyword.trim() ? 'lucide:search-x' : 'lucide:inbox'" width="24" />
          </XhEmptyStateIcon>
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
          <XUserAvatar :avatar="conv.avatar" :name="conv.displayName" :size="32" />
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
  transition: background 0.12s ease;
}

.chat-forward-item:hover:not(:disabled) {
  background: hsl(var(--accent));
}

.chat-forward-item:disabled {
  cursor: not-allowed;
  opacity: 0.7;
}
</style>
