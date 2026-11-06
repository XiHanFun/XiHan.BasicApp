<!--
  代码生成 / 模板页签（照 4.x template-panel）
  接口取自后端契约（codegen-templates.*）：内置模板随程序版本走，不能编辑、不能删除、可以启停；自有模板可增删改；正文在大编辑器里写，可先检查语法
  与 4.x 不同：「是否启用」与状态合成一个启停（编辑时一起提交）；引擎只有 Scriban（去掉选了即报错的 T4）
-->
<script setup lang="ts">
import type { TemplateEngine, TemplateRow, WriteMode } from './use-templates'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot } from '@xihan-ui/vue'
import { computed, reactive, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, XCodeEditor, XContentEditorField, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { codegenTemplates } from '~/contracts'
import { ApiError, throwOnFailed } from '~/request'
import { enumKey, TEMPLATE_TYPES } from './use-tables'
import { TEMPLATE_ENGINES, templatesResource, WRITE_MODES } from './use-templates'

defineOptions({ name: 'CodegenTemplatePanel' })

type TemplateType = TemplateRow['templateType']
type TemplateStatus = TemplateRow['status']

interface TemplateForm {
  id: null | string
  concurrencyStamp: string
  templateCode: string
  templateName: string
  templateDescription: string
  templateGroup: string
  templateType: TemplateType
  templateEngine: TemplateEngine
  writeMode: WriteMode
  templateContent: string
  fileNameExpression: string
  filePathExpression: string
  fileExtension: string
  order: null | number
  status: TemplateStatus
  remark: string
}

const { t } = useI18n()
const resource = templatesResource()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()

function reload() {
  void schemaPageRef.value?.reload()
}

const asRow = (row: object) => row as TemplateRow

function options<T extends string>(group: string, values: readonly T[]): SelectOption[] {
  return values.map(value => ({
    label: t(enumKey(group, value)),
    value,
  }))
}

// 模板的生成模式含「通用」（适用于全部模式），表配置那边不含
const templateTypeOptions = computed(() => options('template_type', [...TEMPLATE_TYPES, 'Universal'] as TemplateType[]))
const writeModeOptions = computed(() => options('write_mode', WRITE_MODES))
const engineOptions = computed(() => options('template_engine', TEMPLATE_ENGINES))
const statusOptions = computed<SelectOption[]>(() => [
  {
    label: t('common.statuses.enabled'),
    value: 'Active',
  },
  {
    label: t('common.statuses.disabled'),
    value: 'Disabled',
  },
])

const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  {
    key: 'keyword',
    title: t('codegeneration.code_gen.template.col_template_name'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('codegeneration.code_gen.template.search_placeholder'),
    order: 0,
  },
  {
    key: 'templateName',
    title: t('codegeneration.code_gen.template.col_template_name'),
    dataType: 'string',
    minWidth: 160,
    fixed: 'left',
    sortable: true,
    order: 1,
  },
  {
    key: 'templateCode',
    title: t('codegeneration.code_gen.template.col_template_code'),
    dataType: 'string',
    minWidth: 190,
    sortable: true,
    order: 2,
  },
  {
    key: 'templateGroup',
    title: t('codegeneration.code_gen.template.col_template_group'),
    dataType: 'string',
    width: 140,
    sortable: true,
    order: 3,
  },
  {
    key: 'templateType',
    title: t('codegeneration.code_gen.template.col_template_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: templateTypeOptions.value,
    searchPlaceholder: t('codegeneration.code_gen.template.filter_template_type'),
    width: 100,
    order: 4,
    render: row => t(enumKey('template_type', asRow(row).templateType)),
  },
  {
    key: 'writeMode',
    title: t('codegeneration.code_gen.template.form_write_mode'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: writeModeOptions.value,
    searchPlaceholder: t('codegeneration.code_gen.template.form_write_mode'),
    width: 110,
    order: 5,
    render: row => t(enumKey('write_mode', asRow(row).writeMode)),
  },
  {
    key: 'templateEngine',
    title: t('codegeneration.code_gen.template.col_template_engine'),
    dataType: 'enum',
    options: engineOptions.value,
    width: 90,
    order: 6,
    render: row => t(enumKey('template_engine', asRow(row).templateEngine)),
  },
  {
    key: 'fileExtension',
    title: t('codegeneration.code_gen.template.col_file_extension'),
    dataType: 'string',
    width: 90,
    order: 6,
  },
  {
    key: 'isBuiltIn',
    title: t('codegeneration.code_gen.template.col_built_in'),
    dataType: 'boolean',
    width: 90,
    order: 7,
    // 内置模板随程序版本对齐、不能编辑删除，单列呈现比挂在名称后更容易扫读（照 4.x）
    render: row => renderTag(asRow(row).isBuiltIn
      ? t('codegeneration.code_gen.template.built_in')
      : t('codegeneration.code_gen.template.custom'), asRow(row).isBuiltIn
      ? 'warning'
      : 'neutral'),
  },
  {
    key: 'status',
    title: t('codegeneration.code_gen.common.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('codegeneration.code_gen.common.status'),
    width: 90,
    order: 8,
    render: row => renderTag(asRow(row).status === 'Active'
      ? t('common.statuses.enabled')
      : t('common.statuses.disabled'), asRow(row).status === 'Active'
      ? 'success'
      : 'danger'),
  },
  {
    key: 'createdAt',
    title: t('codegeneration.code_gen.common.created_time'),
    dataType: 'datetime',
    minWidth: 170,
    sortable: true,
    order: 9,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'codegeneration.code-gen.template',
  pageName: t('codegeneration.code_gen.tabs.template'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'create',
      title: t('codegeneration.code_gen.template.add'),
      scope: 'page',
      tone: 'brand',
      icon: 'lucide:plus',
      permission: 'codegeneration.codegen-templates.create',
    },
    // 内置模板启动时会按程序带的文件对齐，改了也会被覆盖，故与删除一样禁用（照 4.x）
    {
      key: 'edit',
      title: t('common.actions.edit'),
      scope: 'row',
      icon: 'lucide:pencil',
      disabled: row => asRow(row).isBuiltIn,
      permission: 'codegeneration.codegen-templates.update',
    },
    {
      key: 'toggle',
      title: t('codegeneration.code_gen.template.action_toggle'),
      scope: 'row',
      icon: 'lucide:power',
      permission: 'codegeneration.codegen-templates.update',
    },
    {
      key: 'delete',
      title: t('common.actions.delete'),
      scope: 'row',
      tone: 'danger',
      icon: 'lucide:trash-2',
      disabled: row => asRow(row).isBuiltIn,
      confirm: true,
      confirmText: row => deleteConfirmText(t, asRow(row).templateName),
      permission: 'codegeneration.codegen-templates.delete',
    },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  switch (payload.key) {
    case 'create':
      startCreate()
      break
    case 'edit':
      if (row)
        void startEdit(row)
      break
    case 'toggle':
      if (row)
        void toggle(row)
      break
    case 'delete':
      if (row)
        void remove(row)
      break
  }
}

async function toggle(row: TemplateRow) {
  try {
    await codegenTemplates.setStatus({
      id: row.id,
      concurrencyStamp: row.concurrencyStamp,
      status: row.status === 'Active'
        ? 'Disabled'
        : 'Active',
    })
    toast.success(t('common.messages.save_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
}

async function remove(row: TemplateRow) {
  try {
    const result = await codegenTemplates.delete({
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

// ── 编辑弹窗 ───────────────────────────────────────────────────
const open = ref(false)
const submitting = ref(false)
const validating = ref(false)
const fieldErrors = ref<Record<string, string[]>>({})
const form = reactive<TemplateForm>(emptyForm())
const isEdit = computed(() => form.id !== null)
const modalTitle = computed(() => (isEdit.value
  ? t('codegeneration.code_gen.template.modal_edit_title')
  : t('codegeneration.code_gen.template.modal_add_title')))

/** 编辑器按目标文件的扩展名高亮模板里字面代码的部分（照 4.x） */
const editorFileName = computed(() => {
  const extension = form.fileExtension.trim()
  if (!extension)
    return null
  return extension.startsWith('.')
    ? `template${extension}`
    : `template.${extension}`
})

function emptyForm(): TemplateForm {
  return {
    id: null,
    concurrencyStamp: '',
    templateCode: '',
    templateName: '',
    templateDescription: '',
    templateGroup: '',
    // 缺省是通用模板：适用于单表、树表、主从全部模式（照 4.x）
    templateType: 'Universal',
    templateEngine: 'Scriban',
    writeMode: 'AlwaysOverwrite',
    templateContent: '',
    fileNameExpression: '',
    filePathExpression: '',
    fileExtension: '',
    order: 100,
    status: 'Active',
    remark: '',
  }
}

function fieldError(field: keyof TemplateForm): string | undefined {
  return fieldErrors.value[field]?.[0]
}

function blankToNull(value: string): null | string {
  return value.trim() === ''
    ? null
    : value.trim()
}

function startCreate() {
  Object.assign(form, emptyForm())
  fieldErrors.value = {}
  open.value = true
}

async function startEdit(row: TemplateRow) {
  if (row.isBuiltIn) {
    toast.warning(t('codegeneration.code_gen.template.builtin_cannot_edit'))
    return
  }
  try {
    const detail = await codegenTemplates.get({ id: row.id })
    const template = detail.template
    Object.assign(form, {
      id: template.id,
      concurrencyStamp: template.concurrencyStamp,
      templateCode: template.templateCode,
      templateName: template.templateName,
      templateDescription: template.templateDescription ?? '',
      templateGroup: template.templateGroup ?? '',
      templateType: template.templateType,
      templateEngine: template.templateEngine,
      writeMode: template.writeMode,
      templateContent: detail.templateContent ?? '',
      fileNameExpression: detail.fileNameExpression ?? '',
      filePathExpression: detail.filePathExpression ?? '',
      fileExtension: template.fileExtension ?? '',
      order: template.order,
      status: template.status,
      remark: detail.remark ?? '',
    } satisfies TemplateForm)
    fieldErrors.value = {}
    open.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.template.load_detail_failed'))
  }
}

/** 检查的是大编辑器里正在写的草稿，不是已回写表单的值（照 4.x） */
async function validateSyntax(content: string) {
  if (!content.trim()) {
    toast.warning(t('codegeneration.code_gen.template.validate_content_required'))
    return
  }
  validating.value = true
  try {
    const result = await codegenTemplates.validate({ templateContent: content })
    if (result.valid)
      toast.success(t('codegeneration.code_gen.template.validate_pass'))
    else
      toast.danger(t('codegeneration.code_gen.template.validate_fail', { errors: result.errors.join('; ') || t('codegeneration.code_gen.template.validate_unknown_error') }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.template.validate_error'))
  }
  finally {
    validating.value = false
  }
}

async function onSubmit() {
  submitting.value = true
  fieldErrors.value = {}
  const shared = {
    templateName: form.templateName.trim(),
    templateType: form.templateType,
    templateEngine: form.templateEngine,
    writeMode: form.writeMode,
    templateContent: form.templateContent === ''
      ? null
      : form.templateContent,
    templateDescription: blankToNull(form.templateDescription),
    templateGroup: blankToNull(form.templateGroup),
    fileNameExpression: blankToNull(form.fileNameExpression),
    filePathExpression: blankToNull(form.filePathExpression),
    fileExtension: blankToNull(form.fileExtension),
    status: form.status,
    order: form.order ?? 0,
    remark: blankToNull(form.remark),
  }
  try {
    if (form.id) {
      await codegenTemplates.update({ id: form.id, concurrencyStamp: form.concurrencyStamp, ...shared })
    }
    else {
      await codegenTemplates.create({
        templateCode: form.templateCode.trim(),
        ...shared,
      })
    }
    toast.success(t('common.messages.save_success'))
    open.value = false
    reload()
  }
  catch (error) {
    if (error instanceof ApiError && error.hasFieldErrors)
      fieldErrors.value = error.errors ?? {}
    else
      toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal v-model:show="open" :title="modalTitle" :loading="submitting" :width="920" :form-id="editFormId">
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" novalidate @submit="onSubmit">
        <XhFieldRoot :invalid="!!fieldError('templateCode')" :required="!isEdit">
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_template_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.templateCode" name="templateCode" clearable :disabled="isEdit" :placeholder="t('codegeneration.code_gen.template.form_template_code_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('templateCode')">
            {{ fieldError('templateCode') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('templateName')" required>
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_template_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.templateName" name="templateName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('templateName')">
            {{ fieldError('templateName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('templateGroup')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_template_group') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.templateGroup" name="templateGroup" clearable :placeholder="t('codegeneration.code_gen.template.form_template_group_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('templateGroup')">
            {{ fieldError('templateGroup') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_template_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.templateType" name="templateType" :options="templateTypeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_template_engine') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.templateEngine" name="templateEngine" :options="engineOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_write_mode') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.writeMode" name="writeMode" :options="writeModeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('fileExtension')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_file_extension') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.fileExtension" name="fileExtension" clearable :placeholder="t('codegeneration.code_gen.template.form_file_extension_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('fileExtension')">
            {{ fieldError('fileExtension') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('fileNameExpression')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_file_name_expression') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.fileNameExpression" name="fileNameExpression" clearable :placeholder="t('codegeneration.code_gen.template.form_file_name_expression_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('fileNameExpression')">
            {{ fieldError('fileNameExpression') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('filePathExpression')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_file_path_expression') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.filePathExpression" name="filePathExpression" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('filePathExpression')">
            {{ fieldError('filePathExpression') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_sort') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="form.order" name="order" :min="0" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.common.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!fieldError('templateDescription')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_template_description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.templateDescription" name="templateDescription" clearable :rows="2" type="textarea" />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('templateDescription')">
            {{ fieldError('templateDescription') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('codegeneration.code_gen.template.form_template_content') }}</XhFieldLabel>
          <XhFieldControl :as-child="false">
            <XContentEditorField
              v-model="form.templateContent"
              :title="t('codegeneration.code_gen.template.form_template_content_modal_title')"
              :placeholder="t('codegeneration.code_gen.template.content_placeholder')"
              :edit-text="t('codegeneration.code_gen.template.form_template_content_edit')"
              :confirm-text="t('common.actions.confirm')"
              :cancel-text="t('common.actions.cancel')"
              :count-label="(count: number) => t('codegeneration.code_gen.template.form_template_content_count', { count })"
            >
              <template #editor="{ value, update }">
                <XCodeEditor :value="value" :file-name="editorFileName" height="100%" @update:value="update" />
              </template>
              <template #footer-extra="{ value }">
                <XhButton variant="subtle" size="sm" :loading="validating" @click="validateSyntax(value)">
                  <XhButtonIndicator />
                  <span><Icon icon="lucide:check-check" /></span>
                  <XhButtonLabel>{{ t('codegeneration.code_gen.template.validate_syntax') }}</XhButtonLabel>
                </XhButton>
              </template>
            </XContentEditorField>
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!fieldError('remark')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.common.remark') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.remark" name="remark" clearable :rows="2" type="textarea" />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('remark')">
            {{ fieldError('remark') }}
          </XhFieldErrorText>
        </XhFieldRoot>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
