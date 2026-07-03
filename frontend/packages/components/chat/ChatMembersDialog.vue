<script setup lang="ts">
import type { ChatConversationListItem, ChatMemberItem } from '~/types'
import { NButton, NEmpty, NInput, NModal, NPopconfirm, NScrollbar, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_PERMISSIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useAppContext, useChatStore, useUserStore } from '~/stores'
import { CHAT_MAX_GROUP_NAME_LENGTH } from '~/types'
import { ChatConversationType, ChatMemberRole } from '~/types/enums'
import XUserAvatar from '../common/UserAvatar.vue'
import { formatMessageTime } from './chat-helpers'
import ChatUserSelect from './ChatUserSelect.vue'

defineOptions({ name: 'ChatMembersDialog' })

const props = defineProps<{
  conversation: ChatConversationListItem | null
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const message = useMessage()
const chatStore = useChatStore()
const userStore = useUserStore()
const appContext = useAppContext()

const loading = ref(false)
const members = ref<ChatMemberItem[]>([])
const addUserIds = ref<string[]>([])
const adding = ref(false)
const showAdd = ref(false)

// 群信息编辑（群主/管理员）
const infoName = ref('')
const infoAnnouncement = ref('')
const infoDescription = ref('')
const infoSaving = ref(false)

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

function roleTagType(role: ChatMemberRole): 'default' | 'info' | 'warning' {
  switch (role) {
    case ChatMemberRole.Owner:
      return 'warning'
    case ChatMemberRole.Admin:
      return 'info'
    default:
      return 'default'
  }
}

async function loadMembers() {
  const id = props.conversation?.conversationId
  if (!id) {
    return
  }
  loading.value = true
  try {
    members.value = await appContext.apis.chatApi.members(id)
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
    void loadMembers()
  }
})

async function handleSaveInfo() {
  const id = props.conversation?.conversationId
  if (!id || infoSaving.value) {
    return
  }
  infoSaving.value = true
  try {
    await appContext.apis.chatApi.updateConversationInfo({
      conversationId: id,
      // 部门群名称随部门同步禁改（null 不改）
      conversationName: isGroup.value ? infoName.value.trim() || null : null,
      announcement: infoAnnouncement.value,
      description: infoDescription.value,
    })
    chatStore.loadConversations().catch(() => {})
    message.success(t('chat.members.info_saved'))
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
    await appContext.apis.chatApi.transferOwner(id, member.userId)
    await loadMembers()
    chatStore.loadConversations().catch(() => {})
    message.success(t('chat.members.transfer_done'))
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
    await appContext.apis.chatApi.setMemberSilence(id, member.userId, !member.isSilenced)
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
    await appContext.apis.chatApi.addMembers(id, addUserIds.value)
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
    await appContext.apis.chatApi.removeMember(id, member.userId)
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
    await appContext.apis.chatApi.removeMember(id, currentUserId.value)
    show.value = false
    chatStore.closeActiveConversation()
    chatStore.loadConversations().catch(() => {})
    message.success(t('chat.members.leave_done'))
  }
  catch {
    // 请求层已有统一错误提示
  }
}
</script>

