<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatLocalMessage } from '../store'
import type { ChatRecord } from './chat-record'
import type { ChatCallMedia, ChatConversationType } from '~/contracts'
import { XhButton } from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XTooltip from '~/components/common/XTooltip.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { getChatApi } from '../api-contract'
import { callRecordText, parseCallRecord } from '../call/call-helpers'
import { useChatFileUrl, useChatFileUrls } from '../composables/useChatFileUrl'
import { useChatStore } from '../store'
import { chatErrorText, formatFileSize, formatMessageTime, isChatVideoFile, splitMentionText } from './chat-helpers'
import { useChatImageGallery } from './chat-image-gallery'
import { chatRecordPreviewLines, chatRecordTitle, parseChatRecord } from './chat-record'
import ChatAssistantMarkdown from './ChatAssistantMarkdown.vue'
import ChatMessageImage from './ChatMessageImage.vue'
import ChatMessageVideo from './ChatMessageVideo.vue'

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
  /** 头像能点开资料卡（发送人是这一侧的用户；助手与本人的头像不能） */
  avatarClickable?: boolean
  /** 会话成员的名字（userId → 名字）：正文里对得上的「@名字」能点开那个人的资料卡；不给就都是普通文字 */
  mentionNames?: Readonly<Record<string, string>>
}>()

const emit = defineEmits<{
  retry: []
  remove: []
  react: [emoji: string]
  /** 点通话记录：用同样的媒体再打一次 */
  call: [media: ChatCallMedia]
  /** 点聊天记录：打开看里面的消息 */
  record: [record: ChatRecord]
  /** 右键他人头像（QQ 式成员菜单，由消息流统一承载） */
  avatarContextmenu: [event: MouseEvent]
  /** 点他人头像：看他的资料 */
  avatarClick: []
  /** 点引用块：跳到被回复的那条消息 */
  jumpReply: []
  /** 点正文里的「@名字」：看那个人的资料 */
  mention: [userId: string]
}>()

const { t } = useI18n()
const userStore = useUserStore()
const chatStore = useChatStore()

const isSystem = computed(() => props.message.messageType === 'System')
/** 带操作人身份的系统消息=公告卡片（走对话流归属样式）；没有发送人的为中性时间线（居中） */
const isAnnouncement = computed(() => isSystem.value && Boolean(props.message.senderUserId))
const isImage = computed(() => props.message.messageType === 'Image')
const isVoice = computed(() => props.message.messageType === 'Voice')
const isFile = computed(() => props.message.messageType === 'File')
const isCall = computed(() => props.message.messageType === 'Call')
const isRecord = computed(() => props.message.messageType === 'Record')
const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')

/** 通话记录：发送人是发起人，本人发的即我打出去的；群里的是多人通话，说法不点「对方」 */
const callRecord = computed(() => (isCall.value
  ? parseCallRecord(props.message.content)
  : null))
const callRecordLine = computed(() => (callRecord.value
  ? callRecordText(t, callRecord.value, {
      outgoing: props.isSelf,
      group: props.conversationType !== 'Single',
      myUserId: currentUserId.value,
    })
  : t('chat.call.record_unknown')))

/** 聊天记录：合并转发出来的一条，卡片上是标题与前几行，点开看全部 */
const chatRecord = computed(() => (isRecord.value
  ? parseChatRecord(props.message.content)
  : null))
const chatRecordLines = computed(() => (chatRecord.value
  ? chatRecordPreviewLines(chatRecord.value)
  : []))

const isAssistantReply = computed(() => props.conversationType === 'Assistant' && !props.isSelf)

/** 被 @ 到我（点了我的名，或提及了所有人）：气泡高亮 */
const mentionsMe = computed(() =>
  !props.isSelf && (props.message.mentionAll || props.message.mentionedUserIds?.includes(currentUserId.value)))

/** 正文按「@名字」切开：点到人的那几段能点 */
const textSegments = computed(() =>
  splitMentionText(props.message.content ?? '', props.message.mentionedUserIds ?? [], props.mentionNames ?? {}))

