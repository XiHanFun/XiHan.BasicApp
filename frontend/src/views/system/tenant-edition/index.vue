<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiId, TenantEditionCreateDto, TenantEditionListItemDto, TenantEditionUpdateDto } from '@/api'
import {
  NButton,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NInputNumber,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NSwitch,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import { createPageRequest, EnableStatus, tenantEditionApi } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemTenantEditionPage' })

interface TenantEditionGridResult {
  items: TenantEditionListItemDto[]
  total: number
}

interface TenantEditionFormModel extends TenantEditionCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<TenantEditionListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const editionForm = ref<TenantEditionFormModel>(createDefaultForm())

const queryParams = reactive({
  isDefault: undefined as number | undefined,
  isFree: undefined as number | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
})

const yesNoOptions = [
  { label: '是', value: 1 },
  { label: '否', value: 0 },
]

const modalTitle = computed(() => (editionForm.value.basicId ? '编辑租户版本' : '新增租户版本'))

function createDefaultForm(): TenantEditionFormModel {
  return {
    billingPeriodMonths: null,
    description: null,
    editionCode: '',
    editionName: '',
    isDefault: false,
    isFree: false,
    price: null,
    remark: null,
    sort: 100,
    status: EnableStatus.Enabled,
    storageLimit: null,
    userLimit: null,
  }
}

function toOptionalBoolean(value: number | undefined) {
  if (value === undefined) {
    return undefined
  }

  return value === 1
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<TenantEditionGridResult> {
  return tenantEditionApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      isDefault: toOptionalBoolean(queryParams.isDefault),
      isFree: toOptionalBoolean(queryParams.isFree),
      keyword: normalizeNullable(queryParams.keyword),
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询租户版本失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<TenantEditionListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'editionName', minWidth: 150, showOverflow: 'tooltip', sortable: true, title: '版本名称' },
      { field: 'editionCode', minWidth: 150, showOverflow: 'tooltip', title: '版本编码' },
      { field: 'description', minWidth: 220, showOverflow: 'tooltip', title: '描述' },
      {
        field: 'isFree',
        slots: { default: 'col_free' },
        title: '免费',
        width: 82,
      },
      {
        field: 'isDefault',
        slots: { default: 'col_default' },
        title: '默认',
        width: 82,
      },
      { field: 'price', minWidth: 90, title: '价格' },
      { field: 'billingPeriodMonths', minWidth: 110, title: '计费月数' },
      { field: 'userLimit', minWidth: 100, title: '用户上限' },
      { field: 'storageLimit', minWidth: 120, title: '存储上限' },
      { field: 'sort', minWidth: 80, sortable: true, title: '排序' },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 82,
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
        width: 148,
      },
    ],
    id: 'sys_tenant_edition',
    name: '租户版本',
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

function canDisableEdition(row: TenantEditionListItemDto) {
  return !(row.isDefault && row.status === EnableStatus.Enabled)
}

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.isDefault = undefined
  queryParams.isFree = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  editingStatus.value = null
  editionForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: TenantEditionListItemDto) {
  editingStatus.value = row.status
  editionForm.value = {
    basicId: row.basicId,
    billingPeriodMonths: row.billingPeriodMonths ?? null,
    description: row.description ?? null,
    editionCode: row.editionCode,
    editionName: row.editionName,
    isDefault: row.isDefault,
    isFree: row.isFree,
    price: row.price ?? null,
    remark: null,
    sort: row.sort,
    status: row.status,
    storageLimit: row.storageLimit ?? null,
    userLimit: row.userLimit ?? null,
  }
  modalVisible.value = true
}

