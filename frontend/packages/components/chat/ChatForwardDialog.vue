<script setup lang="ts">
import type { ChatLocalMessage } from '~/stores'
import { NEmpty, NInput, NModal, NScrollbar, useMessage } from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useChatStore } from '~/stores'
import XUserAvatar from '../common/UserAvatar.vue'

defineOptions({ name: 'ChatForwardDialog' })

const props = defineProps<{
  /** 待转发的消息（文本原样重发；图片/文件复用 fileId） */
  message: ChatLocalMessage | null
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const message$ = useMessage()
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
      fileId: target.fileId,
      fileName: target.fileName,
      fileSize: target.fileSize,
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
  <NModal
    v-model:show="show"
    preset="card"
    :title="t('chat.forward.title')"
    style="width: 380px; max-width: calc(100vw - 24px);"
  >
    <NInput
      v-model:value="keyword"
      size="small"
      clearable
      :placeholder="t('chat.list.search_placeholder')"
    >
      <template #prefix>
        <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
      </template>
    </NInput>

    <div v-if="!candidates.length" class="py-8">
      <NEmpty :description="t('chat.list.empty')" size="small" />
    </div>
    <NScrollbar v-else class="mt-2" style="max-height: 320px">
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
    </NScrollbar>
  </NModal>
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