/** 回应按 emoji 分组（含我是否已回应） */
const groupedReactions = computed(() => {
  const groups = new Map<string, { emoji: string, count: number, mine: boolean, users: string[] }>()
  for (const reaction of props.message.reactions ?? []) {
    let group = groups.get(reaction.emoji)
    if (!group) {
      group = {
        emoji: reaction.emoji,
        count: 0,
        mine: false,
        users: [],
      }
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
  return [
    ...groups.values(),
  ]
})

/** 已读回执文案（仅本人已落库消息） */
const readReceiptLabel = computed(() => {
  // 通话记录不是谁发的话，不挂已读回执
  if (!props.isSelf || isCall.value || props.message.pending || props.message.failed || props.message.isRecalled) {
    return ''
  }
  if (props.readCount == null) {
    return ''
  }
  if (props.conversationType === 'Single') {
    return props.readCount > 0
      ? t('chat.thread.read')
      : t('chat.thread.unread')
  }
  return props.readCount > 0
    ? t('chat.thread.read_count', { n: props.readCount })
    : t('chat.thread.unread')
})

/** 附件列表（图片/文件消息） */
const attachments = computed(() => props.message.attachments ?? [])

/** 图片相册网格：单图自适应，多图方形缩略图（2 张两列、≥3 张三列） */
const imageGridClass = computed(() => {
  const count = attachments.value.length
  if (count <= 1) {
    return ''
  }
  return count === 2
    ? 'chat-image-grid chat-image-grid--2'
    : 'chat-image-grid chat-image-grid--3'
})

/** 相册各图的可显示地址，与 attachments 逐项对应；还在按会话换签名地址的为空 */
const imageUrls = useChatFileUrls(() => (isImage.value
  ? attachments.value.map(att => ({ conversationId: props.message.conversationId, fileId: att.fileId }))
  : []))

/** 点缩略图从这一张打开会话图片组，左右翻得到同会话的其他图片 */
const gallery = useChatImageGallery()

// ── 语音气泡：单个音频附件，就地播放 ─────────────────────────────────
const voiceAttachment = computed(() => (isVoice.value
  ? attachments.value[0] ?? null
  : null))
const voiceUrl = useChatFileUrl(() => (voiceAttachment.value?.fileId
  ? { conversationId: props.message.conversationId, fileId: voiceAttachment.value.fileId }
  : null))
const audioRef = ref<HTMLAudioElement | null>(null)
const voicePlaying = ref(false)
const voicePlayedRatio = ref(0)

/** 时长优先取发送端记录的秒数；缺失时回落到音频元数据 */
const voiceSeconds = computed(() => voiceAttachment.value?.durationSeconds ?? 0)

const voiceDurationText = computed(() => {
  const seconds = voiceSeconds.value
  return seconds >= 60
    ? `${Math.floor(seconds / 60)}'${String(seconds % 60).padStart(2, '0')}"`
    : `${seconds}"`
})

/** 气泡宽度随时长增长，最短 90px、到上限 60 秒时 220px——微信式的长短条 */
const voiceBarWidth = computed(() => `${Math.round(90 + Math.min(voiceSeconds.value, 60) / 60 * 130)}px`)

/** 他人发来且本机没听过：气泡角上挂红点，播放后消失（记录仅存本机） */
const voiceUnplayed = computed(() =>
  isVoice.value && !props.isSelf && !chatStore.isVoicePlayed(props.message.messageId))

function toggleVoicePlay(): void {
  const audio = audioRef.value
  if (!audio) {
    return
  }
  if (audio.paused) {
    void audio.play().then(() => {
      // 播成功了才算听过：编解码不支持时 play() 会 reject，不该把红点消掉
      chatStore.markVoicePlayed(props.message.messageId)
    }).catch(() => {
      // 编解码不被当前浏览器支持（如 Safari 放 webm/opus）时静默失败，保持暂停态
    })
    return
  }
  audio.pause()
}

function onVoicePlay(): void {
  voicePlaying.value = true
  chatStore.playingMediaId = props.message.messageId
}

function onVoicePause(): void {
  voicePlaying.value = false
  releaseVoiceSlot()
}

function onVoiceTimeUpdate(): void {
  const audio = audioRef.value
  if (!audio?.duration) {
    return
  }
  voicePlayedRatio.value = Math.min(audio.currentTime / audio.duration, 1)
}

function onVoiceEnded(): void {
  voicePlaying.value = false
  voicePlayedRatio.value = 0
  // 播完不触发 pause 事件，独占位得在这里让出来
  releaseVoiceSlot()
}

function releaseVoiceSlot(): void {
  if (chatStore.playingMediaId === props.message.messageId) {
    chatStore.playingMediaId = null
  }
}

// 别的语音或视频开始出声：自己让位。暂停会触发 pause 事件，但那时占位已不是自己，不会互相清空
watch(() => chatStore.playingMediaId, (playingId) => {
  if (playingId !== props.message.messageId && audioRef.value && !audioRef.value.paused) {
    audioRef.value.pause()
  }
})

// 正发声时被卸载（切会话、消息滚出等）：占位得还回去，否则一直挂着已不存在的消息
onBeforeUnmount(releaseVoiceSlot)

// ── 视频附件：认得出的在气泡里就地播放 ──────────────────────────────
/** 浏览器里放不了的视频（解不了的编码）：退回按普通文件显示 */
const unplayableVideos = ref<string[]>([])

function isInlineVideo(attachment: { fileId: string, fileName?: null | string }): boolean {
  return isChatVideoFile(attachment.fileName) && !unplayableVideos.value.includes(attachment.fileId)
}

function handleVideoUnplayable(fileId: string) {
  unplayableVideos.value = [
    ...unplayableVideos.value,
    fileId,
  ]
  toast.warning(t('chat.thread.video_unplayable'))
}

async function handleDownload(fileId: string) {
  if (!fileId) {
    return
  }
  try {
    // 签名地址会过期，下载每次即时按会话换取（以附件下载、带原始文件名）
    const signed = await getChatApi().getFileUrl(props.message.conversationId, fileId, true)
    window.open(signed.url, '_blank', 'noopener')
  }
  catch (error) {
    toast.danger(chatErrorText(error) || t('chat.thread.download_failed'))
  }
}
</script>

<template>
  <!-- 中性系统提示：居中时间线（公告卡片走下方对话流归属样式） -->
  <div v-if="isSystem && !isAnnouncement" class="my-2 flex justify-center">
    <span class="rounded-full bg-muted/60 px-3 py-0.5 text-[11px] text-muted-foreground">
      {{ message.content }}
    </span>
  </div>

  <!-- 普通消息行（本人头像取当前用户资料，在右侧；他人取消息快照名首字，在左侧，右键弹成员菜单） -->
  <div v-else class="group my-1.5 flex gap-2" :class="isSelf ? 'flex-row-reverse' : 'flex-row'">
    <!-- 头像右键独立成员菜单：stop 阻断冒泡（否则消息菜单同时弹出）；本人头像不弹任何菜单 -->
    <!-- 能看资料的头像是一颗按钮（键盘也点得到），别的只是一个头像 -->
    <component
      :is="props.avatarClickable ? 'button' : 'span'"
      class="chat-msg-avatar shrink-0"
      :class="{ 'cursor-pointer': !isSelf }"
      :type="props.avatarClickable ? 'button' : undefined"
      :aria-label="props.avatarClickable ? t('chat.user_card.view_of', { name: message.senderUserName ?? '' }) : undefined"
      @click="props.avatarClickable && emit('avatarClick')"
      @contextmenu.stop.prevent="!isSelf && emit('avatarContextmenu', $event)"
    >
      <XUserAvatar
        :avatar="isSelf ? userStore.avatar : null"
        :name="isSelf ? (userStore.nickname || userStore.username) : message.senderUserName"
        :size="30"
      />
    </component>
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
          {
            'chat-bubble--mention': mentionsMe,
            'chat-bubble--announcement': isAnnouncement,
          },
        ]"
      >
        <!-- 被 Pin 标记 -->
        <div v-if="message.isPinned" class="mb-1 flex items-center gap-1 text-[11px] text-primary/80">
          <Icon icon="lucide:pin" width="11" height="11" />
          {{ t('chat.thread.pinned_flag') }}
        </div>

        <!-- 回复引用块：知道回复的是哪条就能点，跳到那条消息 -->
        <button
          v-if="message.replyPreview && message.replyToMessageId"
          type="button"
          class="chat-reply-quote chat-reply-quote--link"
          :title="t('chat.thread.reply_jump')"
          @click="emit('jumpReply')"
        >
          <span class="chat-reply-quote__text">{{ message.replyPreview }}</span>
        </button>
        <div v-else-if="message.replyPreview" class="chat-reply-quote">
          <span class="chat-reply-quote__text">{{ message.replyPreview }}</span>
        </div>

        <!-- 图片（相册：点击预览可左右切换） -->
        <template v-if="isImage">
          <!-- 点哪张就从哪张打开会话图片组：浮层由消息流统一提供，左右翻得到同会话的其他图片 -->
          <div :class="imageGridClass">
            <ChatMessageImage
              v-for="(att, index) in attachments"
              :key="att.fileId"
              :url="imageUrls[index] ?? ''"
              :alt="att.fileName"
              :thumb="attachments.length > 1"
              @select="gallery.open(message.messageId, att.fileId)"
            />
          </div>
          <div v-if="message.content" class="mt-1 text-[13px]">
            {{ message.content }}
          </div>
        </template>

        <!-- 文件卡片（多文件逐个堆叠）；视频在气泡里就地播放 -->
        <template v-else-if="isFile">
          <div class="flex flex-col gap-1.5">
            <template v-for="att in attachments" :key="att.fileId">
              <ChatMessageVideo
                v-if="isInlineVideo(att)"
                :conversation-id="message.conversationId"
                :file-id="att.fileId"
                :file-name="att.fileName ?? ''"
                :file-size="att.fileSize"
                @download="handleDownload(att.fileId)"
                @unplayable="handleVideoUnplayable(att.fileId)"
              />
              <button
                v-else
                type="button"
                class="chat-file-card"
                @click="handleDownload(att.fileId)"
              >
                <Icon icon="lucide:file" width="26" height="26" class="shrink-0 text-primary" />
                <span class="min-w-0 flex-1 text-left">
                  <span class="block truncate text-[13px] font-medium">{{ att.fileName }}</span>
                  <span class="block text-[11px] opacity-70">{{ formatFileSize(att.fileSize) }}</span>
                </span>
                <Icon icon="lucide:download" width="14" height="14" class="shrink-0 opacity-70" />
              </button>
            </template>
          </div>
          <div v-if="message.content" class="mt-1 text-[13px]">
            {{ message.content }}
          </div>
        </template>

        <template v-else-if="isVoice">
          <button
            type="button"
            class="chat-voice"
            :class="{ 'is-unplayed': voiceUnplayed }"
            :style="{ width: voiceBarWidth }"
            :title="voiceUnplayed ? t('chat.thread.voice_unplayed') : undefined"
            @click="toggleVoicePlay"
          >
            <Icon :icon="voicePlaying ? 'lucide:pause' : 'lucide:play'" width="16" height="16" class="shrink-0" />
            <span class="chat-voice__track">
              <span class="chat-voice__bar" :style="{ width: `${voicePlayedRatio * 100}%` }" />
            </span>
            <span class="chat-voice__time">{{ voiceDurationText }}</span>
            <audio
              v-if="voiceUrl"
              ref="audioRef"
              :src="voiceUrl"
              preload="none"
              @play="onVoicePlay"
              @pause="onVoicePause"
              @timeupdate="onVoiceTimeUpdate"
              @ended="onVoiceEnded"
            />
          </button>
        </template>

        <!-- 通话记录：一行结果（时长、未接听、已拒绝、已取消），点它用同样的媒体再打一次 -->
        <template v-else-if="isCall">
          <button
            type="button"
            class="chat-call-record"
            :title="t('chat.call.call_back')"
            :disabled="!callRecord"
            @click="callRecord && emit('call', callRecord.media)"
          >
            <Icon :icon="callRecord?.media === 'Video' ? 'lucide:video' : 'lucide:phone'" width="14" height="14" class="shrink-0" />
            <span class="text-[13px] leading-relaxed">{{ callRecordLine }}</span>
          </button>
        </template>

        <!-- 聊天记录：标题、前几行与条数，整张卡片可点开 -->
        <template v-else-if="isRecord">
          <button type="button" class="chat-record-card" :disabled="!chatRecord" @click="chatRecord && emit('record', chatRecord)">
            <span class="block truncate text-[13px] font-medium">{{ chatRecord ? chatRecordTitle(t, chatRecord) : t('chat.record.title') }}</span>
            <span v-for="(line, index) in chatRecordLines" :key="index" class="block truncate text-xs opacity-70">{{ line }}</span>
            <span class="chat-record-card__footer">
              <Icon icon="lucide:messages-square" width="12" height="12" class="shrink-0" />
              {{ chatRecord ? t('chat.record.count', { n: chatRecord.items.length }) : t('chat.record.title') }}
            </span>
          </button>
        </template>

        <!-- 公告卡片（钉钉式：小喇叭标题 + 全文保留换行，发布人经头像/昵称归属） -->
        <template v-else-if="isAnnouncement">
          <div class="mb-1 flex items-center gap-1.5 text-[13px] font-semibold text-amber-600">
            <Icon icon="lucide:megaphone" width="14" height="14" />
            {{ t('chat.members.announcement_title') }}
          </div>
          <span class="text-[13px] leading-relaxed whitespace-pre-wrap break-words">{{ message.content }}</span>
        </template>

        <!-- 文本：助手的回复按 Markdown 显示，别的照原文；点到人的「@名字」是按钮，点开他的资料卡 -->
        <template v-else>
          <ChatAssistantMarkdown v-if="isAssistantReply" :text="message.content ?? ''" />
          <span v-else class="text-[13px] leading-relaxed whitespace-pre-wrap break-words">
            <template v-for="(segment, index) in textSegments" :key="index">
              <button
                v-if="segment.userId"
                type="button"
                class="chat-mention-link"
                :title="t('chat.user_card.view_of', { name: segment.text.slice(1) })"
                @click="emit('mention', segment.userId)"
              >
                {{ segment.text }}
              </button>
              <span v-else>{{ segment.text }}</span>
            </template>
          </span>
        </template>
      </div>

      <!-- 回应汇总 -->
      <div v-if="groupedReactions.length" class="mt-1 flex flex-wrap gap-1" :class="isSelf ? 'justify-end' : ''">
        <XTooltip v-for="group in groupedReactions" :key="group.emoji" :content="group.users.join('、')">
          <button
            type="button"
            class="chat-reaction-chip"
            :class="{ 'chat-reaction-chip--mine': group.mine }"
            @click="emit('react', group.emoji)"
          >
            <span>{{ group.emoji }}</span>
            <span class="text-[11px]">{{ group.count }}</span>
          </button>
        </XTooltip>
      </div>

      <!-- 元信息：时间 + 状态 + 已读回执 + 悬浮操作 -->
      <div class="mt-0.5 flex items-center gap-1.5 px-1 text-[11px] text-muted-foreground">
        <template v-if="message.failed">
          <Icon icon="lucide:circle-alert" width="12" height="12" class="text-destructive" />
          <span class="text-destructive">{{ t('chat.thread.send_failed') }}</span>
          <XhButton variant="link" size="sm" class="xh-link-button chat-meta-link" @click="emit('retry')">
            {{ t('chat.thread.resend') }}
          </XhButton>
          <XhButton variant="link" size="sm" class="xh-link-button chat-meta-link" @click="emit('remove')">
            {{ t('chat.thread.remove') }}
          </XhButton>
        </template>
        <template v-else-if="message.pending">
          <Icon icon="lucide:loader-circle" width="12" height="12" class="animate-spin" />
          <span>{{ t('chat.thread.sending') }}</span>
        </template>
        <template v-else>
          <span>{{ formatMessageTime(message.createdAt) }}</span>
          <span v-if="message.editedAt" class="opacity-70">{{ t('chat.thread.edited') }}</span>
          <span v-if="readReceiptLabel" class="text-primary/70">{{ readReceiptLabel }}</span>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* 头像作按钮时清掉全局按钮重置留下的内距，聚焦环贴着圆形头像画 */
.chat-msg-avatar {
  align-self: flex-start;
  padding: 0;
  border-radius: var(--xh-shape-circle);
}

.chat-msg-avatar:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.chat-bubble {
  padding: var(--xh-space-2) var(--xh-space-3);
  border-radius: var(--xh-shape-sheet);
  word-break: break-word;
}

.chat-bubble--other {
  background: color-mix(in srgb, var(--xh-bg-muted) 60%, transparent);
  color: var(--xh-fg-default);
  border-top-left-radius: var(--xh-shape-inset);
}

.chat-bubble--self {
  background: color-mix(in srgb, var(--xh-fg-brand) 12%, transparent);
  color: var(--xh-fg-default);
  border-top-right-radius: var(--xh-shape-inset);
}

.chat-bubble--mention {
  box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--xh-fg-brand) 45%, transparent);
}

