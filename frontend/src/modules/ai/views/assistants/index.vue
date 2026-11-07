<!--
  AI / 助手
  接口取自后端契约（ai-assistants.*）：编码建后不可改、缺省最多一个；启停走行内操作（只有状态一个字段）
  工具从后端目录勾选（方案 A3），没有向量库时给出提示（方案 D7b）；提供商与提示词填编码，保存时后端核对
-->
<script setup lang="ts">
import type { AssistantRow, AssistantStatus } from './use-assistants'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, renderTag, SchemaPage, statusConfirmText, XEditModal, XFieldLabel, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { aiAssistants } from '~/contracts'
import { throwOnFailed } from '~/request'
import { assistantsResource, useAssistantEditor } from './use-assistants'

defineOptions({ name: 'AiAssistantsPage' })

const { t } = useI18n()
const editor = useAssistantEditor()
const resource = assistantsResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as AssistantRow

function statusLabel(status: AssistantStatus): string {
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
    title: t('ai.assistants.col_name'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('ai.assistants.search_placeholder'),
    order: 0,
  },
  {
    key: 'name',
    title: t('ai.assistants.col_name'),
    dataType: 'string',
    minWidth: 160,
    fixed: 'left',
    sortable: true,
    order: 1,
  },
  {
    key: 'code',
    title: t('ai.assistants.col_code'),
    dataType: 'string',
    minWidth: 150,
    sortable: true,
    order: 2,
  },
  {
    key: 'providerCode',
    title: t('ai.assistants.col_provider_code'),
    dataType: 'string',
    width: 140,
    order: 3,
  },
  {
    key: 'promptCode',
    title: t('ai.assistants.col_prompt_code'),
    dataType: 'string',
    width: 140,
    order: 4,
  },
  {
    key: 'enableKnowledge',
    title: t('ai.assistants.col_enable_knowledge'),
    dataType: 'boolean',
    width: 120,
    sortable: true,
    order: 5,
    render: row => renderTag(asRow(row).enableKnowledge
      ? t('common.statuses.yes')
      : t('common.statuses.no'), asRow(row).enableKnowledge
      ? 'success'
      : 'neutral'),
  },
  {
    key: 'isDefault',
    title: t('ai.assistants.col_default'),
    dataType: 'boolean',
    width: 100,
    sortable: true,
    order: 6,
    render: row => renderTag(asRow(row).isDefault
      ? t('common.statuses.yes')
      : t('common.statuses.no'), asRow(row).isDefault
      ? 'info'
      : 'neutral'),
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
    width: 100,
    order: 7,
    render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active'
      ? 'success'
      : 'danger'),
  },
  {
    key: 'order',
    title: t('ai.common.order'),
    dataType: 'number',
    width: 100,
    sortable: true,
    order: 8,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'ai.assistants',
  batchRemovable: true,
  removePermission: 'ai.ai-assistants.delete',
  pageName: t('ai.assistants.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'create',
      title: t('ai.assistants.add'),
      scope: 'page',
      tone: 'brand',
      icon: 'lucide:plus',
      permission: 'ai.ai-assistants.create',
    },
    {
      key: 'edit',
      title: t('common.actions.edit'),
      scope: 'row',
      icon: 'lucide:pencil',
      permission: 'ai.ai-assistants.update',
    },
    {
      key: 'default',
      title: t('ai.assistants.action_default'),
      scope: 'row',
      icon: 'lucide:star',
      confirm: true,
      confirmText: row => actionConfirmText(t, t('ai.assistants.action_default'), asRow(row).name),
      disabled: row => asRow(row).isDefault,
      permission: 'ai.ai-assistants.update',
    },
    {
      key: 'toggle',
      title: t('ai.common.toggle_status'),
      scope: 'row',
      icon: 'lucide:power',
      confirm: true,
      confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name),
      permission: 'ai.ai-assistants.update',
    },
    {
      key: 'delete',
      title: t('common.actions.delete'),
      scope: 'row',
      tone: 'danger',
      icon: 'lucide:trash-2',
      confirm: true,
      confirmText: () => t('ai.assistants.confirm_delete'),
      permission: 'ai.ai-assistants.delete',
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
      loadTools()
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
        loadTools()
      }
      break
    case 'default':
      if (row)
        void handleSetDefault(row)
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

