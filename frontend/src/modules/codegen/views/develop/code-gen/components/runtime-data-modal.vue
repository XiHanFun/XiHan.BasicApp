<script setup lang="ts">
import type {
  DynamicRuntimeColumnDto,
  DynamicRuntimeSchemaDto,
} from '../../../../api'
import type {
  ApiId,
} from '@/api'
import type { XDataTableColumn } from '~/components'
import { XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XDataTable } from '~/components'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
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

const columns = computed<XDataTableColumn<RuntimeRow>[]>(() => {
  const cols = schema.value?.columns ?? []
  return cols.map((column: DynamicRuntimeColumnDto) => ({
    key: column.propertyName,
    title: column.label || column.columnName,
    minWidth: 140,
    ellipsis: true,
    render: (row: RuntimeRow) => formatCell(row[column.propertyName]),
  }))
})

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
    toast.danger((error as Error)?.message || t('develop.code_gen.runtime.load_schema_failed'))
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
    toast.danger((error as Error)?.message || t('develop.code_gen.runtime.load_data_failed'))
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
  <XhDialogRoot
    :open="show"
    @update:open="(open: boolean) => emit('update:show', open)"
  >
    <XhDialogContent style="--xh-dialog-max-w: min(96vw, 1200px)">
      <XhDialogTitle>{{ modalTitle }}</XhDialogTitle>
      <XhDialogCloseTrigger />
      <div class="xh-loading-stage" :class="{ 'is-loading': schemaLoading }">
        <div class="xh-loading-stage__veil">
          <XhSpinner />
        </div>
        <div class="runtime">
          <XhEmptyStateRoot
            v-if="!schemaLoading && (!schema || schema.columns.length === 0)"
          >
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('develop.code_gen.runtime.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <template v-else>
            <!-- 表格撑满定高容器（max-height 100% 撤掉皮肤 24rem 的缺省上限），横向滚动条贴在表格底部，
                 分页底栏与列表页同一副 -->
            <XDataTable
              class="runtime__table"
              :columns="columns"
              :data="rows"
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
          </template>
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
  gap: 12px;
  height: 60vh;
  min-height: 320px;
}

.runtime__table {
  flex: 1;
  min-height: 0;
}
</style>
