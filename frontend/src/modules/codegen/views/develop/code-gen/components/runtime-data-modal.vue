<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  DynamicRuntimeColumnDto,
  DynamicRuntimeSchemaDto,
} from '../../../../api'
import type {
  ApiId,
} from '@/api'
import {
  NDataTable,
  NEmpty,
  NModal,
  NPagination,
  NSpin,
  useMessage,
} from 'naive-ui'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  codeGenRuntimeApi,
} from '../../../../api'

defineOptions({ name: 'CodeGenRuntimeDataModal' })

const props = defineProps<{
  show: boolean
  tableId: ApiId | null
  tableName?: string
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

type RuntimeRow = Record<string, unknown>

const { t } = useI18n()
const message = useMessage()

const modalTitle = computed(() =>
  props.tableName
    ? `${t('develop.code_gen.runtime.title')} · ${props.tableName}`
    : t('develop.code_gen.runtime.title'),
)

const schemaLoading = ref(false)
const dataLoading = ref(false)
const schema = ref<DynamicRuntimeSchemaDto | null>(null)
const rows = ref<RuntimeRow[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)

const columns = computed<DataTableColumns<RuntimeRow>>(() => {
  const cols = schema.value?.columns ?? []
  return cols.map((column: DynamicRuntimeColumnDto) => ({
    key: column.propertyName,
    title: column.label || column.columnName,
    minWidth: 140,
    ellipsis: { tooltip: true },
    render: (row: RuntimeRow) => formatCell(row[column.propertyName]),
  }))
})

const scrollX = computed(() => Math.max(640, columns.value.length * 160))

function formatCell(value: unknown): string {
  if (value === null || value === undefined) {
    return ''
  }
  if (typeof value === 'object') {
    return JSON.stringify(value)
  }
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

async function loadSchema(tableId: ApiId) {
  schemaLoading.value = true
  try {
    schema.value = await codeGenRuntimeApi.getSchema(tableId)
    await loadData()
  }
  catch (error) {
    message.error((error as Error)?.message || t('develop.code_gen.runtime.load_schema_failed'))
    schema.value = null
    rows.value = []
    total.value = 0
  }
  finally {
    schemaLoading.value = false
  }
}

async function loadData() {
  if (!props.tableId) {
    return
  }
  dataLoading.value = true
  try {
    const result = await codeGenRuntimeApi.page({
      tableId: props.tableId,
      pageIndex: page.value,
      pageSize: pageSize.value,
    })
    rows.value = result.rows ?? []
    total.value = result.totalCount
  }
  catch (error) {
    message.error((error as Error)?.message || t('develop.code_gen.runtime.load_data_failed'))
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
  <NModal
    :auto-focus="false"
    :bordered="false"
    preset="card"
    :show="show"
    style="width: 96vw; max-width: 1200px"
    :title="modalTitle"
    @update:show="emit('update:show', $event)"
  >
    <NSpin :show="schemaLoading">
      <div class="runtime">
        <NEmpty
          v-if="!schemaLoading && (!schema || schema.columns.length === 0)"
          :description="t('develop.code_gen.runtime.empty')"
        />
        <template v-else>
          <!-- flex-height：让表体撑满容器，横向滚动条贴在表格底部而不是跟着内容高度浮在中间 -->
          <NDataTable
            class="runtime__table"
            :columns="columns"
            :data="rows"
            flex-height
            :loading="dataLoading"
            :scroll-x="scrollX"
            size="small"
          />
          <div class="runtime__foot">
            <NPagination
              v-model:page="page"
              v-model:page-size="pageSize"
              :item-count="total"
              :page-sizes="[10, 20, 50, 100]"
              show-size-picker
              @update:page="handlePageChange"
              @update:page-size="handlePageSizeChange"
            />
          </div>
        </template>
      </div>
    </NSpin>
  </NModal>
</template>

<style scoped>
/* 定高容器：flex-height 的表格要求父级高度确定，否则表体无法撑满、滚动条仍会跟着内容走 */
.runtime {
  display: flex;
  flex-direction: column;
  gap: 12px;
  height: 60vh;
  min-height: 320px;
}

.runtime__table {
  flex: 1;
  min-height: 0;
}

.runtime__foot {
  display: flex;
  flex-shrink: 0;
  justify-content: flex-end;
}
</style>
