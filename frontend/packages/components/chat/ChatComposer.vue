<script setup lang="ts">
import type { InputInst } from 'naive-ui'
import { NInput, NPopover, NProgress, NTooltip, useMessage } from 'naive-ui'
import { computed, defineAsyncComponent, nextTick, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_PERMISSIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useAppContext, useChatStore, useUserStore } from '~/stores'
import { CHAT_MAX_CONTENT_LENGTH } from '~/types'
import { ChatMessageType } from '~/types/enums'

defineOptions({ name: 'ChatComposer' })

const props = defineProps<{
  conversationId: string
}>()

// emoji-mart 及其数据（~600KB）异步拆包：首次点开表情按钮才加载
const ChatEmojiPicker = defineAsyncComponent(() => import('./ChatEmojiPicker.vue'))

const { t } = useI18n()
const message = useMessage()
const chatStore = useChatStore()
const userStore = useUserStore()
const appContext = useAppContext()

const draft = ref('')
const sending = ref(false)
const uploadingPercent = ref<null | number>(null)
const imageInputRef = ref<HTMLInputElement>()
const fileInputRef = ref<HTMLInputElement>()
const textInputRef = ref<InputInst | null>(null)
const showEmojiPicker = ref(false)

const canSend = computed(() => userStore.hasPermission(CHAT_PERMISSIONS.send))
const trimmedDraft = computed(() => draft.value.trim())

function handleInput() {
  chatStore.sendTyping(props.conversationId)
}

/** 在光标处插入表情（超长丢弃；插入后回焦并把光标落在表情后，面板保持打开支持连选） */
function insertEmoji(emoji: string) {
  if (draft.value.length + emoji.length > CHAT_MAX_CONTENT_LENGTH) {
    return
  }
  const textarea = textInputRef.value?.textareaElRef
  const start = textarea?.selectionStart ?? draft.value.length
  const end = textarea?.selectionEnd ?? draft.value.length
  draft.value = draft.value.slice(0, start) + emoji + draft.value.slice(end)
  void nextTick(() => {
    textarea?.focus()
    const caret = start + emoji.length
    textarea?.setSelectionRange(caret, caret)
  })
}

async function handleSendText() {
  const content = trimmedDraft.value
  if (!content || sending.value) {
    return
  }
  if (content.length > CHAT_MAX_CONTENT_LENGTH) {
    message.warning(t('chat.composer.too_long', { max: CHAT_MAX_CONTENT_LENGTH }))
    return
  }
  draft.value = ''
  sending.value = true
  try {
    await chatStore.sendMessage({
      conversationId: props.conversationId,
      messageType: ChatMessageType.Text,
      content,
    })
  }
  catch {
    // 失败条目留在消息流中可重发；请求层已有统一错误提示
  }
  finally {
    sending.value = false
  }
}

async function handlePickedFile(event: Event, messageType: ChatMessageType) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || uploadingPercent.value != null) {
    return
  }
  uploadingPercent.value = 0
  try {
    const uploaded = await appContext.apis.chatApi.uploadAttachment(file, (percent) => {
      uploadingPercent.value = percent
    })
    await chatStore.sendMessage({
      conversationId: props.conversationId,
      messageType,
      content: null,
      fileId: uploaded.fileId,
      fileName: uploaded.fileName,
      fileSize: uploaded.fileSize,
    })
  }
  catch {
    message.error(t('chat.composer.upload_failed'))
  }
  finally {
    uploadingPercent.value = null
  }
}
</script>

<template>
  <div class="border-t border-border p-2.5">
    <div v-if="!canSend" class="py-2 text-center text-xs text-muted-foreground">
      {{ t('chat.composer.no_permission') }}
    </div>
    <template v-else>
      <!-- 附件上传进度 -->
      <div v-if="uploadingPercent != null" class="mb-2 flex items-center gap-2">
        <NProgress
          type="line"
          :percentage="uploadingPercent"
          :show-indicator="false"
          :height="4"
          class="flex-1"
        />
        <span class="shrink-0 text-[11px] text-muted-foreground">
          {{ t('chat.composer.uploading', { percent: uploadingPercent }) }}
        </span>
      </div>

      <div class="flex items-end gap-1.5">
        <!-- 表情 -->
        <NPopover v-model:show="showEmojiPicker" trigger="click" placement="top-start" :show-arrow="false" raw>
          <template #trigger>
            <button type="button" class="chat-composer-btn" :title="t('chat.composer.emoji')">
              <Icon icon="lucide:smile" width="17" height="17" />
            </button>
          </template>
          <ChatEmojiPicker @select="insertEmoji" />
        </NPopover>

        <!-- 附件按钮 -->
        <NTooltip>
          <template #trigger>
            <button
              type="button"
              class="chat-composer-btn"
              :disabled="uploadingPercent != null"
              @click="imageInputRef?.click()"
            >
              <Icon icon="lucide:image" width="17" height="17" />
            </button>
          </template>
          {{ t('chat.composer.image') }}
        </NTooltip>
        <NTooltip>
          <template #trigger>
            <button
              type="button"
              class="chat-composer-btn"
              :disabled="uploadingPercent != null"
              @click="fileInputRef?.click()"
            >
              <Icon icon="lucide:paperclip" width="17" height="17" />
            </button>
          </template>
          {{ t('chat.composer.file') }}
        </NTooltip>
        <input
          ref="imageInputRef"
          type="file"
          accept="image/*"
          class="hidden"
          @change="handlePickedFile($event, ChatMessageType.Image)"
        >
        <input
          ref="fileInputRef"
          type="file"
          class="hidden"
          @change="handlePickedFile($event, ChatMessageType.File)"
        >

        <!-- 文本输入：Enter 发送 / Shift+Enter 换行 -->
        <NInput
          ref="textInputRef"
          v-model:value="draft"
          type="textarea"
          :autosize="{ minRows: 1, maxRows: 5 }"
          :maxlength="CHAT_MAX_CONTENT_LENGTH"
          :placeholder="t('chat.composer.placeholder')"
          class="flex-1"
          @input="handleInput"
          @keydown.enter.exact.prevent="handleSendText"
        />

        <NTooltip>
          <template #trigger>
            <button
              type="button"
              class="chat-composer-btn chat-composer-btn--send"
              :disabled="!trimmedDraft || sending"
              @click="handleSendText"
            >
              <Icon icon="lucide:send-horizontal" width="17" height="17" />
            </button>
          </template>
          {{ t('chat.composer.send') }}
        </NTooltip>
      </div>
    </template>
  </div>
</template>

<style scoped>
.chat-composer-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  flex-shrink: 0;
  transition: all 0.15s ease;
}

.chat-composer-btn:hover:not(:disabled) {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.chat-composer-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.chat-composer-btn--send {
  color: hsl(var(--primary));
}
</style>
