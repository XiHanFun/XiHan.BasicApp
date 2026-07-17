<script setup lang="ts">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type {
  AiProviderCreateDto,
  AiProviderListItemDto,
  AiProviderUpdateDto,
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
  AI_PROVIDER_OPTIONS,
  aiProviderApi,
  createPageRequest,
  EnableStatus,
  querySortsFromSchema,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XEditModal } from '~/components'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'DevelopAiProviderPage' })

interface ProviderFormModel {
  basicId?: string
  configCode: string
  configName: string
  provider: string
  model: string
  embeddingModel?: string | null
  baseUrl?: string | null
  apiKey?: string | null
  maxOutputTokens?: number | null
  temperature?: number | null
  timeoutSeconds?: number | null
  extraJson?: string | null
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
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('develop.ai_provider.col_config_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('develop.ai_provider.search_placeholder'), order: 0 },
  {
    key: 'configName',
    title: t('develop.ai_provider.col_config_name'),
    dataType: 'string',
    minWidth: 160,
    fixed: 'left',
    sortable: true,
    order: 1,
    render: (row) => {
      const r = row as unknown as AiProviderListItemDto
      return h('div', { class: 'ap-name' }, [
        h('span', { class: 'ap-name__text' }, r.configName),
        r.isDefault
          ? h(NTag, { size: 'tiny', type: 'info', round: true, bordered: false }, () => t('common.statuses.default_tag'))
          : null,
      ])
    },
  },
  { key: 'configCode', title: t('develop.ai_provider.col_config_code'), dataType: 'string', minWidth: 140, sortable: true, order: 2 },
  { key: 'provider', title: t('develop.ai_provider.col_provider'), dataType: 'string', width: 120, sortable: true, order: 3 },
  { key: 'model', title: t('develop.ai_provider.col_model'), dataType: 'string', minWidth: 150, sortable: true, order: 4 },
  {
    key: 'hasApiKey',
    title: t('develop.ai_provider.col_api_key'),
    dataType: 'boolean',
    width: 100,
    order: 5,
    render: (row) => {
      const r = row as unknown as AiProviderListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.hasApiKey ? 'success' : 'warning' }, () => (r.hasApiKey ? t('develop.ai_provider.tag_configured') : t('develop.ai_provider.tag_unconfigured')))
    },
  },
  {
    key: 'isEnabled',
    title: t('develop.ai_provider.col_enabled'),
    dataType: 'boolean',
    width: 80,
    sortable: true,
    order: 6,
    render: (row) => {
      const r = row as unknown as AiProviderListItemDto
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
    order: 7,
    render: (row) => {
      const r = row as unknown as AiProviderListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(statusEnumOptions.value, r.status))
    },
  },
  { key: 'sort', title: t('common.fields.sort'), dataType: 'number', width: 80, sortable: true, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.ai.provider',
  pageName: t('develop.ai_provider.page_name'),
  rowKey: 'basicId',
  scrollX: 1100,
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return aiProviderApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(status) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => aiProviderApi.delete(id),
  },
  actions: [
    { key: 'create', title: t('develop.ai_provider.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'test', title: t('develop.ai_provider.action_test'), scope: 'row', type: 'info', icon: 'lucide:plug' },
    { key: 'default', title: t('develop.ai_provider.action_default'), scope: 'row', icon: 'lucide:star', disabled: row => (row as unknown as AiProviderListItemDto).isDefault },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as AiProviderListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'test':
      if (row) {
        void handleTest(row)
      }
      break
    case 'default':
      if (row) {
        void handleSetDefault(row)
      }
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'delete':
      if (row) {
        handleDelete(row)
      }
      break
  }
}

async function handleTest(row: AiProviderListItemDto) {
  const reset = message.loading(t('develop.ai_provider.testing'), { duration: 0 })
  try {
    const result = await aiProviderApi.testConnection(row.basicId)
    reset.destroy()
    if (result.success) {
      message.success(t('develop.ai_provider.test_success', { ms: result.latencyMs }))
    }
    else {
      message.error(result.message || t('develop.ai_provider.test_failed'))
    }
  }
  catch {
    reset.destroy()
    message.error(t('develop.ai_provider.test_error'))
  }
}

async function handleSetDefault(row: AiProviderListItemDto) {
  try {
    await aiProviderApi.setDefault(row.basicId)
    message.success(t('develop.ai_provider.set_default_success'))
    reload()
  }
  catch {
    message.error(t('develop.ai_provider.set_default_error'))
  }
}

function handleDelete(row: AiProviderListItemDto) {
  dialog.warning({
    title: t('common.actions.delete'),
    content: t('develop.ai_provider.confirm_delete'),
    positiveText: t('common.actions.confirm'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await aiProviderApi.delete(row.basicId)
        message.success(t('common.messages.delete_success'))
        reload()
      }
      catch {
        message.error(t('common.messages.delete_failed'))
      }
    },
  })
}

