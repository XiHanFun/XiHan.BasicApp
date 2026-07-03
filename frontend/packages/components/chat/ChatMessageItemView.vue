<script setup lang="ts">
import type { ChatLocalMessage } from '~/stores'
import { NImage, NSpin, NTooltip } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAvatarUrl } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { ChatMessageType } from '~/types/enums'
import XUserAvatar from '../common/UserAvatar.vue'
import { formatFileSize, formatMessageTime } from './chat-helpers'

defineOptions({ name: 'ChatMessageItemView' })

const props = defineProps<{
  message: ChatLocalMessage
  /** 是否本人发送（气泡靠右） */
  isSelf: boolean
  /** 群聊/部门群显示他人昵称 */
  showSenderName: boolean
  /** 是否可撤回（本人 + 2 分钟窗口内 + 未撤回 + 已落库） */
  canRecall: boolean
}>()

const emit = defineEmits<{
  recall: []
  retry: []
  remove: []
}>()

const { t } = useI18n()
const appContext = useAppContext()

const isSystem = computed(() => props.message.messageType === ChatMessageType.System)
const isImage = computed(() => props.message.messageType === ChatMessageType.Image)
const isFile = computed(() => props.message.messageType === ChatMessageType.File)

// 图片消息：fileId → 预签名 URL（内存缓存 + 并发去重，复用头像解析链路）
const imageUrl = useAvatarUrl(computed(() =>
  isImage.value && !props.message.isRecalled ? props.message.fileId : null,
))

async function handleDownload() {
  const fileId = props.message.fileId
  if (!fileId) {
    return
  }
  try {
    // 预签名 URL 会过期，下载每次即时换取
    const url = await appContext.apis.chatApi.getFileUrl(fileId)
    if (url) {
      window.open(url, '_blank', 'noopener')
    }
  }
  catch {
    // 请求层已有统一错误提示
  }
}
</script>

<template>
  <!-- 系统提示：居中时间线 -->
  <div v-if="isSystem" class="my-2 flex justify-center">
    <span class="rounded-full bg-muted/60 px-3 py-0.5 text-[11px] text-muted-foreground">
      {{ message.content }}
    </span>
  </div>

  <!-- 普通消息行 -->
  <div v-else class="group my-1.5 flex gap-2" :class="isSelf ? 'flex-row-reverse' : 'flex-row'">
    <XUserAvatar v-if="!isSelf" :name="message.senderUserName" :size="30" />
    <div class="flex max-w-[86%] min-w-0 flex-col sm:max-w-[72%]" :class="isSelf ? 'items-end' : 'items-start'">
      <div v-if="showSenderName && !isSelf" class="mb-0.5 px-1 text-[11px] text-muted-foreground">
        {{ message.senderUserName }}
      </div>

      <!-- 已撤回占位 -->
      <div v-if="message.isRecalled" class="chat-bubble chat-bubble--recalled">
        {{ t('chat.thread.recalled') }}
      </div>

      <!-- 气泡内容 -->
      <div v-else class="chat-bubble" :class="isSelf ? 'chat-bubble--self' : 'chat-bubble--other'">
        <!-- 图片 -->
        <template v-if="isImage">
          <NImage
            v-if="imageUrl"
            :src="imageUrl"
            object-fit="cover"
            class="chat-image"
            :img-props="{ style: 'max-width: 240px; max-height: 240px; border-radius: 6px; display: block;' }"
          />
          <div v-else class="flex h-24 w-40 items-center justify-center rounded bg-muted/40">
            <NSpin size="small" />
          </div>
          <div v-if="message.content" class="mt-1 text-[13px]">
            {{ message.content }}
          </div>
        </template>

        <!-- 文件卡片 -->
        <template v-else-if="isFile">
          <button type="button" class="chat-file-card" @click="handleDownload">
            <Icon icon="lucide:file" width="26" height="26" class="shrink-0 text-primary" />
            <span class="min-w-0 flex-1 text-left">
              <span class="block truncate text-[13px] font-medium">{{ message.fileName }}</span>
              <span class="block text-[11px] opacity-70">{{ formatFileSize(message.fileSize) }}</span>
            </span>
            <Icon icon="lucide:download" width="14" height="14" class="shrink-0 opacity-70" />
          </button>
          <div v-if="message.content" class="mt-1 text-[13px]">
            {{ message.content }}
          </div>
        </template>

        <!-- 文本 -->
        <template v-else>
          <span class="text-[13px] leading-relaxed whitespace-pre-wrap break-words">{{ message.content }}</span>
        </template>
      </div>

      <!-- 元信息：时间 + 发送状态 + 悬浮操作 -->
      <div class="mt-0.5 flex items-center gap-1.5 px-1 text-[11px] text-muted-foreground">
        <template v-if="message.failed">
          <Icon icon="lucide:circle-alert" width="12" height="12" class="text-destructive" />
          <span class="text-destructive">{{ t('chat.thread.send_failed') }}</span>
          <button type="button" class="chat-meta-action" @click="emit('retry')">
            {{ t('chat.thread.resend') }}
          </button>
          <button type="button" class="chat-meta-action" @click="emit('remove')">
            {{ t('chat.thread.remove') }}
          </button>
        </template>
        <template v-else-if="message.pending">
          <Icon icon="lucide:loader-circle" width="12" height="12" class="animate-spin" />
          <span>{{ t('chat.thread.sending') }}</span>
        </template>
        <template v-else>
          <span>{{ formatMessageTime(message.createdTime) }}</span>
          <NTooltip v-if="canRecall">
            <template #trigger>
              <button
                type="button"
                class="chat-meta-action opacity-0 transition-opacity group-hover:opacity-100"
                @click="emit('recall')"
              >
                {{ t('chat.thread.recall') }}
              </button>
            </template>
            {{ t('chat.thread.recall_window', { n: 2 }) }}
          </NTooltip>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.chat-bubble {
  padding: 8px 12px;
  border-radius: 10px;
  word-break: break-word;
}

.chat-bubble--other {
  background: hsl(var(--muted) / 60%);
  color: hsl(var(--foreground));
  border-top-left-radius: 2px;
}

.chat-bubble--self {
  background: hsl(var(--primary) / 12%);
  color: hsl(var(--foreground));
  border-top-right-radius: 2px;
}

.chat-bubble--recalled {
  background: transparent;
  border: 1px dashed hsl(var(--border));
  color: hsl(var(--muted-foreground));
  font-size: 12px;
  font-style: italic;
}

.chat-file-card {
  display: flex;
  gap: 10px;
  align-items: center;
  min-width: 200px;
  max-width: 280px;
  padding: 8px 10px;
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  background: hsl(var(--card));
  color: hsl(var(--foreground));
  cursor: pointer;
  transition: border-color 0.15s ease;
}

.chat-file-card:hover {
  border-color: hsl(var(--primary) / 50%);
}

.chat-meta-action {
  padding: 0;
  border: none;
  background: transparent;
  color: hsl(var(--primary));
  font-size: 11px;
  cursor: pointer;
}

.chat-meta-action:hover {
  text-decoration: underline;
}
</style>
