<!--
  代码生成 / 运行时数据（照 4.x runtime-data-modal）：按表配置只读查看表里的数据，不生成代码
  接口取自后端契约（codegen-runtime.get-schema / list）：列取列配置，数据行的键是列配置的属性名
  与 4.x 不同：按表配置的数据源读（4.x 一律查主库）；行键按页内序号给（4.x 落到缺省的 basicId，行里没有这一列）
-->
<script setup lang="ts">
import type { XDataTableColumn } from '~/components'
import type { ResponseOf } from '~/contracts'
import { XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, shallowRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XDataTable } from '~/components'
import { toast } from '~/composables'
import { codegenRuntime } from '~/contracts'

defineOptions({ name: 'CodegenRuntimeDataModal' })

const props = defineProps<{
  show: boolean
  tableId: null | string
  tableName?: string
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

type RuntimeSchema = ResponseOf<'codegen-runtime.get-schema'>
type RuntimeRow = Record<string, unknown>

const { t } = useI18n()

const modalTitle = computed(() =>
  props.tableName
    ? `${t('codegeneration.code_gen.runtime.title')} · ${props.tableName}`
    : t('codegeneration.code_gen.runtime.title'),
)

const schemaLoading = ref(false)
const dataLoading = ref(false)
const schema = ref<null | RuntimeSchema>(null)
/** 只读的数据行不做深层响应：行键按行对象本身记 */
const rows = shallowRef<RuntimeRow[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
/** 行键：表没有统一的主键列，按页内序号给 */
const rowKeys = new WeakMap<RuntimeRow, string>()

const columns = computed<XDataTableColumn<RuntimeRow>[]>(() =>
  (schema.value?.columns ?? []).map(column => ({
    key: column.propertyName,
    title: column.label || column.columnName,
    minWidth: 140,
    ellipsis: true,
    render: (row: RuntimeRow) => formatCell(row[column.propertyName]),
  })),
)

function formatCell(value: unknown): string {
  if (value === null || value === undefined)
    return ''
  if (typeof value === 'object')
    return JSON.stringify(value)
  return String(value)
}

watch(
  () => [props.show, props.tableId] as const,
  ([visible, tableId]) => {
    if (visible && tableId) {
      page.value = 1
      void loadSchema(tableId)
    }
    else if (!visible) {
      reset()
    }
  },
)

function reset() {
  schema.value = null
  rows.value = []
  total.value = 0
  page.value = 1
}

async function loadSchema(tableId: string) {
  schemaLoading.value = true
  try {
    schema.value = await codegenRuntime.getSchema({ id: tableId })
    await loadData()
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.runtime.load_schema_failed'))
    schema.value = null
    rows.value = []
    total.value = 0
  }
  finally {
    schemaLoading.value = false
  }
}

async function loadData() {
  if (!props.tableId)
    return
  dataLoading.value = true
  try {
    const result = await codegenRuntime.list({
      id: props.tableId,
      page: { index: page.value, size: pageSize.value },
    })
    result.items.forEach((row, index) => rowKeys.set(row, String(index)))
    rows.value = result.items
    total.value = result.total
  }
  catch (error) {
    toast.danger((error as Error).message || t('codegeneration.code_gen.runtime.load_data_failed'))
    rows.value = []
    total.value = 0
  }
  finally {
    dataLoading.value = false
  }
}

function handlePageChange(value: number) {
  page.value = value
  void loadData()
}

function handlePageSizeChange(value: number) {
  pageSize.value = value
  page.value = 1
  void loadData()
}
</script>

<template>
  <XhDialogRoot :open="show" @update:open="(open: boolean) => emit('update:show', open)">
    <XhDialogContent style="--xh-dialog-max-w: min(96vw, 75rem)">
      <XhDialogTitle>{{ modalTitle }}</XhDialogTitle>
      <XhDialogCloseTrigger />
      <div class="xh-loading-stage" :class="{ 'is-loading': schemaLoading }">
        <div class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <div class="runtime">
          <XhEmptyStateRoot v-if="!schemaLoading && (!schema || schema.columns.length === 0)">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('codegeneration.code_gen.runtime.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <!-- 表格撑满定高容器（max-height 100% 撤掉皮肤 24rem 的缺省上限），横向滚动条贴在表格底部，
               分页底栏与列表页同一副 -->
          <XDataTable
            v-else
            class="runtime__table"
            :columns="columns"
            :data="rows"
            :row-key="(row: RuntimeRow) => rowKeys.get(row) ?? ''"
            :loading="dataLoading"
            max-height="100%"
            :pagination="{
              page,
              pageSize,
              itemCount: total,
              onUpdatePage: handlePageChange,
              onUpdatePageSize: handlePageSizeChange,
            }"
          />
        </div>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
/* 定高容器：flex-height 的表格要求父级高度确定，否则表体无法撑满、滚动条仍会跟着内容走 */
.runtime {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
  height: 60vh;
  min-height: 20rem;
}

.runtime__table {
  flex: 1;
  min-height: 0;
}
</style>
