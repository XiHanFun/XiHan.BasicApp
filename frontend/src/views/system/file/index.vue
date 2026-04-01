<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysFile } from '@/api'
import {
  NButton,
  NPopconfirm,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { fileApi } from '@/api'
import { FILE_STATUS_OPTIONS, FILE_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, formatFileSize, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemFilePage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  fileType: undefined as number | undefined,
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return fileApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    fileType: queryParams.fileType,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysFile>({
  id: 'sys_file',
  name: '文件管理',
  columns: [
    { type: 'seq', title: '序号', width: 60, fixed: 'left' },
    { field: 'originalName', title: '原始文件名', minWidth: 220, showOverflow: 'tooltip', sortable: true },
    { field: 'fileName', title: '存储文件名', minWidth: 180, showOverflow: 'tooltip' },
    { field: 'fileExtension', title: '扩展名', width: 80 },
    {
      field: 'fileType',
      title: '类型',
      width: 80,
      formatter: ({ cellValue }) => getOptionLabel(FILE_TYPE_OPTIONS, cellValue),
    },
    { field: 'mimeType', title: 'MIME', minWidth: 140, showOverflow: 'tooltip' },
    {
      field: 'fileSize',
      title: '文件大小',
      width: 110,
      formatter: ({ cellValue }) => formatFileSize(cellValue ?? 0),
      sortable: true,
    },
    {
      field: 'isPublic',
      title: '公开',
      width: 70,
      slots: { default: 'col_public' },
    },
    {
      field: 'status',
      title: '状态',
      width: 90,
      formatter: ({ cellValue }) => getOptionLabel(FILE_STATUS_OPTIONS, cellValue),
    },
    { field: 'createTime', title: '创建时间', width: 170, formatter: ({ cellValue }) => formatDate(cellValue), sortable: true },
    {
      title: '操作',
      width: 80,
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
  queryParams.fileType = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

async function handleDelete(id: string) {
  try {
    await fileApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}
</script>

<template>
  <div class="h-full flex flex-col gap-2 overflow-hidden p-3">
    <vxe-card style="padding: 10px 16px">
      <div class="flex items-center gap-3 flex-wrap">
        <vxe-input v-model="queryParams.keyword" placeholder="搜索文件名/MIME" clearable style="width: 260px" @keyup.enter="handleSearch" />
        <NSelect v-model:value="queryParams.fileType" :options="FILE_TYPE_OPTIONS" placeholder="文件类型" clearable style="width: 130px" />
        <NSelect v-model:value="queryParams.status" :options="FILE_STATUS_OPTIONS" placeholder="状态" clearable style="width: 130px" />
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
        <template #col_public="{ row }">
          <NTag :type="row.isPublic ? 'success' : 'warning'" size="small">
            {{ row.isPublic ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NPopconfirm @positive-click="handleDelete(row.basicId)">
            <template #trigger>
              <NButton size="small" type="error" text>
                删除
              </NButton>
            </template>
            确认删除该文件？
          </NPopconfirm>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
