<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatConversationListItem, ChatMemberItem } from '../types'
import type { RowMenuAction } from '~/components/schema'
import type { ChatMemberRole } from '~/contracts'
import { XhButton, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import { VNodeRender } from '~/components/common/VNodeRender'
import XIconButton from '~/components/common/XIconButton.vue'
import XInput from '~/components/common/XInput.vue'
import { renderRowActionMenu } from '~/components/schema'
import { confirmDanger, dialog, toast } from '~/composables'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { getChatApi } from '../api-contract'
import { CHAT_PERMISSIONS } from '../constants'
import { useChatStore } from '../store'
import { chatErrorText, formatMessageTime } from './chat-helpers'
import ChatAddMembersDialog from './ChatAddMembersDialog.vue'
import ChatConversationAvatar from './ChatConversationAvatar.vue'
import ChatGroupInfoDialog from './ChatGroupInfoDialog.vue'

/**
 * 会话详情：会话资料、群公告、个人设置（置顶、免打扰）与成员治理
 * 宽屏嵌在消息流旁边当第三栏，窄处顶替消息流（由聊天面板决定，这里只换头上的关闭 / 返回钮）
 */
defineOptions({ name: 'ChatConversationDetails' })

const props = withDefaults(defineProps<{
  conversation: ChatConversationListItem
  /** 嵌在消息流旁边（头上是关闭钮）；否则顶替消息流（头上是返回钮） */
  inline?: boolean
}>(), {
  inline: false,
})

const emit = defineEmits<{
  close: []
}>()

/** 成员多于这个数才出成员搜索 */
const MEMBER_SEARCH_THRESHOLD = 8

const { t } = useI18n()
const chatStore = useChatStore()
const userStore = useUserStore()

const loading = ref(false)
const members = ref<ChatMemberItem[]>([])
const memberKeyword = ref('')
const showAddMembers = ref(false)
const showEditInfo = ref(false)
const settingBusy = ref(false)
let loadSeq = 0

const conversationId = computed(() => props.conversation.conversationId)
const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')
const myRole = computed(() => props.conversation.memberRole)
const isGroup = computed(() => props.conversation.conversationType === 'Group')
const isGroupLike = computed(() => isGroup.value || props.conversation.conversationType === 'Department')
const isOwner = computed(() => myRole.value === 'Owner')
const canManage = computed(() =>
  userStore.hasPermission(CHAT_PERMISSIONS.manage) && (isOwner.value || myRole.value === 'Admin'))
// 群主不能退群（须先移交，后端硬校验），普通成员与管理员可退
const canLeave = computed(() => isGroup.value && !isOwner.value)

const typeLabel = computed(() => {
  switch (props.conversation.conversationType) {
    case 'Single':
      return t('chat.type.single')
    case 'Group':
      return t('chat.type.group')
    case 'Department':
      return t('chat.type.department')
    default:
      return t('chat.type.assistant')
  }
})

const memberIds = computed(() => members.value.map(member => member.userId))

const filteredMembers = computed(() => {
  const key = memberKeyword.value.trim().toLowerCase()
  return key
    ? members.value.filter(member =>
        memberName(member).toLowerCase().includes(key) || (member.userName ?? '').toLowerCase().includes(key))
    : members.value
})

/** 成员的展示名：显示名优先，没有再用用户名 */
function memberName(member: ChatMemberItem): string {
  return member.displayName || member.userName || ''
}

function roleLabel(role: ChatMemberRole): string {
  switch (role) {
    case 'Owner':
      return t('chat.role.owner')
    case 'Admin':
      return t('chat.role.admin')
    default:
      return t('chat.role.member')
  }
}

function roleTone(role: ChatMemberRole): 'info' | 'warning' {
  return role === 'Owner'
    ? 'warning'
    : 'info'
}

/** 用户操作的失败由界面提示（请求层不弹统一提示） */
function notifyFailure(error: unknown) {
  toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
}

async function loadMembers() {
  if (!isGroupLike.value) {
    members.value = []
    return
  }
  const seq = ++loadSeq
  // 已有成员时后台重取，不拿加载罩盖住列表
  loading.value = members.value.length === 0
  try {
    const items = await getChatApi().members(conversationId.value)
    if (seq === loadSeq) {
      members.value = items
    }
  }
  catch (error) {
    if (seq === loadSeq) {
      members.value = []
      notifyFailure(error)
    }
  }
  finally {
    if (seq === loadSeq) {
      loading.value = false
    }
  }
}

// 换会话从头取；成员、角色、禁言的推送（多端与他人操作）到了就重取
watch(conversationId, () => {
  members.value = []
  memberKeyword.value = ''
  void loadMembers()
}, { immediate: true })
watch(() => chatStore.conversationRevisions[conversationId.value], () => void loadMembers())

async function runSetting(action: () => Promise<void>) {
  if (settingBusy.value) {
    return
  }
  settingBusy.value = true
  try {
    await action()
  }
  catch (error) {
    notifyFailure(error)
  }
  finally {
    settingBusy.value = false
  }
}

/** 这一位成员此刻能做的操作（与后端的群治理规则一致，只做显隐） */
function memberActions(member: ChatMemberItem): RowMenuAction[] {
  if (member.userId === currentUserId.value) {
    return []
  }
  const actions: RowMenuAction[] = [
    {
      key: 'dm',
      title: t('chat.member_menu.send_message'),
      icon: 'lucide:message-circle',
    },
    {
      key: 'mention',
      title: t('chat.member_menu.mention'),
      icon: 'lucide:at-sign',
    },
  ]
  const role = member.memberRole
  if (isGroup.value && isOwner.value && userStore.hasPermission(CHAT_PERMISSIONS.manage) && role !== 'Owner') {
    actions.push(role === 'Admin'
      ? {
          key: 'unset-admin',
          title: t('chat.member_menu.unset_admin'),
          icon: 'lucide:shield-off',
        }
      : {
          key: 'set-admin',
          title: t('chat.member_menu.set_admin'),
          icon: 'lucide:shield',
        })
    actions.push({
      key: 'transfer',
      title: t('chat.members.transfer'),
      icon: 'lucide:crown',
    })
  }
  if (canManage.value && role === 'Member') {
    actions.push(member.isSilenced
      ? {
          key: 'unsilence',
          title: t('chat.members.unsilence'),
          icon: 'lucide:mic',
        }
      : {
          key: 'silence',
          title: t('chat.members.silence'),
          icon: 'lucide:mic-off',
        })
  }
  if (isGroup.value && canManage.value && role !== 'Owner') {
    actions.push({
      key: 'remove',
      title: t('chat.member_menu.remove'),
      icon: 'lucide:user-minus',
      tone: 'danger',
    })
  }
  return actions
}

async function refreshAfterChange() {
  await loadMembers()
  chatStore.loadConversations().catch(() => {})
}

function handleMemberAction(member: ChatMemberItem, key: string) {
  const id = conversationId.value
  const name = memberName(member)
  switch (key) {
    case 'dm':
      chatStore.startSingleConversation(member.userId).catch(notifyFailure)
      break
    case 'mention':
      chatStore.requestMention(id, member.userId, member.userName ?? name)
      // 顶替着消息流时让出位置，输入框才看得见
      if (!props.inline) {
        emit('close')
      }
      break
    case 'set-admin':
      getChatApi().setMemberRole(id, member.userId, 'Admin').then(refreshAfterChange).catch(notifyFailure)
      break
    case 'unset-admin':
      getChatApi().setMemberRole(id, member.userId, 'Member').then(refreshAfterChange).catch(notifyFailure)
      break
    case 'silence':
      getChatApi().setMemberSilence(id, member.userId, true).then(refreshAfterChange).catch(notifyFailure)
      break
    case 'unsilence':
      getChatApi().setMemberSilence(id, member.userId, false).then(refreshAfterChange).catch(notifyFailure)
      break
    case 'transfer':
      void dialog.confirm({
        badge: 'warning',
        title: t('chat.members.transfer'),
        content: t('chat.members.transfer_confirm', { name }),
        onOk: async () => {
          try {
            await getChatApi().transferOwner(id, member.userId)
            toast.success(t('chat.members.transfer_done'))
            await refreshAfterChange()
          }
          catch (error) {
            notifyFailure(error)
          }
        },
      })
      break
    case 'remove':
      void confirmDanger({
        title: t('chat.member_menu.remove'),
        content: t('chat.members.remove_confirm', { name }),
        onOk: async () => {
          try {
            await getChatApi().removeMember(id, member.userId)
            await refreshAfterChange()
          }
          catch (error) {
            notifyFailure(error)
          }
        },
      })
      break
  }
}

function handleLeave() {
  const id = conversationId.value
  void confirmDanger({
    title: t('chat.members.leave'),
    content: t('chat.members.leave_confirm'),
    onOk: async () => {
      try {
        await getChatApi().leaveConversation(id)
        emit('close')
        chatStore.closeActiveConversation()
        chatStore.loadConversations().catch(() => {})
        toast.success(t('chat.members.leave_done'))
      }
      catch (error) {
        notifyFailure(error)
      }
    },
  })
}
</script>

<template>
  <div class="flex h-full min-h-0 flex-col">
    <!-- 头：与会话列表、消息流的头同高，三栏的底线连成一条 -->
    <div class="flex h-[56px] shrink-0 items-center gap-2 border-b border-border px-3">
      <XIconButton v-if="!props.inline" icon="lucide:arrow-left" :label="t('header.toolbar.nav_back')" @click="emit('close')" />
      <span class="min-w-0 flex-1 truncate text-sm font-semibold text-foreground">{{ t('chat.details.title') }}</span>
      <XIconButton v-if="props.inline" icon="lucide:x" :label="t('chat.details.close')" @click="emit('close')" />
    </div>

    <div class="xh-scroll-area min-h-0 flex-1">
      <!-- 会话资料 -->
      <div class="flex items-center gap-3 px-3 py-3">
        <ChatConversationAvatar
          :conversation-id="conversation.conversationId"
          :conversation-type="conversation.conversationType"
          :avatar="conversation.avatar"
          :name="conversation.displayName"
          :size="48"
          :round="!isGroupLike"
        />
        <div class="min-w-0 flex-1">
          <div class="truncate text-sm font-semibold text-foreground">
            {{ conversation.displayName }}
          </div>
          <div class="mt-1 flex items-center gap-1.5 text-xs text-muted-foreground">
            <XhTagRoot variant="subtle" size="sm">
              <XhTagLabel>{{ typeLabel }}</XhTagLabel>
            </XhTagRoot>
            <span v-if="isGroupLike">{{ t('chat.members.count', { n: conversation.memberCount }) }}</span>
          </div>
        </div>
        <XIconButton v-if="canManage" icon="lucide:pencil" :label="t('chat.details.edit_info')" @click="showEditInfo = true" />
      </div>

      <p v-if="conversation.description" class="m-0 px-3 pb-3 text-xs leading-relaxed whitespace-pre-wrap text-muted-foreground">
        {{ conversation.description }}
      </p>

      <!-- 群公告（全文，换行经 pre-wrap 保留） -->
      <section v-if="isGroupLike && conversation.announcement" class="chat-details__section">
        <h3 class="chat-details__heading">
          <Icon icon="lucide:megaphone" width="12" height="12" class="text-amber-500" />
          {{ t('chat.members.announcement_title') }}
        </h3>
        <div class="text-xs leading-relaxed whitespace-pre-wrap text-foreground">
          {{ conversation.announcement }}
        </div>
      </section>

      <!-- 个人设置：只影响自己这一端的列表 -->
      <section class="chat-details__section">
        <h3 class="chat-details__heading">
          {{ t('chat.details.section_settings') }}
        </h3>
        <div class="flex items-center justify-between gap-3 py-1">
          <span class="text-[13px] text-foreground">{{ t('chat.list.pin') }}</span>
          <XhSwitch
            :checked="conversation.isPinned"
            :disabled="settingBusy"
            :aria-label="t('chat.list.pin')"
            @update:checked="runSetting(() => chatStore.togglePinConversation(conversation.conversationId))"
          />
        </div>
        <div class="flex items-center justify-between gap-3 py-1">
          <span class="text-[13px] text-foreground">{{ t('chat.list.mute') }}</span>
          <XhSwitch
            :checked="conversation.isMuted"
            :disabled="settingBusy"
            :aria-label="t('chat.list.mute')"
            @update:checked="runSetting(() => chatStore.toggleMuteConversation(conversation.conversationId))"
          />
        </div>
      </section>

      <!-- 成员 -->
      <section v-if="isGroupLike" class="chat-details__section">
        <div class="flex items-center justify-between gap-2">
          <h3 class="chat-details__heading">
            {{ t('chat.members.section_members', { n: members.length || conversation.memberCount }) }}
          </h3>
          <XhButton v-if="canManage && isGroup" variant="subtle" size="sm" @click="showAddMembers = true">
            <Icon icon="lucide:user-plus" />
            {{ t('chat.members.add') }}
          </XhButton>
        </div>
        <XInput
          v-if="members.length > MEMBER_SEARCH_THRESHOLD"
          v-model:value="memberKeyword"
          class="mt-2"
          size="sm"
          clearable
          :placeholder="t('chat.details.member_search')"
          :aria-label="t('chat.details.member_search')"
        >
          <template #prefix>
            <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
          </template>
        </XInput>

        <div class="xh-loading-stage mt-1" :class="{ 'is-loading': loading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <div v-if="!filteredMembers.length && !loading" class="py-6">
            <XhEmptyStateRoot size="sm">
              <XhEmptyStateIndicator>
                <Icon :icon="memberKeyword.trim() ? 'lucide:search-x' : 'lucide:inbox'" width="24" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ memberKeyword.trim() ? t('common.no_result') : t('common.no_data') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ t('chat.details.member_empty') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
          </div>
          <ul v-else class="m-0 list-none p-0">
            <li
              v-for="member in filteredMembers"
              :key="member.userId"
              class="flex items-center gap-2.5 border-b border-border/50 py-2 last:border-b-0"
            >
              <XUserAvatar :avatar="member.avatarFileId" :name="memberName(member)" :size="32" />
              <div class="min-w-0 flex-1">
                <div class="flex items-center gap-1.5">
                  <span class="truncate text-[13px] text-foreground">{{ memberName(member) }}</span>
                  <XhTagRoot v-if="member.memberRole !== 'Member'" variant="subtle" size="sm" :tone="roleTone(member.memberRole)">
                    <XhTagLabel>{{ roleLabel(member.memberRole) }}</XhTagLabel>
                  </XhTagRoot>
                  <XhTagRoot v-if="member.isSilenced" variant="subtle" size="sm" tone="danger">
                    <XhTagLabel>{{ t('chat.members.silenced') }}</XhTagLabel>
                  </XhTagRoot>
                </div>
                <div class="truncate text-[11px] text-muted-foreground">
                  {{ t('chat.members.joined', { time: formatMessageTime(member.joinedAt) }) }}
                </div>
              </div>
              <!-- 与列表页同一个「更多」菜单；本人这一行没有可做的操作 -->
              <VNodeRender
                v-if="memberActions(member).length"
                :content="renderRowActionMenu(memberActions(member), t('component.schema_page.more'), key => handleMemberAction(member, key))"
              />
            </li>
          </ul>
        </div>
      </section>
    </div>

    <div v-if="canLeave" class="shrink-0 border-t border-border px-3 py-2">
      <XhButton variant="subtle" size="sm" tone="danger" full-width @click="handleLeave">
        <Icon icon="lucide:log-out" />
        {{ t('chat.members.leave') }}
      </XhButton>
    </div>

    <ChatAddMembersDialog
      v-model:show="showAddMembers"
      :conversation-id="conversation.conversationId"
      :member-ids="memberIds"
      @added="refreshAfterChange"
    />
    <ChatGroupInfoDialog v-model:show="showEditInfo" :conversation="conversation" />
  </div>
</template>

<style scoped>
/* 分区：上面一道分隔线，标题是小号次要文字 */
.chat-details__section {
  padding: var(--xh-space-3);
  border-block-start: var(--xh-stroke-thin) solid var(--xh-border-subtle);
}

.chat-details__heading {
  display: flex;
  gap: var(--xh-space-1_5);
  align-items: center;
  margin: var(--xh-space-0);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-medium);
}

.chat-details__section > .chat-details__heading {
  margin-block-end: var(--xh-space-2);
}
</style>
