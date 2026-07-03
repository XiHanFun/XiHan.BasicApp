<script setup lang="ts">
import type { DropdownOption, InputInst } from 'naive-ui'
import type { ChatMemberItem } from '~/types'
import { NButton, NDropdown, NInput, NPopover, NProgress, NTooltip, useMessage } from 'naive-ui'
import { computed, defineAsyncComponent, h, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_PERMISSIONS, CHAT_SEND_KEY_STORAGE_KEY } from '~/constants'
import { Icon } from '~/iconify'
import { useAppContext, useChatStore, useUserStore } from '~/stores'
import { CHAT_MAX_CONTENT_LENGTH, CHAT_MAX_MENTION_COUNT } from '~/types'
import { ChatConversationType, ChatMessageType } from '~/types/enums'
import { LocalStorage } from '~/utils'
import XUserAvatar from '../common/UserAvatar.vue'

defineOptions({ name: 'ChatComposer' })

const props = defineProps<{
  conversationId: string
}>()

// emoji-mart 及其数据（~600KB）异步拆包：首次点开表情按钮才加载
const ChatEmojiPicker = defineAsyncComponent(() => import('./ChatEmojiPicker.vue'))

type ChatSendKey = 'ctrl-enter' | 'enter'

/** 待发附件（粘贴/选择后暂存输入区，发送时才上传） */
interface PendingAttachment {
  id: number
  file: File
  messageType: ChatMessageType
  /** 图片的本地预览 objectURL（发送/移除时 revoke） */
  previewUrl: null | string
}

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
const showMentionPicker = ref(false)
const mentionMembers = ref<ChatMemberItem[]>([])
/** 已插入的 @ 名单：显示名 → userId（发送时按「@名字仍在正文中」过滤） */
const mentionDrafts = new Map<string, string>()

const pendingAttachments = ref<PendingAttachment[]>([])
/** 会话切换时暂存各会话的待发附件（File 对象仅存内存，不入 localStorage 草稿） */
const pendingStash = new Map<string, PendingAttachment[]>()
let attachmentSeq = 0

/** 发送键偏好（QQ 式可切换：Enter 直发 / Ctrl+Enter 发送），localStorage 持久化 */
const sendKey = ref<ChatSendKey>(LocalStorage.get<ChatSendKey>(CHAT_SEND_KEY_STORAGE_KEY) ?? 'enter')

const canSend = computed(() => userStore.hasPermission(CHAT_PERMISSIONS.send))
const isSilenced = computed(() => chatStore.activeConversation?.isSilenced ?? false)
const trimmedDraft = computed(() => draft.value.trim())
const isEditing = computed(() => chatStore.editTarget?.conversationId === props.conversationId && !!chatStore.editTarget)
const replyTarget = computed(() =>
  chatStore.replyTarget?.conversationId === props.conversationId ? chatStore.replyTarget : null)
const isGroupLike = computed(() =>
  chatStore.activeConversation?.conversationType === ChatConversationType.Group
  || chatStore.activeConversation?.conversationType === ChatConversationType.Department)

const placeholder = computed(() =>
  sendKey.value === 'enter' ? t('chat.composer.placeholder_enter') : t('chat.composer.placeholder_ctrl_enter'))

/** 发送模式下拉（当前项打勾，另一项占位对齐） */
const sendKeyOptions = computed<DropdownOption[]>(() => {
  const check = () => h(Icon, { icon: 'lucide:check', width: 14, height: 14 })
  const blank = () => h('span', { style: 'display:inline-block;width:14px' })
  return [
    { key: 'enter', label: t('chat.composer.send_key_enter'), icon: sendKey.value === 'enter' ? check : blank },
    { key: 'ctrl-enter', label: t('chat.composer.send_key_ctrl_enter'), icon: sendKey.value === 'ctrl-enter' ? check : blank },
  ]
})

function handleSendKeySelect(key: number | string) {
  sendKey.value = key === 'ctrl-enter' ? 'ctrl-enter' : 'enter'
  LocalStorage.set(CHAT_SEND_KEY_STORAGE_KEY, sendKey.value)
}

// 会话切换：恢复草稿与待发附件、清 @ 名单与回复/编辑态残留
watch(() => props.conversationId, (id, oldId) => {
  if (oldId) {
    pendingStash.set(oldId, pendingAttachments.value)
  }
  pendingAttachments.value = pendingStash.get(id) ?? []
  draft.value = chatStore.getDraft(id)
  mentionDrafts.clear()
  showMentionPicker.value = false
}, { immediate: true })

