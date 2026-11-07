<script setup lang="ts">
import type {
  ChatDepartmentPickerNode,
} from '../types'
import type { SelectOption, TreeSelectOption } from '~/types'
import {
  XhButton,
  XhButtonIndicator,
  XhButtonLabel,
  XhDialogBody,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogFooter,
  XhDialogHeader,
  XhDialogRoot,
  XhDialogTitle,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
} from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XInput from '~/components/common/XInput.vue'
import XSelect from '~/components/common/XSelect.vue'
import XTreeSelect from '~/components/common/XTreeSelect.vue'
import { toast } from '~/composables'
import { getChatAssistantProvider, useUserStore } from '~/stores'
import {
  getChatApi,
} from '../api-contract'
import { useChatStore } from '../store'
import {
  CHAT_MAX_GROUP_NAME_LENGTH,
} from '../types'
import { chatErrorText } from './chat-helpers'

import ChatUserSelect from './ChatUserSelect.vue'

export type ChatStartMode = 'assistant' | 'department' | 'group' | 'single'

defineOptions({ name: 'ChatStartDialog' })

const props = defineProps<{
  mode: ChatStartMode
}>()

const emit = defineEmits<{
  started: []
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()
const chatStore = useChatStore()
const userStore = useUserStore()

const submitting = ref(false)
const singleUserId = ref<null | string>(null)
const groupName = ref('')
const groupMemberIds = ref<string[]>([])
const departmentId = ref<null | string>(null)
const departmentOptions = ref<TreeSelectOption[]>([])
const departmentLoading = ref(false)
const assistantId = ref<null | string>(null)
const assistantOptions = ref<SelectOption[]>([])
const assistantLoading = ref(false)

const title = computed(() => {
  switch (props.mode) {
    case 'single':
      return t('chat.start.single_title')
    case 'group':
      return t('chat.start.group_title')
    case 'assistant':
      return t('chat.start.assistant_title')
    default:
      return t('chat.start.department_title')
  }
})

const currentUserId = computed(() => userStore.userInfo?.basicId ?? '')

function mapDepartment(node: ChatDepartmentPickerNode): TreeSelectOption {
  return {
    value: node.departmentId,
    label: node.departmentName,
    children: node.children?.length
      ? node.children.map(mapDepartment)
      : undefined,
  }
}

async function loadDepartments() {
  if (departmentOptions.value.length || departmentLoading.value) {
    return
  }
  departmentLoading.value = true
  try {
    const nodes = await getChatApi().departmentTree()
    departmentOptions.value = nodes.map(mapDepartment)
  }
  catch (error) {
    departmentOptions.value = []
    toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
  }
  finally {
    departmentLoading.value = false
  }
}

async function loadAssistants() {
  if (assistantLoading.value) {
    return
  }
  assistantLoading.value = true
  try {
    const items = await getChatAssistantProvider()!.availableAssistants()
    assistantOptions.value = items.map(item => ({
      value: item.id,
      label: item.name,
    }))
    assistantId.value = items.find(item => item.isDefault)?.id ?? items[0]?.id ?? null
  }
  catch (error) {
    assistantOptions.value = []
    toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
  }
  finally {
    assistantLoading.value = false
  }
}

watch(show, (visible) => {
  if (visible) {
    singleUserId.value = null
    groupName.value = ''
    groupMemberIds.value = []
    departmentId.value = null
    assistantId.value = null
    if (props.mode === 'department') {
      void loadDepartments()
    }
    if (props.mode === 'assistant') {
      void loadAssistants()
    }
  }
})

async function handleConfirm() {
  if (submitting.value) {
    return
  }
  if (props.mode === 'single' && !singleUserId.value) {
    toast.warning(t('chat.start.user_required'))
    return
  }
  if (props.mode === 'group') {
    if (!groupName.value.trim()) {
      toast.warning(t('chat.start.group_name_required'))
      return
    }
    if (!groupMemberIds.value.length) {
      toast.warning(t('chat.start.members_required'))
      return
    }
  }
  if (props.mode === 'department' && !departmentId.value) {
    toast.warning(t('chat.start.department_required'))
    return
  }
  if (props.mode === 'assistant' && !assistantId.value) {
    toast.warning(t('chat.start.assistant_required'))
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
    else if (props.mode === 'assistant') {
      await chatStore.startAssistantConversation(assistantId.value!)
    }
    else {
      await chatStore.startDepartmentConversation(departmentId.value!)
    }
    show.value = false
    emit('started')
  }
  catch (error) {
    // 请求层不弹统一提示：后端的本地化正文由这里提示
    toast.danger(chatErrorText(error) || t('common.messages.operation_failed'))
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent style="--xh-dialog-max-w: 420px">
      <XhDialogHeader>
        <XhDialogTitle>{{ title }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody>
        <div class="flex flex-col gap-3">
          <XhFieldRoot v-if="props.mode === 'single'" required>
            <XhFieldLabel>{{ t('chat.start.user_label') }}</XhFieldLabel>
            <XhFieldControl>
              <ChatUserSelect
                v-model="singleUserId"
                :exclude-user-ids="[currentUserId]"
                :placeholder="t('chat.start.user_placeholder')"
              />
            </XhFieldControl>
          </XhFieldRoot>

          <template v-else-if="props.mode === 'group'">
            <XhFieldRoot required>
              <XhFieldLabel>{{ t('chat.start.group_name_label') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput
                  v-model:value="groupName"
                  :max-length="CHAT_MAX_GROUP_NAME_LENGTH"
                  show-count
                  :placeholder="t('chat.start.group_name_placeholder')"
                />
              </XhFieldControl>
            </XhFieldRoot>
            <XhFieldRoot required>
              <XhFieldLabel>{{ t('chat.start.members_label') }}</XhFieldLabel>
              <XhFieldControl>
                <ChatUserSelect
                  v-model="groupMemberIds"
                  multiple
                  :exclude-user-ids="[currentUserId]"
                  :placeholder="t('chat.start.users_placeholder')"
                />
              </XhFieldControl>
            </XhFieldRoot>
          </template>

          <XhFieldRoot v-else-if="props.mode === 'assistant'" required>
            <XhFieldLabel>{{ t('chat.start.assistant_label') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect
                v-model:value="assistantId"
                :options="assistantOptions"
                clearable
                :placeholder="t('chat.start.assistant_placeholder')"
              />
            </XhFieldControl>
          </XhFieldRoot>

          <XhFieldRoot v-else required>
            <XhFieldLabel>{{ t('chat.start.department_label') }}</XhFieldLabel>
            <XhFieldControl>
              <XTreeSelect
                v-model:value="departmentId"
                :options="departmentOptions"
                :loading="departmentLoading"
                clearable
                :placeholder="t('chat.start.department_placeholder')"
              />
            </XhFieldControl>
          </XhFieldRoot>
        </div>
      </XhDialogBody>

      <XhDialogFooter>
        <XhButton variant="subtle" size="sm" :disabled="submitting" @click="show = false">
          {{ t('chat.start.cancel') }}
        </XhButton>
        <XhButton variant="subtle" size="sm" tone="brand" :loading="submitting" @click="handleConfirm">
          <XhButtonIndicator />
          <XhButtonLabel>{{ t('chat.start.confirm') }}</XhButtonLabel>
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>
