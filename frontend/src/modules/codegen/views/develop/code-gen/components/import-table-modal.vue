<script setup lang="ts">
import type {
  CodeGenImportTablesResultDto,
  DatabaseType,
} from '../../../../api'
import type {
  ApiId,
} from '@/api'
import { XhButton, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XEditModal, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import {
  codeGenDataSourceApi,
  codeGenerationApi,
  DATABASE_TYPE_OPTIONS,
  DatabaseType as DatabaseTypeEnum,
} from '../../../../api'

defineOptions({ name: 'CodeGenImportTableModal' })

const props = defineProps<{
  show: boolean
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'imported': []
}>()

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

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
    const dataSources = await codeGenDataSourceApi.options()
    dataSourceOptions.value = [
      { label: t('develop.code_gen.import.data_source_primary'), value: '' },
      ...(dataSources ?? []).map(item => ({
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
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.import.load_tables_failed'))
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
    toast.warning(t('develop.code_gen.import.validate_select_table'))
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
      toast.success(t('develop.code_gen.import.import_batch_success', { count: result.succeeded.length }))
      emit('update:show', false)
    }
    else {
      // 有失败：保留弹窗展示明细，成功的已刷新到列表
      toast.warning(t('develop.code_gen.import.import_batch_partial', {
        ok: result.succeeded.length,
        fail: result.failed.length,
      }))
    }
  }
  catch (error) {
    toast.error((error as Error)?.message || t('develop.code_gen.import.import_failed'))
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
    :form-id="editFormId"
    @update:show="emit('update:show', $event)"
  >
    <div class="import-filters">
      <XSelect
        v-model:value="dataSourceId"
        class="import-filters__item"
        :options="dataSourceOptions"
        :placeholder="t('develop.code_gen.import.data_source_placeholder')"
        @update:value="onDataSourceChange"
      />
      <XInput
        v-model:value="queryKeyword"
        class="import-filters__item"
        clearable
        :placeholder="t('develop.code_gen.import.keyword_placeholder')"
        @keyup.enter="loadTables"
      />
      <XhButton :loading="tableLoading" tone="brand" @click="loadTables">
        <span><Icon icon="lucide:search" /></span>
        {{ t('common.actions.search') }}
      </XhButton>
    </div>
    <XhFormRoot
      :id="editFormId"
      validate-on="blur"
      class="xh-edit-form-grid"
      @submit="handleImport"
    >
      <XhFieldRoot>
        <XhFieldLabel>{{ t('develop.code_gen.import.form_database_type') }}</XhFieldLabel>
        <XhFieldControl>
          <XSelect v-model:value="databaseType" :options="DATABASE_TYPE_OPTIONS" />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
      <XhFieldRoot>
        <XhFieldLabel>{{ t('develop.code_gen.import.form_select_tables', { count: selectedCount }) }}</XhFieldLabel>
        <XhFieldControl>
          <XSelect
            v-model:value="selectedTables"
            clearable
            multiple
            :max-tag-count="6"
            :options="tableOptions"
            :placeholder="t('develop.code_gen.import.select_tables_placeholder')"
          />
        </XhFieldControl>
        <XhFieldErrorText />
      </XhFieldRoot>
    </XhFormRoot>

    <div v-if="importResult && importResult.failed.length > 0" class="import-result">
      <div class="import-result__title">
        {{ t('develop.code_gen.import.result_failed_title') }}
      </div>
      <div v-for="item in importResult.failed" :key="item.tableName" class="import-result__row">
        <XhTagRoot variant="subtle" size="sm" tone="danger">
          <XhTagLabel>
            {{ item.tableName }}
          </XhTagLabel>
        </XhTagRoot>
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
