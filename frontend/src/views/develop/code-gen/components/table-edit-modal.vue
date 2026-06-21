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

defineOptions({ name: 'CodeGenTableEditModal' })

const props = defineProps<{
  show: boolean
  tableId: ApiId | null
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'saved': []
}>()

const message = useMessage()

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
      message.error('表配置不存在')
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
    message.error('加载表配置失败')
  }
  finally {
    loading.value = false
  }
}

function validateForm() {
  if (!form.value.tableName.trim()) {
    message.warning('请输入表名')
    return false
  }
  if (!form.value.className.trim()) {
    message.warning('请输入实体类名')
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
        remark: '前端更新表配置状态',
        status: form.value.status,
      })
    }
    message.success('保存成功')
    emit('saved')
    emit('update:show', false)
  }
  catch {
    message.error('保存失败')
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
    title="编辑表配置"
    @update:show="emit('update:show', $event)"
  >
    <NForm v-if="!loading" :model="form" class="xh-edit-form-grid" label-placement="top">
      <NFormItem label="表名" path="tableName">
        <NInput v-model:value="form.tableName" clearable />
      </NFormItem>
      <NFormItem label="实体类名" path="className">
        <NInput v-model:value="form.className" clearable />
      </NFormItem>
      <NFormItem label="命名空间" path="namespace">
        <NInput v-model:value="form.namespace" clearable />
      </NFormItem>
      <NFormItem label="模块名" path="moduleName">
        <NInput v-model:value="form.moduleName" clearable />
      </NFormItem>
      <NFormItem label="业务名" path="businessName">
        <NInput v-model:value="form.businessName" clearable />
      </NFormItem>
      <NFormItem label="功能名" path="functionName">
        <NInput v-model:value="form.functionName" clearable />
      </NFormItem>
      <NFormItem label="作者" path="author">
        <NInput v-model:value="form.author" clearable />
      </NFormItem>
      <NFormItem label="模板类型" path="templateType">
        <NSelect v-model:value="form.templateType" :options="TEMPLATE_TYPE_OPTIONS" />
      </NFormItem>
      <NFormItem label="生成方式" path="genType">
        <NSelect v-model:value="form.genType" :options="GEN_TYPE_OPTIONS" />
      </NFormItem>
      <NFormItem label="数据库类型" path="databaseType">
        <NSelect v-model:value="form.databaseType" :options="DATABASE_TYPE_OPTIONS" />
      </NFormItem>
      <NFormItem label="生成路径" path="genPath">
        <NInput v-model:value="form.genPath" clearable placeholder="自定义路径生成时使用" />
      </NFormItem>
      <NFormItem label="状态" path="status">
        <NSelect v-model:value="form.status" :options="STATUS_OPTIONS" />
      </NFormItem>
      <NFormItem class="xh-form-full" label="表说明" path="tableComment">
        <NInput v-model:value="form.tableComment" clearable :rows="2" type="textarea" />
      </NFormItem>
    </NForm>

    <template #footer>
      <NSpace justify="end">
        <NButton @click="emit('update:show', false)">
          取消
        </NButton>
        <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
          保存
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>