<template>
  <NModal
    v-model:show="show"
    preset="card"
    :title="t('chat.members.title', { n: members.length })"
    style="width: 480px; max-width: calc(100vw - 24px);"
  >
    <NSpin :show="loading">
      <!-- 群信息（群主/管理员可编辑；部门群名称随部门禁改） -->
      <div v-if="canManage && !loading" class="mb-3 flex flex-col gap-2 rounded-lg border border-border/60 p-2.5">
        <NInput
          v-model:value="infoName"
          size="small"
          :maxlength="CHAT_MAX_GROUP_NAME_LENGTH"
          :disabled="isDepartment"
          :placeholder="t('chat.members.info_name')"
        />
        <NInput
          v-model:value="infoAnnouncement"
          type="textarea"
          size="small"
          :autosize="{ minRows: 2, maxRows: 4 }"
          :maxlength="2000"
          :placeholder="t('chat.members.info_announcement')"
        />
        <NInput
          v-model:value="infoDescription"
          type="textarea"
          size="small"
          :autosize="{ minRows: 1, maxRows: 3 }"
          :maxlength="500"
          :placeholder="t('chat.members.info_description')"
        />
        <div class="flex justify-end">
          <NButton size="tiny" type="primary" secondary :loading="infoSaving" @click="handleSaveInfo">
            {{ t('chat.members.info_save') }}
          </NButton>
        </div>
      </div>

      <!-- 公告展示（普通成员只读） -->
      <div
        v-else-if="props.conversation?.announcement"
        class="mb-3 rounded-lg border border-border/60 bg-muted/30 p-2.5 text-xs whitespace-pre-wrap text-muted-foreground"
      >
        <div class="mb-1 flex items-center gap-1 text-foreground">
          <Icon icon="lucide:megaphone" width="12" height="12" class="text-primary" />
          {{ t('chat.members.announcement_title') }}
        </div>
        {{ props.conversation.announcement }}
      </div>

      <!-- 添加成员 -->
      <div v-if="canManage" class="mb-3">
        <NButton v-if="!showAdd" size="small" dashed block @click="showAdd = true">
          <template #icon>
            <Icon icon="lucide:user-plus" width="14" height="14" />
          </template>
          {{ t('chat.members.add') }}
        </NButton>
        <div v-else class="flex flex-col gap-2">
          <ChatUserSelect
            v-model="addUserIds"
            multiple
            :exclude-user-ids="memberIds"
            :placeholder="t('chat.start.users_placeholder')"
          />
          <div class="flex justify-end gap-2">
            <NButton size="tiny" :disabled="adding" @click="showAdd = false">
              {{ t('chat.start.cancel') }}
            </NButton>
            <NButton
              size="tiny"
              type="primary"
              :loading="adding"
              :disabled="!addUserIds.length"
              @click="handleAddMembers"
            >
              {{ t('chat.start.confirm') }}
            </NButton>
          </div>
        </div>
      </div>

      <!-- 成员列表 -->
      <div v-if="!members.length && !loading" class="py-8">
        <NEmpty size="small" />
      </div>
      <NScrollbar v-else style="max-height: min(52vh, 420px)">
        <div
          v-for="member in members"
          :key="member.userId"
          class="flex items-center gap-2.5 border-b border-border/50 py-2 pr-3 last:border-b-0"
        >
          <XUserAvatar :name="member.userName" :size="32" />
          <div class="min-w-0 flex-1">
            <div class="flex items-center gap-1.5">
              <span class="truncate text-[13px] text-foreground">{{ member.userName }}</span>
              <NTag size="tiny" :bordered="false" round :type="roleTagType(member.memberRole)">
                {{ roleLabel(member.memberRole) }}
              </NTag>
              <NTag v-if="member.isSilenced" size="tiny" :bordered="false" round type="error">
                {{ t('chat.members.silenced') }}
              </NTag>
            </div>
            <div class="text-[11px] text-muted-foreground">
              {{ t('chat.members.joined', { time: formatMessageTime(member.joinTime) }) }}
            </div>
          </div>
          <div class="flex shrink-0 items-center gap-0.5">
            <NPopconfirm v-if="canTransferTo(member)" @positive-click="handleTransferOwner(member)">
              <template #trigger>
                <NButton size="tiny" quaternary type="warning">
                  {{ t('chat.members.transfer') }}
                </NButton>
              </template>
              {{ t('chat.members.transfer_confirm', { name: member.userName ?? '' }) }}
            </NPopconfirm>
            <NButton v-if="canSilence(member)" size="tiny" quaternary @click="handleToggleSilence(member)">
              {{ member.isSilenced ? t('chat.members.unsilence') : t('chat.members.silence') }}
            </NButton>
            <NPopconfirm v-if="canRemove(member)" @positive-click="handleRemove(member)">
              <template #trigger>
                <NButton size="tiny" quaternary type="error">
                  {{ t('chat.members.remove') }}
                </NButton>
              </template>
              {{ t('chat.members.remove_confirm', { name: member.userName ?? '' }) }}
            </NPopconfirm>
          </div>
        </div>
      </NScrollbar>
    </NSpin>

    <template v-if="canLeave" #footer>
      <div class="flex justify-end">
        <NPopconfirm @positive-click="handleLeave">
          <template #trigger>
            <NButton size="small" type="error" secondary>
              {{ t('chat.members.leave') }}
            </NButton>
          </template>
          {{ t('chat.members.leave_confirm') }}
        </NPopconfirm>
      </div>
    </template>
  </NModal>
</template>
