<script setup lang="ts">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type {
  AiAssistantCreateDto,
  AiAssistantListItemDto,
  AiAssistantUpdateDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NSelect,
  NSwitch,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  aiAssistantApi,
  createPageRequest,
  EnableStatus,
  querySortsFromSchema,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XEditModal } from '~/components'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'

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
const message = useMessage()
const dialog = useDialog()

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
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.enableKnowledge ? 'success' : 'default' }, () => (r.enableKnowledge ? t('common.statuses.yes') : t('common.statuses.no')))
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
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.isDefault ? 'info' : 'default' }, () => (r.isDefault ? t('common.statuses.yes') : t('common.statuses.no')))
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
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.isEnabled ? 'success' : 'default' }, () => (r.isEnabled ? t('common.statuses.yes') : t('common.statuses.no')))
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
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(statusEnumOptions.value, r.status))
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
    message.success(t('develop.ai_assistant.set_default_success'))
    reload()
  }
  catch (error) {
    message.error((error as Error)?.message || t('develop.ai_assistant.set_default_error'))
  }
}

function handleDelete(row: AiAssistantListItemDto) {
  dialog.warning({
    title: t('common.actions.delete'),
    content: t('develop.ai_assistant.confirm_delete'),
    positiveText: t('common.actions.confirm'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await aiAssistantApi.delete(row.basicId)
        message.success(t('common.messages.delete_success'))
        reload()
      }
      catch (error) {
        message.error((error as Error)?.message || t('common.messages.delete_failed'))
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
      message.error(t('develop.ai_assistant.not_found'))
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
    message.error((error as Error)?.message || t('develop.ai_assistant.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.assistantName.trim()) {
    message.warning(t('develop.ai_assistant.validate_name'))
    return false
  }
  if (!form.value.basicId && !form.value.assistantCode.trim()) {
    message.warning(t('develop.ai_assistant.validate_code'))
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
    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    message.error((error as Error)?.message || t('common.messages.save_failed'))
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
      @save="handleSubmit"
    >
      <NForm :model="form" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('develop.ai_assistant.form_assistant_code')" path="assistantCode">
          <NInput
            v-model:value="form.assistantCode"
            clearable
            :disabled="Boolean(form.basicId)"
            :placeholder="t('develop.ai_assistant.form_assistant_code_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_assistant_name')" path="assistantName">
          <NInput v-model:value="form.assistantName" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_provider_code')" path="providerCode">
          <NInput v-model:value="form.providerCode" clearable :placeholder="t('develop.ai_assistant.form_provider_code_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_prompt_code')" path="promptCode">
          <NInput v-model:value="form.promptCode" clearable :placeholder="t('develop.ai_assistant.form_prompt_code_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_enable_knowledge')" path="enableKnowledge">
          <NSwitch v-model:value="form.enableKnowledge" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_knowledge_provider_code')" path="knowledgeProviderCode">
          <NInput v-model:value="form.knowledgeProviderCode" clearable :placeholder="t('develop.ai_assistant.form_knowledge_provider_code_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_knowledge_top_k')" path="knowledgeTopK">
          <NInputNumber v-model:value="form.knowledgeTopK" :min="1" :max="20" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_history_rounds')" path="historyRounds">
          <NInputNumber v-model:value="form.historyRounds" :min="0" :max="50" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_avatar')" path="avatar">
          <NInput v-model:value="form.avatar" clearable :placeholder="t('develop.ai_assistant.form_avatar_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('common.fields.sort')" path="sort">
          <NInputNumber v-model:value="form.sort" :min="0" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_is_default')" path="isDefault">
          <NSwitch v-model:value="form.isDefault" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_assistant.form_is_enabled')" path="isEnabled">
          <NSwitch v-model:value="form.isEnabled" />
        </NFormItem>
        <NFormItem v-if="!form.basicId" :label="t('common.fields.status')" path="status">
          <NSelect v-model:value="form.status" :options="statusEnumOptions as unknown as SelectMixedOption[]" />
        </NFormItem>
        <NFormItem class="xh-span-2" :label="t('develop.ai_assistant.form_description')" path="description">
          <NInput v-model:value="form.description" clearable :rows="2" type="textarea" />
        </NFormItem>
        <NFormItem class="xh-span-2" :label="t('develop.ai_assistant.form_greeting')" path="greeting">
          <NInput
            v-model:value="form.greeting"
            clearable
            :rows="3"
            type="textarea"
            :placeholder="t('develop.ai_assistant.form_greeting_placeholder')"
          />
        </NFormItem>
        <NFormItem class="xh-span-2" :label="t('common.fields.remark')" path="remark">
          <NInput v-model:value="form.remark" clearable :rows="2" type="textarea" />
        </NFormItem>
      </NForm>
    </XEditModal>
  </SchemaPage>
</template>