// 组件销毁：释放所有会话待发图片的本地预览 URL
onBeforeUnmount(() => {
  pendingStash.set(props.conversationId, pendingAttachments.value)
  for (const list of pendingStash.values()) {
    for (const attachment of list) {
      if (attachment.previewUrl) {
        URL.revokeObjectURL(attachment.previewUrl)
      }
    }
  }
})

// 进入/退出编辑态：正文替换为被编辑消息，退出恢复草稿
watch(isEditing, (editing) => {
  if (editing) {
    draft.value = chatStore.editTarget?.content ?? ''
    void nextTick(() => textInputRef.value?.focus())
  }
  else {
    draft.value = chatStore.getDraft(props.conversationId)
  }
})

// 头像菜单「@TA」→ 在光标处插入提及
watch(() => chatStore.mentionRequest, (request) => {
  if (!request || request.conversationId !== props.conversationId || isEditing.value) {
    return
  }
  const name = request.userName || request.userId
  const textarea = textInputRef.value?.textareaElRef
  const pos = textarea?.selectionStart ?? draft.value.length
  draft.value = `${draft.value.slice(0, pos)}@${name} ${draft.value.slice(pos)}`
  mentionDrafts.set(name, request.userId)
  chatStore.setDraft(props.conversationId, draft.value)
  void nextTick(() => {
    textarea?.focus()
    const caret = pos + name.length + 2
    textarea?.setSelectionRange(caret, caret)
  })
})

function buildReplyPreview(): null | string {
  const target = replyTarget.value
  if (!target) {
    return null
  }
  const body = target.content
    || (target.messageType === ChatMessageType.Image ? '[图片]' : target.fileName ? `[文件] ${target.fileName}` : '')
  return `${target.senderUserName ?? ''}: ${body}`.slice(0, 300)
}

/** 收集仍存在于正文中的 @ 名单 */
function collectMentionIds(content: string): string[] {
  const ids: string[] = []
  for (const [name, userId] of mentionDrafts) {
    if (content.includes(`@${name}`) && !ids.includes(userId)) {
      ids.push(userId)
    }
  }
  return ids.slice(0, CHAT_MAX_MENTION_COUNT)
}

function handleInput() {
  if (!isEditing.value) {
    chatStore.setDraft(props.conversationId, draft.value)
  }
  chatStore.sendTyping(props.conversationId)
  detectMentionTrigger()
}

/** 在光标处插入换行（Enter 直发模式下 Ctrl+Enter 换行，textarea 默认不插入需手动） */
function insertNewlineAtCursor() {
  const textarea = textInputRef.value?.textareaElRef
  const start = textarea?.selectionStart ?? draft.value.length
  const end = textarea?.selectionEnd ?? draft.value.length
  draft.value = `${draft.value.slice(0, start)}\n${draft.value.slice(end)}`
  if (!isEditing.value) {
    chatStore.setDraft(props.conversationId, draft.value)
  }
  void nextTick(() => {
    textarea?.focus()
    textarea?.setSelectionRange(start + 1, start + 1)
  })
}

/** 键盘分发：NInput 的 onKeydown 只接受单个函数；发送键位随偏好切换 */
function handleKeydown(event: KeyboardEvent) {
  if (event.key === 'Enter') {
    // 中文输入法组合态的回车是选词，不发送
    if (event.isComposing) {
      return
    }
    if (sendKey.value === 'ctrl-enter') {
      // Ctrl+Enter 发送模式：普通回车默认换行
      if (event.ctrlKey || event.metaKey) {
        event.preventDefault()
        void handleSendText()
      }
      return
    }
    // Enter 直发模式
    if (event.shiftKey || event.altKey) {
      return
    }
    if (event.ctrlKey || event.metaKey) {
      event.preventDefault()
      insertNewlineAtCursor()
      return
    }
    event.preventDefault()
    void handleSendText()
    return
  }
  if (event.key === 'Escape') {
    if (isEditing.value) {
      cancelEdit()
    }
    else {
      showMentionPicker.value = false
    }
  }
}

/** 群聊中输入 @ 唤起成员选择 */
function detectMentionTrigger() {
  if (!isGroupLike.value || isEditing.value) {
    return
  }
  const textarea = textInputRef.value?.textareaElRef
  const pos = textarea?.selectionStart ?? draft.value.length
  if (pos > 0 && draft.value[pos - 1] === '@') {
    void loadMentionMembers()
    showMentionPicker.value = true
  }
}

