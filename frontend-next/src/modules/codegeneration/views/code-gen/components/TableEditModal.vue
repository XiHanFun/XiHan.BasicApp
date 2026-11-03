<!--
  代码生成 / 编辑表配置弹窗（照 4.x table-edit-modal）
  4.x 的父菜单不再需要（6.0 生成的页面登记在所属包的 Pages.cs、目录按包），另加生成目标（新建包或加入已有包）；状态随整体修改一起提交
-->
<script setup lang="ts">
import type { DeliveryType, GenerationScope, GenerationTarget, TableAction, TableDetail, TemplateType } from './use-tables'
import type { SelectOption } from '~/types'
import { XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSpinner } from '@xihan-ui/vue'
import { computed, reactive, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XEditModal, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { codegenTables } from '~/contracts'
import { ApiError } from '~/request'
import { DATABASE_TYPE_OPTIONS } from './use-datasources'
import { actionsConflict, ALL_ACTIONS, DELIVERY_TYPES, enumKey, GENERATION_SCOPES, GENERATION_TARGETS, parseEnabledActions, serializeEnabledActions, TEMPLATE_TYPES } from './use-tables'

defineOptions({ name: 'CodegenTableEditModal' })

const props = defineProps<{
  show: boolean
  tableId: null | string
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'saved': []
}>()

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单 */
const editFormId = useId()

type Table = TableDetail['table']

interface TableForm {
  tableName: string
  tableComment: string
  className: string
  namespace: string
  moduleName: string
  businessName: string
  functionName: string
  author: string
  templateType: TemplateType
  deliveryType: DeliveryType
  scope: GenerationScope
  target: GenerationTarget
  enabledActions: TableAction[]
  databaseType: Table['databaseType']
  primaryKeyColumn: null | string
  treeParentColumn: null | string
  treeNameColumn: null | string
  masterTableId: null | string
  masterForeignKey: null | string
  status: Table['status']
}

const loading = ref(false)
const submitLoading = ref(false)
const current = ref<Table | null>(null)
const form = reactive<TableForm>(emptyForm())
const fieldErrors = ref<Record<string, string[]>>({})
/** 本表的列（主键列、树列、外键列的下拉，来源是详情带的列） */
const columnOptions = ref<SelectOption[]>([])
/** 别的表（主从模式选主表） */
const tableOptions = ref<SelectOption[]>([])

const isTree = computed(() => form.templateType === 'Tree')
const isMasterDetail = computed(() => form.templateType === 'MasterDetail')

function emptyForm(): TableForm {
  return { tableName: '', tableComment: '', className: '', namespace: '', moduleName: '', businessName: '', functionName: '', author: '', templateType: 'Single', deliveryType: 'Zip', scope: 'All', target: 'NewPackage', enabledActions: [...ALL_ACTIONS], databaseType: 'PostgreSql', primaryKeyColumn: null, treeParentColumn: null, treeNameColumn: null, masterTableId: null, masterForeignKey: null, status: 'Active' }
}

function options<T extends string>(group: string, values: readonly T[]): SelectOption[] {
  return values.map(value => ({ label: t(enumKey(group, value)), value }))
}

const templateTypeOptions = computed(() => options('template_type', TEMPLATE_TYPES))
const deliveryTypeOptions = computed(() => options('delivery_type', DELIVERY_TYPES))
const scopeOptions = computed(() => options('scope', GENERATION_SCOPES))
const targetOptions = computed(() => options('target', GENERATION_TARGETS))
const actionOptions = computed(() => options('action', ALL_ACTIONS))
const databaseTypeOptions = computed<SelectOption[]>(() => DATABASE_TYPE_OPTIONS.map(option => ({ label: option.label, value: option.value })))
const statusOptions = computed<SelectOption[]>(() => [
  { label: t('common.statuses.enabled'), value: 'Active' },
  { label: t('common.statuses.disabled'), value: 'Disabled' },
])

watch(
  () => props.show,
  (visible) => {
    if (!visible)
      return
    // 同步进入加载态并清掉上一行残留，免得弹窗先渲染旧数据再整体跳一次
    loading.value = true
    Object.assign(form, emptyForm())
    fieldErrors.value = {}
    columnOptions.value = []
    current.value = null
    void loadDetail()
  },
)

async function loadDetail() {
  if (!props.tableId) {
    loading.value = false
    return
  }
  try {
    const detail = await codegenTables.get({ id: props.tableId })
    const table = detail.table
    current.value = table
    columnOptions.value = detail.columns.map(column => ({
      label: column.columnComment ? `${column.columnName}（${column.columnComment}）` : column.columnName,
      value: column.columnName,
    }))
    Object.assign(form, {
      tableName: table.tableName,
      tableComment: table.tableComment ?? '',
      className: table.className,
      namespace: table.namespace ?? '',
      moduleName: table.moduleName ?? '',
      businessName: table.businessName ?? '',
      functionName: table.functionName ?? '',
      author: table.author ?? '',
      templateType: table.templateType,
      deliveryType: table.deliveryType,
      scope: table.scope,
      target: table.target,
      enabledActions: parseEnabledActions(table.enabledActions),
      databaseType: table.databaseType,
      primaryKeyColumn: table.primaryKeyColumn ?? null,
      treeParentColumn: table.treeParentColumn ?? null,
      treeNameColumn: table.treeNameColumn ?? null,
      masterTableId: table.masterTableId ?? null,
      masterForeignKey: table.masterForeignKey ?? null,
      status: table.status,
    } satisfies TableForm)
    if (form.templateType === 'MasterDetail')
      void ensureTableOptions()
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.table_edit.load_failed'))
    emit('update:show', false)
  }
  finally {
    loading.value = false
  }
}

/** 惰性加载别的表（主从模式选主表），排除本表 */
async function ensureTableOptions() {
  if (tableOptions.value.length > 0)
    return
  try {
    const { items } = await codegenTables.options({})
    tableOptions.value = items.filter(item => item.id !== props.tableId).map(item => ({ label: `${item.tableName}（${item.className}）`, value: item.id }))
  }
  catch {
    tableOptions.value = []
  }
}

// 换生成模式：清掉与新模式无关的结构字段，主从时惰性拉取表
watch(() => form.templateType, (type) => {
  if (type !== 'Tree') {
    form.treeParentColumn = null
    form.treeNameColumn = null
  }
  if (type !== 'MasterDetail') {
    form.masterTableId = null
    form.masterForeignKey = null
  }
  else {
    void ensureTableOptions()
  }
})

function fieldError(field: string): string | undefined {
  return fieldErrors.value[field]?.[0]
}

function validateForm(): boolean {
  if (!form.tableName.trim()) {
    toast.warning(t('codegeneration.code_gen.table_edit.validate_table_name'))
    return false
  }
  if (!form.className.trim()) {
    toast.warning(t('codegeneration.code_gen.table_edit.validate_class_name'))
    return false
  }
  if (isTree.value && !form.treeParentColumn) {
    toast.warning(t('codegeneration.code_gen.table_edit.validate_tree_parent_column'))
    return false
  }
  if (isTree.value && !form.treeNameColumn) {
    toast.warning(t('codegeneration.code_gen.table_edit.validate_tree_name_column'))
    return false
  }
  if (isMasterDetail.value && !form.masterTableId) {
    toast.warning(t('codegeneration.code_gen.table_edit.validate_master_table'))
    return false
  }
  if (isMasterDetail.value && !form.masterForeignKey) {
    toast.warning(t('codegeneration.code_gen.table_edit.validate_master_foreign_key'))
    return false
  }
  if (actionsConflict(form.enabledActions)) {
    toast.warning(t('codegeneration.code_gen.table_edit.validate_import_requires_create'))
    return false
  }
  return true
}

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

async function handleSubmit() {
  const table = current.value
  if (!table || !validateForm())
    return
  submitLoading.value = true
  fieldErrors.value = {}
  try {
    await codegenTables.update({
      id: table.id,
      concurrencyStamp: table.concurrencyStamp,
      tableName: form.tableName.trim(),
      tableComment: blankToNull(form.tableComment),
      className: form.className.trim(),
      namespace: blankToNull(form.namespace),
      moduleName: blankToNull(form.moduleName),
      businessName: blankToNull(form.businessName),
      functionName: blankToNull(form.functionName),
      author: blankToNull(form.author),
      templateType: form.templateType,
      deliveryType: form.deliveryType,
      scope: form.scope,
      target: form.target,
      enabledActions: serializeEnabledActions(form.enabledActions),
      databaseType: form.databaseType,
      primaryKeyColumn: form.primaryKeyColumn,
      treeParentColumn: isTree.value ? form.treeParentColumn : null,
      treeNameColumn: isTree.value ? form.treeNameColumn : null,
      masterTableId: isMasterDetail.value ? form.masterTableId : null,
      masterForeignKey: isMasterDetail.value ? form.masterForeignKey : null,
      dataSourceId: table.dataSourceId ?? null,
      options: table.options ?? null,
      status: form.status,
      remark: table.remark ?? null,
    })
    toast.success(t('common.messages.save_success'))
    emit('saved')
    emit('update:show', false)
  }
  catch (error) {
    if (error instanceof ApiError && error.hasFieldErrors)
      fieldErrors.value = error.errors ?? {}
    else
      toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="t('codegeneration.code_gen.table_edit.title')"
    :loading="submitLoading"
    :form-id="editFormId"
    @update:show="emit('update:show', $event)"
  >
    <!-- 表单常驻、加载期叠一层遮罩：v-if 摘挂会让弹窗高度在打开瞬间连跳两次 -->
    <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
      <div class="xh-loading-stage__veil">
        <XhSpinner />
      </div>
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" novalidate @submit="handleSubmit">
        <XhFieldRoot :invalid="!!fieldError('tableName')" required>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_table_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.tableName" name="tableName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('tableName')">
            {{ fieldError('tableName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('className')" required>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_class_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.className" name="className" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('className')">
            {{ fieldError('className') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('namespace')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_namespace') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.namespace" name="namespace" clearable :placeholder="t('codegeneration.code_gen.table_edit.form_namespace_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('namespace')">
            {{ fieldError('namespace') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('moduleName')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_module_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.moduleName" name="moduleName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('moduleName')">
            {{ fieldError('moduleName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('businessName')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_business_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.businessName" name="businessName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('businessName')">
            {{ fieldError('businessName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('functionName')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_function_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.functionName" name="functionName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('functionName')">
            {{ fieldError('functionName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('author')">
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_author') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.author" name="author" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('author')">
            {{ fieldError('author') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_template_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.templateType" name="templateType" :options="templateTypeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_target') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.target" name="target" :options="targetOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_gen_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.deliveryType" name="deliveryType" :options="deliveryTypeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_generation_scope') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.scope" name="scope" :options="scopeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_enabled_actions') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect
              v-model:value="form.enabledActions"
              name="enabledActions"
              multiple
              :max-tag-count="3"
              :options="actionOptions"
              :placeholder="t('codegeneration.code_gen.table_edit.form_enabled_actions_placeholder')"
            />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_database_type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.databaseType" name="databaseType" :options="databaseTypeOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_primary_key_column') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.primaryKeyColumn" name="primaryKeyColumn" clearable :options="columnOptions" :placeholder="t('codegeneration.code_gen.table_edit.form_column_placeholder')" />
          </XhFieldControl>
        </XhFieldRoot>
        <template v-if="isTree">
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_tree_parent_column') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.treeParentColumn" name="treeParentColumn" clearable :options="columnOptions" :placeholder="t('codegeneration.code_gen.table_edit.form_column_placeholder')" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_tree_name_column') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.treeNameColumn" name="treeNameColumn" clearable :options="columnOptions" :placeholder="t('codegeneration.code_gen.table_edit.form_column_placeholder')" />
            </XhFieldControl>
          </XhFieldRoot>
        </template>
        <template v-if="isMasterDetail">
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_master_table') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.masterTableId" name="masterTableId" clearable :options="tableOptions" :placeholder="t('codegeneration.code_gen.table_edit.form_master_table_placeholder')" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_master_foreign_key') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.masterForeignKey" name="masterForeignKey" clearable :options="columnOptions" :placeholder="t('codegeneration.code_gen.table_edit.form_column_placeholder')" />
            </XhFieldControl>
          </XhFieldRoot>
        </template>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('codegeneration.code_gen.common.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!fieldError('tableComment')" class="xh-span-2">
          <XhFieldLabel>{{ t('codegeneration.code_gen.table_edit.form_table_comment') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="form.tableComment" name="tableComment" clearable :rows="2" type="textarea" />
          </XhFieldControl>
          <XhFieldErrorText v-if="fieldError('tableComment')">
            {{ fieldError('tableComment') }}
          </XhFieldErrorText>
        </XhFieldRoot>
      </XhFormRoot>
    </div>
  </XEditModal>
</template>