// ── 表单/弹窗 ───────────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const form = ref<ProviderFormModel>(createDefaultForm())
const modalTitle = computed(() => (form.value.basicId ? t('develop.ai_provider.modal_edit_title') : t('develop.ai_provider.modal_add_title')))

function createDefaultForm(): ProviderFormModel {
  return {
    configCode: '',
    configName: '',
    provider: 'OpenAI',
    model: '',
    embeddingModel: null,
    baseUrl: null,
    apiKey: null,
    maxOutputTokens: null,
    temperature: null,
    timeoutSeconds: null,
    extraJson: null,
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

async function handleEdit(row: AiProviderListItemDto) {
  try {
    const detail = await aiProviderApi.detail(row.basicId)
    if (!detail) {
      message.error(t('develop.ai_provider.not_found'))
      return
    }
    editingStatus.value = detail.status
    form.value = {
      basicId: detail.basicId,
      configCode: detail.configCode,
      configName: detail.configName,
      provider: detail.provider,
      model: detail.model,
      embeddingModel: detail.embeddingModel ?? null,
      baseUrl: detail.baseUrl ?? null,
      // 编辑态密钥留空 = 保留原密钥（后端 hasApiKey 标志是否已配置）
      apiKey: null,
      maxOutputTokens: detail.maxOutputTokens ?? null,
      temperature: detail.temperature ?? null,
      timeoutSeconds: detail.timeoutSeconds ?? null,
      extraJson: detail.extraJson ?? null,
      isDefault: detail.isDefault,
      isEnabled: detail.isEnabled,
      sort: detail.sort,
      status: detail.status,
      remark: detail.remark ?? null,
    }
    modalVisible.value = true
  }
  catch {
    message.error(t('develop.ai_provider.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.configName.trim()) {
    message.warning(t('develop.ai_provider.validate_config_name'))
    return false
  }
  if (!form.value.basicId && !form.value.configCode.trim()) {
    message.warning(t('develop.ai_provider.validate_config_code'))
    return false
  }
  if (!form.value.model.trim()) {
    message.warning(t('develop.ai_provider.validate_model'))
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
      const updateInput: AiProviderUpdateDto = {
        basicId: form.value.basicId,
        configName: form.value.configName.trim(),
        provider: form.value.provider.trim(),
        model: form.value.model.trim(),
        embeddingModel: form.value.embeddingModel?.trim() || null,
        baseUrl: form.value.baseUrl?.trim() || null,
        apiKey: form.value.apiKey?.trim() || null,
        maxOutputTokens: form.value.maxOutputTokens,
        temperature: form.value.temperature,
        timeoutSeconds: form.value.timeoutSeconds,
        extraJson: form.value.extraJson,
        isDefault: form.value.isDefault,
        isEnabled: form.value.isEnabled,
        sort: form.value.sort,
        remark: form.value.remark,
      }
      await aiProviderApi.update(updateInput)
      if (editingStatus.value !== form.value.status) {
        await aiProviderApi.updateStatus({
          basicId: form.value.basicId,
          remark: t('develop.ai_provider.update_status_remark'),
          status: form.value.status,
        })
      }
    }
    else {
      const createInput: AiProviderCreateDto = {
        configCode: form.value.configCode.trim(),
        configName: form.value.configName.trim(),
        provider: form.value.provider.trim(),
        model: form.value.model.trim(),
        embeddingModel: form.value.embeddingModel?.trim() || null,
        baseUrl: form.value.baseUrl?.trim() || null,
        apiKey: form.value.apiKey?.trim() || null,
        maxOutputTokens: form.value.maxOutputTokens,
        temperature: form.value.temperature,
        timeoutSeconds: form.value.timeoutSeconds,
        extraJson: form.value.extraJson,
        isDefault: form.value.isDefault,
        isEnabled: form.value.isEnabled,
        sort: form.value.sort,
        status: form.value.status,
        remark: form.value.remark,
      }
      await aiProviderApi.create(createInput)
    }
    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    reload()
  }
  catch {
    message.error(t('common.messages.save_failed'))
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
        <NFormItem :label="t('develop.ai_provider.form_config_code')" path="configCode">
          <NInput
            v-model:value="form.configCode"
            clearable
            :disabled="Boolean(form.basicId)"
            :placeholder="t('develop.ai_provider.form_config_code_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_config_name')" path="configName">
          <NInput v-model:value="form.configName" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_provider')" path="provider">
          <NSelect
            v-model:value="form.provider"
            filterable
            tag
            :options="AI_PROVIDER_OPTIONS as unknown as SelectMixedOption[]"
            :placeholder="t('develop.ai_provider.form_provider_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_model')" path="model">
          <NInput v-model:value="form.model" clearable :placeholder="t('develop.ai_provider.form_model_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_embedding_model')" path="embeddingModel">
          <NInput v-model:value="form.embeddingModel" clearable :placeholder="t('develop.ai_provider.form_embedding_model_placeholder')" />
        </NFormItem>
        <NFormItem class="xh-span-2" :label="t('develop.ai_provider.form_base_url')" path="baseUrl">
          <NInput v-model:value="form.baseUrl" clearable :placeholder="t('develop.ai_provider.form_base_url_placeholder')" :input-props="{ autocomplete: 'off' }" />
        </NFormItem>
        <NFormItem class="xh-span-2" :label="form.basicId ? t('develop.ai_provider.form_api_key_edit') : t('develop.ai_provider.form_api_key')" path="apiKey">
          <NInput
            v-model:value="form.apiKey"
            clearable
            :placeholder="t('develop.ai_provider.form_api_key_placeholder')"
            show-password-on="click"
            type="password"
            :input-props="{ autocomplete: 'new-password' }"
          />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_max_output_tokens')" path="maxOutputTokens">
          <NInputNumber v-model:value="form.maxOutputTokens" :min="1" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_temperature')" path="temperature">
          <NInputNumber v-model:value="form.temperature" :max="2" :min="0" :precision="2" :step="0.1" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_timeout_seconds')" path="timeoutSeconds">
          <NInputNumber v-model:value="form.timeoutSeconds" :min="1" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_sort')" path="sort">
          <NInputNumber v-model:value="form.sort" :min="0" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_is_default')" path="isDefault">
          <NSwitch v-model:value="form.isDefault" />
        </NFormItem>
        <NFormItem :label="t('develop.ai_provider.form_is_enabled')" path="isEnabled">
          <NSwitch v-model:value="form.isEnabled" />
        </NFormItem>
        <NFormItem v-if="!form.basicId" :label="t('common.fields.status')" path="status">
          <NSelect v-model:value="form.status" :options="statusEnumOptions as unknown as SelectMixedOption[]" />
        </NFormItem>
        <NFormItem class="xh-span-2" :label="t('develop.ai_provider.form_extra_json')" path="extraJson">
          <NInput
            v-model:value="form.extraJson"
            clearable
            :placeholder="t('develop.ai_provider.form_extra_json_placeholder')"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
      </NForm>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.ap-name {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.ap-name__text {
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