async function loadMentionMembers() {
  if (mentionMembers.value.length) {
    return
  }
  try {
    const members = await appContext.apis.chatApi.members(props.conversationId)
    const me = userStore.userInfo?.basicId
    mentionMembers.value = members.filter(member => member.userId !== me)
  }
  catch {
    mentionMembers.value = []
  }
}

function insertMention(member: ChatMemberItem) {
  const textarea = textInputRef.value?.textareaElRef
  const pos = textarea?.selectionStart ?? draft.value.length
  const before = draft.value.slice(0, pos)
  const atIndex = before.lastIndexOf('@')
  if (atIndex < 0) {
    showMentionPicker.value = false
    return
  }
  const name = member.userName || member.userId
  draft.value = `${draft.value.slice(0, atIndex)}@${name} ${draft.value.slice(pos)}`
  mentionDrafts.set(name, member.userId)
  showMentionPicker.value = false
  chatStore.setDraft(props.conversationId, draft.value)
  void nextTick(() => {
    textarea?.focus()
    const caret = atIndex + name.length + 2
    textarea?.setSelectionRange(caret, caret)
  })
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
  if (!isEditing.value) {
    chatStore.setDraft(props.conversationId, draft.value)
  }
  void nextTick(() => {
    textarea?.focus()
    const caret = start + emoji.length
    textarea?.setSelectionRange(caret, caret)
  })
}

async function handleSendText() {
  const content = trimmedDraft.value
  const attachments = pendingAttachments.value
  if ((!content && !attachments.length) || sending.value) {
    return
  }
  if (content.length > CHAT_MAX_CONTENT_LENGTH) {
    message.warning(t('chat.composer.too_long', { max: CHAT_MAX_CONTENT_LENGTH }))
    return
  }

  sending.value = true
  try {
    if (isEditing.value && chatStore.editTarget) {
      await chatStore.editMessage(props.conversationId, chatStore.editTarget.messageId, content)
      draft.value = chatStore.getDraft(props.conversationId)
    }
    else {
      // 待发附件按加入顺序逐个上传并发送；某个失败即中断，剩余附件与正文保留可重试
      for (const attachment of [...attachments]) {
        await uploadAndSend(attachment.file, attachment.messageType)
        const index = attachments.indexOf(attachment)
        if (index >= 0) {
          attachments.splice(index, 1)
        }
        if (attachment.previewUrl) {
          URL.revokeObjectURL(attachment.previewUrl)
        }
      }
      if (!content) {
        return
      }
      const replyPreview = buildReplyPreview()
      const replyToMessageId = replyTarget.value?.messageId ?? null
      const mentionedUserIds = collectMentionIds(content)
      draft.value = ''
      chatStore.setDraft(props.conversationId, '')
      chatStore.replyTarget = null
      mentionDrafts.clear()
      await chatStore.sendMessage({
        conversationId: props.conversationId,
        messageType: ChatMessageType.Text,
        content,
        replyToMessageId,
        mentionedUserIds: mentionedUserIds.length ? mentionedUserIds : null,
      }, { replyPreview })
    }
  }
  catch {
    // 失败条目留在消息流中可重发；请求层已有统一错误提示
  }
  finally {
    sending.value = false
  }
}

function cancelEdit() {
  chatStore.editTarget = null
}

function cancelReply() {
  chatStore.replyTarget = null
}

/** 上传单个文件并作为消息发送（发送阶段调用；失败上抛由调用方保留待发列表） */
async function uploadAndSend(file: File, messageType: ChatMessageType) {
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
  catch (error) {
    message.error(t('chat.composer.upload_failed'))
    throw error
  }
  finally {
    uploadingPercent.value = null
  }
}

/** 暂存到待发列表（QQ 语义：先进输入区，发送时才上传）；不传 forcedType 时按 MIME 归类 */
function addAttachments(files: File[], forcedType?: ChatMessageType) {
  for (const file of files) {
    const messageType = forcedType ?? (file.type.startsWith('image/') ? ChatMessageType.Image : ChatMessageType.File)
    pendingAttachments.value.push({
      id: ++attachmentSeq,
      file,
      messageType,
      previewUrl: messageType === ChatMessageType.Image ? URL.createObjectURL(file) : null,
    })
  }
}

