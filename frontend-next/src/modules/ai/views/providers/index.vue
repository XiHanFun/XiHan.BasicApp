<!--
  AI / 提供商（照 4.x develop/ai-provider）
  接口取自后端契约（ai-providers.*）：测试连接要执行权限；API 密钥不回显、编辑时留空即保留；启停走行内操作（4.x 只在新建时能选状态，方案 N4 D4）
-->
<script setup lang="ts">
import type { ProviderRow, ProviderStatus, ProviderTestResult } from './use-providers'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, deleteConfirmText, renderTag, SchemaPage, statusConfirmText, XCombobox, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { notification, toast } from '~/composables'
import { aiProviders } from '~/contracts'
import { probeLines, providersResource, useProviderEditor, vendorOptions } from './use-providers'

defineOptions({ name: 'AiProvidersPage' })

const { t } = useI18n()
const editor = useProviderEditor()
const resource = providersResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)
const testingId = ref<null | string>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as ProviderRow

function statusLabel(status: ProviderStatus): string {
  return status === 'Active' ? t('common.statuses.enabled') : t('common.statuses.disabled')
}

const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({ label: statusLabel(status), value: status })))
const vendors = computed(() => vendorOptions(t))

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('ai.providers.col_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('ai.providers.search_placeholder'), order: 0 },
  {
    key: 'name',
    title: t('ai.providers.col_name'),
    dataType: 'string',
    minWidth: 160,
    fixed: 'left',
    sortable: true,
    order: 1,
    render: (row) => {
      const r = asRow(row)
      return h('div', { class: 'ap-name' }, [
        h('span', { class: 'ap-name__text' }, r.name),
        r.isDefault
          ? h(XhTagRoot, { variant: 'subtle', tone: 'info' }, () => h(XhTagLabel, () => t('ai.common.default_tag')))
          : null,
      ])
    },
  },
  { key: 'code', title: t('ai.providers.col_code'), dataType: 'string', minWidth: 140, sortable: true, order: 2 },
  { key: 'vendor', title: t('ai.providers.col_vendor'), dataType: 'string', width: 120, sortable: true, order: 3 },
  { key: 'chatModel', title: t('ai.providers.col_chat_model'), dataType: 'string', minWidth: 150, sortable: true, order: 4 },
  {
    key: 'hasApiKey',
    title: t('ai.providers.col_api_key'),
    dataType: 'enum',
    width: 100,
    order: 5,
    render: row => asRow(row).hasApiKey
      ? renderTag(t('ai.providers.tag_configured'), 'success')
      : renderTag(t('ai.providers.tag_unconfigured'), 'warning'),
  },
  {
    key: 'status',
    title: t('ai.common.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('ai.common.status'),
    width: 90,
    order: 6,
    render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active' ? 'success' : 'danger'),
  },
  { key: 'order', title: t('ai.common.order'), dataType: 'number', width: 80, sortable: true, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'ai.providers',
  pageName: t('ai.providers.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('ai.providers.add'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'ai.ai-providers.create' },
    { key: 'test', title: t('ai.providers.action_test'), scope: 'row', tone: 'info', icon: 'lucide:plug', permission: 'ai.ai-providers.execute', disabled: row => testingId.value === asRow(row).id },
    { key: 'default', title: t('ai.providers.action_default'), scope: 'row', icon: 'lucide:star', confirm: true, confirmText: row => actionConfirmText(t, t('ai.providers.action_default'), asRow(row).name), disabled: row => asRow(row).isDefault || asRow(row).status !== 'Active', permission: 'ai.ai-providers.update' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'ai.ai-providers.update' },
    { key: 'toggle', title: t('ai.common.toggle_status'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name), permission: 'ai.ai-providers.update' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), permission: 'ai.ai-providers.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'test':
      if (row)
        void handleTest(row)
      break
    case 'default':
      if (row)
        void handleSetDefault(row)
      break
    case 'edit':
      if (row)
        void handleEdit(row)
      break
    case 'toggle':
      if (row)
        void handleToggle(row)
      break
    case 'delete':
      if (row)
        void handleDelete(row)
      break
  }
}

/** 测试连接的通知正文：每个探测一行（照 4.x） */
function probeText(result: ProviderTestResult): string {
  const { lines, embeddingAbsent } = probeLines(result)
  const text = lines.map((line) => {
    const name = line.kind === 'chat' ? t('ai.providers.probe_chat') : t('ai.providers.probe_embedding')
    if (!line.success)
      return `${name} ${t('ai.providers.probe_failed', { message: line.message ?? '' })}`
    const dimensions = line.dimensions === null ? '' : t('ai.providers.probe_dimensions', { n: line.dimensions })
    return `${name} ${t('ai.providers.probe_ok', { ms: line.elapsedMilliseconds })}${dimensions}`
  })
  if (embeddingAbsent)
    text.push(t('ai.providers.probe_embedding_absent'))
  return text.join('\n')
}

async function handleTest(row: ProviderRow) {
  testingId.value = row.id
  const loading = toast.loading(t('ai.providers.testing'))
  try {
    const result = await aiProviders.test({ id: row.id })
    loading.destroy()
    // 成功 5 秒后自动关闭，失败常驻待看完（照 4.x）
    if (result.success)
      notification.success(t('ai.providers.test_success'), { description: probeText(result), duration: 5000 })
    else
      notification.danger(t('ai.providers.test_failed'), { description: probeText(result), duration: 0 })
  }
  catch (error) {
    loading.destroy()
    toast.danger((error as Error).message || t('ai.providers.test_error'))
  }
  finally {
    testingId.value = null
  }
}

async function handleSetDefault(row: ProviderRow) {
  try {
    await aiProviders.setDefault({ id: row.id })
    toast.success(t('ai.providers.set_default_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.providers.set_default_error'))
  }
}

async function handleEdit(row: ProviderRow) {
  try {
    await editor.startEdit(row.id)
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.common.load_detail_failed'))
  }
}

async function handleToggle(row: ProviderRow) {
  try {
    await aiProviders.setStatus({ id: row.id, concurrencyStamp: row.concurrencyStamp, status: row.status === 'Active' ? 'Disabled' : 'Active' })
    toast.success(t('ai.common.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.common.status_update_failed'))
  }
}

async function handleDelete(row: ProviderRow) {
  try {
    const result = await aiProviders.delete({ ids: [row.id] })
    if (result.failed.length > 0)
      throw new Error(result.failed.map(f => f.code).join('; '))
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

const modalTitle = computed(() => editor.isEdit.value ? t('ai.providers.modal_edit_title') : t('ai.providers.modal_add_title'))

async function onSubmit() {
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
  else if (editor.submitError.value) {
    toast.danger(editor.submitError.value)
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal
      v-model:show="editor.open.value"
      :title="modalTitle"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" novalidate @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('code')" required>
          <XhFieldLabel>{{ t('ai.providers.form_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable :disabled="editor.isEdit.value" :placeholder="t('ai.providers.form_code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('ai.providers.form_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('vendor')" required>
          <XhFieldLabel>{{ t('ai.providers.form_vendor') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox v-model:value="editor.form.vendor" name="vendor" allow-custom-value :options="vendors" :placeholder="t('ai.providers.form_vendor_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('vendor')">
            {{ editor.fieldError('vendor') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('chatModel')" required>
          <XhFieldLabel>{{ t('ai.providers.form_chat_model') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.chatModel" name="chatModel" clearable :placeholder="t('ai.providers.form_chat_model_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('chatModel')">
            {{ editor.fieldError('chatModel') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('embeddingModel')">
          <XhFieldLabel>{{ t('ai.providers.form_embedding_model') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.embeddingModel" name="embeddingModel" clearable :placeholder="t('ai.providers.form_embedding_model_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('embeddingModel')">
            {{ editor.fieldError('embeddingModel') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('ai.common.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('endpoint')" class="xh-span-2">
          <XhFieldLabel>{{ t('ai.providers.form_endpoint') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.endpoint" name="endpoint" clearable :placeholder="t('ai.providers.form_endpoint_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('endpoint')">
            {{ editor.fieldError('endpoint') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('apiKey')" class="xh-span-2">
          <XhFieldLabel>{{ editor.isEdit.value ? t('ai.providers.form_api_key_edit') : t('ai.providers.form_api_key') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.apiKey" name="apiKey" clearable type="password" autocomplete="new-password" :placeholder="t('ai.providers.form_api_key_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('apiKey')">
            {{ editor.fieldError('apiKey') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('maxOutputTokens')">
          <XhFieldLabel>{{ t('ai.providers.form_max_output_tokens') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.maxOutputTokens" name="maxOutputTokens" :min="1" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('maxOutputTokens')">
            {{ editor.fieldError('maxOutputTokens') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('temperature')">
          <XhFieldLabel>{{ t('ai.providers.form_temperature') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.temperature" name="temperature" :min="0" :max="2" :step="0.1" :precision="2" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('temperature')">
            {{ editor.fieldError('temperature') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('timeoutSeconds')">
          <XhFieldLabel>{{ t('ai.providers.form_timeout_seconds') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.timeoutSeconds" name="timeoutSeconds" :min="1" :max="3600" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('timeoutSeconds')">
            {{ editor.fieldError('timeoutSeconds') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="!editor.isEdit.value" :invalid="!!editor.fieldError('status')">
          <XhFieldLabel>{{ t('ai.common.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('status')">
            {{ editor.fieldError('status') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('isDefault')">
          <XhFieldLabel>{{ t('ai.providers.form_is_default') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.isDefault" name="isDefault" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('isDefault')">
            {{ editor.fieldError('isDefault') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('remark')" class="xh-span-2">
          <XhFieldLabel>{{ t('ai.common.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.remark" name="remark" clearable :rows="2" type="textarea" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('remark')">
            {{ editor.fieldError('remark') }}
          </XhFieldErrorText>
        </XhFieldRoot>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
:deep(.ap-name) {
  display: flex;
  align-items: center;
  gap: var(--xh-space-1);
  min-width: 0;
}

:deep(.ap-name__text) {
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
