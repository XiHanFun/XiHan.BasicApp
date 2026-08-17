<script setup lang="ts">
import type {
  AiAssistantCreateDto,
  AiAssistantListItemDto,
  AiAssistantUpdateDto,
} from '../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhBadge, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import {
  aiAssistantApi,
  EnableStatus,
} from '../../../api'

defineOptions({ name: 'DevelopAiAssistantPage' })

interface AssistantFormModel {
  basicId?: string
  assistantCode: string
  assistantName: string
  avatar?: string | null
  description?: string | null
  greeting?: string | null
  promptCode?: string | null
  providerCode?: string | null
  enableKnowledge: boolean
  knowledgeProviderCode?: string | null
  knowledgeTopK: number
  historyRounds: number
  isDefault: boolean
  isEnabled: boolean
  sort: number
  status: EnableStatus
  remark?: string | null
}

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('develop.ai_assistant.col_assistant_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('develop.ai_assistant.search_placeholder'), order: 0 },
  { key: 'assistantName', title: t('develop.ai_assistant.col_assistant_name'), dataType: 'string', minWidth: 160, fixed: 'left', sortable: true, order: 1 },
  { key: 'assistantCode', title: t('develop.ai_assistant.col_assistant_code'), dataType: 'string', minWidth: 150, sortable: true, order: 2 },
  { key: 'providerCode', title: t('develop.ai_assistant.col_provider_code'), dataType: 'string', width: 140, order: 3 },
  { key: 'promptCode', title: t('develop.ai_assistant.col_prompt_code'), dataType: 'string', width: 140, order: 4 },
  {
    key: 'enableKnowledge',
    title: t('develop.ai_assistant.col_enable_knowledge'),
    dataType: 'boolean',
    width: 100,
    sortable: true,
    order: 5,
    render: (row) => {
      const r = row as unknown as AiAssistantListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: r.enableKnowledge ? 'success' : 'neutral' }, () => (r.enableKnowledge ? t('common.statuses.yes') : t('common.statuses.no')))
    },
  },
  {
    key: 'isDefault',
    title: t('develop.ai_assistant.col_default'),
    dataType: 'boolean',
    width: 80,
    sortable: true,
    order: 6,
    render: (row) => {
      const r = row as unknown as AiAssistantListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: r.isDefault ? 'info' : 'neutral' }, () => (r.isDefault ? t('common.statuses.yes') : t('common.statuses.no')))
    },
  },
  {
    key: 'isEnabled',
    title: t('develop.ai_assistant.col_enabled'),
    dataType: 'boolean',
    width: 80,
    sortable: true,
    order: 7,
    render: (row) => {
      const r = row as unknown as AiAssistantListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: r.isEnabled ? 'success' : 'neutral' }, () => (r.isEnabled ? t('common.statuses.yes') : t('common.statuses.no')))
    },
  },
  {
    key: 'status',
    title: t('common.fields.status'),
    dataType: 'enum',
    dictionaryCode: 'EnableStatus',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: STATUS_OPTIONS,
    searchPlaceholder: t('common.fields.status'),
    width: 90,
    order: 8,
    render: (row) => {
      const r = row as unknown as AiAssistantListItemDto
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: r.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => getOptionLabel(statusEnumOptions.value, r.status))
    },
  },
  { key: 'sort', title: t('common.fields.sort'), dataType: 'number', width: 80, sortable: true, order: 9 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.ai.assistant',
  pageName: t('develop.ai_assistant.page_name'),
  rowKey: 'basicId',
  scrollX: 1200,
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return aiAssistantApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => aiAssistantApi.delete(id),
  },
  actions: [
    { key: 'create', title: t('develop.ai_assistant.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil' },
    { key: 'default', title: t('develop.ai_assistant.action_default'), scope: 'row', icon: 'lucide:star', disabled: row => (row as unknown as AiAssistantListItemDto).isDefault },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as AiAssistantListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'default':
      if (row) {
        void handleSetDefault(row)
      }
      break
    case 'delete':
      if (row) {
        handleDelete(row)
      }
      break
  }
}

async function handleSetDefault(row: AiAssistantListItemDto) {
  try {
    await aiAssistantApi.setDefault(row.basicId)
    toast.success(t('develop.ai_assistant.set_default_success'))
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.ai_assistant.set_default_error'))
  }
}

function handleDelete(row: AiAssistantListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('common.actions.delete'),
    content: t('develop.ai_assistant.confirm_delete'),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await aiAssistantApi.delete(row.basicId)
        toast.success(t('common.messages.delete_success'))
        reload()
      }
      catch (error) {
        toast.error((error as Error)?.message || t('common.messages.delete_failed'))
      }
    },
  })
}

// ── 表单/弹窗 ───────────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const form = ref<AssistantFormModel>(createDefaultForm())
const modalTitle = computed(() => (form.value.basicId ? t('develop.ai_assistant.modal_edit_title') : t('develop.ai_assistant.modal_add_title')))

function createDefaultForm(): AssistantFormModel {
  return {
    assistantCode: '',
    assistantName: '',
    avatar: null,
    description: null,
    greeting: null,
    promptCode: null,
    providerCode: null,
    enableKnowledge: true,
    knowledgeProviderCode: null,
    knowledgeTopK: 5,
    historyRounds: 10,
    isDefault: false,
    isEnabled: true,
    sort: 100,
    status: EnableStatus.Enabled,
    remark: null,
  }
}

