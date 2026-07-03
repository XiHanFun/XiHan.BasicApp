<script setup lang="ts">
import type { ChatLocalMessage } from '~/stores'
import { NImage, NSpin, NTooltip } from 'naive-ui'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAvatarUrl } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext, useUserStore } from '~/stores'
import { ChatConversationType, ChatMessageType } from '~/types/enums'
import XUserAvatar from '../common/UserAvatar.vue'
import { formatFileSize, formatMessageTime } from './chat-helpers'

defineOptions({ name: 'ChatMessageItemView' })

const props = defineProps<{
  message: ChatLocalMessage
  /** 是否本人发送（气泡靠右） */
  isSelf: boolean
  /** 群聊/部门群显示他人昵称 */
  showSenderName: boolean
  /** 会话类型（已读回执文案区分单聊/群聊） */
  conversationType: ChatConversationType
  /** 本人消息的已读人数（未加载为 null；单聊 >0 即已读） */
  readCount?: null | number
  /** 搜索定位高亮（3s 自清） */
  highlighted?: boolean
}>()

const emit = defineEmits<{
  retry: []
  remove: []
  react: [emoji: string]
}>()

const { t } = useI18n()
const appContext = useAppContext()
const userStore = useUserStore()

const isSystem = computed(() => props.message.messageType === ChatMessageType.System)
const isImage = computed(() => props.message.messageType === ChatMessageType.Image)
const isFile = computed(() => props.message.messageType === ChatMessageType.File)
const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')

/** 被 @ 到我：气泡高亮 */
const mentionsMe = computed(() =>
  !props.isSelf && props.message.mentionedUserIds?.includes(currentUserId.value))

/** 回应按 emoji 分组（含我是否已回应） */
const groupedReactions = computed(() => {
  const groups = new Map<string, { emoji: string, count: number, mine: boolean, users: string[] }>()
  for (const reaction of props.message.reactions ?? []) {
    let group = groups.get(reaction.emoji)
    if (!group) {
      group = { emoji: reaction.emoji, count: 0, mine: false, users: [] }
      groups.set(reaction.emoji, group)
    }
    group.count += 1
    if (reaction.userId === currentUserId.value) {
      group.mine = true
    }
    if (reaction.userName) {
      group.users.push(reaction.userName)
    }
  }
  return [...groups.values()]
})

/** 已读回执文案（仅本人已落库消息） */
const readReceiptLabel = computed(() => {
  if (!props.isSelf || props.message.pending || props.message.failed || props.message.isRecalled) {
    return ''
  }
  if (props.readCount == null) {
    return ''
  }
  if (props.conversationType === ChatConversationType.Single) {
    return props.readCount > 0 ? t('chat.thread.read') : t('chat.thread.unread')
  }
  return props.readCount > 0 ? t('chat.thread.read_count', { n: props.readCount }) : t('chat.thread.unread')
})

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

  <!-- 普通消息行（本人头像取当前用户资料，在右侧；他人取消息快照名首字，在左侧） -->
  <div v-else class="group my-1.5 flex gap-2" :class="isSelf ? 'flex-row-reverse' : 'flex-row'">
    <XUserAvatar
      :avatar="isSelf ? userStore.avatar : null"
      :name="isSelf ? (userStore.nickname || userStore.username) : message.senderUserName"
      :size="30"
    />
    <div class="flex max-w-[86%] min-w-0 flex-col sm:max-w-[72%]" :class="isSelf ? 'items-end' : 'items-start'">
      <!-- 群聊双侧都显示昵称（本人取当前用户资料，右对齐），与对方样式一致 -->
      <div v-if="showSenderName" class="mb-0.5 px-1 text-[11px] text-muted-foreground">
        {{ isSelf ? (userStore.nickname || userStore.username) : message.senderUserName }}
      </div>

      <!-- 已撤回占位 -->
      <div v-if="message.isRecalled" class="chat-bubble chat-bubble--recalled">
        {{ t('chat.thread.recalled') }}
      </div>

      <!-- 气泡内容 -->
      <div
        v-else
        class="chat-bubble"
        :class="[
          isSelf ? 'chat-bubble--self' : 'chat-bubble--other',
          { 'chat-bubble--mention': mentionsMe, 'chat-bubble--highlight': props.highlighted },
        ]"
      >
        <!-- 被 Pin 标记 -->
        <div v-if="message.isPinned" class="mb-1 flex items-center gap-1 text-[11px] text-primary/80">
          <Icon icon="lucide:pin" width="11" height="11" />
          {{ t('chat.thread.pinned_flag') }}
        </div>

        <!-- 回复引用块 -->
        <div v-if="message.replyPreview" class="chat-reply-quote">
          {{ message.replyPreview }}
        </div>

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
          <div class="mt-1 flex items-center gap-2">
            <span v-if="message.content" class="min-w-0 flex-1 text-[13px]">{{ message.content }}</span>
            <button type="button" class="chat-meta-action shrink-0" @click="handleDownload">
              {{ t('chat.thread.download') }}
            </button>
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

      <!-- 回应汇总 -->
      <div v-if="groupedReactions.length" class="mt-1 flex flex-wrap gap-1" :class="isSelf ? 'justify-end' : ''">
        <NTooltip v-for="group in groupedReactions" :key="group.emoji">
          <template #trigger>
            <button
              type="button"
              class="chat-reaction-chip"
              :class="{ 'chat-reaction-chip--mine': group.mine }"
              @click="emit('react', group.emoji)"
            >
              <span>{{ group.emoji }}</span>
              <span class="text-[11px]">{{ group.count }}</span>
            </button>
          </template>
          {{ group.users.join('、') }}
        </NTooltip>
      </div>

      <!-- 元信息：时间 + 状态 + 已读回执 + 悬浮操作 -->
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
          <span v-if="message.editedTime" class="opacity-70">{{ t('chat.thread.edited') }}</span>
          <span v-if="readReceiptLabel" class="text-primary/70">{{ readReceiptLabel }}</span>
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

.chat-bubble--mention {
  box-shadow: inset 0 0 0 1px hsl(var(--primary) / 45%);
}

.chat-bubble--highlight {
  animation: chat-highlight-pulse 3s ease-out;
}

@keyframes chat-highlight-pulse {
  0%,
  60% {
    box-shadow: 0 0 0 2px hsl(var(--primary) / 60%);
  }

  100% {
    box-shadow: 0 0 0 0 hsl(var(--primary) / 0%);
  }
}

.chat-bubble--recalled {
  background: transparent;
  border: 1px dashed hsl(var(--border));
  color: hsl(var(--muted-foreground));
  font-size: 12px;
  font-style: italic;
}

.chat-reply-quote {
  margin-bottom: 6px;
  padding: 4px 8px;
  border-left: 2px solid hsl(var(--primary) / 50%);
  border-radius: 4px;
  background: hsl(var(--muted) / 50%);
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
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

.chat-reaction-chip {
  display: inline-flex;
  gap: 3px;
  align-items: center;
  padding: 1px 7px;
  border: 1px solid hsl(var(--border));
  border-radius: 9999px;
  background: hsl(var(--card));
  font-size: 13px;
  line-height: 1.4;
  cursor: pointer;
  transition: all 0.12s ease;
}

.chat-reaction-chip:hover {
  border-color: hsl(var(--primary) / 50%);
}

.chat-reaction-chip--mine {
  border-color: hsl(var(--primary) / 60%);
  background: hsl(var(--primary) / 10%);
}

.chat-meta-action {
  display: inline-flex;
  align-items: center;
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
