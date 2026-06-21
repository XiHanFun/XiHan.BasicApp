<script setup lang="ts">
import type { DatabaseType } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NModal,
  NSelect,
  NSpace,
  useMessage,
} from 'naive-ui'
import { ref, watch } from 'vue'
import {
  codeGenerationApi,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
} from '@/api'
import { Icon } from '~/components'

defineOptions({ name: 'CodeGenImportTableModal' })

const props = defineProps<{
  show: boolean
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'imported': []
}>()

const message = useMessage()

const step = ref<1 | 2>(1)
const tableLoading = ref(false)
const submitLoading = ref(false)

const queryKeyword = ref('')
const connectionConfigId = ref<string | null>(null)
const databaseType = ref<DatabaseType>(DatabaseTypeEnum.MySql)
const tableOptions = ref<{ label: string, value: string }[]>([])
const selectedTable = ref<string | null>(null)

const form = ref({
  className: '',
  namespace: '',
  moduleName: '',
  businessName: '',
  functionName: '',
  author: '',
})

watch(
  () => props.show,
  (visible) => {
    if (visible) {
      reset()
      void loadTables()
    }
  },
)

function reset() {
  step.value = 1
  queryKeyword.value = ''
  selectedTable.value = null
  tableOptions.value = []
  form.value = {
    className: '',
    namespace: '',
    moduleName: '',
    businessName: '',
    functionName: '',
    author: '',
  }
}

async function loadTables() {
  tableLoading.value = true
  try {
    const tables = await codeGenerationApi.listDatabaseTables({
      connectionConfigId: connectionConfigId.value?.trim() || undefined,
      keyword: queryKeyword.value?.trim() || undefined,
    })
    tableOptions.value = (tables ?? []).map(name => ({ label: name, value: name }))
  }
  catch {
    message.error('加载数据库表失败')
    tableOptions.value = []
  }
  finally {
    tableLoading.value = false
  }
}

function toPascalCase(name: string) {
  return name
    .split(/[_\s-]+/)
    .filter(Boolean)
    .map(part => part.charAt(0).toUpperCase() + part.slice(1))
    .join('')
}

function handleNext() {
  if (!selectedTable.value) {
    message.warning('请选择要导入的数据库表')
    return
  }
  const pascal = toPascalCase(selectedTable.value)
  form.value.className = form.value.className || pascal
  form.value.businessName = form.value.businessName || pascal
  step.value = 2
}

async function handleSubmit() {
  if (!selectedTable.value) {
    message.warning('请选择要导入的数据库表')
    return
  }
  if (!form.value.className.trim()) {
    message.warning('请输入实体类名')
    return
  }
  submitLoading.value = true
  try {
    await codeGenerationApi.importTable({
      tableName: selectedTable.value,
      connectionConfigId: connectionConfigId.value?.trim() || undefined,
      className: form.value.className.trim(),
      namespace: form.value.namespace?.trim() || undefined,
      moduleName: form.value.moduleName?.trim() || undefined,
      businessName: form.value.businessName?.trim() || undefined,
      functionName: form.value.functionName?.trim() || undefined,
      author: form.value.author?.trim() || undefined,
      databaseType: databaseType.value,
    })
    message.success('导入成功')
    emit('imported')
    emit('update:show', false)
  }
  catch {
    message.error('导入失败')
  }
  finally {
    submitLoading.value = false
  }
}

function handleClose() {
  emit('update:show', false)
}
</script>

<template>
  <NModal
    :auto-focus="false"
    :bordered="false"
    preset="card"
    :show="show"
    style="width: 680px; max-width: 92vw"
    title="导入数据库表"
    @update:show="emit('update:show', $event)"
  >
    <template v-if="step === 1">
      <div class="import-filters">
        <NInput
          v-model:value="connectionConfigId"
          class="import-filters__item"
          clearable
          placeholder="连接配置标识（留空为主库）"
          size="small"
        />
        <NInput
          v-model:value="queryKeyword"
          class="import-filters__item"
          clearable
          placeholder="表名关键字"
          size="small"
          @keyup.enter="loadTables"
        />
        <NButton :loading="tableLoading" size="small" type="primary" @click="loadTables">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
      </div>
      <NForm label-placement="top">
        <NFormItem label="数据库类型">
          <NSelect v-model:value="databaseType" :options="DATABASE_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="选择数据库表">
          <NSelect
            v-model:value="selectedTable"
            clearable
            filterable
            :loading="tableLoading"
            :options="tableOptions"
            placeholder="请选择要导入的表"
          />
        </NFormItem>
      </NForm>
    </template>

    <template v-else>
      <NForm class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="数据库表">
          <NInput :value="selectedTable ?? ''" disabled />
        </NFormItem>
        <NFormItem label="实体类名">
          <NInput v-model:value="form.className" clearable placeholder="如: SysUser" />
        </NFormItem>
        <NFormItem label="命名空间">
          <NInput v-model:value="form.namespace" clearable />
        </NFormItem>
        <NFormItem label="模块名">
          <NInput v-model:value="form.moduleName" clearable />
        </NFormItem>
        <NFormItem label="业务名">
          <NInput v-model:value="form.businessName" clearable />
        </NFormItem>
        <NFormItem label="功能名">
          <NInput v-model:value="form.functionName" clearable />
        </NFormItem>
        <NFormItem label="作者">
          <NInput v-model:value="form.author" clearable />
        </NFormItem>
      </NForm>
    </template>

    <template #footer>
      <NSpace justify="space-between">
        <NButton v-if="step === 2" @click="step = 1">
          上一步
        </NButton>
        <span v-else />
        <NSpace>
          <NButton @click="handleClose">
            取消
          </NButton>
          <NButton v-if="step === 1" type="primary" @click="handleNext">
            下一步
          </NButton>
          <NButton v-else :loading="submitLoading" type="primary" @click="handleSubmit">
            导入
          </NButton>
        </NSpace>
      </NSpace>
    </template>
  </NModal>
</template>

<style scoped>
.import-filters {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}

.import-filters__item {
  flex: 1;
}
</style>
