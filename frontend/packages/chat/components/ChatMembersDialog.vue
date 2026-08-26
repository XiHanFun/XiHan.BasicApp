<script setup lang="ts">
import type {
  ChatConversationListItem,
  ChatMemberItem,
} from '../types'
import { XhButton, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhPopconfirmCancelTrigger, XhPopconfirmConfirmTrigger, XhPopconfirmContent, XhPopconfirmDescription, XhPopconfirmPositioner, XhPopconfirmRoot, XhPopconfirmTrigger, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XInput from '~/components/common/XInput.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'

import { useUserStore } from '~/stores'
import { getChatApi } from '../api-contract'
import {
  CHAT_PERMISSIONS,
} from '../constants'
import { ChatConversationType, ChatMemberRole } from '../enums'
import { useChatStore } from '../store'
import {
  CHAT_MAX_GROUP_NAME_LENGTH,
} from '../types'
import { formatMessageTime } from './chat-helpers'
import ChatUserSelect from './ChatUserSelect.vue'

defineOptions({ name: 'ChatMembersDialog' })

const props = defineProps<{
  conversation: ChatConversationListItem | null
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const chatStore = useChatStore()
const userStore = useUserStore()

const loading = ref(false)
const members = ref<ChatMemberItem[]>([])
const addUserIds = ref<string[]>([])
const adding = ref(false)
const showAdd = ref(false)

// 群信息编辑（群主/管理员）
const infoName = ref('')
const infoAnnouncement = ref('')
const infoDescription = ref('')
const infoAvatar = ref('')
const infoSaving = ref(false)
const avatarUploading = ref(false)
const avatarInputRef = ref<HTMLInputElement>()

const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')
const myRole = computed(() => props.conversation?.memberRole ?? ChatMemberRole.Member)
const canManage = computed(() =>
  userStore.hasPermission(CHAT_PERMISSIONS.manage)
  && (myRole.value === ChatMemberRole.Owner || myRole.value === ChatMemberRole.Admin),
)
const isOwner = computed(() => myRole.value === ChatMemberRole.Owner)
const isGroup = computed(() => props.conversation?.conversationType === ChatConversationType.Group)
const isDepartment = computed(() => props.conversation?.conversationType === ChatConversationType.Department)
// 群主不能退群（须先移交，后端硬校验），普通成员/管理员可退
const canLeave = computed(() => isGroup.value && myRole.value !== ChatMemberRole.Owner)

const memberIds = computed(() => members.value.map(m => m.userId))

function roleLabel(role: ChatMemberRole): string {
  switch (role) {
    case ChatMemberRole.Owner:
      return t('chat.role.owner')
    case ChatMemberRole.Admin:
      return t('chat.role.admin')
    default:
      return t('chat.role.member')
  }
}

function roleTagType(role: ChatMemberRole): 'neutral' | 'info' | 'warning' {
  switch (role) {
    case ChatMemberRole.Owner:
      return 'warning'
    case ChatMemberRole.Admin:
      return 'info'
    default:
      return 'neutral'
  }
}

async function loadMembers() {
  const id = props.conversation?.conversationId
  if (!id) {
    return
  }
  loading.value = true
  try {
    members.value = await getChatApi().members(id)
  }
  catch {
    members.value = []
  }
  finally {
    loading.value = false
  }
}

watch(show, (visible) => {
  if (visible) {
    showAdd.value = false
    addUserIds.value = []
    infoName.value = props.conversation?.displayName ?? ''
    infoAnnouncement.value = props.conversation?.announcement ?? ''
    infoDescription.value = props.conversation?.description ?? ''
    infoAvatar.value = props.conversation?.avatar ?? ''
    void loadMembers()
  }
})

/** 群头像上传：复用聊天附件链路（含秒传），存文件主键（XUserAvatar 自动换预签名 URL） */
async function handleAvatarPicked(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || avatarUploading.value) {
    return
  }
  avatarUploading.value = true
  try {
    const uploaded = await getChatApi().uploadAttachment(file)
    infoAvatar.value = uploaded.fileId
  }
  catch {
    // 请求层已有统一错误提示
  }
  finally {
    avatarUploading.value = false
  }
}

async function handleSaveInfo() {
  const id = props.conversation?.conversationId
  if (!id || infoSaving.value) {
    return
  }
  infoSaving.value = true
  try {
    await getChatApi().updateConversationInfo({
      conversationId: id,
      // 部门群名称随部门同步禁改（null 不改）
      conversationName: isGroup.value ? infoName.value.trim() || null : null,
      announcement: infoAnnouncement.value,
      description: infoDescription.value,
      avatar: infoAvatar.value,
    })
    chatStore.loadConversations().catch(() => {})
    toast.success(t('chat.members.info_saved'))
  }
  catch {
    // 请求层已有统一错误提示
  }
  finally {
    infoSaving.value = false
  }
}

async function handleTransferOwner(member: ChatMemberItem) {
  const id = props.conversation?.conversationId
  if (!id) {
    return
  }
  try {
    await getChatApi().transferOwner(id, member.userId)
    await loadMembers()
    chatStore.loadConversations().catch(() => {})
    toast.success(t('chat.members.transfer_done'))
  }
  catch {
    // 请求层已有统一错误提示
  }
}

async function handleToggleSilence(member: ChatMemberItem) {
  const id = props.conversation?.conversationId
  if (!id) {
    return
  }
  try {
    await getChatApi().setMemberSilence(id, member.userId, !member.isSilenced)
    await loadMembers()
  }
  catch {
    // 请求层已有统一错误提示
  }
}

/** 可转让群主：我是群主 + 群聊 + 目标非本人 */
function canTransferTo(member: ChatMemberItem): boolean {
  return isOwner.value && isGroup.value && member.userId !== currentUserId.value
}

/** 可禁言/解除：我可管理 + 目标是普通成员 */
function canSilence(member: ChatMemberItem): boolean {
  return canManage.value && member.memberRole === ChatMemberRole.Member && member.userId !== currentUserId.value
}

async function handleAddMembers() {
  const id = props.conversation?.conversationId
  if (!id || !addUserIds.value.length || adding.value) {
    return
  }
  adding.value = true
  try {
    await getChatApi().addMembers(id, addUserIds.value)
    addUserIds.value = []
    showAdd.value = false
    await loadMembers()
    chatStore.loadConversations().catch(() => {})
  }
  catch {
    // 请求层已有统一错误提示
  }
  finally {
    adding.value = false
  }
}

/** 可移出：我可管理 + 非本人 + 目标非群主 */
function canRemove(member: ChatMemberItem): boolean {
  return canManage.value
    && member.userId !== currentUserId.value
    && member.memberRole !== ChatMemberRole.Owner
}

async function handleRemove(member: ChatMemberItem) {
  const id = props.conversation?.conversationId
  if (!id) {
    return
  }
  try {
    await getChatApi().removeMember(id, member.userId)
    await loadMembers()
    chatStore.loadConversations().catch(() => {})
  }
  catch {
    // 请求层已有统一错误提示
  }
}

async function handleLeave() {
  const id = props.conversation?.conversationId
  if (!id) {
    return
  }
  try {
    await getChatApi().removeMember(id, currentUserId.value)
    show.value = false
    chatStore.closeActiveConversation()
    chatStore.loadConversations().catch(() => {})
    toast.success(t('chat.members.leave_done'))
  }
  catch {
    // 请求层已有统一错误提示
  }
}
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: 480px">
      <XhDialogTitle>{{ t('chat.members.title', { n: members.length }) }}</XhDialogTitle>
      <XhDialogCloseTrigger />
      <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
        <div class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <!-- 群信息（群主/管理员可编辑；部门群名称随部门禁改） -->
        <div v-if="canManage && !loading" class="mb-4 flex flex-col gap-3">
          <!-- 头像 + 名称 -->
          <div class="flex items-center gap-3">
            <button
              type="button"
              class="chat-avatar-upload"
              :title="t('chat.members.info_avatar')"
              :disabled="avatarUploading"
              @click="avatarInputRef?.click()"
            >
              <XUserAvatar :avatar="infoAvatar || null" :name="infoName" :size="52" :round="false" />
              <span class="chat-avatar-upload__mask">
                <Icon
                  :icon="avatarUploading ? 'lucide:loader-circle' : 'lucide:camera'"
                  width="16"
                  height="16"
                  :class="{ 'animate-spin': avatarUploading }"
                />
              </span>
            </button>
            <input ref="avatarInputRef" type="file" accept="image/*" class="hidden" @change="handleAvatarPicked">
            <div class="min-w-0 flex-1">
              <div class="mb-1 text-xs text-muted-foreground">
                {{ t('chat.members.info_name') }}
              </div>
              <XInput
                v-model:value="infoName"
                size="sm"
                :max-length="CHAT_MAX_GROUP_NAME_LENGTH"
                :disabled="isDepartment"
                :placeholder="t('chat.members.info_name')"
              />
            </div>
          </div>

          <div>
            <div class="mb-1 text-xs text-muted-foreground">
              {{ t('chat.members.info_announcement') }}
            </div>
            <XInput
              v-model:value="infoAnnouncement"
              type="textarea"
              size="sm"
              :autosize="{ minRows: 2, maxRows: 5 }"
              :max-length="2000"
              show-count
              :placeholder="t('chat.members.info_announcement_placeholder')"
            />
          </div>

          <div>
            <div class="mb-1 text-xs text-muted-foreground">
              {{ t('chat.members.info_description') }}
            </div>
            <XInput
              v-model:value="infoDescription"
              type="textarea"
              size="sm"
              :autosize="{ minRows: 1, maxRows: 3 }"
              :max-length="500"
              :placeholder="t('chat.members.info_description')"
            />
          </div>

          <div class="flex justify-end">
            <XhButton size="sm" tone="brand" :loading="infoSaving" @click="handleSaveInfo">
              {{ t('chat.members.info_save') }}
            </XhButton>
          </div>
        </div>

        <!-- 公告展示（普通成员只读，pre-wrap 保留换行） -->
        <div
          v-else-if="props.conversation?.announcement"
          class="mb-4 rounded-lg border border-border/60 bg-amber-500/5 p-2.5"
        >
          <div class="mb-1 flex items-center gap-1.5 text-xs font-medium text-foreground">
            <Icon icon="lucide:megaphone" width="12" height="12" class="text-amber-500" />
            {{ t('chat.members.announcement_title') }}
          </div>
          <div class="text-xs leading-relaxed whitespace-pre-wrap text-muted-foreground">
            {{ props.conversation.announcement }}
          </div>
        </div>

        <!-- 成员分区 -->
        <div class="mb-2 flex items-center gap-2">
          <span class="text-xs font-medium text-muted-foreground">{{ t('chat.members.section_members', { n: members.length }) }}</span>
          <div class="h-px flex-1 bg-border/60" />
        </div>

        <!-- 添加成员 -->
        <div v-if="canManage" class="mb-3">
          <XhButton v-if="!showAdd" size="sm" dashed full-width @click="showAdd = true">
            <Icon icon="lucide:user-plus" width="14" height="14" />
            {{ t('chat.members.add') }}
          </XhButton>
          <div v-else class="flex flex-col gap-2">
            <ChatUserSelect
              v-model="addUserIds"
              multiple
              :exclude-user-ids="memberIds"
              :placeholder="t('chat.start.users_placeholder')"
            />
            <div class="flex justify-end gap-2">
              <XhButton size="sm" :disabled="adding" @click="showAdd = false">
                {{ t('chat.start.cancel') }}
              </XhButton>
              <XhButton
                size="sm"
                tone="brand"
                :loading="adding"
                :disabled="!addUserIds.length"
                @click="handleAddMembers"
              >
                {{ t('chat.start.confirm') }}
              </XhButton>
            </div>
          </div>
        </div>

        <!-- 成员列表 -->
        <div v-if="!members.length && !loading" class="py-8">
          <XhEmptyStateRoot size="sm">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('common.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
        </div>
        <div v-else class="xh-scroll-area" style="max-height: min(52vh, 420px)">
          <div
            v-for="member in members"
            :key="member.userId"
            class="flex items-center gap-2.5 border-b border-border/50 py-2 pr-3 last:border-b-0"
          >
            <XUserAvatar :name="member.userName" :size="32" />
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-1.5">
                <span class="truncate text-[13px] text-foreground">{{ member.userName }}</span>
                <XhTagRoot variant="subtle" size="sm" :tone="roleTagType(member.memberRole)">
                  <XhTagLabel>
                    {{ roleLabel(member.memberRole) }}
                  </XhTagLabel>
                </XhTagRoot>
                <XhTagRoot v-if="member.isSilenced" variant="subtle" size="sm" tone="danger">
                  <XhTagLabel>
                    {{ t('chat.members.silenced') }}
                  </XhTagLabel>
                </XhTagRoot>
              </div>
              <div class="text-[11px] text-muted-foreground">
                {{ t('chat.members.joined', { time: formatMessageTime(member.joinTime) }) }}
              </div>
            </div>
            <div class="flex shrink-0 items-center gap-0.5">
              <XhPopconfirmRoot v-if="canTransferTo(member)" @confirm="handleTransferOwner(member)">
                <XhPopconfirmTrigger class="xh-linklike-trigger">
                  {{ t('chat.members.transfer') }}
                </XhPopconfirmTrigger>
                <XhPopconfirmPositioner>
                  <XhPopconfirmContent>
                    <XhPopconfirmDescription>{{ t('chat.members.transfer_confirm', { name: member.userName ?? '' }) }}</XhPopconfirmDescription>
                    <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                    <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                  </XhPopconfirmContent>
                </XhPopconfirmPositioner>
              </XhPopconfirmRoot>
              <XhButton v-if="canSilence(member)" size="sm" variant="ghost" @click="handleToggleSilence(member)">
                {{ member.isSilenced ? t('chat.members.unsilence') : t('chat.members.silence') }}
              </XhButton>
              <XhPopconfirmRoot v-if="canRemove(member)" @confirm="handleRemove(member)">
                <XhPopconfirmTrigger class="xh-linklike-trigger">
                  {{ t('chat.members.remove') }}
                </XhPopconfirmTrigger>
                <XhPopconfirmPositioner>
                  <XhPopconfirmContent>
                    <XhPopconfirmDescription>{{ t('chat.members.remove_confirm', { name: member.userName ?? '' }) }}</XhPopconfirmDescription>
                    <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                    <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                  </XhPopconfirmContent>
                </XhPopconfirmPositioner>
              </XhPopconfirmRoot>
            </div>
          </div>
        </div>
      </div>

      <div v-if="canLeave" class="xh-dialog-footer">
        <div class="flex justify-end">
          <XhPopconfirmRoot @confirm="handleLeave">
            <XhPopconfirmTrigger class="xh-linklike-trigger">
              {{ t('chat.members.leave') }}
            </XhPopconfirmTrigger>
            <XhPopconfirmPositioner>
              <XhPopconfirmContent>
                <XhPopconfirmDescription>{{ t('chat.members.leave_confirm') }}</XhPopconfirmDescription>
                <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
              </XhPopconfirmContent>
            </XhPopconfirmPositioner>
          </XhPopconfirmRoot>
        </div>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 群头像上传：悬浮出相机遮罩 */
.chat-avatar-upload {
  position: relative;
  padding: 0;
  border: none;
  border-radius: 10px;
  background: transparent;
  cursor: pointer;
  flex-shrink: 0;
  overflow: hidden;
}

.chat-avatar-upload__mask {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
  background: hsl(0deg 0% 0% / 40%);
  color: #fff;
  opacity: 0;
  transition: opacity 0.15s ease;
}

.chat-avatar-upload:hover .chat-avatar-upload__mask,
.chat-avatar-upload:disabled .chat-avatar-upload__mask {
  opacity: 1;
}
</style>
