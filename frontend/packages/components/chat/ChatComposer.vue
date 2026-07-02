<script setup lang="ts">
import { NInput, NProgress, NTooltip, useMessage } from 'naive-ui'
import { computed, ref } from 'vue'
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

const canSend = computed(() => userStore.hasPermission(CHAT_PERMISSIONS.send))
const trimmedDraft = computed(() => draft.value.trim())

function handleInput() {
  chatStore.sendTyping(props.conversationId)
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
