<script setup lang="ts">
import type {
  AiProviderCreateDto,
  AiProviderListItemDto,
  AiProviderProbeResultDto,
  AiProviderUpdateDto,
} from '../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { SchemaPage, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { dialog, notification, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import {
  AI_PROVIDER_OPTIONS,
  aiProviderApi,
  EnableStatus,
} from '../../../api'

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

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

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
          ? h(XhTagRoot, { variant: 'solid', tone: 'info' }, () => h(XhTagLabel, () => t('common.statuses.default_tag')))
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
      return h(XhTagRoot, { variant: 'solid', tone: r.hasApiKey ? 'success' : 'warning' }, () => h(XhTagLabel, () => (r.hasApiKey ? t('develop.ai_provider.tag_configured') : t('develop.ai_provider.tag_unconfigured'))))
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
      return h(XhTagRoot, { variant: 'solid', tone: r.isEnabled ? 'success' : 'neutral' }, () => h(XhTagLabel, () => (r.isEnabled ? t('common.statuses.yes') : t('common.statuses.no'))))
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
      return h(XhTagRoot, { variant: 'solid', tone: r.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => getOptionLabel(statusEnumOptions.value, r.status)))
    },
  },
  { key: 'sort', title: t('common.fields.sort'), dataType: 'number', width: 80, sortable: true, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.ai.provider',
  pageName: t('develop.ai_provider.page_name'),
  rowKey: 'basicId',
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

// 会话与嵌入走各自的端点，逐项成文，避免只看总体结论时误判另一项也可用
function describeProbe(label: string, probe: AiProviderProbeResultDto) {
  if (!probe.success) {
    const reason = probe.message ? `：${probe.message}` : ''
    return `${label} ${t('develop.ai_provider.probe_failed')}${reason}`
  }
  const dimensions = probe.dimensions
    ? t('develop.ai_provider.probe_dimensions', { n: probe.dimensions })
    : ''
  return `${label} ${t('develop.ai_provider.probe_ok', { ms: probe.latencyMs })}${dimensions}`
}

async function handleTest(row: AiProviderListItemDto) {
  const reset = toast.loading(t('develop.ai_provider.testing'), { duration: 0 })
  try {
    const result = await aiProviderApi.testConnection(row.basicId)
    reset.destroy()
    const lines = [describeProbe(t('develop.ai_provider.probe_chat'), result.chat)]
    lines.push(result.embedding
      ? describeProbe(t('develop.ai_provider.probe_embedding'), result.embedding)
      : t('develop.ai_provider.probe_embedding_absent'))

    // 探测结果逐行罗列，一句话说不完：走通知的标题加正文两层
    notification[result.success ? 'success' : 'error'](
      t(result.success ? 'develop.ai_provider.test_success' : 'develop.ai_provider.test_failed'),
      { description: lines.join('\n'), duration: result.success ? 5000 : 0 },
    )
  }
  catch (error) {
    reset.destroy()
    toast.error((error as Error)?.message || t('develop.ai_provider.test_error'))
  }
}

async function handleSetDefault(row: AiProviderListItemDto) {
  try {
    await aiProviderApi.setDefault(row.basicId)
    toast.success(t('develop.ai_provider.set_default_success'))
    reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.ai_provider.set_default_error'))
  }
}

function handleDelete(row: AiProviderListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('common.actions.delete'),
    content: t('develop.ai_provider.confirm_delete'),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await aiProviderApi.delete(row.basicId)
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
      toast.error(t('develop.ai_provider.not_found'))
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
  catch (error) {
    toast.error((error as Error)?.message || t('develop.ai_provider.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.configName.trim()) {
    toast.warning(t('develop.ai_provider.validate_config_name'))
    return false
  }
  if (!form.value.basicId && !form.value.configCode.trim()) {
    toast.warning(t('develop.ai_provider.validate_config_code'))
    return false
  }
  if (!form.value.model.trim()) {
    toast.warning(t('develop.ai_provider.validate_model'))
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
        <XhFormFieldGroup value="configCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_config_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.configCode"
                clearable
                :disabled="Boolean(form.basicId)"
                :placeholder="t('develop.ai_provider.form_config_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="configName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_config_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.configName" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="provider">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_provider') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect
                v-model:value="form.provider"
                :options="AI_PROVIDER_OPTIONS"
                :placeholder="t('develop.ai_provider.form_provider_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="model">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_model') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.model" clearable :placeholder="t('develop.ai_provider.form_model_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="embeddingModel">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_embedding_model') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.embeddingModel" clearable :placeholder="t('develop.ai_provider.form_embedding_model_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="baseUrl" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_base_url') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.baseUrl" clearable :placeholder="t('develop.ai_provider.form_base_url_placeholder')" autocomplete="off" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="apiKey" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ form.basicId ? t('develop.ai_provider.form_api_key_edit') : t('develop.ai_provider.form_api_key') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.apiKey"
                clearable
                :placeholder="t('develop.ai_provider.form_api_key_placeholder')"
                type="password"
                autocomplete="new-password"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="maxOutputTokens">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_max_output_tokens') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.maxOutputTokens" :min="1" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="temperature">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_temperature') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.temperature" :max="2" :min="0" :precision="2" :step="0.1" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="timeoutSeconds">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_timeout_seconds') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.timeoutSeconds" :min="1" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isDefault">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_is_default') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.isDefault" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isEnabled">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_is_enabled') }}</XhFieldLabel>
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
        <XhFormFieldGroup value="extraJson" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_provider.form_extra_json') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.extraJson"
                clearable
                :placeholder="t('develop.ai_provider.form_extra_json_placeholder')"
                :rows="3"
                type="textarea"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
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