function validateForm() {
  if (!editionForm.value.editionName.trim()) {
    message.warning('请输入版本名称')
    return false
  }

  if (!editionForm.value.basicId && !editionForm.value.editionCode.trim()) {
    message.warning('请输入版本编码')
    return false
  }

  if (editionForm.value.isFree && editionForm.value.price && editionForm.value.price > 0) {
    message.warning('免费版本价格必须为空或 0')
    return false
  }

  if (editionForm.value.isDefault && editionForm.value.status !== EnableStatus.Enabled) {
    message.warning('默认租户版本必须启用')
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
    if (editionForm.value.basicId) {
      if (editingStatus.value !== editionForm.value.status && editionForm.value.status === EnableStatus.Enabled) {
        await tenantEditionApi.updateStatus({
          basicId: editionForm.value.basicId,
          status: editionForm.value.status,
        })
      }

      const updateInput: TenantEditionUpdateDto = {
        basicId: editionForm.value.basicId,
        billingPeriodMonths: editionForm.value.billingPeriodMonths ?? null,
        description: normalizeNullable(editionForm.value.description),
        editionName: editionForm.value.editionName.trim(),
        isDefault: editionForm.value.isDefault,
        isFree: editionForm.value.isFree,
        price: editionForm.value.price ?? null,
        remark: normalizeNullable(editionForm.value.remark),
        sort: editionForm.value.sort,
        storageLimit: editionForm.value.storageLimit ?? null,
        userLimit: editionForm.value.userLimit ?? null,
      }

      await tenantEditionApi.update(updateInput)

      if (editingStatus.value !== editionForm.value.status && editionForm.value.status !== EnableStatus.Enabled) {
        await tenantEditionApi.updateStatus({
          basicId: editionForm.value.basicId,
          status: editionForm.value.status,
        })
      }
    }
    else {
      const createInput: TenantEditionCreateDto = {
        billingPeriodMonths: editionForm.value.billingPeriodMonths ?? null,
        description: normalizeNullable(editionForm.value.description),
        editionCode: editionForm.value.editionCode.trim(),
        editionName: editionForm.value.editionName.trim(),
        isDefault: editionForm.value.isDefault,
        isFree: editionForm.value.isFree,
        price: editionForm.value.price ?? null,
        remark: normalizeNullable(editionForm.value.remark),
        sort: editionForm.value.sort,
        status: editionForm.value.status,
        storageLimit: editionForm.value.storageLimit ?? null,
        userLimit: editionForm.value.userLimit ?? null,
      }

      await tenantEditionApi.create(createInput)
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

async function handleToggleStatus(row: TenantEditionListItemDto) {
  await tenantEditionApi.updateStatus({
    basicId: row.basicId,
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  message.success('状态已更新')
  xGrid.value?.commitProxy('query')
}

async function handleSetDefault(row: TenantEditionListItemDto) {
  await tenantEditionApi.updateDefault({
    basicId: row.basicId,
  })
  message.success('默认版本已更新')
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
          placeholder="搜索版本名称/编码"
          style="width: 240px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.isFree"
          :options="yesNoOptions"
          clearable
          placeholder="免费"
          style="width: 100px"
        />
        <NSelect
          v-model:value="queryParams.isDefault"
          :options="yesNoOptions"
          clearable
          placeholder="默认"
          style="width: 100px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          clearable
          placeholder="状态"
          style="width: 110px"
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
            新增版本
          </NButton>
        </template>

        <template #col_free="{ row }">
          <NTag :type="row.isFree ? 'success' : 'default'" round size="small">
            {{ row.isFree ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_default="{ row }">
          <NTag :type="row.isDefault ? 'warning' : 'default'" round size="small">
            {{ row.isDefault ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_status="{ row }">
          <NTag :type="row.status === EnableStatus.Enabled ? 'success' : 'error'" round size="small">
            {{ row.status === EnableStatus.Enabled ? '启用' : '禁用' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NSpace size="small">
            <!-- 操作列仅图标 -->
            <NButton aria-label="编辑" circle quaternary size="small" type="primary" @click="handleEdit(row)">
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
            </NButton>

            <NPopconfirm :disabled="!canDisableEdition(row)" @positive-click="handleToggleStatus(row)">
              <template #trigger>
                <NButton :disabled="!canDisableEdition(row)" aria-label="停用或启用" circle quaternary size="small" type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                </NButton>
              </template>
              确认更新版本状态？
            </NPopconfirm>

            <NPopconfirm
              :disabled="row.isDefault || row.status !== EnableStatus.Enabled"
              @positive-click="handleSetDefault(row)"
            >
              <template #trigger>
                <NButton
                  :disabled="row.isDefault || row.status !== EnableStatus.Enabled"
                  aria-label="设为默认版本"
                  circle
                  quaternary
                  size="small"
                  type="info"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:star" /></NIcon>
                  </template>
                </NButton>
              </template>
              确认设置为默认版本？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 720px; max-width: 92vw"
    >
      <NForm :model="editionForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="版本名称" path="editionName">
          <NInput v-model:value="editionForm.editionName" clearable placeholder="请输入版本名称" />
        </NFormItem>
        <NFormItem label="版本编码" path="editionCode">
          <NInput
            v-model:value="editionForm.editionCode"
            :disabled="Boolean(editionForm.basicId)"
            clearable
            placeholder="如: basic"
          />
        </NFormItem>
        <NFormItem label="免费版本" path="isFree">
          <NSwitch v-model:value="editionForm.isFree" />
        </NFormItem>
        <NFormItem label="默认版本" path="isDefault">
          <NSwitch v-model:value="editionForm.isDefault" />
        </NFormItem>
        <NFormItem label="价格" path="price">
          <NInputNumber v-model:value="editionForm.price" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="计费周期(月)" path="billingPeriodMonths">
          <NInputNumber v-model:value="editionForm.billingPeriodMonths" :min="1" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="用户上限" path="userLimit">
          <NInputNumber v-model:value="editionForm.userLimit" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="存储上限" path="storageLimit">
          <NInputNumber v-model:value="editionForm.storageLimit" :min="0" clearable style="width: 100%" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="editionForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="editionForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="editionForm.description"
            clearable
            placeholder="请输入版本描述"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput
            v-model:value="editionForm.remark"
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
