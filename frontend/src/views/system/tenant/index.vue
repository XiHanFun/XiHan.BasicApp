<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiId, TenantCreateDto, TenantListItemDto, TenantUpdateDto } from '@/api'
import {
  NButton,
  NDatePicker,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import {
  createPageRequest,
  tenantApi,
  TenantConfigStatus,
  TenantIsolationMode,
  TenantStatus,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemTenantPage' })

interface TenantGridResult {
  items: TenantListItemDto[]
  total: number
}

interface TenantFormModel extends TenantCreateDto {
  basicId?: ApiId
  tenantStatus?: TenantStatus
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<TenantListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<TenantStatus | null>(null)
const tenantForm = ref<TenantFormModel>(createDefaultForm())

const queryParams = reactive({
  configStatus: undefined as TenantConfigStatus | undefined,
  editionId: undefined as number | null | undefined,
  expireTimeEnd: null as string | null,
  expireTimeStart: null as string | null,
  keyword: '',
  tenantStatus: undefined as TenantStatus | undefined,
})

const tenantStatusOptions = [
  { label: '正常', value: TenantStatus.Normal },
  { label: '暂停', value: TenantStatus.Suspended },
  { label: '过期', value: TenantStatus.Expired },
  { label: '禁用', value: TenantStatus.Disabled },
]

const configStatusOptions = [
  { label: '待配置', value: TenantConfigStatus.Pending },
  { label: '配置中', value: TenantConfigStatus.Configuring },
  { label: '已配置', value: TenantConfigStatus.Configured },
  { label: '配置失败', value: TenantConfigStatus.Failed },
  { label: '已停用', value: TenantConfigStatus.Disabled },
]

const isolationModeOptions = [
  { label: '字段隔离', value: TenantIsolationMode.Field },
  { label: '数据库隔离', value: TenantIsolationMode.Database },
  { label: 'Schema 隔离', value: TenantIsolationMode.Schema },
]

const modalTitle = computed(() => (tenantForm.value.basicId ? '编辑租户' : '新增租户'))

function createDefaultForm(): TenantFormModel {
  return {
    domain: null,
    editionId: null,
    expireTime: null,
    isolationMode: TenantIsolationMode.Field,
    logo: null,
    remark: null,
    sort: 100,
    storageLimit: null,
    tenantCode: '',
    tenantName: '',
    tenantShortName: null,
    tenantStatus: TenantStatus.Normal,
    userLimit: null,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<TenantGridResult> {
  return tenantApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      configStatus: queryParams.configStatus,
      editionId: queryParams.editionId ?? null,
      expireTimeEnd: queryParams.expireTimeEnd,
      expireTimeStart: queryParams.expireTimeStart,
      keyword: normalizeNullable(queryParams.keyword),
      tenantStatus: queryParams.tenantStatus,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询租户失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<TenantListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'tenantName', minWidth: 160, showOverflow: 'tooltip', sortable: true, title: '租户名称' },
      { field: 'tenantCode', minWidth: 150, showOverflow: 'tooltip', title: '租户编码' },
      { field: 'tenantShortName', minWidth: 130, showOverflow: 'tooltip', title: '简称' },
      { field: 'domain', minWidth: 180, showOverflow: 'tooltip', title: '域名' },
      {
        field: 'isolationMode',
        formatter: ({ cellValue }) => getOptionLabel(isolationModeOptions, cellValue),
        minWidth: 120,
        title: '隔离模式',
      },
      { field: 'editionId', minWidth: 100, title: '版本' },
      {
        field: 'tenantStatus',
        slots: { default: 'col_tenant_status' },
        title: '租户状态',
        width: 100,
      },
      {
        field: 'configStatus',
        formatter: ({ cellValue }) => getOptionLabel(configStatusOptions, cellValue),
        minWidth: 110,
        title: '配置状态',
      },
      {
        field: 'isExpired',
        slots: { default: 'col_expired' },
        title: '过期',
        width: 82,
      },
      { field: 'userLimit', minWidth: 100, title: '用户上限' },
      { field: 'storageLimit', minWidth: 120, title: '存储上限' },
      { field: 'sort', minWidth: 80, sortable: true, title: '排序' },
      {
        field: 'expireTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '到期时间',
      },
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
        width: 56,
      },
    ],
    id: 'sys_tenant',
    name: '租户管理',
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

function getTenantStatusTagType(status: TenantStatus) {
  if (status === TenantStatus.Normal) {
    return 'success'
  }

  if (status === TenantStatus.Disabled) {
    return 'error'
  }

  return 'warning'
}

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.tenantStatus = undefined
  queryParams.configStatus = undefined
  queryParams.editionId = undefined
  queryParams.expireTimeStart = null
  queryParams.expireTimeEnd = null
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  editingStatus.value = null
  tenantForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: TenantListItemDto) {
  editingStatus.value = row.tenantStatus
  tenantForm.value = {
    basicId: row.basicId,
    domain: row.domain ?? null,
    editionId: row.editionId ?? null,
    expireTime: row.expireTime ?? null,
    isolationMode: row.isolationMode,
    logo: row.logo ?? null,
    remark: null,
    sort: row.sort,
    storageLimit: row.storageLimit ?? null,
    tenantCode: row.tenantCode,
    tenantName: row.tenantName,
    tenantShortName: row.tenantShortName ?? null,
    tenantStatus: row.tenantStatus,
    userLimit: row.userLimit ?? null,
  }
  modalVisible.value = true
}

function validateForm() {
  if (!tenantForm.value.tenantName.trim()) {
    message.warning('请输入租户名称')
    return false
  }

  if (!tenantForm.value.basicId && !tenantForm.value.tenantCode.trim()) {
    message.warning('请输入租户编码')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  submitLoading.value = true
  try {
    if (tenantForm.value.basicId) {
      const updateInput: TenantUpdateDto = {
        basicId: tenantForm.value.basicId,
        domain: normalizeNullable(tenantForm.value.domain),
        editionId: tenantForm.value.editionId ?? null,
        expireTime: tenantForm.value.expireTime,
        isolationMode: tenantForm.value.isolationMode,
        logo: normalizeNullable(tenantForm.value.logo),
        remark: normalizeNullable(tenantForm.value.remark),
        sort: tenantForm.value.sort,
        storageLimit: tenantForm.value.storageLimit ?? null,
        tenantName: tenantForm.value.tenantName.trim(),
        tenantShortName: normalizeNullable(tenantForm.value.tenantShortName),
        userLimit: tenantForm.value.userLimit ?? null,
      }

      await tenantApi.update(updateInput)

      if (editingStatus.value !== tenantForm.value.tenantStatus && tenantForm.value.tenantStatus !== undefined) {
        await tenantApi.updateStatus({
          basicId: tenantForm.value.basicId,
          reason: '前端更新租户状态',
          tenantStatus: tenantForm.value.tenantStatus,
        })
      }
    }
    else {
      const createInput: TenantCreateDto = {
        domain: normalizeNullable(tenantForm.value.domain),
        editionId: tenantForm.value.editionId ?? null,
        expireTime: tenantForm.value.expireTime,
        isolationMode: tenantForm.value.isolationMode,
        logo: normalizeNullable(tenantForm.value.logo),
        remark: normalizeNullable(tenantForm.value.remark),
        sort: tenantForm.value.sort,
        storageLimit: tenantForm.value.storageLimit ?? null,
        tenantCode: tenantForm.value.tenantCode.trim(),
        tenantName: tenantForm.value.tenantName.trim(),
        tenantShortName: normalizeNullable(tenantForm.value.tenantShortName),
        userLimit: tenantForm.value.userLimit ?? null,
      }

      await tenantApi.create(createInput)
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
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索租户名称/编码/域名"
          style="width: 250px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.tenantStatus"
          :options="tenantStatusOptions"
          clearable
          placeholder="租户状态"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.configStatus"
          :options="configStatusOptions"
          clearable
          placeholder="配置状态"
          style="width: 120px"
        />
        <NInputNumber
          v-model:value="queryParams.editionId"
          clearable
          placeholder="版本 ID"
          style="width: 120px"
        />
        <NDatePicker
          v-model:formatted-value="queryParams.expireTimeStart"
          clearable
          placeholder="到期开始"
          style="width: 160px"
          type="date"
          value-format="yyyy-MM-dd"
        />
        <NDatePicker
          v-model:formatted-value="queryParams.expireTimeEnd"
          clearable
          placeholder="到期结束"
          style="width: 160px"
          type="date"
          value-format="yyyy-MM-dd"
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
            新增租户
          </NButton>
        </template>

        <template #col_tenant_status="{ row }">
          <NTag :type="getTenantStatusTagType(row.tenantStatus)" round size="small">
            {{ getOptionLabel(tenantStatusOptions, row.tenantStatus) }}
          </NTag>
        </template>

        <template #col_expired="{ row }">
          <NTag :type="row.isExpired ? 'error' : 'success'" round size="small">
            {{ row.isExpired ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <!-- 操作列仅图标 -->
          <NButton aria-label="编辑" circle quaternary size="small" type="primary" @click="handleEdit(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:pencil" /></NIcon>
            </template>
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 760px; max-width: 92vw"
    >
      <NForm :model="tenantForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="租户名称" path="tenantName">
          <NInput v-model:value="tenantForm.tenantName" clearable placeholder="请输入租户名称" />
        </NFormItem>
        <NFormItem label="租户编码" path="tenantCode">
          <NInput
            v-model:value="tenantForm.tenantCode"
            :disabled="Boolean(tenantForm.basicId)"
            clearable
            placeholder="如: demo"
          />
        </NFormItem>
        <NFormItem label="简称" path="tenantShortName">
          <NInput v-model:value="tenantForm.tenantShortName" clearable placeholder="请输入租户简称" />
        </NFormItem>
        <NFormItem label="域名" path="domain">
          <NInput v-model:value="tenantForm.domain" clearable placeholder="如: demo.example.com" />
        </NFormItem>
        <NFormItem label="隔离模式" path="isolationMode">
          <NSelect v-model:value="tenantForm.isolationMode" :options="isolationModeOptions" />
        </NFormItem>
        <NFormItem label="版本 ID" path="editionId">
          <NInputNumber v-model:value="tenantForm.editionId" :min="1" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="用户上限" path="userLimit">
          <NInputNumber v-model:value="tenantForm.userLimit" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="存储上限" path="storageLimit">
          <NInputNumber v-model:value="tenantForm.storageLimit" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="tenantForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="tenantForm.basicId" label="租户状态" path="tenantStatus">
          <NSelect v-model:value="tenantForm.tenantStatus" :options="tenantStatusOptions" />
        </NFormItem>
        <NFormItem label="到期时间" path="expireTime">
          <NDatePicker
            v-model:formatted-value="tenantForm.expireTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem label="Logo" path="logo">
          <NInput v-model:value="tenantForm.logo" clearable placeholder="请输入 Logo 地址" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="tenantForm.remark"
            clearable
            placeholder="请输入备注"
            :rows="3"
            type="textarea"
          />
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
