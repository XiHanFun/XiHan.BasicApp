<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatDepartmentPickerNode, ChatUserOption } from '../types'
import type { TreeSelectOption } from '~/types'
import { watchDebounced } from '@vueuse/core'
import { XhButton, XhListboxContent, XhListboxEmpty, XhListboxItem, XhListboxItemDescription, XhListboxItemIndicator, XhListboxItemPrefix, XhListboxItemSuffix, XhListboxItemText, XhListboxLoading, XhListboxLoadMoreTrigger, XhListboxRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XIconButton from '~/components/common/XIconButton.vue'
import XInput from '~/components/common/XInput.vue'
import XSegmented from '~/components/common/XSegmented.vue'
import XTreeSelect from '~/components/common/XTreeSelect.vue'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { getChatApi } from '../api-contract'
import { useChatUserOptions } from '../composables/useChatUserOptions'
import { useChatStore } from '../store'
import { chatErrorText } from './chat-helpers'
import { mergePickedUsers } from './chat-member-picker'
import ChatPresenceBadge from './ChatPresenceBadge.vue'

/**
 * 选人：左边是可勾选的候选列表（搜索、最近联系人、按部门筛、逐页加载），多选时右边列出已选的人
 * 已选的人跨搜索与翻页保留；值恒为用户主键数组，单选时至多一项
 */
defineOptions({ name: 'ChatMemberPicker' })

const props = withDefaults(defineProps<{
  /** 多选（建群、加成员）；false 为单选（发起单聊），不出已选栏 */
  multiple?: boolean
  /** 不列出的用户（本人） */
  excludeUserIds?: string[]
  /** 列出但选不了的用户（已在群里），行尾标注 lockedLabel */
  lockedUserIds?: string[]
  lockedLabel?: string
}>(), {
  multiple: true,
  excludeUserIds: () => [],
  lockedUserIds: () => [],
  lockedLabel: undefined,
})

const model = defineModel<string[]>('value', {
  default: () => [],
})

type Scope = 'all' | 'recent'

const { t } = useI18n()
const chatStore = useChatStore()
const directory = useChatUserOptions()

const scope = ref<Scope>('all')
const keyword = ref('')
const departmentId = ref<null | string>(null)
const departmentOptions = ref<TreeSelectOption[]>([])
// 已选的人按勾选先后排；候选换了一批（搜索、翻页、换范围）也留着
const picked = shallowRef<ChatUserOption[]>([])

const excluded = computed(() => new Set(props.excludeUserIds))
const locked = computed(() => new Set(props.lockedUserIds))

const scopeOptions = computed(() => [
  {
    value: 'all' as const,
    label: t('chat.picker.scope_all'),
  },
  {
    value: 'recent' as const,
    label: t('chat.picker.scope_recent'),
  },
])

/** 最近联系人取自单聊会话（已按置顶与最后消息时间排好），名字与会话列表同一个 */
const recentUsers = computed<ChatUserOption[]>(() => {
  const key = keyword.value.trim().toLowerCase()
  return chatStore.conversations
    .filter(conversation => conversation.conversationType === 'Single' && conversation.peerUserId)
    .filter(conversation => !key || conversation.displayName.toLowerCase().includes(key))
    .map(conversation => ({
      userId: conversation.peerUserId!,
      userName: conversation.displayName,
      displayName: conversation.displayName,
      avatarFileId: conversation.avatar ?? null,
      departmentName: null,
      isOnline: conversation.peerOnline,
    }))
})

const candidates = computed(() =>
  (scope.value === 'recent'
    ? recentUsers.value
    : directory.users.value).filter(user => !excluded.value.has(user.userId)))

const collection = computed(() => candidates.value.map(user => ({
  value: user.userId,
  label: user.displayName,
  disabled: locked.value.has(user.userId),
})))

const loading = computed(() => scope.value === 'all' && directory.loading.value)
const canLoadMore = computed(() => scope.value === 'all' && directory.hasMore.value)

/** 第 2 行：主部门，再带上与显示名不同的用户名 */
function describe(user: ChatUserOption): string {
  return [
    user.departmentName,
    user.userName !== user.displayName
      ? user.userName
      : null,
  ].filter(Boolean).join(' · ')
}

function notifyFailure(error: unknown) {
  toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
}

function reload() {
  if (scope.value === 'all') {
    directory.search(keyword.value, departmentId.value).catch(notifyFailure)
  }
}

function handleLoadMore() {
  directory.loadMore().catch(notifyFailure)
}

function handleValueChange(next: string[]) {
  if (!props.multiple) {
    model.value = next.slice(-1)
    return
  }
  picked.value = mergePickedUsers(picked.value, candidates.value, next, locked.value)
  model.value = picked.value.map(user => user.userId)
}

function handleRemove(userId: string) {
  picked.value = picked.value.filter(user => user.userId !== userId)
  model.value = picked.value.map(user => user.userId)
}

function handleClear() {
  picked.value = []
  model.value = []
}

function mapDepartment(node: ChatDepartmentPickerNode): TreeSelectOption {
  return {
    value: node.departmentId,
    label: node.departmentName,
    children: node.children?.length
      ? node.children.map(mapDepartment)
      : undefined,
  }
}

// 外面把值清掉（对话框重开）时，已选栏跟着清
watch(model, (ids) => {
  const chosen = new Set(ids)
  if (picked.value.some(user => !chosen.has(user.userId))) {
    picked.value = picked.value.filter(user => chosen.has(user.userId))
  }
})

// 候选读回来的在线状态并进聊天 store：在线点一律从那里取，之后的上下线推送也改在那里
watch(directory.users, users => chatStore.notePresence(users))

watchDebounced(keyword, reload, { debounce: 250 })
watch([
  scope,
  departmentId,
], reload)

onMounted(() => {
  reload()
  // 平台一侧没有部门：树是空的就不出部门筛选
  getChatApi().departmentTree().then((nodes) => {
    departmentOptions.value = nodes.map(mapDepartment)
  }).catch(notifyFailure)
})
</script>

<template>
  <!-- 一组控件：调用方经 aria-label 给它名字 -->
  <div role="group" class="flex min-w-0 flex-col gap-2">
    <div class="flex flex-wrap items-center gap-2">
      <XInput
        v-model:value="keyword"
        class="min-w-40 flex-1"
        size="sm"
        clearable
        :placeholder="t('chat.picker.search_placeholder')"
        :aria-label="t('chat.picker.search_placeholder')"
      >
        <template #prefix>
          <Icon icon="lucide:search" width="14" height="14" class="text-muted-foreground" />
        </template>
      </XInput>
      <XSegmented v-model:value="scope" :options="scopeOptions" :aria-label="t('chat.picker.scope')" />
    </div>
    <XTreeSelect
      v-if="scope === 'all' && departmentOptions.length"
      v-model:value="departmentId"
      class="w-full"
      :options="departmentOptions"
      clearable
      :placeholder="t('chat.picker.department_placeholder')"
      :aria-label="t('chat.picker.department_placeholder')"
    />

    <div class="grid min-w-0 gap-3" :class="props.multiple ? 'sm:grid-cols-2' : ''">
      <XhListboxRoot
        class="chat-member-picker__pane chat-member-picker__list min-w-0"
        :value="model"
        :collection="collection"
        :selection-mode="props.multiple ? 'multiple' : 'single'"
        :loading="loading"
        :aria-label="t('chat.picker.candidates')"
        @update:value="handleValueChange"
      >
        <XhListboxContent>
          <XhListboxItem v-for="user in candidates" :key="user.userId" :value="user.userId">
            <XhListboxItemPrefix>
              <ChatPresenceBadge :user-id="user.userId">
                <XUserAvatar :avatar="user.avatarFileId" :name="user.displayName" :size="28" />
              </ChatPresenceBadge>
            </XhListboxItemPrefix>
            <XhListboxItemText>{{ user.displayName }}</XhListboxItemText>
            <XhListboxItemDescription v-if="describe(user)">
              {{ describe(user) }}
            </XhListboxItemDescription>
            <XhListboxItemSuffix v-if="locked.has(user.userId) && props.lockedLabel">
              {{ props.lockedLabel }}
            </XhListboxItemSuffix>
            <XhListboxItemIndicator />
          </XhListboxItem>
        </XhListboxContent>
        <XhListboxEmpty>
          {{ scope === 'recent' ? t('chat.picker.recent_empty') : t('chat.picker.empty') }}
        </XhListboxEmpty>
        <XhListboxLoading>{{ t('chat.picker.loading') }}</XhListboxLoading>
        <XhListboxLoadMoreTrigger v-if="canLoadMore" @click="handleLoadMore">
          {{ t('chat.picker.load_more') }}
        </XhListboxLoadMoreTrigger>
      </XhListboxRoot>

      <section v-if="props.multiple" class="chat-member-picker__pane min-w-0" :aria-label="t('chat.picker.picked', { n: picked.length })">
        <div class="flex items-center justify-between gap-2 px-3 py-2">
          <span class="text-xs font-medium text-muted-foreground" aria-live="polite">{{ t('chat.picker.picked', { n: picked.length }) }}</span>
          <XhButton v-if="picked.length" variant="link" size="sm" class="xh-link-button" @click="handleClear">
            {{ t('chat.picker.clear') }}
          </XhButton>
        </div>
        <ul v-if="picked.length" class="xh-scroll-area m-0 min-h-0 flex-1 list-none px-1 pt-0 pb-1">
          <li v-for="user in picked" :key="user.userId" class="app-fade flex items-center gap-2 py-1 pr-1 pl-2">
            <ChatPresenceBadge :user-id="user.userId">
              <XUserAvatar :avatar="user.avatarFileId" :name="user.displayName" :size="28" />
            </ChatPresenceBadge>
            <span class="min-w-0 flex-1 truncate text-[13px] text-foreground">{{ user.displayName }}</span>
            <XIconButton icon="lucide:x" :label="t('chat.picker.remove', { name: user.displayName })" @click="handleRemove(user.userId)" />
          </li>
        </ul>
        <p v-else class="m-0 flex flex-1 items-center justify-center px-3 pb-3 text-center text-xs text-muted-foreground">
          {{ t('chat.picker.picked_empty') }}
        </p>
      </section>
    </div>
  </div>
</template>

<style scoped>
/* 候选列表与已选栏同一副定高承载面：换一批候选、增减已选都不让对话框跳动 */
.chat-member-picker__pane {
  display: flex;
  flex-direction: column;
  block-size: var(--xh-viewport-h-lg);
  border: var(--xh-stroke-thin) solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background: var(--xh-bg-surface);
  overflow: hidden;
}

/* 描边画在外面的承载面上：空态、在途与加载更多都落在同一个框里，列表自己不再画框、不另限高 */
.chat-member-picker__list {
  --xh-listbox-gap: var(--xh-space-0);
  --xh-listbox-content-border: transparent;
  --xh-listbox-content-bg: transparent;
  --xh-listbox-content-max-h: none;
}
</style>
