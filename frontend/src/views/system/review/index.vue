<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysReview } from '~/types'
import {
  NButton,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { deleteReviewApi } from '@/api'
import { buildPageRequest, flattenPageResponse } from '@/api/helpers'
import requestClient from '@/api/request'
import { REVIEW_RESULT_OPTIONS, REVIEW_STATUS_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemReviewPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const detailVisible = ref(false)
const detailData = ref<Record<string, any>>({})

const queryParams = reactive({
  keyword: '',
  reviewStatus: undefined as number | undefined,
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return requestClient.post('/api/Review/Page', buildPageRequest({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    reviewStatus: queryParams.reviewStatus,
    status: queryParams.status,
  }, {
    keywordFields: ['ReviewCode', 'ReviewTitle', 'ReviewType'],
    filterFieldMap: { reviewStatus: 'ReviewStatus', status: 'Status' },
  })).then(flattenPageResponse)
}

const options = useVxeTable<SysReview>({
  id: 'sys_review',
  name: '审查管理',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'reviewCode', title: '审查编码', minWidth: 140, showOverflow: 'tooltip' },
    { field: 'reviewTitle', title: '审查标题', minWidth: 180, showOverflow: 'tooltip', sortable: true },
    { field: 'reviewType', title: '审查类型', minWidth: 100, showOverflow: 'tooltip' },
    {
      field: 'reviewStatus',
      title: '审查状态',
      width: 100,
      slots: { default: 'col_reviewStatus' },
    },
    {
      field: 'reviewResult',
      title: '审查结果',
      width: 100,
      formatter: ({ cellValue }) => cellValue !== undefined && cellValue !== null ? getOptionLabel(REVIEW_RESULT_OPTIONS, cellValue) : '-',
    },
    { field: 'priority', title: '优先级', width: 80 },
    { field: 'reviewLevel', title: '审查级数', width: 90 },
    { field: 'currentLevel', title: '当前级', width: 80 },
    { field: 'submitTime', title: '提交时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    {
      field: 'status',
      title: '状态',
      width: 80,
      slots: { default: 'col_status' },
    },
    {
      title: '操作',
      width: 120,
      fixed: 'right',
      slots: { default: 'col_actions' },
    },
  ],
}, {
  proxyConfig: {
    autoLoad: true,
    ajax: { query: ({ page }) => handleQueryApi(page) },
  },
})

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}
function handleReset() {
  queryParams.keyword = ''
  queryParams.reviewStatus = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

function handleDetail(row: Record<string, any>) {
  detailData.value = row
  detailVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteReviewApi(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

function getReviewStatusType(status: number) {
  const map: Record<number, 'default' | 'info' | 'success' | 'error' | 'warning'> = { 0: 'default', 1: 'info', 2: 'success', 3: 'error', 4: 'warning' }
  return map[status] ?? 'default'
}
</script>

<template>
  <div class="h-full flex flex-col gap-2 overflow-hidden p-3">
    <vxe-card style="padding: 10px 16px">
      <div class="flex items-center gap-3 flex-wrap">
        <vxe-input v-model="queryParams.keyword" placeholder="搜索编码/标题/类型" clearable style="width: 260px" @keyup.enter="handleSearch" />
        <NSelect v-model:value="queryParams.reviewStatus" :options="REVIEW_STATUS_OPTIONS" placeholder="审查状态" clearable style="width: 130px" />
        <NSelect v-model:value="queryParams.status" :options="STATUS_OPTIONS" placeholder="状态" clearable style="width: 120px" />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          重置
        </NButton>
      </div>
    </vxe-card>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #col_reviewStatus="{ row }">
          <NTag :type="getReviewStatusType(row.reviewStatus)" size="small">
            {{ getOptionLabel(REVIEW_STATUS_OPTIONS, row.reviewStatus) }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton size="small" type="primary" text @click="handleDetail(row)">
              详情
            </NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该审查？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal v-model:show="detailVisible" title="审查详情" preset="card" style="width: 700px" :auto-focus="false">
      <div class="space-y-3 text-sm">
        <div><span class="font-medium text-gray-500">审查编码：</span>{{ detailData.reviewCode }}</div>
        <div><span class="font-medium text-gray-500">审查标题：</span>{{ detailData.reviewTitle }}</div>
        <div><span class="font-medium text-gray-500">审查类型：</span>{{ detailData.reviewType }}</div>
        <div><span class="font-medium text-gray-500">提交时间：</span>{{ formatDate(detailData.submitTime) }}</div>
        <div v-if="detailData.reviewContent">
          <span class="font-medium text-gray-500">审查内容：</span>
          <pre class="mt-1 max-h-60 overflow-auto rounded bg-gray-100 p-3 text-xs dark:bg-gray-800">{{ detailData.reviewContent }}</pre>
        </div>
      </div>
    </NModal>
  </div>
</template>