/* 正文里点到人的「@名字」：是个按钮，排在文字里、样子是品牌色的字 */
.chat-mention-link {
  display: inline;
  padding: 0;
  border: none;
  border-radius: var(--xh-shape-inset);
  background: none;
  font: inherit;
  font-weight: var(--xh-font-weight-medium);
  color: var(--xh-fg-brand);
  cursor: pointer;
}

.chat-mention-link:hover {
  text-decoration: underline;
}

.chat-mention-link:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.chat-bubble--announcement {
  min-width: 220px;
  max-width: 100%;
  background: hsl(38deg 92% 50% / 8%);
  border: 1px solid hsl(38deg 92% 50% / 25%);
}

.chat-bubble--recalled {
  background: transparent;
  border: 1px dashed var(--xh-border-default);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
  font-style: italic;
}

.chat-reply-quote {
  margin-bottom: 6px;
  padding: var(--xh-space-1) var(--xh-space-2);
  border-left: 2px solid color-mix(in srgb, var(--xh-fg-brand) 50%, transparent);
  border-radius: var(--xh-shape-inset);
  background: color-mix(in srgb, var(--xh-bg-muted) 50%, transparent);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

/* 最多两行：截断放在里面的文字上，外面的块可以是按钮 */
.chat-reply-quote__text {
  display: -webkit-box;
  overflow: hidden;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

/* 能点的引用块：是个按钮，样子还是那块引用 */
.chat-reply-quote--link {
  display: block;
  inline-size: 100%;
  border-block: none;
  border-inline-end: none;
  font-family: inherit;
  text-align: start;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-reply-quote--link:hover {
  background: var(--xh-bg-subtle-hover);
}

.chat-reply-quote--link:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.chat-image-grid {
  display: grid;
  gap: var(--xh-space-1);
}

.chat-image-grid--2 {
  grid-template-columns: repeat(2, 88px);
}

.chat-image-grid--3 {
  grid-template-columns: repeat(3, 88px);
}

/* 语音条：宽度随时长伸缩（见 voiceBarWidth），播放进度铺在轨道上 */
.chat-voice {
  position: relative;
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  padding: var(--xh-space-1_5) var(--xh-space-2_5);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-pill);
  background: var(--xh-bg-surface);
  color: var(--xh-fg-default);
  cursor: pointer;
  transition: border-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-voice:hover {
  border-color: color-mix(in srgb, var(--xh-fg-brand) 50%, transparent);
}

.chat-voice__track {
  position: relative;
  flex: 1;
  height: 4px;
  border-radius: var(--xh-shape-pill);
  background: color-mix(in srgb, var(--xh-fg-muted) 30%, transparent);
  overflow: hidden;
}

.chat-voice__bar {
  position: absolute;
  inset: 0 auto 0 0;
  border-radius: var(--xh-shape-pill);
  background: var(--xh-bg-brand);
  transition: width var(--xh-motion-duration-nudge) var(--xh-motion-ease-continuous);
}

.chat-voice__time {
  flex-shrink: 0;
  font-size: var(--xh-font-size-xs);
  font-variant-numeric: tabular-nums;
  opacity: 0.75;
}

/* 未听红点：挂在语音条右上角外沿，播放后随 is-unplayed 一起消失 */
.chat-voice.is-unplayed::after {
  content: '';
  position: absolute;
  top: -2px;
  inset-inline-end: -2px;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--xh-fg-danger);
  box-shadow: 0 0 0 2px var(--xh-bg-surface);
}

/* 通话记录：气泡里的一行，整行可点（回拨）；全局按钮重置留着 UA 内距，这里清掉 */
.chat-call-record {
  display: inline-flex;
  gap: var(--xh-space-1_5);
  align-items: center;
  padding: 0;
  border-radius: var(--xh-shape-inset);
  color: inherit;
  cursor: pointer;
}

.chat-call-record:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.chat-call-record:disabled {
  cursor: default;
}

/* 聊天记录卡片：与文件卡片同一副承载面，整张可点 */
.chat-record-card {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-0_5);
  inline-size: calc(var(--xh-space-8) * 8);
  max-inline-size: 100%;
  padding: var(--xh-space-2) var(--xh-space-2_5);
  border: var(--xh-stroke-thin) solid var(--xh-border-default);
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-surface);
  color: var(--xh-fg-default);
  text-align: start;
  cursor: pointer;
  transition: border-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-record-card:hover:not(:disabled) {
  border-color: color-mix(in srgb, var(--xh-fg-brand) 50%, transparent);
}

.chat-record-card:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}

