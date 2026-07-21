<script setup lang="ts">
import type {
  ApiId,
  CodeGenTableUpdateDto,
  DatabaseType,
  EnableStatus,
  GenType,
  TemplateType,
} from '@/api'
import {
  NForm,
  NFormItem,
  NInput,
  NSelect,
  useMessage,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenTableApi,
  createPageRequest,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
  EnableStatus as EnableStatusEnum,
  GEN_TYPE_OPTIONS,
  GenType as GenTypeEnum,
  TEMPLATE_TYPE_OPTIONS,
  TemplateType as TemplateTypeEnum,
} from '@/api'
import { STATUS_OPTIONS } from '@/constants'
import { XEditModal } from '~/components'
import { useEnumOptions } from '~/hooks'

defineOptions({ name: 'CodeGenTableEditModal' })

const props = defineProps<{
  show: boolean
  tableId: ApiId | null
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'saved': []
}>()

const { t } = useI18n()
const message = useMessage()

const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

interface TableFormModel {
  basicId: string
  tableName: string
  tableComment?: string | null
  className: string
  namespace?: string | null
  moduleName?: string | null
  businessName?: string | null
  functionName?: string | null
  author?: string | null
  templateType: TemplateType
  genType: GenType
  genPath?: string | null
  // 上级菜单：M1 仅随详情回传（不写死 null），M3 接入菜单树选择控件
  parentMenuId?: ApiId | null
  primaryKeyColumn?: string | null
  treeParentColumn?: string | null
  treeNameColumn?: string | null
  masterTableId?: ApiId | null
  masterForeignKey?: string | null
  databaseType: DatabaseType
  dataSourceId?: ApiId | null
  status: EnableStatus
  remark?: string | null
}

const loading = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const form = ref<TableFormModel>(createDefaultForm())

/** 本表列（供主键列/树列/子表外键列下拉选择，来源为详情随附的 columns） */
const columnOptions = ref<{ label: string, value: string }[]>([])
/** 其他表（供主子表的主表选择） */
const tableOptions = ref<{ label: string, value: ApiId }[]>([])

const isTreeTemplate = computed(() => form.value.templateType === TemplateTypeEnum.Tree)
const isMasterDetailTemplate = computed(() => form.value.templateType === TemplateTypeEnum.MasterDetail)

function createDefaultForm(): TableFormModel {
  return {
    basicId: '',
    tableName: '',
    tableComment: null,
    className: '',
    namespace: null,
    moduleName: null,
    businessName: null,
    functionName: null,
    author: null,
    templateType: TemplateTypeEnum.Single,
    genType: GenTypeEnum.Zip,
    genPath: null,
    parentMenuId: null,
    primaryKeyColumn: null,
    treeParentColumn: null,
    treeNameColumn: null,
    masterTableId: null,
    masterForeignKey: null,
    databaseType: DatabaseTypeEnum.MySql,
    dataSourceId: null,
    status: EnableStatusEnum.Enabled,
    remark: null,
  }
}

watch(
  () => props.show,
  (visible) => {
    if (visible && props.tableId) {
      void loadDetail()
    }
  },
)

async function loadDetail() {
  if (!props.tableId) {
    return
  }
  loading.value = true
  try {
    const detail = await codeGenTableApi.detail(props.tableId)
    if (!detail) {
      message.error(t('develop.code_gen.table_edit.not_found'))
      emit('update:show', false)
      return
    }
    editingStatus.value = detail.status
    columnOptions.value = (detail.columns ?? []).map(column => ({
      label: column.columnComment ? `${column.columnName}（${column.columnComment}）` : column.columnName,
      value: column.columnName,
    }))
    form.value = {
      basicId: detail.basicId,
      tableName: detail.tableName,
      tableComment: detail.tableComment ?? null,
      className: detail.className,
      namespace: detail.namespace ?? null,
      moduleName: detail.moduleName ?? null,
      businessName: detail.businessName ?? null,
      functionName: detail.functionName ?? null,
      author: detail.author ?? null,
      templateType: detail.templateType,
      genType: detail.genType,
      genPath: detail.genPath ?? null,
      parentMenuId: detail.parentMenuId ?? null,
      primaryKeyColumn: detail.primaryKeyColumn ?? null,
      treeParentColumn: detail.treeParentColumn ?? null,
      treeNameColumn: detail.treeNameColumn ?? null,
      masterTableId: detail.masterTableId ?? null,
      masterForeignKey: detail.masterForeignKey ?? null,
      databaseType: detail.databaseType,
      dataSourceId: detail.dataSourceId ?? null,
      status: detail.status,
      remark: detail.remark ?? null,
    }
    if (form.value.templateType === TemplateTypeEnum.MasterDetail) {
      void ensureTableOptions()
    }
  }
  catch {
    message.error(t('develop.code_gen.table_edit.load_failed'))
  }
  finally {
    loading.value = false
  }
}

