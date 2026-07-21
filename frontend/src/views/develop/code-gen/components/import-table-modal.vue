<script setup lang="ts">
import type { ApiId, DatabaseType } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NSelect,
  useMessage,
} from 'naive-ui'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenDataSourceApi,
  codeGenerationApi,
  createPageRequest,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
} from '@/api'
import { Icon, XEditModal } from '~/components'

defineOptions({ name: 'CodeGenImportTableModal' })

const props = defineProps<{
  show: boolean
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'imported': []
}>()

const { t } = useI18n()
const message = useMessage()

const step = ref<1 | 2>(1)
const tableLoading = ref(false)
const submitLoading = ref(false)

const queryKeyword = ref('')
/** 数据源：空串表示本系统主库（Naive UI 选项值不接受 null，故用空串作哨兵） */
const dataSourceId = ref<ApiId>('')
const dataSourceOptions = ref<{ label: string, value: ApiId }[]>([])
const databaseType = ref<DatabaseType>(DatabaseTypeEnum.MySql)

/**
 * 加载可选数据源。
 * 选项文案带上库类型与库名，避免只显示名称时要回想「这个数据源指向哪」。
 */
async function loadDataSources() {
  try {
    const result = await codeGenDataSourceApi.page(
      createPageRequest({ page: { pageIndex: 1, pageSize: 200 } }),
    )
    dataSourceOptions.value = [
      { label: t('develop.code_gen.import.data_source_primary'), value: '' },
      ...(result?.items ?? []).map(item => ({
        label: `${item.sourceName}（${item.databaseType} · ${item.databaseName}）`,
        value: item.basicId,
      })),
    ]
  }
  catch {
    dataSourceOptions.value = [{ label: t('develop.code_gen.import.data_source_primary'), value: '' }]
  }
}
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
      void loadDataSources()
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
      dataSourceId: dataSourceId.value || undefined,
      keyword: queryKeyword.value?.trim() || undefined,
    })
    tableOptions.value = (tables ?? []).map(name => ({ label: name, value: name }))
  }
  catch {
    message.error(t('develop.code_gen.import.load_tables_failed'))
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
    message.warning(t('develop.code_gen.import.validate_select_table'))
    return
  }
  const pascal = toPascalCase(selectedTable.value)
  form.value.className = form.value.className || pascal
  form.value.businessName = form.value.businessName || pascal
  step.value = 2
}

async function handleSubmit() {
  if (!selectedTable.value) {
    message.warning(t('develop.code_gen.import.validate_select_table'))
    return
  }
  if (!form.value.className.trim()) {
    message.warning(t('develop.code_gen.import.validate_class_name'))
    return
  }
  submitLoading.value = true
  try {
    await codeGenerationApi.importTable({
      tableName: selectedTable.value,
      dataSourceId: dataSourceId.value || undefined,
      className: form.value.className.trim(),
      namespace: form.value.namespace?.trim() || undefined,
      moduleName: form.value.moduleName?.trim() || undefined,
      businessName: form.value.businessName?.trim() || undefined,
      functionName: form.value.functionName?.trim() || undefined,
      author: form.value.author?.trim() || undefined,
      databaseType: databaseType.value,
    })
    message.success(t('develop.code_gen.import.import_success'))
    emit('imported')
    emit('update:show', false)
  }
  catch {
    message.error(t('develop.code_gen.import.import_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="t('develop.code_gen.import.title')"
    :loading="submitLoading"
    :save-text="step === 1 ? t('common.actions.next_step') : t('develop.code_gen.import.action_import')"
    @update:show="emit('update:show', $event)"
    @save="step === 1 ? handleNext() : handleSubmit()"
  >
    <template v-if="step === 1">
      <div class="import-filters">
        <NSelect
          v-model:value="dataSourceId"
          class="import-filters__item"
          filterable
          :options="dataSourceOptions"
          :placeholder="t('develop.code_gen.import.data_source_placeholder')"
          @update:value="loadTables"
        />
        <NInput
          v-model:value="queryKeyword"
          class="import-filters__item"
          clearable
          :placeholder="t('develop.code_gen.import.keyword_placeholder')"
          @keyup.enter="loadTables"
        />
        <NButton :loading="tableLoading" type="primary" @click="loadTables">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          {{ t('common.actions.search') }}
        </NButton>
      </div>
      <NForm class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('develop.code_gen.import.form_database_type')">
          <NSelect v-model:value="databaseType" :options="DATABASE_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.import.form_select_table')">
          <NSelect
            v-model:value="selectedTable"
            clearable
            filterable
            :loading="tableLoading"
            :options="tableOptions"
            :placeholder="t('develop.code_gen.import.select_table_placeholder')"
          />
        </NFormItem>
      </NForm>
    </template>

    <template v-else>
      <NForm :model="form" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('develop.code_gen.import.form_table')">
          <NInput :value="selectedTable ?? ''" disabled />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.import.form_class_name')">
          <NInput v-model:value="form.className" clearable :placeholder="t('develop.code_gen.import.form_class_name_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.import.form_namespace')">
          <NInput v-model:value="form.namespace" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.import.form_module_name')">
          <NInput v-model:value="form.moduleName" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.import.form_business_name')">
          <NInput v-model:value="form.businessName" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.import.form_function_name')">
          <NInput v-model:value="form.functionName" clearable />
        </NFormItem>
        <NFormItem :label="t('develop.code_gen.import.form_author')">
          <NInput v-model:value="form.author" clearable />
        </NFormItem>
      </NForm>
    </template>

    <template #footer-extra>
      <NButton v-if="step === 2" size="small" @click="step = 1">
        {{ t('common.actions.prev_step') }}
      </NButton>
    </template>
  </XEditModal>
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
