<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  ConfigCreateDto,
  ConfigDetailDto,
  ConfigListItemDto,
  ConfigUpdateDto,
} from '@/api'
import {
  NButton,
  NCard,
  NConfigProvider,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPagination,
  NPopconfirm,
  NScrollbar,
  NSelect,
  NSkeleton,
  NSpace,
  NSpin,
  NSwitch,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import {
  ConfigDataType,
  configManagementApi,
  ConfigType,
  createPageRequest,
  EnableStatus,
} from '@/api'
import { Icon } from '~/components'
import { CONFIG_DATA_TYPE_OPTIONS, CONFIG_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformConfigPage' })

interface ConfigFormModel {
  basicId?: string
  configDescription?: string | null
  configGroup?: string | null
  configKey: string
  configName: string
  configType: ConfigType
  configValue?: string | null
  dataType: ConfigDataType
  defaultValue?: string | null
  isBuiltIn: boolean
  isEncrypted: boolean
  isGlobal: boolean
  remark?: string | null
  sort: number
  status: EnableStatus
}

const message = useMessage()
const tableLoading = ref(false)
const dataList = ref<ConfigListItemDto[]>([])
const totalCount = ref(0)
const currentPage = ref(1)
const pageSize = ref(20)

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

const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<ConfigDetailDto | null>(null)
const configForm = ref<ConfigFormModel>(createDefaultConfigForm())

const modalTitle = computed(() => (configForm.value.basicId ? '编辑配置' : '新增配置'))

function createDefaultConfigForm(): ConfigFormModel {
  return {
    configDescription: null,
    configGroup: null,
    configKey: '',
    configName: '',
    configType: ConfigType.Application,
    configValue: null,
    dataType: ConfigDataType.String,
    defaultValue: null,
    isBuiltIn: false,
    isEncrypted: false,
    isGlobal: false,
    remark: null,
    sort: 100,
    status: EnableStatus.Enabled,
  }
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }
  return value ? '是' : '否'
}

function canMaintainConfig(row: ConfigListItemDto) {
  return !row.isBuiltIn
}

async function fetchData() {
  tableLoading.value = true
  try {
    const result = await configManagementApi.page({
      ...createPageRequest({
        page: {
          pageIndex: currentPage.value,
          pageSize: pageSize.value,
        },
      }),
      configType: queryParams.configType,
      dataType: queryParams.dataType,
      isGlobal: queryParams.isGlobal === undefined ? undefined : Boolean(queryParams.isGlobal),
      keyword: queryParams.keyword?.trim() || undefined,
      status: queryParams.status,
    })
    dataList.value = result.items
    totalCount.value = result.page.totalCount
  }
  catch {
    message.error('查询配置失败')
    dataList.value = []
    totalCount.value = 0
  }
  finally {
    tableLoading.value = false
  }
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

function handlePageChange(page: number) {
  currentPage.value = page
  fetchData()
}

function handlePageSizeChange(size: number) {
  pageSize.value = size
  currentPage.value = 1
  fetchData()
}

const tableColumns = computed<DataTableColumns<ConfigListItemDto>>(() => [
  {
    key: 'configName',
    title: '配置名称',
    minWidth: 160,
    ellipsis: { tooltip: true },
    sorter: true,
  },
  {
    key: 'configKey',
    title: '配置键',
    minWidth: 180,
    ellipsis: { tooltip: true },
  },
  {
    key: 'configGroup',
    title: '分组',
    minWidth: 100,
    ellipsis: { tooltip: true },
  },
  {
    key: 'configType',
    title: '配置类型',
    width: 100,
    render(row) {
      return h('span', { style: 'font-size:13px;' }, getOptionLabel(configTypeOptions, row.configType))
    },
  },
  {
    key: 'dataType',
    title: '数据类型',
    width: 90,
    render(row) {
      return h('span', { style: 'font-size:13px;' }, getOptionLabel(dataTypeOptions, row.dataType))
    },
  },
  {
    key: 'isGlobal',
    title: '全局',
    width: 70,
    render(row) {
      return h(NTag, { type: row.isGlobal ? 'info' : 'default', round: true, size: 'small' }, () => row.isGlobal ? '是' : '否')
    },
  },
  {
    key: 'isBuiltIn',
    title: '内置',
    width: 70,
    render(row) {
      return h(NTag, { type: row.isBuiltIn ? 'warning' : 'default', round: true, size: 'small' }, () => row.isBuiltIn ? '是' : '否')
    },
  },
  {
    key: 'isEncrypted',
    title: '加密',
    width: 70,
    render(row) {
      return h(NTag, { type: row.isEncrypted ? 'error' : 'default', round: true, size: 'small' }, () => row.isEncrypted ? '是' : '否')
    },
  },
  {
    key: 'status',
    title: '状态',
    width: 80,
    render(row) {
      return h(NTag, { type: row.status === EnableStatus.Enabled ? 'success' : 'error', round: true, size: 'small' }, () => getOptionLabel(statusOptions, row.status))
    },
  },
  {
    key: 'sort',
    title: '排序',
    width: 70,
    sorter: true,
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    sorter: true,
    render(row) {
      return h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate(row.createdTime))
    },
  },
  {
    key: 'actions',
    title: '操作',
    width: 160,
    fixed: 'right',
    render(row) {
      return h(NSpace, { size: 'small' }, () => [
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, {
              ariaLabel: '查看详情',
              circle: true,
              quaternary: true,
              size: 'small',
              onClick: () => handleView(row),
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
          default: () => '查看详情',
        }),
        h(NTooltip, null, {
          trigger: () =>
            h(NButton, {
              disabled: !canMaintainConfig(row),
              ariaLabel: '编辑',
              circle: true,
              quaternary: true,
              size: 'small',
              type: 'primary',
              onClick: () => handleEdit(row),
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:pencil' })) }),
          default: () => '编辑',
        }),
        h(NPopconfirm, {
          disabled: !canMaintainConfig(row),
          onPositiveClick: () => handleToggleStatus(row),
        }, {
          trigger: () =>
            h(NButton, {
              disabled: !canMaintainConfig(row),
              ariaLabel: '停用或启用',
              circle: true,
              quaternary: true,
              size: 'small',
              type: 'warning',
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check' })) }),
          default: () => '确认更新配置状态？',
        }),
        h(NPopconfirm, {
          disabled: !canMaintainConfig(row),
          onPositiveClick: () => handleDelete(row),
        }, {
          trigger: () =>
            h(NButton, {
              disabled: !canMaintainConfig(row),
              ariaLabel: '删除',
              circle: true,
              quaternary: true,
              size: 'small',
              type: 'error',
            }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
          default: () => '确认删除该配置？',
        }),
      ])
    },
  },
])

function handleSearch() {
  currentPage.value = 1
  fetchData()
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.configType = undefined
  queryParams.dataType = undefined
  queryParams.isGlobal = undefined
  queryParams.status = undefined
  currentPage.value = 1
  fetchData()
}

function handleAdd() {
  editingStatus.value = null
  configForm.value = createDefaultConfigForm()
  modalVisible.value = true
}

function handleEdit(row: ConfigListItemDto) {
  editingStatus.value = row.status
  configForm.value = {
    basicId: row.basicId,
    configDescription: row.configDescription ?? null,
    configGroup: row.configGroup ?? null,
    configKey: row.configKey,
    configName: row.configName,
    configType: row.configType,
    configValue: null,
    dataType: row.dataType,
    defaultValue: null,
    isBuiltIn: row.isBuiltIn,
    isEncrypted: row.isEncrypted,
    isGlobal: row.isGlobal,
    remark: null,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

async function handleView(row: ConfigListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await configManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning('未查询到配置详情')
    }
  }
  catch {
    message.error('加载配置详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

function validateConfigForm() {
  if (!configForm.value.configName.trim()) {
    message.warning('请输入配置名称')
    return false
  }

  if (!configForm.value.basicId && !configForm.value.configKey.trim()) {
    message.warning('请输入配置键')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateConfigForm()) {
    return
  }

  submitLoading.value = true

  try {
    if (configForm.value.basicId) {
      const updateInput: ConfigUpdateDto = {
        basicId: configForm.value.basicId,
        configDescription: configForm.value.configDescription,
        configGroup: configForm.value.configGroup,
        configName: configForm.value.configName.trim(),
        configType: configForm.value.configType,
        configValue: configForm.value.configValue,
        dataType: configForm.value.dataType,
        defaultValue: configForm.value.defaultValue,
        isEncrypted: configForm.value.isEncrypted,
        isGlobal: configForm.value.isGlobal,
        remark: configForm.value.remark,
        sort: configForm.value.sort,
      }

      await configManagementApi.update(updateInput)
      if (editingStatus.value !== configForm.value.status) {
        await configManagementApi.updateStatus({
          basicId: configForm.value.basicId,
          remark: configForm.value.remark,
          status: configForm.value.status,
        })
      }
    }
    else {
      const createInput: ConfigCreateDto = {
        configDescription: configForm.value.configDescription,
        configGroup: configForm.value.configGroup,
        configKey: configForm.value.configKey.trim(),
        configName: configForm.value.configName.trim(),
        configType: configForm.value.configType,
        configValue: configForm.value.configValue,
        dataType: configForm.value.dataType,
        defaultValue: configForm.value.defaultValue,
        isEncrypted: configForm.value.isEncrypted,
        isGlobal: configForm.value.isGlobal,
        remark: configForm.value.remark,
        sort: configForm.value.sort,
        status: configForm.value.status,
      }

      await configManagementApi.create(createInput)
    }

    message.success('保存成功')
    modalVisible.value = false
    fetchData()
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: ConfigListItemDto) {
  await configManagementApi.delete(row.basicId)
  message.success('删除成功')
  fetchData()
}

async function handleToggleStatus(row: ConfigListItemDto) {
  await configManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用配置' : '前端启用配置',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  fetchData()
}

onMounted(() => fetchData())
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="flex-shrink:0;padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <NConfigProvider size="small" abstract>
        <div class="xh-query-panel__content">
        <NInput
          v-model:value="queryParams.keyword"
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
      </NConfigProvider>
    </div>

    <NCard content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <div style="padding:12px 16px;flex-shrink:0;">
        <NButton size="small" type="primary" @click="handleAdd">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" /></NIcon>
          </template>
          新增配置
        </NButton>
      </div>

      <NSkeleton v-if="tableLoading && dataList.length === 0" :height="48" :repeat="5" text style="padding: 16px" />
      <NDataTable
        v-else
        :columns="tableColumns"
        :data="dataList"
        :loading="tableLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row: ConfigListItemDto) => row.basicId"
        :scroll-x="2000"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />
      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">
          共 <strong>{{ totalCount }}</strong> 条，第 <strong>{{ currentPage }}</strong> / {{ totalPages }} 页
        </div>
        <NPagination
          size="small"
          :page="currentPage"
          :page-size="pageSize"
          :page-count="totalPages"
          :page-sizes="[10, 20, 50, 100]"
          show-size-picker
          @update:page="handlePageChange"
          @update:page-size="handlePageSizeChange"
        />
      </div>
    </NCard>

    <NDrawer v-model:show="detailVisible" :width="720">
      <NDrawerContent closable title="配置详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无配置详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NDescriptions :column="2" bordered size="small">
              <NDescriptionsItem label="配置名称">
                {{ currentDetail.configName }}
              </NDescriptionsItem>
              <NDescriptionsItem label="配置键">
                {{ currentDetail.configKey }}
              </NDescriptionsItem>
              <NDescriptionsItem label="配置分组">
                {{ formatNullable(currentDetail.configGroup) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="配置类型">
                {{ getOptionLabel(configTypeOptions, currentDetail.configType) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="数据类型">
                {{ getOptionLabel(dataTypeOptions, currentDetail.dataType) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="是否全局">
                {{ formatBoolean(currentDetail.isGlobal) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="是否内置">
                {{ formatBoolean(currentDetail.isBuiltIn) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="是否加密">
                {{ formatBoolean(currentDetail.isEncrypted) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="排序">
                {{ currentDetail.sort }}
              </NDescriptionsItem>
              <NDescriptionsItem label="状态">
                {{ getOptionLabel(statusOptions, currentDetail.status) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="描述">
                {{ formatNullable(currentDetail.configDescription) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="创建时间">
                {{ formatNullableDate(currentDetail.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="修改时间">
                {{ formatNullableDate(currentDetail.modifiedTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="创建人">
                {{ formatNullable(currentDetail.createdBy) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="修改人">
                {{ formatNullable(currentDetail.modifiedBy) }}
              </NDescriptionsItem>
            </NDescriptions>
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 720px; max-width: 92vw"
    >
      <NConfigProvider size="small" abstract>
        <NForm :model="configForm" size="small" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="配置名称" path="configName">
          <NInput v-model:value="configForm.configName" clearable placeholder="请输入配置名称" />
        </NFormItem>
        <NFormItem label="配置键" path="configKey">
          <NInput
            v-model:value="configForm.configKey"
            clearable
            :disabled="Boolean(configForm.basicId)"
            placeholder="如: app.site_name"
          />
        </NFormItem>
        <NFormItem label="配置分组" path="configGroup">
          <NInput v-model:value="configForm.configGroup" clearable placeholder="请输入配置分组" />
        </NFormItem>
        <NFormItem label="配置类型" path="configType">
          <NSelect v-model:value="configForm.configType" :options="configTypeOptions" />
        </NFormItem>
        <NFormItem label="数据类型" path="dataType">
          <NSelect v-model:value="configForm.dataType" :options="dataTypeOptions" />
        </NFormItem>
        <NFormItem label="配置值" path="configValue" style="grid-column: span 2">
          <NInput
            v-model:value="configForm.configValue"
            :rows="5"
            clearable
            placeholder="请输入配置值"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="默认值" path="defaultValue">
          <NInput v-model:value="configForm.defaultValue" clearable placeholder="请输入默认值" />
        </NFormItem>
        <NFormItem label="是否全局" path="isGlobal">
          <NSwitch v-model:value="configForm.isGlobal" />
        </NFormItem>
        <NFormItem label="是否内置" path="isBuiltIn">
          <NSwitch :value="configForm.isBuiltIn" disabled />
        </NFormItem>
        <NFormItem label="是否加密" path="isEncrypted">
          <NSwitch v-model:value="configForm.isEncrypted" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="configForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="configForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
        <NFormItem v-if="!configForm.basicId" label="状态" path="status">
          <NSelect v-model:value="configForm.status" :options="statusOptions" />
        </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            取消
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}
</style>
