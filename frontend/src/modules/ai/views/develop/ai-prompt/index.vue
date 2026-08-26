<script setup lang="ts">
import type {
  AiPromptCreateDto,
  AiPromptListItemDto,
  AiPromptUpdateDto,
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
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import {
  aiPromptApi,
  EnableStatus,
} from '../../../api'

defineOptions({ name: 'DevelopAiPromptPage' })

interface PromptFormModel {
  basicId?: string
  promptCode: string
  promptName: string
  category?: string | null
  version?: string | null
  content: string
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
  { key: 'keyword', title: t('develop.ai_prompt.col_prompt_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('develop.ai_prompt.search_placeholder'), order: 0 },
  { key: 'promptName', title: t('develop.ai_prompt.col_prompt_name'), dataType: 'string', minWidth: 180, fixed: 'left', sortable: true, order: 1 },
  { key: 'promptCode', title: t('develop.ai_prompt.col_prompt_code'), dataType: 'string', minWidth: 150, sortable: true, order: 2 },
  { key: 'category', title: t('develop.ai_prompt.col_category'), dataType: 'string', width: 120, sortable: true, order: 3 },
  { key: 'version', title: t('develop.ai_prompt.col_version'), dataType: 'string', width: 100, order: 4 },
  {
    key: 'isEnabled',
    title: t('develop.ai_prompt.col_enabled'),
    dataType: 'boolean',
    width: 80,
    sortable: true,
    order: 5,
    render: (row) => {
      const r = row as unknown as AiPromptListItemDto
      return h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: r.isEnabled ? 'success' : 'neutral' }, () => h(XhTagLabel, () => (r.isEnabled ? t('common.statuses.yes') : t('common.statuses.no'))))
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
    order: 6,
    render: (row) => {
      const r = row as unknown as AiPromptListItemDto
      return h(XhTagRoot, { variant: 'subtle', size: 'sm', tone: r.status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => getOptionLabel(statusEnumOptions.value, r.status)))
    },
  },
  { key: 'sort', title: t('common.fields.sort'), dataType: 'number', width: 80, sortable: true, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.ai.prompt',
  pageName: t('develop.ai_prompt.page_name'),
  rowKey: 'basicId',
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return aiPromptApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => aiPromptApi.delete(id),
  },
  actions: [
    { key: 'create', title: t('develop.ai_prompt.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as AiPromptListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
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

function handleDelete(row: AiPromptListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('common.actions.delete'),
    content: t('develop.ai_prompt.confirm_delete'),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await aiPromptApi.delete(row.basicId)
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
const form = ref<PromptFormModel>(createDefaultForm())
const modalTitle = computed(() => (form.value.basicId ? t('develop.ai_prompt.modal_edit_title') : t('develop.ai_prompt.modal_add_title')))

function createDefaultForm(): PromptFormModel {
  return {
    promptCode: '',
    promptName: '',
    category: null,
    version: null,
    content: '',
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

async function handleEdit(row: AiPromptListItemDto) {
  try {
    const detail = await aiPromptApi.detail(row.basicId)
    if (!detail) {
      toast.error(t('develop.ai_prompt.not_found'))
      return
    }
    editingStatus.value = detail.status
    form.value = {
      basicId: detail.basicId,
      promptCode: detail.promptCode,
      promptName: detail.promptName,
      category: detail.category ?? null,
      version: detail.version ?? null,
      content: detail.content ?? '',
      isEnabled: detail.isEnabled,
      sort: detail.sort,
      status: detail.status,
      remark: detail.remark ?? null,
    }
    modalVisible.value = true
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.ai_prompt.load_detail_failed'))
  }
}

function validateForm() {
  if (!form.value.promptName.trim()) {
    toast.warning(t('develop.ai_prompt.validate_name'))
    return false
  }
  if (!form.value.basicId && !form.value.promptCode.trim()) {
    toast.warning(t('develop.ai_prompt.validate_code'))
    return false
  }
  if (!form.value.content.trim()) {
    toast.warning(t('develop.ai_prompt.validate_content'))
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
      const updateInput: AiPromptUpdateDto = {
        basicId: form.value.basicId,
        promptName: form.value.promptName.trim(),
        category: form.value.category?.trim() || null,
        version: form.value.version?.trim() || null,
        content: form.value.content,
        isEnabled: form.value.isEnabled,
        sort: form.value.sort,
        remark: form.value.remark,
      }
      await aiPromptApi.update(updateInput)
      if (editingStatus.value !== form.value.status) {
        await aiPromptApi.updateStatus({
          basicId: form.value.basicId,
          remark: t('develop.ai_prompt.update_status_remark'),
          status: form.value.status,
        })
      }
    }
    else {
      const createInput: AiPromptCreateDto = {
        promptCode: form.value.promptCode.trim(),
        promptName: form.value.promptName.trim(),
        category: form.value.category?.trim() || null,
        version: form.value.version?.trim() || null,
        content: form.value.content,
        isEnabled: form.value.isEnabled,
        sort: form.value.sort,
        status: form.value.status,
        remark: form.value.remark,
      }
      await aiPromptApi.create(createInput)
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
        <XhFormFieldGroup value="promptCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_prompt.form_prompt_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.promptCode"
                clearable
                :disabled="Boolean(form.basicId)"
                :placeholder="t('develop.ai_prompt.form_prompt_code_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="promptName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_prompt.form_prompt_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.promptName" clearable />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="category">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_prompt.form_category') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.category" clearable :placeholder="t('develop.ai_prompt.form_category_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="version">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_prompt.form_version') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.version" clearable :placeholder="t('develop.ai_prompt.form_version_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_prompt.form_sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="isEnabled">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_prompt.form_is_enabled') }}</XhFieldLabel>
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
        <XhFormFieldGroup value="content" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('develop.ai_prompt.form_content') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="form.content"
                class="prompt-content-input"
                :placeholder="t('develop.ai_prompt.form_content_placeholder')"
                :rows="12"
                type="textarea"
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

<style scoped>
.prompt-content-input :deep(textarea) {
  font-family: var(--font-mono, ui-monospace, 'SFMono-Regular', 'Consolas', monospace);
  font-size: 12px;
  line-height: 1.6;
}
</style>
