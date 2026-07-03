<script setup lang="ts">
import type { ChatConversationListItem, ChatMemberItem } from '~/types'
import { NButton, NEmpty, NModal, NPopconfirm, NScrollbar, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { CHAT_PERMISSIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useAppContext, useChatStore, useUserStore } from '~/stores'
import { ChatMemberRole } from '~/types/enums'
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

const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')
const myRole = computed(() => props.conversation?.memberRole ?? ChatMemberRole.Member)
const canManage = computed(() =>
  userStore.hasPermission(CHAT_PERMISSIONS.manage)
  && (myRole.value === ChatMemberRole.Owner || myRole.value === ChatMemberRole.Admin),
)
// 群主不能退群（须先移交，后端硬校验），普通成员/管理员可退
const canLeave = computed(() => myRole.value !== ChatMemberRole.Owner)

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
    void loadMembers()
  }
})

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
            </div>
            <div class="text-[11px] text-muted-foreground">
              {{ t('chat.members.joined', { time: formatMessageTime(member.joinTime) }) }}
            </div>
          </div>
          <NPopconfirm v-if="canRemove(member)" @positive-click="handleRemove(member)">
            <template #trigger>
              <NButton size="tiny" quaternary type="error">
                {{ t('chat.members.remove') }}
              </NButton>
            </template>
            {{ t('chat.members.remove_confirm', { name: member.userName ?? '' }) }}
          </NPopconfirm>
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
