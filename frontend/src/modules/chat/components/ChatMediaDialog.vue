<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatMessageItem } from '../types'
import type { ChatMediaKind } from '~/contracts'
import { XhButton, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XIconButton from '~/components/common/XIconButton.vue'
import XSegmented from '~/components/common/XSegmented.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { getChatApi } from '../api-contract'
import { useChatFileUrls } from '../composables/useChatFileUrl'
import { chatErrorText, formatFileSize, formatMessageTime } from './chat-helpers'
import { chatGalleryKey, provideChatImageGallery } from './chat-image-gallery'
import ChatImageViewer from './ChatImageViewer.vue'
import ChatMessageImage from './ChatMessageImage.vue'

/**
 * 会话的图片与文件：把会话里发过的图片、文件按时间倒着列出来，逐页往下取
 * 图片点开是这一组的看片浮层；文件点了下载，另能定位到它所在的那条消息
 */
defineOptions({ name: 'ChatMediaDialog' })

const props = defineProps<{
  conversationId: null | string
}>()

const emit = defineEmits<{
  /** 定位到这条消息 */
  locate: [messageId: string]
}>()

const show = defineModel<boolean>('show', { default: false })

/** 每页取几条消息 */
const PAGE_SIZE = 30

const { t } = useI18n()

const kind = ref<ChatMediaKind>('Image')
const messages = shallowRef<ChatMessageItem[]>([])
const hasMore = ref(false)
const loading = ref(false)
let loadSeq = 0

const kindOptions = computed(() => [
  {
    value: 'Image' as const,
    label: t('chat.media.images'),
  },
  {
    value: 'File' as const,
    label: t('chat.media.files'),
  },
])

function notifyFailure(error: unknown) {
  toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
}

async function load(append: boolean) {
  const conversationId = props.conversationId
  if (!conversationId) {
    return
  }
  const seq = ++loadSeq
  loading.value = true
  try {
    const result = await getChatApi().conversationMedia({
      conversationId,
      kind: kind.value,
      beforeMessageId: append
        ? messages.value.at(-1)?.messageId
        : null,
      take: PAGE_SIZE,
    })
    if (seq !== loadSeq) {
      return
    }
    messages.value = append
      ? [
          ...messages.value,
          ...result.items,
        ]
      : result.items
    hasMore.value = result.hasMore
  }
  catch (error) {
    if (seq === loadSeq) {
      notifyFailure(error)
    }
  }
  finally {
    if (seq === loadSeq) {
      loading.value = false
    }
  }
}

// 打开、换会话或换类别都从头取
watch([
  show,
  () => props.conversationId,
  kind,
], ([visible]) => {
  if (visible) {
    messages.value = []
    hasMore.value = false
    void load(false)
  }
})

// ── 图片：一条消息可能有几张，摊开成一格一张 ──────────────────────────
const images = computed(() => (kind.value === 'Image'
  ? messages.value.flatMap(message => (message.attachments ?? []).map(attachment => ({
      messageId: message.messageId,
      conversationId: message.conversationId,
      fileId: attachment.fileId,
      fileName: attachment.fileName,
    })))
  : []))
const imageUrls = useChatFileUrls(() => images.value.map(image => ({ conversationId: image.conversationId, fileId: image.fileId })))
const { items: galleryItems, open: galleryOpen, index: galleryIndex } = provideChatImageGallery(() => (kind.value === 'Image'
  ? messages.value
  : []))

function handleViewImage(messageId: string, fileId: string) {
  const found = galleryItems.value.findIndex(item => item.key === chatGalleryKey(messageId, fileId))
  if (found >= 0) {
    galleryIndex.value = found
    galleryOpen.value = true
  }
}

// ── 文件：一条消息可能有几个，摊开成一行一个 ──────────────────────────
const files = computed(() => (kind.value === 'File'
  ? messages.value.flatMap(message => (message.attachments ?? []).map(attachment => ({
      messageId: message.messageId,
      conversationId: message.conversationId,
      senderUserName: message.senderUserName ?? '',
      createdAt: message.createdAt,
      fileId: attachment.fileId,
      fileName: attachment.fileName,
      fileSize: attachment.fileSize,
    })))
  : []))

async function handleDownload(conversationId: string, fileId: string) {
  try {
    // 签名地址会过期，下载每次即时按会话换取（以附件下载、带原始文件名）
    const signed = await getChatApi().getFileUrl(conversationId, fileId, true)
    window.open(signed.url, '_blank', 'noopener')
  }
  catch (error) {
    toast.danger(chatErrorText(error) || t('chat.thread.download_failed'))
  }
}

function handleLocate(messageId: string) {
  show.value = false
  emit('locate', messageId)
}

const empty = computed(() => !loading.value && messages.value.length === 0)
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: calc(var(--xh-space-8) * 18)">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('chat.media.title') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <XSegmented v-model:value="kind" :options="kindOptions" :aria-label="t('chat.media.title')" />

        <div class="chat-media__pane xh-scroll-area">
          <div v-if="loading && !messages.length" class="flex justify-center py-8">
            <XhSpinner />
          </div>
          <div v-else-if="empty" class="py-8">
            <XhEmptyStateRoot size="sm">
              <XhEmptyStateIndicator>
                <Icon :icon="kind === 'Image' ? 'lucide:image' : 'lucide:file'" width="24" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ kind === 'Image' ? t('chat.media.images_empty') : t('chat.media.files_empty') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
          </div>

          <ul v-else-if="kind === 'Image'" class="chat-media__grid m-0 list-none p-0">
            <li v-for="(image, index) in images" :key="`${image.messageId}:${image.fileId}`">
              <ChatMessageImage
                thumb
                :url="imageUrls[index] ?? ''"
                :alt="image.fileName"
                @select="handleViewImage(image.messageId, image.fileId)"
              />
            </li>
          </ul>

          <ul v-else class="m-0 list-none p-0">
            <li v-for="file in files" :key="`${file.messageId}:${file.fileId}`" class="chat-media__file">
              <button type="button" class="chat-media__file-main" @click="handleDownload(file.conversationId, file.fileId)">
                <Icon icon="lucide:file" width="22" height="22" class="shrink-0 text-primary" />
                <span class="min-w-0 flex-1 text-left">
                  <span class="block truncate text-[13px] text-foreground">{{ file.fileName }}</span>
                  <span class="block truncate text-[11px] text-muted-foreground">
                    {{ formatFileSize(file.fileSize) }} · {{ file.senderUserName }} · {{ formatMessageTime(file.createdAt) }}
                  </span>
                </span>
                <Icon icon="lucide:download" width="14" height="14" class="shrink-0 text-muted-foreground" />
              </button>
              <XIconButton icon="lucide:locate-fixed" :label="t('chat.thread.locate')" @click="handleLocate(file.messageId)" />
            </li>
          </ul>

          <div v-if="hasMore" class="flex justify-center py-2">
            <XhButton variant="subtle" size="sm" :loading="loading" @click="load(true)">
              {{ t('chat.media.load_more') }}
            </XhButton>
          </div>
        </div>
      </XhDialogBody>
    </XhDialogContent>
  </XhDialogRoot>

  <!-- 这一组图片的看片浮层：点哪张从哪张打开，左右翻得到已经取到的其他图片 -->
  <ChatImageViewer v-model:open="galleryOpen" v-model:index="galleryIndex" :items="galleryItems" />
</template>

<style scoped>
/* 定高：换类别、往下取时对话框不跳动 */
.chat-media__pane {
  block-size: var(--xh-viewport-h-lg);
  margin-block-start: var(--xh-space-3);
}

.chat-media__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(calc(var(--xh-space-8) * 3), 1fr));
  gap: var(--xh-space-2);
}

.chat-media__file {
  display: flex;
  gap: var(--xh-space-1);
  align-items: center;
}

.chat-media__file + .chat-media__file {
  border-block-start: var(--xh-stroke-thin) solid var(--xh-border-subtle);
}

.chat-media__file-main {
  display: flex;
  flex: 1;
  gap: var(--xh-space-2_5);
  align-items: center;
  min-inline-size: 0;
  padding: var(--xh-space-2);
  border: none;
  border-radius: var(--xh-shape-control);
  background: transparent;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-media__file-main:hover {
  background: var(--xh-bg-subtle-hover);
}

.chat-media__file-main:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}
</style>