function removeAttachment(id: number) {
  const target = pendingAttachments.value.find(attachment => attachment.id === id)
  if (target?.previewUrl) {
    URL.revokeObjectURL(target.previewUrl)
  }
  pendingAttachments.value = pendingAttachments.value.filter(attachment => attachment.id !== id)
}

function handlePickedFile(event: Event, messageType: ChatMessageType) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) {
    return
  }
  addAttachments([file], messageType)
}

/** 粘贴：剪贴板含文件（截图/复制的文件）时拦截默认粘贴，暂存待发列表；纯文本走默认粘贴 */
function handlePaste(event: ClipboardEvent) {
  if (isEditing.value) {
    return
  }
  const files = [...(event.clipboardData?.files ?? [])]
  if (!files.length) {
    return
  }
  event.preventDefault()
  addAttachments(files)
}
</script>

<template>
  <div class="border-t border-border">
    <div v-if="!canSend" class="py-3 text-center text-xs text-muted-foreground">
      {{ t('chat.composer.no_permission') }}
    </div>
    <div v-else-if="isSilenced" class="py-3 text-center text-xs text-muted-foreground">
      <Icon icon="lucide:mic-off" width="12" height="12" class="mr-1 inline-block align-[-1px]" />
      {{ t('chat.composer.silenced') }}
    </div>
    <template v-else>
      <!-- 编辑态横条 -->
      <div v-if="isEditing" class="mx-2.5 mt-2 flex items-center gap-2 rounded bg-primary/8 px-2 py-1">
        <Icon icon="lucide:pencil" width="12" height="12" class="shrink-0 text-primary" />
        <span class="min-w-0 flex-1 truncate text-xs text-muted-foreground">{{ t('chat.composer.editing') }}</span>
        <button type="button" class="chat-composer-inline-btn" @click="cancelEdit">
          <Icon icon="lucide:x" width="12" height="12" />
        </button>
      </div>

      <!-- 回复引用条 -->
      <div v-else-if="replyTarget" class="mx-2.5 mt-2 flex items-center gap-2 rounded bg-muted/50 px-2 py-1">
        <Icon icon="lucide:reply" width="12" height="12" class="shrink-0 text-primary" />
        <span class="min-w-0 flex-1 truncate text-xs text-muted-foreground">
          {{ t('chat.composer.reply_to', { name: replyTarget.senderUserName ?? '' }) }}：{{ replyTarget.content || replyTarget.fileName || '' }}
        </span>
        <button type="button" class="chat-composer-inline-btn" @click="cancelReply">
          <Icon icon="lucide:x" width="12" height="12" />
        </button>
      </div>

      <!-- 附件上传进度 -->
      <div v-if="uploadingPercent != null" class="mx-2.5 mt-2 flex items-center gap-2">
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

      <!-- 工具条（QQ 式：表情/图片/文件在输入区上方一排） -->
      <div class="flex items-center gap-0.5 px-2 pt-1.5">
        <NPopover v-model:show="showEmojiPicker" trigger="click" placement="top-start" :show-arrow="false" raw>
          <template #trigger>
            <button type="button" class="chat-composer-btn" :title="t('chat.composer.emoji')">
              <Icon icon="lucide:smile" width="18" height="18" />
            </button>
          </template>
          <ChatEmojiPicker @select="insertEmoji" />
        </NPopover>
        <NTooltip>
          <template #trigger>
            <button
              type="button"
              class="chat-composer-btn"
              :disabled="uploadingPercent != null || isEditing"
              @click="imageInputRef?.click()"
            >
              <Icon icon="lucide:image" width="18" height="18" />
            </button>
          </template>
          {{ t('chat.composer.image') }}
        </NTooltip>
        <NTooltip>
          <template #trigger>
            <button
              type="button"
              class="chat-composer-btn"
              :disabled="uploadingPercent != null || isEditing"
              @click="fileInputRef?.click()"
            >
              <Icon icon="lucide:paperclip" width="18" height="18" />
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
      </div>

      <!-- 待发附件（粘贴/选择暂存，发送时才上传）；编辑态下隐藏 -->
      <div v-if="pendingAttachments.length && !isEditing" class="mx-2.5 mt-1.5 flex flex-wrap gap-2">
        <div v-for="attachment in pendingAttachments" :key="attachment.id" class="chat-attach-item">
          <img
            v-if="attachment.previewUrl"
            :src="attachment.previewUrl"
            :alt="attachment.file.name"
            class="chat-attach-thumb"
          >
          <template v-else>
            <Icon icon="lucide:file" width="14" height="14" class="shrink-0 text-muted-foreground" />
            <span class="max-w-40 truncate text-xs">{{ attachment.file.name }}</span>
          </template>
          <button type="button" class="chat-attach-remove" @click="removeAttachment(attachment.id)">
            <Icon icon="lucide:x" width="10" height="10" />
          </button>
        </div>
      </div>

      <!-- 大面积无边框输入区；群聊输入 @ 唤起成员选择 -->
      <div class="relative px-1">
        <NPopover
          v-model:show="showMentionPicker"
          trigger="manual"
          placement="top-start"
          :show-arrow="false"
        >
          <template #trigger>
            <NInput
              ref="textInputRef"
              v-model:value="draft"
              type="textarea"
              :bordered="false"
              :autosize="{ minRows: 3, maxRows: 7 }"
              :maxlength="CHAT_MAX_CONTENT_LENGTH"
              :placeholder="placeholder"
              class="chat-composer-input"
              @input="handleInput"
              @keydown="handleKeydown"
              @paste="handlePaste"
            />
          </template>
          <div class="flex max-h-52 w-52 flex-col overflow-y-auto">
            <div v-if="!mentionMembers.length" class="py-3 text-center text-xs text-muted-foreground">
              {{ t('chat.composer.mention_empty') }}
            </div>
            <button
              v-for="member in mentionMembers"
              :key="member.userId"
              type="button"
              class="chat-mention-item"
              @click="insertMention(member)"
            >
              <XUserAvatar :name="member.userName" :size="24" />
              <span class="min-w-0 flex-1 truncate text-left text-[13px]">{{ member.userName }}</span>
            </button>
          </div>
        </NPopover>
      </div>

      <!-- 底部：发送按钮 + 发送模式下拉（QQ 式分体按钮） -->
      <div class="flex items-center justify-end px-2.5 pb-2.5">
        <div class="chat-send-group">
          <NButton
            type="primary"
            size="small"
            :disabled="(!trimmedDraft && !pendingAttachments.length) || sending"
            :loading="sending"
            class="chat-send-main"
            @click="handleSendText"
          >
            {{ isEditing ? t('chat.composer.save_edit') : t('chat.composer.send') }}
          </NButton>
          <NDropdown :options="sendKeyOptions" trigger="click" placement="top-end" @select="handleSendKeySelect">
            <NButton type="primary" size="small" class="chat-send-arrow">
              <Icon icon="lucide:chevron-up" width="14" height="14" />
            </NButton>
          </NDropdown>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.chat-composer-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
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

