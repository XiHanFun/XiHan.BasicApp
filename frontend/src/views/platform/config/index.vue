<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ConfigCreateDto,
  ConfigDetailDto,
  ConfigListItemDto,
  ConfigUpdateDto,
} from '@/api'
import {
  NButton,
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
  NPopconfirm,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
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
    configType: ConfigType.System,
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
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 160,
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
    xGrid.value?.commitProxy('query')
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
  xGrid.value?.commitProxy('query')
}

async function handleToggleStatus(row: ConfigListItemDto) {
  await configManagementApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用配置' : '前端启用配置',
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  xGrid.value?.commitProxy('query')
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
        <template #toolbar_buttons>
          <NButton size="small" type="primary" @click="handleAdd">
            <template #icon>
              <NIcon><Icon icon="lucide:plus" /></NIcon>
            </template>
            新增配置
          </NButton>
        </template>

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

        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton aria-label="查看详情" circle quaternary size="small" @click="handleView(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:eye" /></NIcon>
              </template>
            </NButton>

            <NButton
              :disabled="!canMaintainConfig(row)"
              aria-label="编辑"
              circle
              quaternary
              size="small"
              type="primary"
              @click="handleEdit(row)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
            </NButton>

            <NPopconfirm
              :disabled="!canMaintainConfig(row)"
              @positive-click="handleToggleStatus(row)"
            >
              <template #trigger>
                <NButton :disabled="!canMaintainConfig(row)" aria-label="停用或启用" circle quaternary size="small" type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                </NButton>
              </template>
              确认更新配置状态？
            </NPopconfirm>

            <NPopconfirm :disabled="!canMaintainConfig(row)" @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton :disabled="!canMaintainConfig(row)" aria-label="删除" circle quaternary size="small" type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确认删除该配置？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NDrawer v-model:show="detailVisible" :width="720">
      <NDrawerContent closable title="配置详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="xh-detail-empty" description="暂无配置详情" />
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
      <NForm :model="configForm" class="xh-edit-form-grid" label-placement="top">
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

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
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
