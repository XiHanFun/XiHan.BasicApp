<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatLocalMessage } from '../store'
import type {
  ChatMessageItem,
} from '../types'
import type { ChatRecord } from './chat-record'
import type { ChatContextMenuItem } from './ChatContextMenu.vue'
import type { ChatCallMedia, ChatMemberRole } from '~/contracts'
import { useElementSize, useEventListener } from '@vueuse/core'
import { useMessageFeed, XhButton, XhCheckbox, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhImageViewerCloseTrigger, XhImageViewerContent, XhImageViewerCounter, XhImageViewerFlipHorizontalTrigger, XhImageViewerFlipVerticalTrigger, XhImageViewerImage, XhImageViewerNextTrigger, XhImageViewerPrevTrigger, XhImageViewerResetTrigger, XhImageViewerRoot, XhImageViewerRotateLeftTrigger, XhImageViewerRotateRightTrigger, XhImageViewerToolbar, XhImageViewerViewport, XhImageViewerZoomInTrigger, XhImageViewerZoomOutTrigger, XhPopoverContent, XhPopoverPositioner, XhPopoverRoot, XhPopoverTitle, XhPopoverTrigger, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XIconButton from '~/components/common/XIconButton.vue'
import XInput from '~/components/common/XInput.vue'
import { dialog, toast, xhTranslationsOfCurrentLocale } from '~/composables'

import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { getChatApi } from '../api-contract'
import { callMediaFailureText, callParticipantName } from '../call/call-helpers'
import { ChatCallMediaError } from '../call/call-media'
import { useChatCallStore } from '../call/call-store'
import {
  CHAT_PERMISSIONS,
} from '../constants'
import { useChatStore } from '../store'
import {
  CHAT_EDIT_WINDOW_MINUTES,
  CHAT_RECALL_WINDOW_MINUTES,
} from '../types'
import { chatErrorText, formatMessageTime, hasReadReceipt, messageBodyLabel } from './chat-helpers'
import { provideChatImageGallery } from './chat-image-gallery'
import { CHAT_MAX_FORWARD_MESSAGES, isForwardableMessage } from './chat-record'
import ChatAssistantMarkdown from './ChatAssistantMarkdown.vue'
import ChatCallInviteDialog from './ChatCallInviteDialog.vue'
import ChatComposer from './ChatComposer.vue'
import ChatContextMenu from './ChatContextMenu.vue'
import ChatForwardDialog from './ChatForwardDialog.vue'
import ChatMessageItemView from './ChatMessageItemView.vue'
import ChatPresenceBadge from './ChatPresenceBadge.vue'
import ChatRecordDialog from './ChatRecordDialog.vue'

defineOptions({ name: 'ChatMessageThread' })

const props = withDefaults(defineProps<{
  /** 抽屉窄布局：显示返回按钮 */
  showBack?: boolean
  /** 会话详情正开着：详情钮呈按下态 */
  detailsOpen?: boolean
}>(), {
  showBack: false,
  detailsOpen: false,
})

const emit = defineEmits<{
  back: []
  details: []
}>()

const { t } = useI18n()
const chatStore = useChatStore()

// ── 会话图片组：整个会话一个看片浮层，按消息先后串起所有图片，左右翻得到 ──
const { items: galleryItems, open: galleryOpen, index: galleryIndex } = provideChatImageGallery(() => chatStore.activeMessages)
const userStore = useUserStore()

// 消息流的滚动机器：新消息来了跟到底，用户上翻就松手，往上补历史时按锚元素补偿滚动位置
// 每条消息是 list 的直接子节点、role=article，根上带方向键 / PageUp / PageDown 的巡航
const { api: feed, rootRef, viewportRef, contentRef } = useMessageFeed({
  // 距底 80px 内算在底，沿用旧判定的阈值
  threshold: 80,
  get count() {
    return chatStore.activeMessages.length
  },
  get translations() {
    return xhTranslationsOfCurrentLocale()['message-feed']
  },
})

// 会话内搜索
const showSearch = ref(false)
const searchKeyword = ref('')
const searchLoading = ref(false)
const searchResults = ref<ChatMessageItem[]>([])
const searchHasMore = ref(false)
let searchSeq = 0

// 撤回窗口是时间敏感 UI：30s 心跳重算 canRecall，避免按钮滞留
const nowTick = ref(Date.now())
let tickTimer: ReturnType<typeof setInterval> | null = null

const conversation = computed(() => chatStore.activeConversation)
const conversationId = computed(() => chatStore.activeConversationId)
const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')
const isGroupLike = computed(() =>
  conversation.value?.conversationType === 'Group'
  || conversation.value?.conversationType === 'Department',
)

// 单聊：头像带对端的在线点，副行写在线或离线
const peerUserId = computed(() =>
  conversation.value?.conversationType === 'Single'
    ? conversation.value.peerUserId ?? null
    : null)

// 通话：单聊直接打给对方，群聊与部门群先点名邀请；要通话权限
// 这个页签眼前已经有一通（在响或在通）、群里已经有一通在进行时不能再打
const callStore = useChatCallStore()
const canCall = computed(() => (Boolean(peerUserId.value) || isGroupLike.value) && userStore.hasPermission(CHAT_PERMISSIONS.call))
const groupCall = computed(() => {
  const id = conversationId.value
  return isGroupLike.value && id
    ? callStore.ongoingCalls[id] ?? null
    : null
})
const callBusy = computed(() => callStore.call !== null || callStore.working || groupCall.value !== null)
const showCallInvite = ref(false)
const callInviteMedia = ref<ChatCallMedia>('Voice')

function notifyCallFailure(error: unknown) {
  toast.danger(error instanceof ChatCallMediaError
    ? callMediaFailureText(t, error.failure)
    : chatErrorText(error) || t('common.messages.operation_failed'))
}

async function handleStartCall(media: ChatCallMedia) {
  const id = conversationId.value
  if (!id || callBusy.value) {
    return
  }
  if (isGroupLike.value) {
    callInviteMedia.value = media
    showCallInvite.value = true
    return
  }
  try {
    await callStore.start(id, media)
  }
  catch (error) {
    notifyCallFailure(error)
  }
}

// 群里还没结束的那通电话：没在里面的成员看得到、能加入；这个页签正占着它（通话窗开着）时不出横幅
const ongoingCall = computed(() => (groupCall.value && !(callStore.engaged && callStore.call?.callId === groupCall.value.callId)
  ? groupCall.value
  : null))
/** 我在别的页签或别的端已经在这通电话里 */
const joinedElsewhere = computed(() =>
  ongoingCall.value?.participants.some(participant => participant.userId === currentUserId.value && participant.status === 'Joined') ?? false)
const ongoingCallText = computed(() => {
  const current = ongoingCall.value
  if (!current) {
    return ''
  }
  if (joinedElsewhere.value) {
    return t('chat.call.banner_elsewhere')
  }
  const video = current.media === 'Video'
  if (current.status === 'Ringing') {
    const initiator = current.participants.find(participant => participant.userId === current.initiatorUserId) ?? null
    return t(video
      ? 'chat.call.banner_ringing_video'
      : 'chat.call.banner_ringing_voice', { name: callParticipantName(initiator) || t('chat.call.unknown_peer') })
  }
  return t(video
    ? 'chat.call.banner_ongoing_video'
    : 'chat.call.banner_ongoing_voice', { n: current.participants.filter(participant => participant.status === 'Joined').length })
})
const canJoinCall = computed(() => ongoingCall.value !== null && !joinedElsewhere.value && userStore.hasPermission(CHAT_PERMISSIONS.call))
/** 眼前在响的正是这一通时加入即接听；占着别的通话、请求在路上时不能加入 */
const joinBusy = computed(() => callStore.working || (callStore.call !== null && callStore.call.callId !== ongoingCall.value?.callId))

async function handleJoinCall() {
  const target = ongoingCall.value
  if (!target || joinBusy.value) {
    return
  }
  try {
    await callStore.join(target)
  }
  catch (error) {
    notifyCallFailure(error)
    // 进不去多半是它变了（结束了、满了）：重取一遍
    callStore.loadOngoing(target.conversationId).catch(() => {})
  }
}

// 打开群会话时取它里面的通话，之后的变化经会话组推来
watch([
  conversationId,
  isGroupLike,
], ([id, group]) => {
  callStore.watchConversation(group && id
    ? id
    : null).catch(() => {})
}, { immediate: true })

const conversationTypeLabel = computed(() => {
  switch (conversation.value?.conversationType) {
    case 'Single':
      return t('chat.type.single')
    case 'Group':
      return t('chat.type.group')
    case 'Department':
      return t('chat.type.department')
    case 'Assistant':
      return t('chat.type.assistant')
    default:
      return ''
  }
})

const historyLoading = computed(() =>
  conversationId.value
    ? chatStore.historyLoading[conversationId.value] ?? false
    : false,
)
const hasMoreOlder = computed(() =>
  conversationId.value
    ? chatStore.hasMoreOlder[conversationId.value] ?? false
    : false,
)

function canRecall(item: ChatLocalMessage): boolean {
  if (item.isRecalled || item.pending || item.failed) {
    return false
  }
  if (item.senderUserId !== currentUserId.value) {
    return false
  }
  if (!userStore.hasPermission(CHAT_PERMISSIONS.send)) {
    return false
  }
  const created = Date.parse(item.createdAt)
  return !Number.isNaN(created) && nowTick.value - created <= CHAT_RECALL_WINDOW_MINUTES * 60 * 1000
}

function canEdit(item: ChatLocalMessage): boolean {
  if (item.isRecalled || item.pending || item.failed || item.messageType !== 'Text') {
    return false
  }
  if (item.senderUserId !== currentUserId.value || !userStore.hasPermission(CHAT_PERMISSIONS.send)) {
    return false
  }
  const created = Date.parse(item.createdAt)
  return !Number.isNaN(created) && nowTick.value - created <= CHAT_EDIT_WINDOW_MINUTES * 60 * 1000
}

// Pin 权限：单聊双方皆可，群/部门群仅群主与管理员（与后端不变量一致）
const canPin = computed(() => {
  if (!userStore.hasPermission(CHAT_PERMISSIONS.send) || !conversation.value) {
    return false
  }
  if (conversation.value.conversationType === 'Single') {
    return true
  }
  return conversation.value.memberRole === 'Owner'
    || conversation.value.memberRole === 'Admin'
})

const pinnedList = computed(() =>
  conversationId.value
    ? chatStore.pinnedMessages[conversationId.value] ?? []
    : [])

/** 公告首行（横条内单行预览，全文在弹层） */
/**
 * 公告全文的浮层与公告条等宽（两侧各让出一点边距），读起来是这一条往下展开
 * 浮层缺省按内容收窄、另有自己的最大宽度，这里把两样都换成公告条的宽度；可用宽度的上限仍归组件库
 */
const ANNOUNCEMENT_PANEL_INSET = 24
const announcementBarRef = ref<HTMLElement | null>(null)
const { width: announcementBarWidth } = useElementSize(announcementBarRef)
const announcementPanelStyle = computed(() => {
  const width = announcementBarWidth.value - ANNOUNCEMENT_PANEL_INSET
  return width > 0
    ? { 'inlineSize': `${width}px`, '--xh-popover-max-w': `${width}px` }
    : undefined
})

const announcementFirstLine = computed(() =>
  (conversation.value?.announcement ?? '').split('\n')[0] ?? '')

/** 本人消息已读回执：已读位加载前为 null（不显示），助手会话也是 null */
function readCountOf(item: ChatLocalMessage): null | number {
  const id = conversationId.value
  if (!id || item.senderUserId !== currentUserId.value || !hasReadReceipt(conversation.value?.conversationType)) {
    return null
  }
  if (!chatStore.readPositions[id]) {
    return null
  }
  return chatStore.readCountFor(id, item.seq)
}

function handleReply(item: ChatLocalMessage) {
  chatStore.editTarget = null
  chatStore.replyTarget = item
}

function handleEdit(item: ChatLocalMessage) {
  chatStore.replyTarget = null
  chatStore.editTarget = item
}

/** 用户操作的失败由界面提示（请求层不弹统一提示） */
function notifyFailure(error: unknown) {
  toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
}

function handleReact(item: ChatLocalMessage, emoji: string) {
  if (!conversationId.value) {
    return
  }
  chatStore.toggleReaction(conversationId.value, item.messageId, emoji).catch(notifyFailure)
}

function handlePin(item: ChatLocalMessage, pin: boolean) {
  if (!conversationId.value) {
    return
  }
  const action = pin
    ? chatStore.pinMessage(conversationId.value, item.messageId)
    : chatStore.unpinMessage(conversationId.value, item.messageId)
  action.catch(notifyFailure)
}

// ===== 右键菜单（QQ 式：分离的快捷表情条 + 操作菜单卡） =====

const QUICK_REACTIONS = ['👍', '❤️', '😂', '😮', '😢', '🎉']

const ctxShow = ref(false)
const ctxX = ref(0)
const ctxY = ref(0)
const ctxMessage = ref<ChatLocalMessage | null>(null)

// 头像右键成员菜单状态（与消息菜单互斥，函数定义见下方成员菜单段）
const memberCtxShow = ref(false)
const memberCtxX = ref(0)
const memberCtxY = ref(0)
const memberTarget = ref<ChatLocalMessage | null>(null)
const memberTargetRole = ref<ChatMemberRole | null>(null)
const memberTargetSilenced = ref(false)

// 多选的状态（右键菜单要看它，函数定义见下方多选段）
const selecting = ref(false)
const selectedIds = ref<string[]>([])

const ctxItems = computed<ChatContextMenuItem[]>(() => {
  const target = ctxMessage.value
  if (!target) {
    return []
  }
  const items: ChatContextMenuItem[] = []
  if (target.messageType === 'Text' && target.content) {
    items.push({
      key: 'copy',
      label: t('chat.thread.copy'),
      icon: 'lucide:copy',
    })
  }
  items.push({
    key: 'reply',
    label: t('chat.thread.reply'),
    icon: 'lucide:reply',
  })
  items.push({
    key: 'forward',
    label: t('chat.thread.forward'),
    icon: 'lucide:corner-up-right',
  })
  items.push({
    key: 'select',
    label: t('chat.thread.select'),
    icon: 'lucide:list-checks',
  })
  if (canEdit(target)) {
    items.push({
      key: 'edit',
      label: t('chat.thread.edit'),
      icon: 'lucide:pencil',
    })
  }
  if (canPin.value) {
    items.push(target.isPinned
      ? {
          key: 'unpin',
          label: t('chat.thread.unpin'),
          icon: 'lucide:pin-off',
        }
      : {
          key: 'pin',
          label: t('chat.thread.pin'),
          icon: 'lucide:pin',
        })
  }
  if (canRecall(target)) {
    items.push({
      key: 'recall',
      label: t('chat.thread.recall'),
      icon: 'lucide:undo-2',
      danger: true,
      divided: true,
    })
  }
  return items
})

function openContextMenu(event: MouseEvent, item: ChatLocalMessage) {
  // 多选时整行只管勾选，不弹菜单
  if (selecting.value) {
    event.preventDefault()
    return
  }
  // 系统提示/通话记录/已撤回/未落库消息无可用操作
  if (item.messageType === 'System' || item.messageType === 'Call' || item.isRecalled || item.pending || item.failed) {
    return
  }
  event.preventDefault()
  memberCtxShow.value = false
  ctxMessage.value = item
  ctxX.value = event.clientX
  ctxY.value = event.clientY
  ctxShow.value = true
}

function handleCtxReact(emoji: string) {
  if (ctxMessage.value) {
    handleReact(ctxMessage.value, emoji)
  }
}

// ===== 转发：单条从右键菜单来，多条先进多选 =====

const showForward = ref(false)
const forwardMessages = ref<ChatLocalMessage[]>([])
const forwardMerge = ref(false)

function openForward(messages: ChatLocalMessage[], merge: boolean) {
  forwardMessages.value = messages
  forwardMerge.value = merge
  showForward.value = true
}

// ===== 多选：勾选消息后逐条或合并转发 =====

/** 勾选的消息按会话里的先后排 */
const selectedMessages = computed(() => {
  const picked = new Set(selectedIds.value)
  return chatStore.activeMessages.filter(item => picked.has(item.messageId))
})

function startSelecting(item: ChatLocalMessage) {
  selecting.value = true
  selectedIds.value = [item.messageId]
}

function stopSelecting() {
  selecting.value = false
  selectedIds.value = []
}

function toggleSelected(item: ChatLocalMessage) {
  if (!isForwardableMessage(item)) {
    return
  }
  if (selectedIds.value.includes(item.messageId)) {
    selectedIds.value = selectedIds.value.filter(id => id !== item.messageId)
    return
  }
  if (selectedIds.value.length >= CHAT_MAX_FORWARD_MESSAGES) {
    toast.warning(t('chat.thread.select_limit', { max: CHAT_MAX_FORWARD_MESSAGES }))
    return
  }
  selectedIds.value = [
    ...selectedIds.value,
    item.messageId,
  ]
}

function handleRowClick(item: ChatLocalMessage) {
  if (selecting.value) {
    toggleSelected(item)
  }
}

// Esc 退出多选：刚从右键菜单过来时焦点不在线程里，得在窗口上听；转发对话框开着时 Esc 归它
useEventListener(window, 'keydown', (event: KeyboardEvent) => {
  if (selecting.value && event.key === 'Escape' && !showForward.value) {
    stopSelecting()
  }
})

// 换了会话，勾选的是上一个会话的消息：收起多选
watch(conversationId, stopSelecting)

// ===== 聊天记录：点卡片看里面的消息 =====

const showRecord = ref(false)
const openedRecord = ref<ChatRecord | null>(null)

function handleOpenRecord(record: ChatRecord) {
  openedRecord.value = record
  showRecord.value = true
}

// ===== 头像右键成员菜单（QQ 式；状态 refs 声明在消息菜单段旁） =====

async function openMemberMenu(event: MouseEvent, item: ChatLocalMessage) {
  const convId = conversationId.value
  // 中性系统提示没有发送人，本人的头像不弹菜单
  if (!convId || !item.senderUserId || item.senderUserId === currentUserId.value) {
    return
  }
  event.preventDefault()
  ctxShow.value = false
  memberTarget.value = item
  memberTargetRole.value = null
  memberTargetSilenced.value = false
  memberCtxX.value = event.clientX
  memberCtxY.value = event.clientY
  memberCtxShow.value = true
  // 异步补充目标成员角色/禁言态（群治理项按此显隐，加载前先展示通用项）
  try {
    const members = await getChatApi().members(convId)
    const target = members.find(member => member.userId === item.senderUserId)
    if (target && memberTarget.value?.senderUserId === target.userId) {
      memberTargetRole.value = target.memberRole
      memberTargetSilenced.value = target.isSilenced
    }
  }
  catch {
    // 拉取失败仅隐藏治理项
  }
}

const memberCtxItems = computed<ChatContextMenuItem[]>(() => {
  const target = memberTarget.value
  const conv = conversation.value
  if (!target || !conv) {
    return []
  }
  const items: ChatContextMenuItem[] = [
    {
      key: 'dm',
      label: t('chat.member_menu.send_message'),
      icon: 'lucide:message-circle',
    },
  ]
  if (isGroupLike.value) {
    items.push({
      key: 'mention',
      label: t('chat.member_menu.mention'),
      icon: 'lucide:at-sign',
    })
  }
  const iAmOwner = conv.memberRole === 'Owner'
  const iCanManage = (iAmOwner || conv.memberRole === 'Admin')
    && userStore.hasPermission(CHAT_PERMISSIONS.manage)
  const role = memberTargetRole.value
  if (conv.conversationType === 'Group' && iAmOwner
    && userStore.hasPermission(CHAT_PERMISSIONS.manage) && role && role !== 'Owner') {
    items.push(role === 'Admin'
      ? {
          key: 'unset-admin',
          label: t('chat.member_menu.unset_admin'),
          icon: 'lucide:shield-off',
          divided: true,
        }
      : {
          key: 'set-admin',
          label: t('chat.member_menu.set_admin'),
          icon: 'lucide:shield',
          divided: true,
        })
  }
  if (isGroupLike.value && iCanManage && role === 'Member') {
    items.push(memberTargetSilenced.value
      ? {
          key: 'unsilence',
          label: t('chat.members.unsilence'),
          icon: 'lucide:mic',
        }
      : {
          key: 'silence',
          label: t('chat.members.silence'),
          icon: 'lucide:mic-off',
        })
  }
  if (conv.conversationType === 'Group' && iCanManage && role && role !== 'Owner') {
    items.push({
      key: 'remove',
      label: t('chat.member_menu.remove'),
      icon: 'lucide:user-minus',
      danger: true,
      divided: true,
    })
  }
  return items
})

function handleMemberCtxSelect(key: string) {
  const target = memberTarget.value
  const convId = conversationId.value
  if (!target || !convId) {
    return
  }
  const userId = target.senderUserId
  if (!userId) {
    return
  }
  const userName = target.senderUserName ?? ''
  switch (key) {
    case 'dm':
      chatStore.startSingleConversation(userId).catch(notifyFailure)
      break
    case 'mention':
      chatStore.requestMention(convId, userId, userName)
      break
    case 'set-admin':
      getChatApi().setMemberRole(convId, userId, 'Admin').catch(notifyFailure)
      break
    case 'unset-admin':
      getChatApi().setMemberRole(convId, userId, 'Member').catch(notifyFailure)
      break
    case 'silence':
      getChatApi().setMemberSilence(convId, userId, true).catch(notifyFailure)
      break
    case 'unsilence':
      getChatApi().setMemberSilence(convId, userId, false).catch(notifyFailure)
      break
    case 'remove':
      void dialog.confirm({
        badge: 'warning',
        tone: 'danger',
        title: t('chat.member_menu.remove'),
        content: t('chat.members.remove_confirm', { name: userName }),
        okText: t('chat.start.confirm'),
        cancelText: t('chat.start.cancel'),
        onOk: async () => {
          try {
            await getChatApi().removeMember(convId, userId)
            chatStore.loadConversations().catch(() => {})
          }
          catch (error) {
            notifyFailure(error)
          }
        },
      })
      break
  }
}

async function handleCtxSelect(key: string | number) {
  ctxShow.value = false
  const target = ctxMessage.value
  if (!target) {
    return
  }
  switch (key) {
    case 'copy':
      try {
        await navigator.clipboard.writeText(target.content ?? '')
        toast.success(t('chat.thread.copied'))
      }
      catch (error) {
        toast.danger(chatErrorText(error) || t('chat.thread.copy_failed'))
      }
      break
    case 'reply':
      handleReply(target)
      break
    case 'forward':
      openForward([target], false)
      break
    case 'select':
      startSelecting(target)
      break
    case 'edit':
      handleEdit(target)
      break
    case 'pin':
      handlePin(target, true)
      break
    case 'unpin':
      handlePin(target, false)
      break
    case 'recall':
      void handleRecall(target)
      break
  }
}

// ===== 会话内搜索 =====

const isDetached = computed(() =>
  conversationId.value
    ? Boolean(chatStore.detachedConversations[conversationId.value])
    : false)

async function runSearch(loadMore = false) {
  const id = conversationId.value
  const keyword = searchKeyword.value.trim()
  if (!id || !keyword) {
    searchResults.value = []
    searchHasMore.value = false
    return
  }
  const seq = ++searchSeq
  searchLoading.value = true
  try {
    const before = loadMore
      ? searchResults.value.at(-1)?.messageId
      : null
    const result = await getChatApi().searchMessages({
      conversationId: id,
      keyword,
      beforeMessageId: before,
      take: 20,
    })
    if (seq !== searchSeq) {
      return
    }
    searchResults.value = loadMore
      ? [
          ...searchResults.value,
          ...result.items,
        ]
      : result.items
    searchHasMore.value = result.hasMore
  }
  catch (error) {
    if (seq === searchSeq) {
      searchResults.value = []
      searchHasMore.value = false
      notifyFailure(error)
    }
  }
  finally {
    if (seq === searchSeq) {
      searchLoading.value = false
    }
  }
}

async function handleLocate(item: ChatMessageItem) {
  const id = conversationId.value
  if (!id) {
    return
  }
  showSearch.value = false
  try {
    await chatStore.jumpToMessage(id, item.messageId)
    await nextTick()
    document.getElementById(`chat-msg-${item.messageId}`)?.scrollIntoView({ block: 'center' })
  }
  catch {
    // 定位失败静默（消息可能已被清理）
  }
}

async function handleBackToLatest() {
  const id = conversationId.value
  if (!id) {
    return
  }
  try {
    await chatStore.reloadLatest(id)
    scrollToBottom()
  }
  catch {
    // 静默
  }
}

function toggleSearch() {
  showSearch.value = !showSearch.value
  if (!showSearch.value) {
    searchKeyword.value = ''
    searchResults.value = []
    searchHasMore.value = false
  }
}

function isNearBottom(): boolean {
  return feed.value.atBottom
}

function scrollToBottom() {
  void nextTick(() => feed.value.scrollToBottom())
}

async function handleLoadOlder() {
  const id = conversationId.value
  if (!id) {
    return
  }
  // 往上补历史后视口停在原消息处：粘底句柄按锚元素补偿，这里不再手算 scrollTop
  await chatStore.loadOlder(id).catch(() => undefined)
}

function handleScroll() {
  const el = viewportRef.value
  if (el && el.scrollTop < 40 && hasMoreOlder.value && !historyLoading.value) {
    void handleLoadOlder()
  }
}

async function handleRecall(item: ChatLocalMessage) {
  if (!conversationId.value) {
    return
  }
  try {
    await chatStore.recallMessage(conversationId.value, item.messageId)
  }
  catch (error) {
    toast.danger(chatErrorText(error) || t('chat.thread.recall_failed'))
  }
}

// 会话切换：滚到底部
watch(conversationId, (id) => {
  if (id) {
    scrollToBottom()
  }
})

// 新消息追加：本人消息或视口在底部附近时跟随滚动
watch(() => chatStore.activeMessages.length, (len, prevLen) => {
  if (len > (prevLen ?? 0)) {
    const last = chatStore.activeMessages[len - 1]
    if (last?.senderUserId === currentUserId.value || isNearBottom()) {
      scrollToBottom()
    }
  }
})

// 助手回复增量：视口在底部附近时跟随滚动，让生成过程一直可见
const assistantStream = computed(() => chatStore.activeAssistantStream)
watch(() => assistantStream.value?.text.length ?? 0, () => {
  if (isNearBottom()) {
    scrollToBottom()
  }
})

onMounted(() => {
  tickTimer = setInterval(() => {
    nowTick.value = Date.now()
  }, 30000)
  scrollToBottom()
})

onBeforeUnmount(() => {
  if (tickTimer) {
    clearInterval(tickTimer)
    tickTimer = null
  }
})
</script>

<template>
  <!-- 未选择会话的空态 -->
  <div v-if="!conversation" class="flex h-full items-center justify-center">
    <XhEmptyStateRoot size="sm">
      <XhEmptyStateIndicator>
        <Icon icon="lucide:mouse-pointer-click" width="28" height="28" />
      </XhEmptyStateIndicator>
      <XhEmptyStateTitle>{{ t('chat.thread.select_conversation_title') }}</XhEmptyStateTitle>
      <XhEmptyStateDescription>{{ t('chat.thread.select_conversation') }}</XhEmptyStateDescription>
    </XhEmptyStateRoot>
  </div>

  <div v-else class="flex h-full min-h-0 flex-col">
    <!-- 会话头（固定高度：单聊无副行时与群聊保持一致） -->
    <div class="flex h-[56px] shrink-0 items-center gap-2 border-b border-border px-3">
      <XIconButton v-if="props.showBack" icon="lucide:arrow-left" :label="t('header.toolbar.nav_back')" @click="emit('back')" />
      <ChatPresenceBadge :user-id="peerUserId">
        <XUserAvatar :avatar="conversation.avatar" :name="conversation.displayName" :size="32" />
      </ChatPresenceBadge>
      <div class="min-w-0 flex-1">
        <div class="flex items-center gap-1.5">
          <span class="truncate text-sm font-semibold text-foreground">{{ conversation.displayName }}</span>
          <XhTagRoot variant="subtle" size="sm">
            <XhTagLabel>
              {{ conversationTypeLabel }}
            </XhTagLabel>
          </XhTagRoot>
        </div>
        <div v-if="isGroupLike" class="text-[11px] text-muted-foreground">
          {{ t('chat.members.count', { n: conversation.memberCount }) }}
        </div>
        <div v-else-if="peerUserId" class="text-[11px] text-muted-foreground">
          {{ chatStore.isUserOnline(peerUserId) ? t('chat.presence.online') : t('chat.presence.offline') }}
        </div>
      </div>
      <!-- 会话头的操作：语音与视频通话（群里先点名邀请），搜索与详情（资料、设置与成员都收在详情里） -->
      <template v-if="canCall">
        <XIconButton icon="lucide:phone" :label="t('chat.call.voice')" :disabled="callBusy" @click="handleStartCall('Voice')" />
        <XIconButton icon="lucide:video" :label="t('chat.call.video')" :disabled="callBusy" @click="handleStartCall('Video')" />
      </template>
      <XIconButton icon="lucide:search" :label="t('chat.thread.search')" :variant="showSearch ? 'subtle' : 'ghost'" :aria-pressed="showSearch" @click="toggleSearch" />
      <XIconButton
        icon="lucide:panel-right"
        :label="t('chat.details.title')"
        :variant="props.detailsOpen ? 'subtle' : 'ghost'"
        :aria-pressed="props.detailsOpen"
        @click="emit('details')"
      />
    </div>

    <!-- 会话内搜索面板 -->
    <div v-if="showSearch" class="border-b border-border bg-muted/20 px-3 py-2">
      <XInput
        v-model:value="searchKeyword"
        size="sm"
        clearable
        :placeholder="t('chat.thread.search_placeholder')"
        :aria-label="t('chat.thread.search')"
        @enter="runSearch(false)"
        @clear="searchResults = []"
      >
        <template #prefix>
          <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
        </template>
      </XInput>
      <div v-if="searchLoading" class="flex justify-center py-3">
        <XhSpinner />
      </div>
      <div v-else-if="searchResults.length" class="mt-1.5 max-h-56 overflow-y-auto">
        <button
          v-for="hit in searchResults"
          :key="hit.messageId"
          type="button"
          class="chat-search-hit"
          @click="handleLocate(hit)"
        >
          <span class="flex items-center justify-between gap-2">
            <span class="text-[11px] text-muted-foreground">{{ hit.senderUserName }} · {{ formatMessageTime(hit.createdAt) }}</span>
            <span class="text-[11px] text-primary">{{ t('chat.thread.locate') }}</span>
          </span>
          <span class="block truncate text-left text-xs text-foreground">{{ messageBodyLabel(hit) }}</span>
        </button>
        <div v-if="searchHasMore" class="flex justify-center py-1">
          <XhButton variant="subtle" size="sm" @click="runSearch(true)">
            {{ t('chat.thread.load_more') }}
          </XhButton>
        </div>
      </div>
      <div v-else-if="searchKeyword.trim()" class="py-3 text-center text-xs text-muted-foreground">
        {{ t('chat.thread.search_empty') }}
      </div>
    </div>

    <!-- 群里有通话在进行：谁发起的、几个人在里面，没在里面的可以加入 -->
    <div v-if="ongoingCall" class="chat-call-banner" role="status">
      <Icon :icon="ongoingCall.media === 'Video' ? 'lucide:video' : 'lucide:phone'" width="13" height="13" class="chat-call-banner__icon shrink-0" />
      <span class="min-w-0 flex-1 truncate text-xs text-muted-foreground">{{ ongoingCallText }}</span>
      <XhButton v-if="canJoinCall" size="sm" variant="subtle" tone="success" :disabled="joinBusy" @click="handleJoinCall">
        {{ t('chat.call.banner_join') }}
      </XhButton>
    </div>

    <!-- 群公告横条（同置顶栏样式，点开看全文，换行经 pre-wrap 正确渲染） -->
    <div v-if="isGroupLike && conversation.announcement" ref="announcementBarRef" class="border-b border-border bg-amber-500/5">
      <XhPopoverRoot placement="bottom">
        <!-- 整条横栏就是触发器：点哪都展开全文 -->
        <XhPopoverTrigger class="flex w-full cursor-pointer items-center gap-2 px-3 py-1.5 text-start">
          <Icon icon="lucide:megaphone" width="13" height="13" class="shrink-0 text-amber-500" />
          <span class="min-w-0 flex-1 truncate text-xs text-muted-foreground">
            {{ announcementFirstLine }}
          </span>
          <Icon icon="lucide:chevron-down" width="12" height="12" class="shrink-0 text-muted-foreground/60" />
        </XhPopoverTrigger>
        <XhPopoverPositioner>
          <XhPopoverContent :style="announcementPanelStyle">
            <div class="max-h-64 overflow-y-auto">
              <!-- 标题部件给浮层起名（content 的 aria-labelledby 指向它） -->
              <XhPopoverTitle class="mb-1 flex items-center gap-1.5 text-[13px] font-semibold text-foreground">
                <Icon icon="lucide:megaphone" width="13" height="13" class="text-amber-500" />
                {{ t('chat.members.announcement_title') }}
              </XhPopoverTitle>
              <div class="text-xs leading-relaxed whitespace-pre-wrap text-muted-foreground">
                {{ conversation.announcement }}
              </div>
            </div>
          </XhPopoverContent>
        </XhPopoverPositioner>
      </XhPopoverRoot>
    </div>

    <!-- Pin 消息栏 -->
    <div v-if="pinnedList.length" class="flex items-center gap-2 border-b border-border bg-primary/5 px-3 py-1.5">
      <Icon icon="lucide:pin" width="13" height="13" class="shrink-0 text-primary" />
      <span class="min-w-0 flex-1 truncate text-xs text-muted-foreground">
        {{ pinnedList[0] ? messageBodyLabel(pinnedList[0]) : '' }}
      </span>
      <XhPopoverRoot placement="bottom-end">
        <XhPopoverTrigger as-child>
          <XhButton variant="link" size="sm" class="xh-link-button">
            {{ pinnedList.length }}
            <Icon icon="lucide:chevron-down" width="12" height="12" />
          </XhButton>
        </XhPopoverTrigger>
        <XhPopoverPositioner>
          <XhPopoverContent :aria-label="t('chat.thread.pinned_count', { n: pinnedList.length })">
            <div class="flex max-h-64 flex-col gap-1 overflow-y-auto">
              <div
                v-for="pinnedItem in pinnedList"
                :key="pinnedItem.messageId"
                class="flex items-start gap-2 border-b border-border/50 py-1.5 last:border-b-0"
              >
                <div class="min-w-0 flex-1">
                  <div class="text-[11px] text-muted-foreground">
                    {{ pinnedItem.senderUserName }} · {{ formatMessageTime(pinnedItem.createdAt) }}
                  </div>
                  <div class="truncate text-xs text-foreground">
                    {{ messageBodyLabel(pinnedItem) }}
                  </div>
                </div>
                <XhButton
                  v-if="canPin"
                  variant="link"
                  size="sm"
                  class="xh-link-button chat-meta-link shrink-0"
                  @click="conversationId && chatStore.unpinMessage(conversationId, pinnedItem.messageId).catch(notifyFailure)"
                >
                  {{ t('chat.thread.unpin') }}
                </XhButton>
              </div>
            </div>
          </XhPopoverContent>
        </XhPopoverPositioner>
      </XhPopoverRoot>
    </div>

    <!-- 消息流 -->
    <div v-bind="feed.getRootProps()" :ref="(el) => (rootRef = el as HTMLElement | null)" class="min-h-0 flex-1">
      <div
        v-bind="feed.getViewportProps()"
        :ref="(el) => (viewportRef = el as HTMLElement | null)"
        class="px-3 py-2"
        @scroll.passive="handleScroll"
      >
        <!-- 补历史与空态不是消息，放在 list 外：role=feed 只认 article 子节点 -->
        <div v-if="hasMoreOlder || historyLoading" class="flex justify-center py-1.5">
          <XhSpinner v-if="historyLoading" size="sm" />
          <XhButton v-else variant="ghost" size="sm" @click="handleLoadOlder">
            {{ t('chat.thread.load_more') }}
          </XhButton>
        </div>

        <div v-if="!chatStore.activeMessages.length && !historyLoading" class="py-12">
          <XhEmptyStateRoot size="sm">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" height="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('chat.thread.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </div>

        <!-- 消息之间的间距各自带着，这一层不再叠加 -->
        <div v-bind="feed.getListProps()" :ref="(el) => (contentRef = el as HTMLElement | null)" style="--xh-message-feed-gap: 0; --xh-message-feed-p: 0">
          <div
            v-for="(item, index) in chatStore.activeMessages"
            v-bind="feed.getItemProps({ id: String(item.messageId), index })"
            :id="`chat-msg-${item.messageId}`"
            :key="item.messageId"
            @contextmenu="openContextMenu($event, item)"
            @click="handleRowClick(item)"
          >
            <!-- 条目的排布归消息流，多选的横排另套一层：每条前面一个勾选框；不能转发的（系统提示、通话记录、已撤回）占着位、选不了 -->
            <div :class="{ 'chat-select-row': selecting, 'chat-select-row--picked': selecting && selectedIds.includes(item.messageId) }">
              <span v-if="selecting" class="chat-select-row__box">
                <XhCheckbox
                  v-if="isForwardableMessage(item)"
                  :checked="selectedIds.includes(item.messageId)"
                  :aria-label="t('chat.thread.select_message', { name: item.senderUserName ?? '' })"
                  @update:checked="toggleSelected(item)"
                  @click.stop
                />
              </span>
              <ChatMessageItemView
                :message="item"
                :is-self="item.senderUserId === currentUserId"
                :show-sender-name="isGroupLike"
                :conversation-type="conversation.conversationType"
                :read-count="readCountOf(item)"
                :highlighted="chatStore.highlightMessageId === item.messageId"
                @retry="item.clientMessageId && chatStore.retryMessage(conversation.conversationId, item.clientMessageId).catch(() => {})"
                @remove="item.clientMessageId && chatStore.removeLocalMessage(conversation.conversationId, item.clientMessageId)"
                @react="emoji => handleReact(item, emoji)"
                @call="media => handleStartCall(media)"
                @record="handleOpenRecord"
                @avatar-contextmenu="event => openMemberMenu(event, item)"
              />
            </div>
          </div>

          <!-- 助手回复流：生成期间的临时气泡，落库消息到达后由 store 丢弃 -->
          <div v-if="assistantStream" class="flex justify-start px-2 py-1">
            <div class="max-w-[80%] rounded-lg bg-card px-3 py-2 text-[13px] leading-relaxed whitespace-pre-wrap break-words">
              <template v-if="assistantStream.error">
                <span class="text-error">{{ assistantStream.error }}</span>
                <XhButton variant="subtle" size="sm" tone="brand" class="ml-2" @click="chatStore.dismissAssistantStream(conversation.conversationId)">
                  {{ t('chat.thread.assistant_dismiss') }}
                </XhButton>
              </template>
              <template v-else>
                <ChatAssistantMarkdown :text="assistantStream.text" :streaming="assistantStream.streaming" />
              </template>
            </div>
          </div>
        </div>

        <!-- 消息右键菜单（QQ 式：分离的表情条 + 菜单卡） -->
        <ChatContextMenu
          v-model:show="ctxShow"
          :x="ctxX"
          :y="ctxY"
          :items="ctxItems"
          :reactions="QUICK_REACTIONS"
          @select="key => handleCtxSelect(key)"
          @react="handleCtxReact"
        />

        <!-- 头像右键成员菜单（QQ 式） -->
        <ChatContextMenu
          v-model:show="memberCtxShow"
          :x="memberCtxX"
          :y="memberCtxY"
          :items="memberCtxItems"
          @select="handleMemberCtxSelect"
        />

        <!-- 转发目标选择 -->
        <ChatForwardDialog v-model:show="showForward" :messages="forwardMessages" :merge="forwardMerge" @forwarded="stopSelecting" />
        <ChatRecordDialog v-model:show="showRecord" :conversation-id="conversationId" :record="openedRecord" />
        <ChatCallInviteDialog v-model:show="showCallInvite" :conversation-id="conversationId" :media="callInviteMedia" />

        <!-- 会话图片组的看片浮层：图片消息点缩略图从那一张打开，左右翻得到同会话的其他图片
             计数、翻页、工具条、关闭各自钉在浮层的一条边上，都是 content 的直接子件；只有一张时不出计数与翻页 -->
        <XhImageViewerRoot v-model:open="galleryOpen" v-model:index="galleryIndex" :collection="galleryItems">
          <XhImageViewerContent>
            <XhImageViewerViewport>
              <XhImageViewerImage />
            </XhImageViewerViewport>
            <template v-if="galleryItems.length > 1">
              <XhImageViewerCounter />
              <XhImageViewerPrevTrigger />
              <XhImageViewerNextTrigger />
            </template>
            <XhImageViewerToolbar>
              <XhImageViewerZoomOutTrigger />
              <XhImageViewerZoomInTrigger />
              <XhImageViewerRotateLeftTrigger />
              <XhImageViewerRotateRightTrigger />
              <XhImageViewerFlipHorizontalTrigger />
              <XhImageViewerFlipVerticalTrigger />
              <XhImageViewerResetTrigger />
            </XhImageViewerToolbar>
            <XhImageViewerCloseTrigger />
          </XhImageViewerContent>
        </XhImageViewerRoot>

        <!-- 视口分离态：回到最新 -->
        <div v-if="isDetached" class="sticky bottom-1 flex justify-center">
          <XhButton size="sm" variant="subtle" tone="brand" @click="handleBackToLatest">
            <Icon icon="lucide:arrow-down-to-line" width="13" height="13" />
            {{ t('chat.thread.back_to_latest') }}
          </XhButton>
        </div>
      </div>
    </div>

    <!-- 输入中提示 -->
    <div v-if="chatStore.activeTyping" class="app-fade px-4 pb-1 text-[11px] text-muted-foreground">
      {{ t('chat.thread.typing', { name: chatStore.activeTyping.userName ?? '' }) }}
    </div>

    <!-- 多选时输入区换成操作条：已选几条，逐条或合并转发 -->
    <div v-if="selecting" class="chat-select-bar" role="toolbar" :aria-label="t('chat.thread.select')">
      <span class="min-w-0 flex-1 truncate text-xs text-muted-foreground" aria-live="polite">{{ t('chat.thread.selected_count', { n: selectedIds.length }) }}</span>
      <XhButton variant="subtle" size="sm" :disabled="!selectedIds.length" @click="openForward(selectedMessages, false)">
        <Icon icon="lucide:corner-up-right" />
        {{ t('chat.thread.forward_one_by_one') }}
      </XhButton>
      <XhButton variant="subtle" size="sm" tone="brand" :disabled="!selectedIds.length" @click="openForward(selectedMessages, true)">
        <Icon icon="lucide:messages-square" />
        {{ t('chat.thread.forward_merged') }}
      </XhButton>
      <XhButton variant="ghost" size="sm" @click="stopSelecting">
        {{ t('chat.start.cancel') }}
      </XhButton>
    </div>

    <!-- 输入区 -->
    <ChatComposer v-else :conversation-id="conversation.conversationId" />
  </div>
</template>

<style scoped>
/* 元信息行里的文字链：字号随这一行压到 xs，经按钮的公开槽给 */
.chat-meta-link {
  --xh-button-font-size: var(--xh-font-size-xs);
}

/* 多选：每条消息前面留一格勾选框，整行可点；消息里的按钮这时不接点击，免得点图片、点文件抢了勾选 */
.chat-select-row {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  border-radius: var(--xh-shape-control);
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-select-row > :deep(:not(.chat-select-row__box)) {
  flex: 1;
  min-inline-size: 0;
  pointer-events: none;
}

.chat-select-row--picked {
  background: var(--xh-bg-subtle);
}

.chat-select-row__box {
  display: flex;
  flex: none;
  justify-content: center;
  inline-size: var(--xh-space-6);
}

/* 多选的操作条：占输入区的位置 */
.chat-select-bar {
  display: flex;
  flex-shrink: 0;
  gap: var(--xh-space-2);
  align-items: center;
  padding: var(--xh-space-3);
  border-block-start: var(--xh-stroke-thin) solid var(--xh-border-default);
}

/* 通话进行中的横条：与公告、置顶同一排的样子，电话图标用成功色 */
.chat-call-banner {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  padding: var(--xh-space-1) var(--xh-space-3);
  border-block-end: var(--xh-stroke-thin) solid var(--xh-border-default);
  background: var(--xh-bg-subtle);
}

.chat-call-banner__icon {
  color: var(--xh-fg-success);
}

.chat-search-hit {
  display: block;
  width: 100%;
  padding: var(--xh-space-1) var(--xh-space-2);
  border: none;
  border-radius: var(--xh-shape-control);
  background: transparent;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.chat-search-hit:hover {
  background: var(--xh-bg-subtle-hover);
}
</style>
