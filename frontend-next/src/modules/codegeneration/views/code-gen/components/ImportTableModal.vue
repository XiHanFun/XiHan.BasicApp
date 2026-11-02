<!--
  代码生成 / 导入库表弹窗（照 4.x import-table-modal）。
  数据源为空即主库；数据库类型按来源取（主库的连接或数据源记录），不再让人选（4.x 让人选，选错了类型映射就错）。
  导入按表隔离：失败的表留在弹窗里列出原因，成功的已刷新到列表。
-->
<script setup lang="ts">
import type { ResponseOf } from '~/contracts'
import { isComposingEvent } from '@xihan-ui/core'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhFieldControl, XhFieldLabel, XhFieldRoot, XhFormRoot, XhInputGroupRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XEditModal, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { codegenDatasources, codegenTables } from '~/contracts'
import { databaseTypeLabel } from './use-datasources'

defineOptions({ name: 'CodegenImportTableModal' })

const props = defineProps<{
  show: boolean
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'imported': []
}>()

const { t } = useI18n()

/** 弹窗的导入钮靠这个 id 关联到表单 */
const editFormId = useId()

/** 主库在下拉里的哨兵值：选项值不收 null */
const PRIMARY = ''

const tableLoading = ref(false)
const submitLoading = ref(false)
const queryKeyword = ref('')
const dataSourceId = ref<string>(PRIMARY)
const dataSourceOptions = ref<{ label: string, value: string }[]>([])
const tableOptions = ref<{ label: string, value: string }[]>([])
const selectedTables = ref<string[]>([])
const importResult = ref<ResponseOf<'codegen-tables.import'> | null>(null)

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

/** 可选数据源：文案带库类型与库名，免得只看名称时要回想它指向哪 */
async function loadDataSources() {
  const primary = { label: t('codegeneration.code_gen.import.data_source_primary'), value: PRIMARY }
  try {
    const { items } = await codegenDatasources.options({})
    dataSourceOptions.value = [primary, ...items.map(item => ({
      label: `${item.sourceName}（${databaseTypeLabel(item.databaseType)} · ${item.databaseName}）`,
      value: item.id,
    }))]
  }
  catch {
    dataSourceOptions.value = [primary]
  }
}

