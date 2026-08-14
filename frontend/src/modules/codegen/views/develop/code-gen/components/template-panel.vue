<script setup lang="ts">
import type {
  CodeGenTemplateCreateDto,
  CodeGenTemplateListItemDto,
  CodeGenTemplateUpdateDto,
  TemplateEngine,
  TemplateType,
} from '../../../../api'
import type {
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NSelect,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  querySortsFromSchema,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XCodeEditor, XContentEditorField, XEditModal } from '~/components'
import { useEnumOptions } from '~/hooks'
import { getOptionLabel } from '~/utils'
import {
  ARTIFACT_WRITE_MODE_OPTIONS,
  ArtifactWriteMode,
  codeGenTemplateApi,
  EnableStatus,
  TEMPLATE_ENGINE_OPTIONS,
  TEMPLATE_TYPE_OPTIONS,
  TemplateEngine as TemplateEngineEnum,
  TemplateType as TemplateTypeEnum,
} from '../../../../api'

defineOptions({ name: 'CodeGenTemplatePanel' })

interface TemplateFormModel {
  basicId?: string
  templateCode: string
  templateName: string
  templateDescription?: string | null
  templateGroup?: string | null
  templateType: TemplateType
  templateEngine: TemplateEngine
  writeMode: ArtifactWriteMode
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

const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
function reload() {
  void schemaPageRef.value?.reload()
}

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('develop.code_gen.template.col_template_name'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('develop.code_gen.template.search_placeholder'), order: 0 },
  { key: 'templateName', title: t('develop.code_gen.template.col_template_name'), dataType: 'string', minWidth: 160, fixed: 'left', sortable: true, order: 1 },
  { key: 'templateCode', title: t('develop.code_gen.template.col_template_code'), dataType: 'string', minWidth: 150, sortable: true, order: 2 },
  { key: 'templateGroup', title: t('develop.code_gen.template.col_template_group'), dataType: 'string', width: 120, sortable: true, order: 3 },
  {
    key: 'templateType',
    title: t('develop.code_gen.template.col_template_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
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
    searchMultiple: true,
    sortable: true,
    options: TEMPLATE_ENGINE_OPTIONS,
    searchPlaceholder: t('develop.code_gen.template.filter_template_engine'),
    width: 90,
    order: 5,
    render: row => getOptionLabel(TEMPLATE_ENGINE_OPTIONS, (row as unknown as CodeGenTemplateListItemDto).templateEngine),
  },
  { key: 'fileExtension', title: t('develop.code_gen.template.col_file_extension'), dataType: 'string', width: 90, sortable: true, order: 6 },
  {
    key: 'isBuiltIn',
    title: t('develop.code_gen.template.col_built_in'),
    dataType: 'boolean',
    width: 90,
    sortable: true,
    order: 7,
    // 内置模板随程序版本回刷、不可编辑删除，单列呈现比挂在名称后更容易扫读
    render: (row) => {
      const r = row as unknown as CodeGenTemplateListItemDto
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: r.isBuiltIn ? 'warning' : 'default' },
        () => (r.isBuiltIn ? t('develop.code_gen.template.built_in') : t('develop.code_gen.template.custom')),
      )
    },
  },
  {
    key: 'isEnabled',
    title: t('develop.code_gen.template.col_enabled'),
    dataType: 'boolean',
    width: 80,
    sortable: true,
    order: 8,
    render: (row) => {
      const r = row as unknown as CodeGenTemplateListItemDto
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
    order: 9,
    render: (row) => {
      const r = row as unknown as CodeGenTemplateListItemDto
      return h(NTag, { size: 'small', round: true, bordered: false, type: r.status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(statusEnumOptions.value, r.status))
    },
  },
  { key: 'createdTime', title: t('common.fields.created_time'), dataType: 'datetime', minWidth: 170, sortable: true, order: 10 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'develop.codegen.template',
  pageName: t('develop.code_gen.tabs.template'),
  rowKey: 'basicId',
  scrollX: 1270,
  batchRemovable: true,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return codeGenTemplateApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(templateType/templateEngine/status) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (f.keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => codeGenTemplateApi.delete(id),
  },
  actions: [
    { key: 'create', title: t('develop.code_gen.template.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    // 内置模板随程序版本回刷，改了也会被覆盖，故与删除一样禁用
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', disabled: row => (row as unknown as CodeGenTemplateListItemDto).isBuiltIn },
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
        if (row.isBuiltIn) {
          message.warning(t('develop.code_gen.template.builtin_cannot_edit'))
          break
        }
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
      catch (error) {
        message.error((error as Error)?.message || t('common.messages.delete_failed'))
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

/** 模板编辑器语言：按目标文件扩展名推断（.sbn 产出 .cs/.ts/.vue，据此高亮字面代码部分） */
/** 编辑器统一收发 string；表单 templateContent 为 string|null，做空值适配 */
const templateContentText = computed<string>({
  get: () => form.value.templateContent ?? '',
  set: (v) => { form.value.templateContent = v || null },
})

const editorFileName = computed(() => {
  const ext = form.value.fileExtension?.trim()
  if (!ext) {
    return null
  }
  return ext.startsWith('.') ? `template${ext}` : `template.${ext}`
})

function createDefaultForm(): TemplateFormModel {
  return {
    templateCode: '',
    templateName: '',
    templateDescription: null,
    templateGroup: null,
    // 默认通用模板：适用于单表/树表/主子表全部类型
    templateType: TemplateTypeEnum.Universal,
    templateEngine: TemplateEngineEnum.Scriban,
    writeMode: ArtifactWriteMode.AlwaysOverwrite,
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
      writeMode: detail.writeMode,
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
  catch (error) {
    message.error((error as Error)?.message || t('develop.code_gen.template.load_detail_failed'))
  }
}

async function handleValidate(templateContent: string) {
  if (!templateContent.trim()) {
    message.warning(t('develop.code_gen.template.validate_content_required'))
    return
  }
  validating.value = true
  try {
    const result = await codeGenTemplateApi.validate({
      templateContent,
      templateEngine: form.value.templateEngine,
    })
    if (result.isValid) {
      message.success(t('develop.code_gen.template.validate_pass'))
    }
    else {
      message.error(t('develop.code_gen.template.validate_fail', { errors: result.errors.join('；') || t('develop.code_gen.template.validate_unknown_error') }))
    }
  }
  catch (error) {
    message.error((error as Error)?.message || t('develop.code_gen.template.validate_error'))
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
        writeMode: form.value.writeMode,
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
        writeMode: form.value.writeMode,
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
      :width="920"
      @save="handleSubmit"
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
        <NFormItem :label="t('develop.code_gen.template.form_write_mode')" path="writeMode">
          <NSelect v-model:value="form.writeMode" :options="ARTIFACT_WRITE_MODE_OPTIONS" />
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
          <NInputNumber v-model:value="form.sort" :min="0" />
        </NFormItem>
        <NFormItem v-if="!form.basicId" :label="t('common.fields.status')" path="status">
          <NSelect v-model:value="form.status" :options="statusEnumOptions" />
        </NFormItem>
        <NFormItem class="xh-span-2" :label="t('develop.code_gen.template.form_template_description')" path="templateDescription">
          <NInput v-model:value="form.templateDescription" clearable :rows="2" type="textarea" />
        </NFormItem>
        <NFormItem class="xh-span-2" :label="t('develop.code_gen.template.form_template_content')" path="templateContent">
          <XContentEditorField
            v-model="templateContentText"
            :title="t('develop.code_gen.template.form_template_content_modal_title')"
            :placeholder="t('develop.code_gen.template.form_template_content_placeholder')"
            :edit-text="t('develop.code_gen.template.form_template_content_edit')"
            :confirm-text="t('common.actions.confirm')"
            :cancel-text="t('common.actions.cancel')"
            :count-label="(count: number) => t('develop.code_gen.template.form_template_content_count', { count })"
          >
            <template #editor="{ value, update }">
              <XCodeEditor
                :value="value"
                :file-name="editorFileName"
                height="100%"
                @update:value="update"
              />
            </template>
            <!-- 校验的是正在编辑的草稿，而非已回写表单的值 -->
            <template #footer-extra="{ value }">
              <NButton size="small" :loading="validating" @click="handleValidate(value)">
                <template #icon>
                  <NIcon><Icon icon="lucide:check-check" /></NIcon>
                </template>
                {{ t('develop.code_gen.template.validate_syntax') }}
              </NButton>
            </template>
          </XContentEditorField>
        </NFormItem>
      </NForm>
    </XEditModal>
  </SchemaPage>
</template>