function handleAdd() {
  editingStatus.value = null
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: AiAssistantListItemDto) {
  try {
    const detail = await aiAssistantApi.detail(row.basicId)
    if (!detail) {
      toast.error(t('develop.ai_assistant.not_found'))
      return
    }
    editingStatus.value = detail.status
    form.value = {
      basicId: detail.basicId,
      assistantCode: detail.assistantCode,
      assistantName: detail.assistantName,
      avatar: detail.avatar ?? null,
      description: detail.description ?? null,
      greeting: detail.greeting ?? null,
      promptCode: detail.promptCode ?? null,
      providerCode: detail.providerCode ?? null,
      enableKnowledge: detail.enableKnowledge,
      knowledgeProviderCode: detail.knowledgeProviderCode ?? null,
      knowledgeTopK: detail.knowledgeTopK,
      historyRounds: detail.historyRounds,
      isDefault: detail.isDefault,
      isEnabled: detail.isEnabled,
      sort: detail.sort,
      status: detail.status,
      remark: detail.remark ?? null,
    }
    modalVisible.value = true
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.ai_assistant.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.assistantName.trim()) {
    toast.warning(t('develop.ai_assistant.validate_name'))
    return false
  }
  if (!form.value.basicId && !form.value.assistantCode.trim()) {
    toast.warning(t('develop.ai_assistant.validate_code'))
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }
  submitLoading.value = true
  try {
    if (form.value.basicId) {
      const updateInput: AiAssistantUpdateDto = {
        basicId: form.value.basicId,
        assistantName: form.value.assistantName.trim(),
        avatar: form.value.avatar?.trim() || null,
        description: form.value.description?.trim() || null,
        greeting: form.value.greeting?.trim() || null,
        promptCode: form.value.promptCode?.trim() || null,
        providerCode: form.value.providerCode?.trim() || null,
        enableKnowledge: form.value.enableKnowledge,
        knowledgeProviderCode: form.value.knowledgeProviderCode?.trim() || null,
        knowledgeTopK: form.value.knowledgeTopK,
        historyRounds: form.value.historyRounds,
        isDefault: form.value.isDefault,
        isEnabled: form.value.isEnabled,
        sort: form.value.sort,
        remark: form.value.remark,
      }
      await aiAssistantApi.update(updateInput)
      if (editingStatus.value !== form.value.status) {
        await aiAssistantApi.updateStatus({
          basicId: form.value.basicId,
          remark: t('develop.ai_assistant.update_status_remark'),
          status: form.value.status,
        })
      }
    }
    else {
      const createInput: AiAssistantCreateDto = {
        assistantCode: form.value.assistantCode.trim(),
        assistantName: form.value.assistantName.trim(),
        avatar: form.value.avatar?.trim() || null,
        description: form.value.description?.trim() || null,
        greeting: form.value.greeting?.trim() || null,
        promptCode: form.value.promptCode?.trim() || null,
        providerCode: form.value.providerCode?.trim() || null,
        enableKnowledge: form.value.enableKnowledge,
        knowledgeProviderCode: form.value.knowledgeProviderCode?.trim() || null,
        knowledgeTopK: form.value.knowledgeTopK,
        historyRounds: form.value.historyRounds,
        isDefault: form.value.isDefault,
        isEnabled: form.value.isEnabled,
        sort: form.value.sort,
        status: form.value.status,
        remark: form.value.remark,
      }
      await aiAssistantApi.create(createInput)
    }
    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="form"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="assistantCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_assistant_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.assistantCode"
                clearable
                :disabled="Boolean(form.basicId)"
                :placeholder="t('develop.ai_assistant.form_assistant_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="assistantName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_assistant_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.assistantName" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="providerCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_provider_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.providerCode" clearable :placeholder="t('develop.ai_assistant.form_provider_code_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="promptCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_prompt_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.promptCode" clearable :placeholder="t('develop.ai_assistant.form_prompt_code_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="enableKnowledge">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_enable_knowledge') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.enableKnowledge" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="knowledgeProviderCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_knowledge_provider_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.knowledgeProviderCode" clearable :placeholder="t('develop.ai_assistant.form_knowledge_provider_code_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="knowledgeTopK">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_knowledge_top_k') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.knowledgeTopK" :min="1" :max="20" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="historyRounds">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_history_rounds') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.historyRounds" :min="0" :max="50" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="avatar">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_avatar') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.avatar" clearable :placeholder="t('develop.ai_assistant.form_avatar_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('common.fields.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isDefault">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_is_default') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.isDefault" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isEnabled">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_is_enabled') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.isEnabled" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="!form.basicId" value="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('common.fields.status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.status" :options="statusEnumOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="description" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.description" clearable :rows="2" type="textarea" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="greeting" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_assistant.form_greeting') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.greeting"
                clearable
                :rows="3"
                type="textarea"
                :placeholder="t('develop.ai_assistant.form_greeting_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('common.fields.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.remark" clearable :rows="2" type="textarea" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
