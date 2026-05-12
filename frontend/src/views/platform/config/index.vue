<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ConfigListItemDto } from '@/api'
import {
  NButton,
  NIcon,
  NSelect,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import {
  ConfigDataType,
  configManagementApi,
  ConfigType,
  createPageRequest,
  EnableStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { CONFIG_DATA_TYPE_OPTIONS, CONFIG_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformConfigPage' })

interface ConfigGridResult {
  items: ConfigListItemDto[]
  total: number
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ConfigListItemDto>>()

const queryParams = reactive({
  configType: undefined as ConfigType | undefined,
  dataType: undefined as ConfigDataType | undefined,
  isGlobal: undefined as number | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
})

const statusOptions = STATUS_OPTIONS

const configTypeOptions = CONFIG_TYPE_OPTIONS

const dataTypeOptions = CONFIG_DATA_TYPE_OPTIONS

const globalOptions = [
  { label: '全局', value: 1 },
  { label: '非全局', value: 0 },
]

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<ConfigGridResult> {
  return configManagementApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      configType: queryParams.configType,
      dataType: queryParams.dataType,
      isGlobal: queryParams.isGlobal === undefined ? undefined : Boolean(queryParams.isGlobal),
      keyword: queryParams.keyword?.trim() || undefined,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询配置失败')
      return { items: [], total: 0 }
    })
}

const tableOptions = useVxeTable<ConfigListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'configName', minWidth: 160, showOverflow: 'tooltip', sortable: true, title: '配置名称' },
      { field: 'configKey', minWidth: 180, showOverflow: 'tooltip', title: '配置键' },
      { field: 'configGroup', minWidth: 100, showOverflow: 'tooltip', title: '分组' },
      {
        field: 'configType',
        formatter: ({ cellValue }) => getOptionLabel(configTypeOptions, cellValue),
        title: '配置类型',
        width: 100,
      },
      {
        field: 'dataType',
        formatter: ({ cellValue }) => getOptionLabel(dataTypeOptions, cellValue),
        title: '数据类型',
        width: 90,
      },
      {
        field: 'isGlobal',
        slots: { default: 'col_global' },
        title: '全局',
        width: 70,
      },
      {
        field: 'isBuiltIn',
        slots: { default: 'col_builtin' },
        title: '内置',
        width: 70,
      },
      {
        field: 'isEncrypted',
        slots: { default: 'col_encrypted' },
        title: '加密',
        width: 70,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 80,
      },
      { field: 'sort', sortable: true, title: '排序', width: 70 },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
    ],
    id: 'sys_config',
    name: '配置管理',
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

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.configType = undefined
  queryParams.dataType = undefined
  queryParams.isGlobal = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索配置名称/键/分组"
          style="width: 250px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.configType"
          :options="configTypeOptions"
          clearable
          placeholder="配置类型"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.dataType"
          :options="dataTypeOptions"
          clearable
          placeholder="数据类型"
          style="width: 100px"
        />
        <NSelect
          v-model:value="queryParams.isGlobal"
          :options="globalOptions"
          clearable
          placeholder="全局"
          style="width: 90px"
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
        <template #col_global="{ row }">
          <NTag :type="row.isGlobal ? 'info' : 'default'" round size="small">
            {{ row.isGlobal ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_builtin="{ row }">
          <NTag :type="row.isBuiltIn ? 'warning' : 'default'" round size="small">
            {{ row.isBuiltIn ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_encrypted="{ row }">
          <NTag :type="row.isEncrypted ? 'error' : 'default'" round size="small">
            {{ row.isEncrypted ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ getOptionLabel(statusOptions, row.status) }}
          </NTag>
        </template>
      </vxe-grid>
    </vxe-card>
  </div>
</template>
