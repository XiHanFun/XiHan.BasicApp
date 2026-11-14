<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatRecord, ChatRecordItem } from './chat-record'
import { XhButton, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogHeader, XhDialogRoot, XhDialogTitle } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XIconButton from '~/components/common/XIconButton.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { getChatApi } from '../api-contract'
import { chatErrorText, formatFileSize, formatMessageTime } from './chat-helpers'
import { chatRecordTitle, parseChatRecord } from './chat-record'

/**
 * 看一条聊天记录：按原来的先后列出里面的每条消息
 * 附件凭这条聊天记录所在的会话换地址（它的附件列带着里面所有消息的附件），每次点开即时换；
 * 聊天记录套着聊天记录时点进去看里面那份，标题栏给返回
 */
defineOptions({ name: 'ChatRecordDialog' })

const props = defineProps<{
  /** 这条聊天记录所在的会话 */
  conversationId: null | string
  record: ChatRecord | null
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()

/** 一层层点进去的聊天记录，最后一个是眼前这份 */
const stack = ref<ChatRecord[]>([])

watch([
  show,
  () => props.record,
], ([visible, record]) => {
  if (visible) {
    stack.value = record
      ? [record]
      : []
  }
}, { immediate: true })

const current = computed(() => stack.value.at(-1) ?? null)
const title = computed(() => (current.value
  ? chatRecordTitle(t, current.value)
  : t('chat.record.title')))

function nestedOf(item: ChatRecordItem): ChatRecord | null {
  return item.messageType === 'Record'
    ? parseChatRecord(item.content)
    : null
}

function handleOpenNested(item: ChatRecordItem) {
  const nested = nestedOf(item)
  if (nested) {
    stack.value = [
      ...stack.value,
      nested,
    ]
  }
}

function handleBack() {
  stack.value = stack.value.slice(0, -1)
}

/** 在新标签页打开附件：图片与语音内联看，文件按原名下载 */
async function handleOpenFile(fileId: string, download: boolean) {
  const conversationId = props.conversationId
  if (!conversationId) {
    return
  }
  try {
    const signed = await getChatApi().getFileUrl(conversationId, fileId, download)
    window.open(signed.url, '_blank', 'noopener')
  }
  catch (error) {
    toast.danger(chatErrorText(error) || t('chat.thread.download_failed'))
  }
}
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: calc(var(--xh-space-8) * 17)">
      <XhDialogHeader>
        <div class="flex min-w-0 items-center gap-1">
          <XIconButton v-if="stack.length > 1" icon="lucide:arrow-left" :label="t('chat.record.back')" @click="handleBack" />
          <XhDialogTitle class="truncate">
            {{ title }}
          </XhDialogTitle>
        </div>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <ol v-if="current" class="chat-record-list xh-scroll-area m-0 list-none p-0">
          <li v-for="(item, index) in current.items" :key="index" class="flex gap-2 py-2">
            <XUserAvatar :name="item.senderUserName ?? ''" :size="28" class="shrink-0" />
            <div class="min-w-0 flex-1">
              <div class="flex items-baseline gap-2 text-[11px] text-muted-foreground">
                <span class="truncate">{{ item.senderUserName }}</span>
                <span class="shrink-0">{{ formatMessageTime(item.createdAt) }}</span>
              </div>

              <!-- 套着的聊天记录：点进去看 -->
              <XhButton v-if="item.messageType === 'Record'" variant="subtle" size="sm" class="mt-1" :disabled="!nestedOf(item)" @click="handleOpenNested(item)">
                <Icon icon="lucide:messages-square" />
                {{ nestedOf(item) ? t('chat.record.count', { n: nestedOf(item)!.items.length }) : t('chat.record.title') }}
              </XhButton>

              <template v-else>
                <ul v-if="item.attachments.length" class="m-0 mt-1 flex list-none flex-col gap-1 p-0">
                  <li v-for="attachment in item.attachments" :key="attachment.fileId">
                    <button type="button" class="chat-record-file" @click="handleOpenFile(attachment.fileId, item.messageType === 'File')">
                      <Icon
                        :icon="item.messageType === 'Image' ? 'lucide:image' : item.messageType === 'Voice' ? 'lucide:mic' : 'lucide:file'"
                        width="16"
                        height="16"
                        class="shrink-0"
                      />
                      <span class="min-w-0 flex-1 truncate text-left">{{ attachment.fileName }}</span>
                      <span v-if="item.messageType === 'Voice'" class="shrink-0 opacity-70">{{ attachment.durationSeconds ?? 0 }}"</span>
                      <span v-else class="shrink-0 opacity-70">{{ formatFileSize(attachment.fileSize) }}</span>
                    </button>
                  </li>
                </ul>
                <p v-if="item.content" class="m-0 mt-1 text-[13px] leading-relaxed whitespace-pre-wrap break-words text-foreground">
                  {{ item.content }}
                </p>
              </template>
            </div>
          </li>
        </ol>
      </XhDialogBody>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 消息多了在对话框里滚，不把对话框撑出视口 */
.chat-record-list {
  max-block-size: var(--xh-viewport-h-lg);
}

.chat-record-list > li + li {
  border-block-start: var(--xh-stroke-thin) solid var(--xh-border-subtle);
}

/* 附件的一行：图标、文件名、大小或时长，点了在新标签页打开 */
.chat-record-file {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  inline-size: 100%;
  padding: var(--xh-space-1_5) var(--xh-space-2);
  border: var(--xh-stroke-thin) solid var(--xh-border-default);
  border-radius: var(--xh-shape-control);
  background: transparent;
  color: var(--xh-fg-default);
  font-size: var(--xh-font-size-xs);
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-record-file:hover {
  background: var(--xh-bg-subtle-hover);
}

.chat-record-file:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}
</style>
