<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { DictItemListItemDto, DictListItemDto } from '@/api'
import {
  NButton,
  NDrawer,
  NDrawerContent,
  NIcon,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, dictApi, EnableStatus } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemDictPage' })

interface DictGridResult {
  items: DictListItemDto[]
  total: number
}

interface DictItemGridResult {
  items: DictItemListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<DictListItemDto>>()
const xItemGrid = ref<VxeGridInstance<DictItemListItemDto>>()

const drawerVisible = ref(false)
const currentDict = ref<DictListItemDto | null>(null)

const queryParams = reactive({
  isBuiltIn: undefined as number | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
})

const itemQueryParams = reactive({
  keyword: '',
})

const statusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

const builtInOptions = [
  { label: '内置', value: 1 },
  { label: '非内置', value: 0 },
]

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<DictGridResult> {
  return dictApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      isBuiltIn: queryParams.isBuiltIn === undefined ? undefined : Boolean(queryParams.isBuiltIn),
      keyword: queryParams.keyword?.trim() || undefined,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询字典失败')
      return { items: [], total: 0 }
    })
}

function handleItemQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<DictItemGridResult> {
  if (!currentDict.value) return Promise.resolve({ items: [], total: 0 })
  return dictApi
    .itemPage({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      dictId: currentDict.value.basicId,
      keyword: itemQueryParams.keyword?.trim() || undefined,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询字典项失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<DictListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'dictName', minWidth: 140, showOverflow: 'tooltip', sortable: true, title: '字典名称' },
      { field: 'dictCode', minWidth: 140, showOverflow: 'tooltip', title: '字典编码' },
      { field: 'dictType', minWidth: 120, showOverflow: 'tooltip', title: '字典类型' },
      {
        field: 'isBuiltIn',
        slots: { default: 'col_builtin' },
        title: '内置',
        width: 70,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 80,
      },
      { field: 'sort', sortable: true, title: '排序', width: 80 },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 80,
      },
    ],
    id: 'sys_dict',
    name: '字典管理',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

const itemTableOptions = useVxeTable<DictItemListItemDto>(
  {
    columns: [
      { title: '序号', type: 'seq', width: 60 },
      { field: 'itemName', minWidth: 130, showOverflow: 'tooltip', sortable: true, title: '字典项名称' },
      { field: 'itemCode', minWidth: 130, showOverflow: 'tooltip', title: '编码' },
      { field: 'itemValue', minWidth: 100, showOverflow: 'tooltip', title: '值' },
      {
        field: 'isDefault',
        slots: { default: 'col_item_default' },
        title: '默认',
        width: 70,
      },
      {
        field: 'status',
        slots: { default: 'col_item_status' },
        title: '状态',
        width: 80,
      },
      { field: 'sort', title: '排序', width: 70 },
    ],
    id: 'sys_dict_item',
    name: '字典项列表',
  },
  {
    proxyConfig: {
      autoLoad: false,
      ajax: {
        query: ({ page }) => handleItemQueryApi(page),
      },
    },
  },
)

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.isBuiltIn = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

function handleViewItems(row: DictListItemDto) {
  currentDict.value = row
  drawerVisible.value = true
  setTimeout(() => {
    xItemGrid.value?.commitProxy('reload')
  }, 200)
}

function handleItemSearch() {
  xItemGrid.value?.commitProxy('reload')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索字典名称/编码/类型"
          style="width: 250px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.isBuiltIn"
          :options="builtInOptions"
          clearable
          placeholder="是否内置"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="statusOptions"
          clearable
          placeholder="状态"
          style="width: 100px"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #col_builtin="{ row }">
          <NTag :type="row.isBuiltIn ? 'warning' : 'default'" round size="small">
            {{ row.isBuiltIn ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NButton aria-label="查看字典项" circle quaternary size="small" type="primary" @click="handleViewItems(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:list" /></NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="drawerVisible" :width="680">
      <NDrawerContent :title="`字典项 - ${currentDict?.dictName ?? ''}`" closable>
        <NSpace class="mb-3" vertical>
          <div class="flex gap-2 items-center">
            <vxe-input
              v-model="itemQueryParams.keyword"
              clearable
              placeholder="搜索字典项"
              style="width: 220px"
              @keyup.enter="handleItemSearch"
            />
            <NButton size="small" type="primary" @click="handleItemSearch">
              查询
            </NButton>
          </div>
        </NSpace>
        <vxe-grid ref="xItemGrid" v-bind="itemTableOptions">
          <template #col_item_default="{ row }">
            <NTag :type="row.isDefault ? 'info' : 'default'" round size="small">
              {{ row.isDefault ? '是' : '否' }}
            </NTag>
          </template>
          <template #col_item_status="{ row }">
            <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
              {{ getOptionLabel(statusOptions, row.status) }}
            </NTag>
          </template>
        </vxe-grid>
      </NDrawerContent>
    </NDrawer>
  </div>
</template>
