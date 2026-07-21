<script setup lang="ts">
import type { ApiId, CodeGenImportTablesResultDto, DatabaseType } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
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

const tableLoading = ref(false)
const submitLoading = ref(false)

const queryKeyword = ref('')
/** 数据源：空串表示本系统主库（Naive UI 选项值不接受 null，故用空串作哨兵） */
const dataSourceId = ref<ApiId>('')
const dataSourceOptions = ref<{ label: string, value: ApiId }[]>([])
const databaseType = ref<DatabaseType>(DatabaseTypeEnum.MySql)
const tableOptions = ref<{ label: string, value: string }[]>([])
/** 多选：一次导入一批表，命名全部由后端推断（零配置） */
const selectedTables = ref<string[]>([])
const importResult = ref<CodeGenImportTablesResultDto | null>(null)

const selectedCount = computed(() => selectedTables.value.length)

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
  queryKeyword.value = ''
  selectedTables.value = []
  tableOptions.value = []
  importResult.value = null
}

/**
 * 加载可选数据源。选项文案带库类型与库名，避免只显示名称时要回想「这个数据源指向哪」。
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

function onDataSourceChange() {
  selectedTables.value = []
  void loadTables()
}

async function handleImport() {
  if (selectedTables.value.length === 0) {
    message.warning(t('develop.code_gen.import.validate_select_table'))
    return
  }
  submitLoading.value = true
  importResult.value = null
  try {
    const result = await codeGenerationApi.importTables({
      dataSourceId: dataSourceId.value || undefined,
      databaseType: databaseType.value,
      tableNames: selectedTables.value,
    })
    importResult.value = result
    emit('imported')

    if (result.failed.length === 0) {
      message.success(t('develop.code_gen.import.import_batch_success', { count: result.succeeded.length }))
      emit('update:show', false)
    }
    else {
      // 有失败：保留弹窗展示明细，成功的已刷新到列表
      message.warning(t('develop.code_gen.import.import_batch_partial', {
        ok: result.succeeded.length,
        fail: result.failed.length,
      }))
    }
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
    :save-text="t('develop.code_gen.import.action_import')"
    @update:show="emit('update:show', $event)"
    @save="handleImport"
  >
    <div class="import-filters">
      <NSelect
        v-model:value="dataSourceId"
        class="import-filters__item"
        filterable
        :options="dataSourceOptions"
        :placeholder="t('develop.code_gen.import.data_source_placeholder')"
        @update:value="onDataSourceChange"
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
      <NFormItem :label="t('develop.code_gen.import.form_select_tables', { count: selectedCount })">
        <NSelect
          v-model:value="selectedTables"
          clearable
          filterable
          multiple
          :loading="tableLoading"
          :max-tag-count="6"
          :options="tableOptions"
          :placeholder="t('develop.code_gen.import.select_tables_placeholder')"
        />
      </NFormItem>
    </NForm>

    <div v-if="importResult && importResult.failed.length > 0" class="import-result">
      <div class="import-result__title">
        {{ t('develop.code_gen.import.result_failed_title') }}
      </div>
      <div v-for="item in importResult.failed" :key="item.tableName" class="import-result__row">
        <NTag :bordered="false" size="small" type="error">
          {{ item.tableName }}
        </NTag>
        <span class="import-result__reason">{{ item.reason }}</span>
      </div>
    </div>
  </XEditModal>
</template>

<style scoped>
.import-filters {
  display: flex;
  gap: 8px;
  margin-bottom: 12px;
}

.import-filters__item {
  flex: 1;
}

.import-result {
  margin-top: 12px;
  padding-top: 8px;
  border-top: 1px solid hsl(var(--border));
}

.import-result__title {
  margin-bottom: 6px;
  font-size: 12px;
  font-weight: 600;
  color: var(--text-secondary);
}

.import-result__row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 2px 0;
}

.import-result__reason {
  font-size: 12px;
  color: var(--text-secondary);
}
</style>
