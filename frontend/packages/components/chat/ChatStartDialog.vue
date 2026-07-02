<script setup lang="ts">
import type { TreeSelectOption } from 'naive-ui'
import type { ChatDepartmentPickerNode } from '~/types'
import { NButton, NInput, NModal, NTreeSelect, useMessage } from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppContext, useChatStore, useUserStore } from '~/stores'
import { CHAT_MAX_GROUP_NAME_LENGTH } from '~/types'
import ChatUserSelect from './ChatUserSelect.vue'

export type ChatStartMode = 'department' | 'group' | 'single'

defineOptions({ name: 'ChatStartDialog' })

const props = defineProps<{
  mode: ChatStartMode
}>()

const emit = defineEmits<{
  started: []
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const message = useMessage()
const chatStore = useChatStore()
const userStore = useUserStore()
const appContext = useAppContext()

const submitting = ref(false)
const singleUserId = ref<null | string>(null)
const groupName = ref('')
const groupMemberIds = ref<string[]>([])
const departmentId = ref<null | string>(null)
const departmentOptions = ref<TreeSelectOption[]>([])
const departmentLoading = ref(false)

const title = computed(() => {
  switch (props.mode) {
    case 'single':
      return t('chat.start.single_title')
    case 'group':
      return t('chat.start.group_title')
    default:
      return t('chat.start.department_title')
  }
})

const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')

function mapDepartment(node: ChatDepartmentPickerNode): TreeSelectOption {
  return {
    key: node.departmentId,
    label: node.departmentName,
    children: node.children?.length ? node.children.map(mapDepartment) : undefined,
  }
}

async function loadDepartments() {
  if (departmentOptions.value.length || departmentLoading.value) {
    return
  }
  departmentLoading.value = true
  try {
    const nodes = await appContext.apis.chatApi.departmentTree()
    departmentOptions.value = nodes.map(mapDepartment)
  }
  catch {
    departmentOptions.value = []
  }
  finally {
    departmentLoading.value = false
  }
}

watch(show, (visible) => {
  if (visible) {
    singleUserId.value = null
    groupName.value = ''
    groupMemberIds.value = []
    departmentId.value = null
    if (props.mode === 'department') {
      void loadDepartments()
    }
  }
})

async function handleConfirm() {
  if (submitting.value) {
    return
  }
  if (props.mode === 'single' && !singleUserId.value) {
    message.warning(t('chat.start.user_required'))
    return
  }
  if (props.mode === 'group') {
    if (!groupName.value.trim()) {
      message.warning(t('chat.start.group_name_required'))
      return
    }
    if (!groupMemberIds.value.length) {
      message.warning(t('chat.start.members_required'))
      return
    }
  }
  if (props.mode === 'department' && !departmentId.value) {
    message.warning(t('chat.start.department_required'))
    return
  }

  submitting.value = true
  try {
    if (props.mode === 'single') {
      await chatStore.startSingleConversation(singleUserId.value!)
    }
    else if (props.mode === 'group') {
      await chatStore.startGroupConversation(groupName.value.trim(), groupMemberIds.value)
    }
    else {
      await chatStore.startDepartmentConversation(departmentId.value!)
    }
    show.value = false
    emit('started')
  }
  catch {
    // 请求层已有统一错误提示（本地化异常），此处不重复弹
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <NModal
    v-model:show="show"
    preset="card"
    :title="title"
    style="width: 420px; max-width: calc(100vw - 32px);"
    :mask-closable="!submitting"
  >
    <div class="flex flex-col gap-3">
      <template v-if="props.mode === 'single'">
        <ChatUserSelect
          v-model="singleUserId"
          :exclude-user-ids="[currentUserId]"
          :placeholder="t('chat.start.user_placeholder')"
        />
      </template>

      <template v-else-if="props.mode === 'group'">
        <NInput
          v-model:value="groupName"
          :maxlength="CHAT_MAX_GROUP_NAME_LENGTH"
          show-count
          :placeholder="t('chat.start.group_name_placeholder')"
        />
        <ChatUserSelect
          v-model="groupMemberIds"
          multiple
          :exclude-user-ids="[currentUserId]"
          :placeholder="t('chat.start.users_placeholder')"
        />
      </template>

      <template v-else>
        <NTreeSelect
          v-model:value="departmentId"
          :options="departmentOptions"
          :loading="departmentLoading"
          filterable
          clearable
          :placeholder="t('chat.start.department_placeholder')"
        />
      </template>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2">
        <NButton size="small" :disabled="submitting" @click="show = false">
          {{ t('chat.start.cancel') }}
        </NButton>
        <NButton size="small" type="primary" :loading="submitting" @click="handleConfirm">
          {{ t('chat.start.confirm') }}
        </NButton>
      </div>
    </template>
  </NModal>
</template>
