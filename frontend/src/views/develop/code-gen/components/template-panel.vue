<script setup lang="ts">
import type {
  CodeGenTemplateCreateDto,
  CodeGenTemplateListItemDto,
  CodeGenTemplateUpdateDto,
  TemplateEngine,
  TemplateType,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { PageResult } from '~/types/contracts'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenTemplateApi,
  createPageRequest,
  EnableStatus,
  TEMPLATE_ENGINE_OPTIONS,
  TEMPLATE_TYPE_OPTIONS,
  TemplateEngine as TemplateEngineEnum,
  TemplateType as TemplateTypeEnum,
} from '@/api'
import { Icon, SchemaPage } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'CodeGenTemplatePanel' })

interface TemplateFormModel {
  basicId?: string
  templateCode: string
  templateName: string
  templateDescription?: string | null
  templateGroup?: string | null
  templateType: TemplateType
  templateEngine: TemplateEngine
  templateContent?: string | null
  fileNameExpression?: string | null
  filePathExpression?: string | null
  fileExtension?: string | null
  sort: number
  isEnabled: boolean
  status: EnableStatus
  remark?: string | null
}

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('develop.code_gen.template.col_template_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('develop.code_gen.template.search_placeholder'), order: 0 },
  {
    key: 'templateName',
    title: t('develop.code_gen.template.col_template_name'),
    dataType: 'string',
    minWidth: 160,
    fixed: 'left',
    order: 1,
    render: (row) => {
      const r = row as unknown as CodeGenTemplateListItemDto
      return h('div', { class: 'tpl-name' }, [
        h('span', { class: 'tpl-name__text' }, r.templateName),
        r.isBuiltIn
          ? h(NTag, { size: 'tiny', type: 'warning', round: true, bordered: false }, () => t('common.statuses.builtin_tag'))
          : null,
      ])
    },
  },
  { key: 'templateCode', title: t('develop.code_gen.template.col_template_code'), dataType: 'string', minWidth: 150, order: 2 },
  { key: 'templateGroup', title: t('develop.code_gen.template.col_template_group'), dataType: 'string', width: 120, order: 3 },
  {
    key: 'templateType',
    title: t('develop.code_gen.template.col_template_type'),
    dataType: 'enum',
    searchable: true,
    options: TEMPLATE_TYPE_OPTIONS,
    searchPlaceholder: t('develop.code_gen.template.filter_template_type'),
    width: 90,
    order: 4,
    render: row => getOptionLabel(TEMPLATE_TYPE_OPTIONS, (row as unknown as CodeGenTemplateListItemDto).templateType),
  },
  {
    key: 'templateEngine',
    title: t('develop.code_gen.template.col_template_engine'),
    dataType: 'enum',
    searchable: true,
    options: TEMPLATE_ENGINE_OPTIONS,
    searchPlaceholder: t('develop.code_gen.template.filter_template_engine'),
    width: 90,
    order: 5,
    render: row => getOptionLabel(TEMPLATE_ENGINE_OPTIONS, (row as unknown as CodeGenTemplateListItemDto).templateEngine),
  },
  { key: 'fileExtension', title: t('develop.code_gen.template.col_file_extension'), dataType: 'string', width: 90, order: 6 },
  {
    key: 'isEnabled',
    title: t('develop.code_gen.template.col_enabled'),
    dataType: 'boolean',
    width: 80,
    order: 7,
    render: (row) => {
      const r = row as unknown as CodeGenTemplateListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.isEnabled ? 'success' : 'default' }, () => (r.isEnabled ? t('common.statuses.yes') : t('common.statuses.no')))
    },
  },
  {
    key: 'status',
    title: t('common.fields.status'),
    dataType: 'enum',
    searchable: true,
    options: STATUS_OPTIONS,
    searchPlaceholder: t('common.fields.status'),
    width: 90,
    order: 8,
    render: (row) => {
      const r = row as unknown as CodeGenTemplateListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(STATUS_OPTIONS, r.status))
    },
  },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', minWidth: 170, order: 9 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.codegen.template',
  pageName: t('develop.code_gen.tabs.template'),
  rowKey: 'basicId',
  scrollX: 1180,
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return codeGenTemplateApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
        templateType: (f.templateType as TemplateType | undefined) ?? undefined,
        templateEngine: (f.templateEngine as TemplateEngine | undefined) ?? undefined,
        status: (f.status as EnableStatus | undefined) ?? undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => codeGenTemplateApi.delete(id),
  },
  actions: [
    { key: 'create', title: t('develop.code_gen.template.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', disabled: row => (row as unknown as CodeGenTemplateListItemDto).isBuiltIn },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as CodeGenTemplateListItemDto | undefined
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

function handleDelete(row: CodeGenTemplateListItemDto) {
  if (row.isBuiltIn) {
    message.warning(t('develop.code_gen.template.builtin_cannot_delete'))
    return
  }
  dialog.warning({
    title: t('common.actions.delete'),
    content: t('develop.code_gen.template.confirm_delete'),
    positiveText: t('common.actions.confirm'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await codeGenTemplateApi.delete(row.basicId)
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
const validating = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const form = ref<TemplateFormModel>(createDefaultForm())
const modalTitle = computed(() => (form.value.basicId ? t('develop.code_gen.template.modal_edit_title') : t('develop.code_gen.template.modal_add_title')))

function createDefaultForm(): TemplateFormModel {
  return {
    templateCode: '',
    templateName: '',
    templateDescription: null,
    templateGroup: null,
    templateType: TemplateTypeEnum.Single,
    templateEngine: TemplateEngineEnum.Scriban,
    templateContent: null,
    fileNameExpression: null,
    filePathExpression: null,
    fileExtension: null,
    sort: 100,
    isEnabled: true,
    status: EnableStatus.Enabled,
    remark: null,
  }
}

function handleAdd() {
  editingStatus.value = null
  form.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: CodeGenTemplateListItemDto) {
  try {
    const detail = await codeGenTemplateApi.detail(row.basicId)
    if (!detail) {
      message.error(t('develop.code_gen.template.not_found'))
      return
    }
    editingStatus.value = detail.status
    form.value = {
      basicId: detail.basicId,
      templateCode: detail.templateCode,
      templateName: detail.templateName,
      templateDescription: detail.templateDescription ?? null,
      templateGroup: detail.templateGroup ?? null,
      templateType: detail.templateType,
      templateEngine: detail.templateEngine,
      templateContent: detail.templateContent ?? null,
      fileNameExpression: detail.fileNameExpression ?? null,
      filePathExpression: detail.filePathExpression ?? null,
      fileExtension: detail.fileExtension ?? null,
      sort: detail.sort,
      isEnabled: detail.isEnabled,
      status: detail.status,
      remark: detail.remark ?? null,
    }
    modalVisible.value = true
  }
  catch {
    message.error(t('develop.code_gen.template.load_detail_failed'))
  }
}

async function handleValidate() {
  if (!form.value.templateContent?.trim()) {
    message.warning(t('develop.code_gen.template.validate_content_required'))
    return
  }
  validating.value = true
  try {
    const result = await codeGenTemplateApi.validate({
      templateContent: form.value.templateContent,
      templateEngine: form.value.templateEngine,
    })
    if (result.isValid) {
      message.success(t('develop.code_gen.template.validate_pass'))
    }
    else {
      message.error(t('develop.code_gen.template.validate_fail', { errors: result.errors.join('；') || t('develop.code_gen.template.validate_unknown_error') }))
    }
  }
  catch {
    message.error(t('develop.code_gen.template.validate_error'))
  }
  finally {
    validating.value = false
  }
}

function validateForm() {
  if (!form.value.templateName.trim()) {
    message.warning(t('develop.code_gen.template.validate_name_required'))
    return false
  }
  if (!form.value.basicId && !form.value.templateCode.trim()) {
    message.warning(t('develop.code_gen.template.validate_code_required'))
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
      const updateInput: CodeGenTemplateUpdateDto = {
        basicId: form.value.basicId,
        templateName: form.value.templateName.trim(),
        templateDescription: form.value.templateDescription,
        templateGroup: form.value.templateGroup,
        templateType: form.value.templateType,
        templateEngine: form.value.templateEngine,
        templateContent: form.value.templateContent,
        fileNameExpression: form.value.fileNameExpression,
        filePathExpression: form.value.filePathExpression,
        fileExtension: form.value.fileExtension,
        isEnabled: form.value.isEnabled,
        sort: form.value.sort,
        remark: form.value.remark,
      }
      await codeGenTemplateApi.update(updateInput)
      if (editingStatus.value !== form.value.status) {
        await codeGenTemplateApi.updateStatus({
          basicId: form.value.basicId,
          remark: t('develop.code_gen.template.update_status_remark'),
          status: form.value.status,
        })
      }
    }
    else {
      const createInput: CodeGenTemplateCreateDto = {
        templateCode: form.value.templateCode.trim(),
        templateName: form.value.templateName.trim(),
        templateDescription: form.value.templateDescription,
        templateGroup: form.value.templateGroup,
        templateType: form.value.templateType,
        templateEngine: form.value.templateEngine,
        templateContent: form.value.templateContent,
        fileNameExpression: form.value.fileNameExpression,
        filePathExpression: form.value.filePathExpression,
        fileExtension: form.value.fileExtension,
        sort: form.value.sort,
        status: form.value.status,
        remark: form.value.remark,
      }
      await codeGenTemplateApi.create(createInput)
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
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 820px; max-width: 94vw"
    >
      <NForm :model="form" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('develop.code_gen.template.form_template_code')" path="templateCode">
          <NInput
            v-model:value="form.templateCode"
            clearable
            :disabled="Boolean(form.basicId)"
            :placeholder="t('develop.code_gen.template.form_template_code_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_template_name')" path="templateName">
          <NInput v-model:value="form.templateName" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_template_group')" path="templateGroup">
          <NInput v-model:value="form.templateGroup" clearable :placeholder="t('develop.code_gen.template.form_template_group_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_template_type')" path="templateType">
          <NSelect v-model:value="form.templateType" :options="TEMPLATE_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_template_engine')" path="templateEngine">
          <NSelect v-model:value="form.templateEngine" :options="TEMPLATE_ENGINE_OPTIONS" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_file_extension')" path="fileExtension">
          <NInput v-model:value="form.fileExtension" clearable :placeholder="t('develop.code_gen.template.form_file_extension_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_file_name_expression')" path="fileNameExpression">
          <NInput v-model:value="form.fileNameExpression" clearable :placeholder="t('develop.code_gen.template.form_file_name_expression_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_file_path_expression')" path="filePathExpression">
          <NInput v-model:value="form.filePathExpression" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.template.form_sort')" path="sort">
          <NInputNumber v-model:value="form.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!form.basicId" :label="t('common.fields.status')" path="status">
          <NSelect v-model:value="form.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem class="xh-form-full" :label="t('develop.code_gen.template.form_template_description')" path="templateDescription">
          <NInput v-model:value="form.templateDescription" clearable :rows="2" type="textarea" />
        </NFormItem>
        <NFormItem class="xh-form-full" :label="t('develop.code_gen.template.form_template_content')" path="templateContent">
          <NInput
            v-model:value="form.templateContent"
            :placeholder="t('develop.code_gen.template.form_template_content_placeholder')"
            :rows="12"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="space-between">
          <NButton :loading="validating" @click="handleValidate">
            <template #icon>
              <NIcon><Icon icon="lucide:check-check" /></NIcon>
            </template>
            {{ t('develop.code_gen.template.validate_syntax') }}
          </NButton>
          <NSpace>
            <NButton @click="modalVisible = false">
              {{ t('common.actions.cancel') }}
            </NButton>
            <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
              {{ t('common.actions.save') }}
            </NButton>
          </NSpace>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.tpl-name {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.tpl-name__text {
  font-weight: 500;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
