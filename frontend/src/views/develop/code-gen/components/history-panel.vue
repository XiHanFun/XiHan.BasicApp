<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  CodeGenHistoryDetailDto,
  CodeGenHistoryListItemDto,
  GenStatus,
} from '@/api'
import {
  NButton,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NInput,
  NModal,
  NPagination,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import {
  codeGenHistoryApi,
  createPageRequest,
  GEN_STATUS_OPTIONS,
  GEN_TYPE_OPTIONS,
  GenStatus as GenStatusEnum,
} from '@/api'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'CodeGenHistoryPanel' })

const message = useMessage()

const loading = ref(false)
const list = ref<CodeGenHistoryListItemDto[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const queryParams = reactive({
  tableName: '',
  genStatus: null as GenStatus | null,
})

function formatDuration(value: string) {
  const ms = Number(value)
  if (!Number.isFinite(ms)) {
    return value
  }
  return ms >= 1000 ? `${(ms / 1000).toFixed(2)}s` : `${ms}ms`
}

function formatSize(value: string) {
  const bytes = Number(value)
  if (!Number.isFinite(bytes)) {
    return value
  }
  if (bytes >= 1024 * 1024) {
    return `${(bytes / 1024 / 1024).toFixed(2)} MB`
  }
  if (bytes >= 1024) {
    return `${(bytes / 1024).toFixed(2)} KB`
  }
  return `${bytes} B`
}

async function fetchData() {
  loading.value = true
  try {
    const result = await codeGenHistoryApi.page({
      ...createPageRequest({ page: { pageIndex: page.value, pageSize: pageSize.value } }),
      genStatus: queryParams.genStatus ?? undefined,
      tableName: queryParams.tableName?.trim() || undefined,
    })
    list.value = result.items
    total.value = result.page.totalCount
  }
  catch {
    message.error('查询生成历史失败')
    list.value = []
    total.value = 0
  }
  finally {
    loading.value = false
  }
}

function handleSearch() {
  page.value = 1
  fetchData()
}

function handlePageChange(value: number) {
  page.value = value
  fetchData()
}

function handlePageSizeChange(value: number) {
  pageSize.value = value
  page.value = 1
  fetchData()
}

function genStatusTagType(status: GenStatus) {
  if (status === GenStatusEnum.Generated) {
    return 'success'
  }
  if (status === GenStatusEnum.Failed) {
    return 'error'
  }
  return 'default'
}

const columns = computed<DataTableColumns<CodeGenHistoryListItemDto>>(() => [
  {
    key: 'tableName',
    title: '表名',
    minWidth: 150,
    ellipsis: { tooltip: true },
  },
  {
    key: 'batchNumber',
    title: '批次号',
    minWidth: 160,
    ellipsis: { tooltip: true },
  },
  {
    key: 'genStatus',
    title: '状态',
    width: 90,
    align: 'center',
    render: (row: CodeGenHistoryListItemDto) =>
      h(NTag, {
        size: 'small',
        round: true,
        bordered: false,
        type: genStatusTagType(row.genStatus),
      }, () => getOptionLabel(GEN_STATUS_OPTIONS, row.genStatus)),
  },
  {
    key: 'genType',
    title: '方式',
    width: 110,
    render: (row: CodeGenHistoryListItemDto) => getOptionLabel(GEN_TYPE_OPTIONS, row.genType),
  },
  {
    key: 'fileCount',
    title: '文件数',
    width: 80,
    align: 'center',
  },
  {
    key: 'totalSize',
    title: '总大小',
    width: 100,
    render: (row: CodeGenHistoryListItemDto) => formatSize(row.totalSize),
  },
  {
    key: 'duration',
    title: '耗时',
    width: 90,
    render: (row: CodeGenHistoryListItemDto) => formatDuration(row.duration),
  },
  {
    key: 'operatorName',
    title: '操作人',
    width: 110,
    ellipsis: { tooltip: true },
  },
  {
    key: 'genTime',
    title: '生成时间',
    minWidth: 170,
  },
  {
    key: 'actions',
    title: '操作',
    width: 80,
    align: 'center',
    render: (row: CodeGenHistoryListItemDto) =>
      h(NButton, {
        quaternary: true,
        size: 'small',
        type: 'primary',
        onClick: () => handleDetail(row),
      }, () => '详情'),
  },
])

// ── 详情弹窗 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detail = ref<CodeGenHistoryDetailDto | null>(null)

const generatedFiles = computed<string[]>(() => {
  if (!detail.value?.generatedFiles) {
    return []
  }
  try {
    const parsed = JSON.parse(detail.value.generatedFiles)
    if (Array.isArray(parsed)) {
      return parsed.map(item => (typeof item === 'string' ? item : (item?.relativePath ?? item?.fileName ?? JSON.stringify(item))))
    }
    return []
  }
  catch {
    return []
  }
})

async function handleDetail(row: CodeGenHistoryListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  detail.value = null
  try {
    detail.value = await codeGenHistoryApi.detail(row.basicId)
    if (!detail.value) {
      message.error('历史记录不存在')
    }
  }
  catch {
    message.error('加载历史详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

onMounted(fetchData)
</script>

<template>
  <div class="panel">
    <div class="panel__toolbar">
      <NInput
        v-model:value="queryParams.tableName"
        class="panel__kw"
        clearable
        placeholder="搜索表名"
        size="small"
        @clear="handleSearch"
        @keyup.enter="handleSearch"
      />
      <NSelect
        v-model:value="queryParams.genStatus"
        class="panel__filter"
        clearable
        :options="GEN_STATUS_OPTIONS"
        placeholder="生成状态"
        size="small"
        @update:value="handleSearch"
      />
      <NButton size="small" type="primary" @click="handleSearch">
        查询
      </NButton>
    </div>

    <div class="panel__body">
      <NDataTable
        class="panel__table"
        flex-height
        :columns="columns"
        :data="list"
        :loading="loading"
        :row-key="(row: CodeGenHistoryListItemDto) => row.basicId"
        :scroll-x="1200"
        size="small"
      />
    </div>

    <div class="panel__foot">
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

    <NModal
      v-model:show="detailVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 820px; max-width: 94vw"
      title="生成历史详情"
    >
      <NSpace v-if="detailLoading" justify="center">
        加载中…
      </NSpace>
      <template v-else-if="detail">
        <NDescriptions :column="2" label-placement="left" size="small">
          <NDescriptionsItem label="表名">
            {{ detail.tableName }}
          </NDescriptionsItem>
          <NDescriptionsItem label="批次号">
            {{ detail.batchNumber ?? '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="状态">
            <NTag :bordered="false" round size="small" :type="genStatusTagType(detail.genStatus)">
              {{ getOptionLabel(GEN_STATUS_OPTIONS, detail.genStatus) }}
            </NTag>
          </NDescriptionsItem>
          <NDescriptionsItem label="方式">
            {{ getOptionLabel(GEN_TYPE_OPTIONS, detail.genType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="文件数">
            {{ detail.fileCount }}
          </NDescriptionsItem>
          <NDescriptionsItem label="总大小">
            {{ formatSize(detail.totalSize) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="耗时">
            {{ formatDuration(detail.duration) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="操作人">
            {{ detail.operatorName ?? '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="生成时间">
            {{ detail.genTime }}
          </NDescriptionsItem>
          <NDescriptionsItem label="操作 IP">
            {{ detail.operatorIp ?? '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="detail.genPath" label="生成路径" :span="2">
            {{ detail.genPath }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="detail.downloadPath" label="下载路径" :span="2">
            {{ detail.downloadPath }}
          </NDescriptionsItem>
        </NDescriptions>

        <div v-if="detail.errorMessage" class="detail-section detail-section--error">
          <div class="detail-section__title">
            错误信息
          </div>
          <pre class="detail-section__pre">{{ detail.errorMessage }}</pre>
        </div>

        <div v-if="generatedFiles.length" class="detail-section">
          <div class="detail-section__title">
            产物清单（{{ generatedFiles.length }}）
          </div>
          <ul class="detail-files">
            <li v-for="file in generatedFiles" :key="file" class="detail-files__item">
              {{ file }}
            </li>
          </ul>
        </div>
      </template>
      <NSpace v-else justify="center">
        无数据
      </NSpace>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="detailVisible = false">
            关闭
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.panel__toolbar {
  display: flex;
  flex-shrink: 0;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  padding-bottom: 10px;
}

.panel__kw {
  width: 220px;
}

.panel__filter {
  width: 130px;
  flex-shrink: 0;
}

.panel__body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.panel__table {
  flex: 1;
  min-height: 0;
}

.panel__foot {
  display: flex;
  flex-shrink: 0;
  justify-content: flex-end;
  padding-top: 10px;
}

.detail-section {
  margin-top: 16px;
}

.detail-section__title {
  margin-bottom: 8px;
  font-size: 13px;
  font-weight: 600;
}

.detail-section__pre {
  margin: 0;
  padding: 10px 12px;
  max-height: 200px;
  overflow: auto;
  background: hsl(var(--muted));
  border-radius: 8px;
  font-size: 12px;
  white-space: pre-wrap;
  word-break: break-all;
}

.detail-section--error .detail-section__pre {
  color: hsl(var(--destructive, 0 84% 60%));
}

.detail-files {
  margin: 0;
  padding: 10px 12px 10px 28px;
  max-height: 240px;
  overflow: auto;
  background: hsl(var(--muted));
  border-radius: 8px;
}

.detail-files__item {
  font-size: 12px;
  line-height: 1.8;
  word-break: break-all;
}
</style>