function loadTools() {
  editor.loadTools().catch((error: Error) => toast.danger(error.message || t('ai.assistants.load_tools_failed')))
}

async function handleEdit(row: AssistantRow) {
  try {
    await editor.startEdit(row.id)
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.common.load_detail_failed'))
  }
}

async function handleSetDefault(row: AssistantRow) {
  try {
    await aiAssistants.setDefault({ id: row.id })
    toast.success(t('ai.assistants.set_default_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('ai.assistants.set_default_error'))
  }
}

async function handleToggle(row: AssistantRow) {
  try {
    await aiAssistants.setStatus({
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

async function handleDelete(row: AssistantRow) {
  try {
    const result = await aiAssistants.delete({
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
  ? t('ai.assistants.modal_edit_title')
  : t('ai.assistants.modal_add_title'))

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
          <XhFieldLabel>{{ t('ai.assistants.form_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable :disabled="editor.isEdit.value" :placeholder="t('ai.assistants.form_code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('ai.assistants.form_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('providerCode')">
          <XhFieldLabel>{{ t('ai.assistants.form_provider_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.providerCode" name="providerCode" clearable :placeholder="t('ai.assistants.form_provider_code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('providerCode')">
            {{ editor.fieldError('providerCode') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('promptCode')">
          <XhFieldLabel>{{ t('ai.assistants.form_prompt_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.promptCode" name="promptCode" clearable :placeholder="t('ai.assistants.form_prompt_code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('promptCode')">
            {{ editor.fieldError('promptCode') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('ai.assistants.form_enable_knowledge') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.enableKnowledge" />
          </XhFieldControl>
          <!-- 这是当前状态下的警告（开着会失败），不是字段的填法：留在字段下方看得见，不收进问号 -->
          <XhFieldDescription v-if="editor.form.enableKnowledge && !editor.knowledgeAvailable.value">
            {{ t('ai.assistants.form_knowledge_unavailable') }}
          </XhFieldDescription>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('knowledgeProviderCode')">
          <XhFieldLabel>{{ t('ai.assistants.form_knowledge_provider_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.knowledgeProviderCode" name="knowledgeProviderCode" clearable :placeholder="t('ai.assistants.form_provider_code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('knowledgeProviderCode')">
            {{ editor.fieldError('knowledgeProviderCode') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('knowledgeTopK')">
          <XhFieldLabel>{{ t('ai.assistants.form_knowledge_top_k') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.knowledgeTopK" name="knowledgeTopK" :min="1" :max="20" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('knowledgeTopK')">
            {{ editor.fieldError('knowledgeTopK') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('historyRounds')">
          <XhFieldLabel>{{ t('ai.assistants.form_history_rounds') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.historyRounds" name="historyRounds" :min="0" :max="50" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('historyRounds')">
            {{ editor.fieldError('historyRounds') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('avatar')">
          <XhFieldLabel>{{ t('ai.assistants.form_avatar') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.avatar" name="avatar" clearable :placeholder="t('ai.assistants.form_avatar_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('avatar')">
            {{ editor.fieldError('avatar') }}
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
        <XhFieldRoot>
          <XhFieldLabel>{{ t('ai.assistants.form_is_default') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.isDefault" />
          </XhFieldControl>
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
        <XhFieldRoot :invalid="!!editor.fieldError('tools')" class="xh-span-2">
          <XFieldLabel :help="t('ai.assistants.form_tools_hint')">
            {{ t('ai.assistants.form_tools') }}
          </XFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.tools" name="tools" multiple clearable :options="editor.toolOptions.value" :placeholder="t('ai.assistants.form_tools_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('tools')">
            {{ editor.fieldError('tools') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('description')" class="xh-span-2">
          <XhFieldLabel>{{ t('ai.assistants.form_description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" clearable :rows="2" type="textarea" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('description')">
            {{ editor.fieldError('description') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('greeting')" class="xh-span-2">
          <XhFieldLabel>{{ t('ai.assistants.form_greeting') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.greeting" name="greeting" clearable :rows="3" type="textarea" :placeholder="t('ai.assistants.form_greeting_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('greeting')">
            {{ editor.fieldError('greeting') }}
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
