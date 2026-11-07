<!--
  AI / 提示词（照 4.x develop/ai-prompt）
  接口取自后端契约（ai-prompts.*）：列表不带正文，编辑先取详情；启停走行内操作（4.x 只在新建时能选状态，方案 N4 D4）
-->
<script setup lang="ts">
import type { PromptRow, PromptStatus } from './use-prompts'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, renderTag, SchemaPage, statusConfirmText, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { aiPrompts } from '~/contracts'
import { throwOnFailed } from '~/request'
import { promptsResource, usePromptEditor } from './use-prompts'

defineOptions({ name: 'AiPromptsPage' })

const { t } = useI18n()
const editor = usePromptEditor()
const resource = promptsResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as PromptRow

function statusLabel(status: PromptStatus): string {
  return status === 'Active'
    ? t('common.statuses.enabled')
    : t('common.statuses.disabled')
}

const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({
  label: statusLabel(status),
  value: status,
})))

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  {
    key: 'keyword',
    title: t('ai.prompts.col_name'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('ai.prompts.search_placeholder'),
    order: 0,
  },
  {
    key: 'name',
    title: t('ai.prompts.col_name'),
    dataType: 'string',
    minWidth: 180,
    fixed: 'left',
    sortable: true,
    order: 1,
  },
  {
    key: 'code',
    title: t('ai.prompts.col_code'),
    dataType: 'string',
    minWidth: 150,
    sortable: true,
    order: 2,
  },
  {
    key: 'category',
    title: t('ai.prompts.col_category'),
    dataType: 'string',
    width: 120,
    sortable: true,
    order: 3,
  },
  {
    key: 'version',
    title: t('ai.prompts.col_version'),
    dataType: 'string',
    width: 100,
    order: 4,
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
    order: 5,
    render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active'
      ? 'success'
      : 'danger'),
  },
  {
    key: 'order',
    title: t('ai.common.order'),
    dataType: 'number',
    width: 80,
    sortable: true,
    order: 6,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'ai.prompts',
  batchRemovable: true,
  removePermission: 'ai.ai-prompts.delete',
  pageName: t('ai.prompts.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'create',
      title: t('ai.prompts.add'),
      scope: 'page',
      tone: 'brand',
      icon: 'lucide:plus',
      permission: 'ai.ai-prompts.create',
    },
    {
      key: 'edit',
      title: t('common.actions.edit'),
      scope: 'row',
      icon: 'lucide:pencil',
      permission: 'ai.ai-prompts.update',
    },
    {
      key: 'toggle',
      title: t('ai.common.toggle_status'),
      scope: 'row',
      icon: 'lucide:power',
      confirm: true,
      confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name),
      permission: 'ai.ai-prompts.update',
    },
    {
      key: 'delete',
      title: t('common.actions.delete'),
      scope: 'row',
      tone: 'danger',
      icon: 'lucide:trash-2',
      confirm: true,
      confirmText: row => deleteConfirmText(t, asRow(row).name),
      permission: 'ai.ai-prompts.delete',
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
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

async function handleEdit(row: PromptRow) {
  try {
    await editor.startEdit(row.id)
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.common.load_detail_failed'))
  }
}

async function handleToggle(row: PromptRow) {
  try {
    await aiPrompts.setStatus({
      id: row.id,
      concurrencyStamp: row.concurrencyStamp,
      status: row.status === 'Active'
        ? 'Disabled'
        : 'Active',
    })
    toast.success(t('ai.common.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.common.status_update_failed'))
  }
}

async function handleDelete(row: PromptRow) {
  try {
    const result = await aiPrompts.delete({
      ids: [row.id],
    })
    throwOnFailed(result)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

const modalTitle = computed(() => editor.isEdit.value
  ? t('ai.prompts.modal_edit_title')
  : t('ai.prompts.modal_add_title'))

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
          <XhFieldLabel>{{ t('ai.prompts.form_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable :disabled="editor.isEdit.value" :placeholder="t('ai.prompts.form_code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('ai.prompts.form_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('category')">
          <XhFieldLabel>{{ t('ai.prompts.form_category') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.category" name="category" clearable :placeholder="t('ai.prompts.form_category_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('category')">
            {{ editor.fieldError('category') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('version')">
          <XhFieldLabel>{{ t('ai.prompts.form_version') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.version" name="version" clearable :placeholder="t('ai.prompts.form_version_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('version')">
            {{ editor.fieldError('version') }}
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
        <XhFieldRoot v-if="!editor.isEdit.value" :invalid="!!editor.fieldError('status')">
          <XhFieldLabel>{{ t('ai.common.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('status')">
            {{ editor.fieldError('status') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('content')" required class="xh-span-2">
          <XhFieldLabel>{{ t('ai.prompts.form_content') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.content" name="content" class="prompt-content" :rows="12" type="textarea" :placeholder="t('ai.prompts.form_content_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('content')">
            {{ editor.fieldError('content') }}
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
/* 正文等宽（照 4.x 的 .prompt-content-input），字号随令牌 */
.prompt-content :deep(textarea) {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-sm);
}
</style>