.chat-record-card:disabled {
  cursor: default;
}

.chat-record-card__footer {
  display: flex;
  gap: var(--xh-space-1);
  align-items: center;
  margin-block-start: var(--xh-space-1);
  padding-block-start: var(--xh-space-1);
  border-block-start: var(--xh-stroke-thin) solid var(--xh-border-subtle);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
}

.chat-file-card {
  display: flex;
  gap: var(--xh-space-2_5);
  align-items: center;
  min-width: 200px;
  max-width: 280px;
  padding: var(--xh-space-2) var(--xh-space-2_5);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-surface);
  color: var(--xh-fg-default);
  cursor: pointer;
  transition: border-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-file-card:hover {
  border-color: color-mix(in srgb, var(--xh-fg-brand) 50%, transparent);
}

.chat-reaction-chip {
  display: inline-flex;
  gap: var(--xh-space-0_5);
  align-items: center;
  padding: var(--xh-space-0_5) var(--xh-space-1_5);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-pill);
  background: var(--xh-bg-surface);
  font-size: var(--xh-font-size-sm);
  line-height: 1.4;
  cursor: pointer;
  transition:
    background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    border-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-reaction-chip:hover {
  border-color: color-mix(in srgb, var(--xh-fg-brand) 50%, transparent);
}

.chat-reaction-chip--mine {
  border-color: color-mix(in srgb, var(--xh-fg-brand) 60%, transparent);
  background: color-mix(in srgb, var(--xh-fg-brand) 10%, transparent);
}

/* 元信息行里的文字链：字号随这一行压到 xs，经按钮的公开槽给 */
.chat-meta-link {
  --xh-button-font-size: var(--xh-font-size-xs);
}
</style>
