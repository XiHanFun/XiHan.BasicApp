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
  NButton,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NSelect,
  NSpace,
  useMessage,
} from 'naive-ui'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenTableApi,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
  EnableStatus as EnableStatusEnum,
  GEN_TYPE_OPTIONS,
  GenType as GenTypeEnum,
  TEMPLATE_TYPE_OPTIONS,
  TemplateType as TemplateTypeEnum,
} from '@/api'
import { STATUS_OPTIONS } from '~/constants'
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
  databaseType: DatabaseType
  dbConnectionName?: string | null
  status: EnableStatus
  remark?: string | null
}

const loading = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const form = ref<TableFormModel>(createDefaultForm())

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
    databaseType: DatabaseTypeEnum.MySql,
    dbConnectionName: null,
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
      databaseType: detail.databaseType,
      dbConnectionName: detail.dbConnectionName ?? null,
      status: detail.status,
      remark: detail.remark ?? null,
    }
  }
  catch {
    message.error(t('develop.code_gen.table_edit.load_failed'))
  }
  finally {
    loading.value = false
  }
}

function validateForm() {
  if (!form.value.tableName.trim()) {
    message.warning(t('develop.code_gen.table_edit.validate_table_name'))
    return false
  }
  if (!form.value.className.trim()) {
    message.warning(t('develop.code_gen.table_edit.validate_class_name'))
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
      parentMenuId: null,
      primaryKeyColumn: null,
      treeParentColumn: null,
      treeNameColumn: null,
      masterTableId: null,
      masterForeignKey: null,
      databaseType: form.value.databaseType,
      dbConnectionName: form.value.dbConnectionName,
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
  <NModal
    :auto-focus="false"
    :bordered="false"
    preset="card"
    :show="show"
    style="width: 760px; max-width: 92vw"
    :title="t('develop.code_gen.table_edit.title')"
    @update:show="emit('update:show', $event)"
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
      <NFormItem :label="t('common.fields.status')" path="status">
        <NSelect v-model:value="form.status" :options="statusEnumOptions" />
      </NFormItem>
      <NFormItem class="xh-form-full" :label="t('develop.code_gen.table_edit.form_table_comment')" path="tableComment">
        <NInput v-model:value="form.tableComment" clearable :rows="2" type="textarea" />
      </NFormItem>
    </NForm>

    <template #footer>
      <NSpace justify="end">
        <NButton @click="emit('update:show', false)">
          {{ t('common.actions.cancel') }}
        </NButton>
        <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
          {{ t('common.actions.save') }}
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>