async function loadTables() {
  tableLoading.value = true
  try {
    // 照 4.x 一次列全：取一页最大的 500 张，关键字再收窄
    const page = await codegenTables.dbTables({
      dataSourceId: dataSourceId.value || null,
      keyword: queryKeyword.value.trim() || null,
      page: { index: 1, size: 500 },
    })
    tableOptions.value = page.items.map(item => ({ label: item.comment ? `${item.name}（${item.comment}）` : item.name, value: item.name }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.import.load_tables_failed'))
    tableOptions.value = []
  }
  finally {
    tableLoading.value = false
  }
}

/** 关键字是筛选条件：回车只查表，不隐式提交外层的导入表单；输入法组合中的回车原样放行 */
function blockImplicitSubmit(event: KeyboardEvent) {
  if (!isComposingEvent(event))
    event.preventDefault()
}

function onDataSourceChange() {
  selectedTables.value = []
  void loadTables()
}

async function handleImport() {
  if (selectedTables.value.length === 0) {
    toast.warning(t('codegeneration.code_gen.import.validate_select_table'))
    return
  }
  submitLoading.value = true
  importResult.value = null
  try {
    const result = await codegenTables.import({ dataSourceId: dataSourceId.value || null, tableNames: selectedTables.value })
    importResult.value = result
    emit('imported')
    if (result.failed.length === 0) {
      toast.success(t('codegeneration.code_gen.import.import_batch_success', { count: result.succeeded.length }))
      emit('update:show', false)
    }
    else {
      // 有失败：留着弹窗列出原因，成功的已刷新到列表
      toast.warning(t('codegeneration.code_gen.import.import_batch_partial', { ok: result.succeeded.length, fail: result.failed.length }))
    }
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.import.import_failed'))
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="t('codegeneration.code_gen.import.title')"
    :loading="submitLoading"
    :save-text="t('codegeneration.code_gen.import.action_import')"
    :form-id="editFormId"
    @update:show="emit('update:show', $event)"
  >
    <XhFormRoot :id="editFormId" class="xh-edit-form-grid" novalidate @submit="handleImport">
      <!-- 数据源与关键字是筛选条件不是提交字段，但排进同一张网格、各带标签 -->
      <XhFieldRoot>
        <XhFieldLabel>{{ t('codegeneration.code_gen.import.form_data_source') }}</XhFieldLabel>
        <XhFieldControl>
          <XSelect
            v-model:value="dataSourceId"
            name="dataSourceId"
            :options="dataSourceOptions"
            :placeholder="t('codegeneration.code_gen.import.data_source_placeholder')"
            @update:value="onDataSourceChange"
          />
        </XhFieldControl>
      </XhFieldRoot>
      <XhFieldRoot>
        <XhFieldLabel>{{ t('codegeneration.code_gen.import.form_keyword') }}</XhFieldLabel>
        <!-- 查询钮贴在关键字输入框末端；字段的接线交给输入框（关掉 asChild，label 的 for 才落在输入框上） -->
        <XhFieldControl v-slot="wiring" :as-child="false">
          <XhInputGroupRoot class="import-keyword-group">
            <XInput
              v-bind="wiring"
              v-model:value="queryKeyword"
              name="keyword"
              clearable
              :placeholder="t('codegeneration.code_gen.import.keyword_placeholder')"
              @enter="loadTables"
              @keydown.enter="blockImplicitSubmit"
            />
            <XhButton variant="subtle" size="sm" :loading="tableLoading" tone="brand" @click="loadTables">
              <XhButtonIndicator />
              <span><Icon icon="lucide:search" /></span>
              <XhButtonLabel>{{ t('common.actions.search') }}</XhButtonLabel>
            </XhButton>
          </XhInputGroupRoot>
        </XhFieldControl>
      </XhFieldRoot>
      <XhFieldRoot class="xh-span-2">
        <XhFieldLabel>{{ t('codegeneration.code_gen.import.form_select_tables', { count: selectedCount }) }}</XhFieldLabel>
        <XhFieldControl>
          <XSelect
            v-model:value="selectedTables"
            name="tableNames"
            clearable
            multiple
            :max-tag-count="6"
            :options="tableOptions"
            :placeholder="t('codegeneration.code_gen.import.select_tables_placeholder')"
          />
        </XhFieldControl>
      </XhFieldRoot>
    </XhFormRoot>

    <div v-if="importResult && importResult.failed.length > 0" class="import-result">
      <div class="import-result__title">
        {{ t('codegeneration.code_gen.import.result_failed_title') }}
      </div>
      <div v-for="item in importResult.failed" :key="item.tableName" class="import-result__row">
        <XhTagRoot variant="subtle" size="sm" tone="danger">
          <XhTagLabel>{{ item.tableName }}</XhTagLabel>
        </XhTagRoot>
        <span class="import-result__reason">{{ item.message }}</span>
      </div>
    </div>
  </XEditModal>
</template>

<style scoped>
/* 输入组铺满字段格；输入框占满余下的宽、钮不缩 */
.import-keyword-group {
  inline-size: 100%;
}

.import-keyword-group > :first-child {
  flex: 1 1 auto;
  min-inline-size: 0;
}

.import-keyword-group > :last-child {
  flex: none;
}

.import-result {
  margin-top: var(--xh-space-3);
  padding-top: var(--xh-space-2);
  border-top: var(--xh-stroke-thin) solid var(--xh-border-subtle);
}

.import-result__title {
  margin-bottom: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  font-weight: var(--xh-font-weight-semibold);
  color: var(--xh-fg-muted);
}

.import-result__row {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  padding: var(--xh-space-1) 0;
}

.import-result__reason {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}
</style>