/** 惰性加载其他表列表（主子表选择主表用）；排除本表自身 */
async function ensureTableOptions() {
  if (tableOptions.value.length > 0) {
    return
  }
  try {
    const result = await codeGenTableApi.page({
      ...createPageRequest({ page: { pageIndex: 1, pageSize: 200 } }),
    })
    tableOptions.value = result.items
      .filter(item => item.basicId !== form.value.basicId)
      .map(item => ({ label: `${item.tableName}（${item.className}）`, value: item.basicId }))
  }
  catch {
    tableOptions.value = []
  }
}

// 切换模板类型：清空与新类型无关的结构字段，主子表时惰性拉取表列表
watch(() => form.value.templateType, (type) => {
  if (type !== TemplateTypeEnum.Tree) {
    form.value.treeParentColumn = null
    form.value.treeNameColumn = null
  }
  if (type !== TemplateTypeEnum.MasterDetail) {
    form.value.masterTableId = null
    form.value.masterForeignKey = null
  }
  else {
    void ensureTableOptions()
  }
})

function validateForm() {
  if (!form.value.tableName.trim()) {
    message.warning(t('develop.code_gen.table_edit.validate_table_name'))
    return false
  }
  if (!form.value.className.trim()) {
    message.warning(t('develop.code_gen.table_edit.validate_class_name'))
    return false
  }
  if (isTreeTemplate.value) {
    if (!form.value.treeParentColumn) {
      message.warning(t('develop.code_gen.table_edit.validate_tree_parent_column'))
      return false
    }
    if (!form.value.treeNameColumn) {
      message.warning(t('develop.code_gen.table_edit.validate_tree_name_column'))
      return false
    }
  }
  if (isMasterDetailTemplate.value) {
    if (!form.value.masterTableId) {
      message.warning(t('develop.code_gen.table_edit.validate_master_table'))
      return false
    }
    if (!form.value.masterForeignKey) {
      message.warning(t('develop.code_gen.table_edit.validate_master_foreign_key'))
      return false
    }
  }
  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }
  submitLoading.value = true
  try {
    const updateInput: CodeGenTableUpdateDto = {
      basicId: form.value.basicId,
      tableName: form.value.tableName.trim(),
      tableComment: form.value.tableComment,
      className: form.value.className.trim(),
      namespace: form.value.namespace,
      moduleName: form.value.moduleName,
      businessName: form.value.businessName,
      functionName: form.value.functionName,
      author: form.value.author,
      templateType: form.value.templateType,
      genType: form.value.genType,
      genPath: form.value.genPath,
      parentMenuId: form.value.parentMenuId,
      primaryKeyColumn: form.value.primaryKeyColumn,
      treeParentColumn: isTreeTemplate.value ? form.value.treeParentColumn : null,
      treeNameColumn: isTreeTemplate.value ? form.value.treeNameColumn : null,
      masterTableId: isMasterDetailTemplate.value ? form.value.masterTableId : null,
      masterForeignKey: isMasterDetailTemplate.value ? form.value.masterForeignKey : null,
      databaseType: form.value.databaseType,
      dataSourceId: form.value.dataSourceId,
      options: null,
      status: form.value.status,
      remark: form.value.remark,
    }
    await codeGenTableApi.update(updateInput)
    if (editingStatus.value !== form.value.status) {
      await codeGenTableApi.updateStatus({
        basicId: form.value.basicId,
        remark: t('develop.code_gen.table_edit.update_status_remark'),
        status: form.value.status,
      })
    }
    message.success(t('common.messages.save_success'))
    emit('saved')
    emit('update:show', false)
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
  <XEditModal
    :show="show"
    :title="t('develop.code_gen.table_edit.title')"
    :loading="submitLoading"
    @update:show="emit('update:show', $event)"
    @save="handleSubmit"
  >
    <NForm v-if="!loading" :model="form" class="xh-edit-form-grid" label-placement="top">
      <NFormItem :label="t('develop.code_gen.table_edit.form_table_name')" path="tableName">
        <NInput v-model:value="form.tableName" clearable />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_class_name')" path="className">
        <NInput v-model:value="form.className" clearable />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_namespace')" path="namespace">
        <NInput v-model:value="form.namespace" clearable />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_module_name')" path="moduleName">
        <NInput v-model:value="form.moduleName" clearable />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_business_name')" path="businessName">
        <NInput v-model:value="form.businessName" clearable />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_function_name')" path="functionName">
        <NInput v-model:value="form.functionName" clearable />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_author')" path="author">
        <NInput v-model:value="form.author" clearable />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_template_type')" path="templateType">
        <NSelect v-model:value="form.templateType" :options="TEMPLATE_TYPE_OPTIONS" />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_gen_type')" path="genType">
        <NSelect v-model:value="form.genType" :options="GEN_TYPE_OPTIONS" />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_database_type')" path="databaseType">
        <NSelect v-model:value="form.databaseType" :options="DATABASE_TYPE_OPTIONS" />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_gen_path')" path="genPath">
        <NInput v-model:value="form.genPath" clearable :placeholder="t('develop.code_gen.table_edit.form_gen_path_placeholder')" />
      </NFormItem>
      <NFormItem :label="t('develop.code_gen.table_edit.form_primary_key_column')" path="primaryKeyColumn">
        <NSelect
          v-model:value="form.primaryKeyColumn"
          clearable
          filterable
          :options="columnOptions"
          :placeholder="t('develop.code_gen.table_edit.form_column_placeholder')"
        />
      </NFormItem>
      <template v-if="isTreeTemplate">
        <NFormItem :label="t('develop.code_gen.table_edit.form_tree_parent_column')" path="treeParentColumn">
          <NSelect
            v-model:value="form.treeParentColumn"
            clearable
            filterable
            :options="columnOptions"
            :placeholder="t('develop.code_gen.table_edit.form_column_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.table_edit.form_tree_name_column')" path="treeNameColumn">
          <NSelect
            v-model:value="form.treeNameColumn"
            clearable
            filterable
            :options="columnOptions"
            :placeholder="t('develop.code_gen.table_edit.form_column_placeholder')"
          />
        </NFormItem>
      </template>
      <template v-if="isMasterDetailTemplate">
        <NFormItem :label="t('develop.code_gen.table_edit.form_master_table')" path="masterTableId">
          <NSelect
            v-model:value="form.masterTableId"
            clearable
            filterable
            :options="tableOptions"
            :placeholder="t('develop.code_gen.table_edit.form_master_table_placeholder')"
          />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.table_edit.form_master_foreign_key')" path="masterForeignKey">
          <NSelect
            v-model:value="form.masterForeignKey"
            clearable
            filterable
            :options="columnOptions"
            :placeholder="t('develop.code_gen.table_edit.form_column_placeholder')"
          />
        </NFormItem>
      </template>
      <NFormItem :label="t('common.fields.status')" path="status">
        <NSelect v-model:value="form.status" :options="statusEnumOptions" />
      </NFormItem>
      <NFormItem class="xh-span-2" :label="t('develop.code_gen.table_edit.form_table_comment')" path="tableComment">
        <NInput v-model:value="form.tableComment" clearable :rows="2" type="textarea" />
      </NFormItem>
    </NForm>
  </XEditModal>
</template>