/* 无边框输入区：去掉聚焦描边，保持 QQ 式纯净输入面 */
.chat-composer-input :deep(.n-input__border),
.chat-composer-input :deep(.n-input__state-border) {
  display: none;
}

.chat-composer-inline-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  padding: 0;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  flex-shrink: 0;
}

.chat-composer-inline-btn:hover {
  background: hsl(var(--accent));
}

.chat-mention-item {
  display: flex;
  gap: 8px;
  align-items: center;
  padding: 5px 8px;
  border: none;
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
  transition: background 0.12s ease;
}

.chat-mention-item:hover {
  background: hsl(var(--accent));
}

/* 待发附件条目：图片显示缩略图，文件显示图标+名称，右上角浮动移除按钮 */
.chat-attach-item {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 6px;
  border: 1px solid hsl(var(--border));
  border-radius: 6px;
  background: hsl(var(--muted) / 40%);
}

.chat-attach-thumb {
  display: block;
  width: 48px;
  height: 48px;
  object-fit: cover;
  border-radius: 4px;
}

.chat-attach-remove {
  position: absolute;
  top: -6px;
  right: -6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  padding: 0;
  border: none;
  border-radius: 9999px;
  background: hsl(var(--foreground) / 70%);
  color: hsl(var(--background));
  cursor: pointer;
}

.chat-attach-remove:hover {
  background: hsl(var(--destructive));
}

/* 发送分体按钮：主按钮 + 模式箭头拼接（QQ 式） */
.chat-send-group {
  display: inline-flex;
  align-items: stretch;
}

.chat-send-group .chat-send-main {
  min-width: 64px;
  border-top-right-radius: 0;
  border-bottom-right-radius: 0;
}

.chat-send-group .chat-send-arrow {
  padding: 0 6px;
  border-top-left-radius: 0;
  border-bottom-left-radius: 0;
  border-left: 1px solid hsl(var(--primary-foreground, 0 0% 100%) / 30%);
}
</style>
