<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiId, ResourceCreateDto, ResourceListItemDto, ResourceUpdateDto } from '@/api'
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
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import {
  createPageRequest,
  EnableStatus,
  ResourceAccessLevel,
  resourceApi,
  ResourceType,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemResourcePage' })

interface ResourceGridResult {
  items: ResourceListItemDto[]
  total: number
}

interface ResourceFormModel extends ResourceCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ResourceListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const resourceForm = ref<ResourceFormModel>(createDefaultForm())

const queryParams = reactive({
  accessLevel: undefined as ResourceAccessLevel | undefined,
  isGlobal: undefined as number | undefined,
  keyword: '',
  resourceType: undefined as ResourceType | undefined,
  status: undefined as EnableStatus | undefined,
})

const resourceTypeOptions = [
  { label: 'API', value: ResourceType.Api },
  { label: '文件', value: ResourceType.File },
  { label: '数据表', value: ResourceType.DataTable },
  { label: '业务对象', value: ResourceType.BusinessObject },
  { label: '其他', value: ResourceType.Other },
]

const accessLevelOptions = [
  { label: '匿名访问', value: ResourceAccessLevel.Public },
  { label: '仅需认证', value: ResourceAccessLevel.Authenticated },
  { label: '需要授权', value: ResourceAccessLevel.Authorized },
]

const globalOptions = [
  { label: '全局资源', value: 1 },
  { label: '租户资源', value: 0 },
]

const modalTitle = computed(() => (resourceForm.value.basicId ? '编辑资源' : '新增资源'))

function createDefaultForm(): ResourceFormModel {
  return {
    accessLevel: ResourceAccessLevel.Authorized,
    description: null,
    metadata: null,
    remark: null,
    resourceCode: '',
    resourceName: '',
    resourcePath: null,
    resourceType: ResourceType.Api,
    sort: 100,
    status: EnableStatus.Enabled,
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

function canMaintainResource(row: ResourceListItemDto) {
  return !row.isGlobal
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<ResourceGridResult> {
  return resourceApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      accessLevel: queryParams.accessLevel,
      isGlobal: toOptionalBoolean(queryParams.isGlobal),
      keyword: normalizeNullable(queryParams.keyword),
      resourceType: queryParams.resourceType,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询资源失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<ResourceListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'resourceName', minWidth: 150, showOverflow: 'tooltip', sortable: true, title: '资源名称' },
      { field: 'resourceCode', minWidth: 180, showOverflow: 'tooltip', title: '资源编码' },
      { field: 'resourcePath', minWidth: 260, showOverflow: 'tooltip', title: '资源路径' },
      {
        field: 'resourceType',
        formatter: ({ cellValue }) => getOptionLabel(resourceTypeOptions, cellValue),
        minWidth: 110,
        title: '资源类型',
      },
      {
        field: 'accessLevel',
        formatter: ({ cellValue }) => getOptionLabel(accessLevelOptions, cellValue),
        minWidth: 110,
        title: '访问级别',
      },
      {
        field: 'isGlobal',
        slots: { default: 'col_global' },
        title: '全局',
        width: 82,
      },
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
        width: 120,
      },
    ],
    id: 'sys_resource',
    name: '资源管理',
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
  queryParams.resourceType = undefined
  queryParams.accessLevel = undefined
  queryParams.isGlobal = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  resourceForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: ResourceListItemDto) {
  resourceForm.value = {
    accessLevel: row.accessLevel,
    basicId: row.basicId,
    description: row.description ?? null,
    metadata: null,
    remark: null,
    resourceCode: row.resourceCode,
    resourceName: row.resourceName,
    resourcePath: row.resourcePath ?? null,
    resourceType: row.resourceType,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

function normalizeMetadataInput(value?: string | null) {
  const normalized = normalizeNullable(value)
  if (normalized === null) {
    return null
  }

  try {
    JSON.parse(normalized) as unknown
  }
  catch {
    message.warning('资源元数据必须是合法 JSON')
    return undefined
  }

  return normalized
}

function validateForm() {
  if (!resourceForm.value.resourceName.trim()) {
    message.warning('请输入资源名称')
    return false
  }

  if (!resourceForm.value.basicId && !resourceForm.value.resourceCode.trim()) {
    message.warning('请输入资源编码')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }

  const metadata = normalizeMetadataInput(resourceForm.value.metadata)
  if (metadata === undefined) {
    return
  }

  submitLoading.value = true
  try {
    if (resourceForm.value.basicId) {
      const updateInput: ResourceUpdateDto = {
        accessLevel: resourceForm.value.accessLevel,
        basicId: resourceForm.value.basicId,
        description: normalizeNullable(resourceForm.value.description),
        metadata,
        remark: normalizeNullable(resourceForm.value.remark),
        resourceName: resourceForm.value.resourceName.trim(),
        resourcePath: normalizeNullable(resourceForm.value.resourcePath),
        resourceType: resourceForm.value.resourceType,
        sort: resourceForm.value.sort,
      }

      await resourceApi.update(updateInput)
    }
    else {
      const createInput: ResourceCreateDto = {
        accessLevel: resourceForm.value.accessLevel,
        description: normalizeNullable(resourceForm.value.description),
        metadata,
        remark: normalizeNullable(resourceForm.value.remark),
        resourceCode: resourceForm.value.resourceCode.trim(),
        resourceName: resourceForm.value.resourceName.trim(),
        resourcePath: normalizeNullable(resourceForm.value.resourcePath),
        resourceType: resourceForm.value.resourceType,
        sort: resourceForm.value.sort,
        status: resourceForm.value.status,
      }

      await resourceApi.create(createInput)
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

async function handleDelete(row: ResourceListItemDto) {
  await resourceApi.delete(row.basicId)
  message.success('删除成功')
  xGrid.value?.commitProxy('query')
}

async function handleToggleStatus(row: ResourceListItemDto) {
  await resourceApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用资源' : '前端启用资源',
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
          placeholder="搜索资源名称/编码/路径"
          style="width: 260px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.resourceType"
          :options="resourceTypeOptions"
          clearable
          placeholder="资源类型"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.accessLevel"
          :options="accessLevelOptions"
          clearable
          placeholder="访问级别"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.isGlobal"
          :options="globalOptions"
          clearable
          placeholder="全局"
          style="width: 110px"
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
            新增资源
          </NButton>
        </template>

        <template #col_global="{ row }">
          <NTag :type="row.isGlobal ? 'warning' : 'default'" round size="small">
            {{ row.isGlobal ? '是' : '否' }}
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
            <NButton
              :disabled="!canMaintainResource(row)"
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
              :disabled="!canMaintainResource(row)"
              @positive-click="handleToggleStatus(row)"
            >
              <template #trigger>
                <NButton :disabled="!canMaintainResource(row)" aria-label="停用或启用" circle quaternary size="small" type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                </NButton>
              </template>
              确认更新资源状态？
            </NPopconfirm>

            <NPopconfirm :disabled="!canMaintainResource(row)" @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton :disabled="!canMaintainResource(row)" aria-label="删除" circle quaternary size="small" type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>
              确认删除该资源？
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
      <NForm :model="resourceForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="资源名称" path="resourceName">
          <NInput v-model:value="resourceForm.resourceName" clearable placeholder="请输入资源名称" />
        </NFormItem>
        <NFormItem label="资源编码" path="resourceCode">
          <NInput
            v-model:value="resourceForm.resourceCode"
            :disabled="Boolean(resourceForm.basicId)"
            clearable
            placeholder="如: saas.user"
          />
        </NFormItem>
        <NFormItem label="资源类型" path="resourceType">
          <NSelect v-model:value="resourceForm.resourceType" :options="resourceTypeOptions" />
        </NFormItem>
        <NFormItem label="访问级别" path="accessLevel">
          <NSelect v-model:value="resourceForm.accessLevel" :options="accessLevelOptions" />
        </NFormItem>
        <NFormItem label="资源路径" path="resourcePath">
          <NInput v-model:value="resourceForm.resourcePath" clearable placeholder="如: /api/User/UserPage" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="resourceForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem v-if="!resourceForm.basicId" label="状态" path="status">
          <NSelect v-model:value="resourceForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="resourceForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
        <NFormItem label="元数据 JSON" path="metadata">
          <NInput
            v-model:value="resourceForm.metadata"
            clearable
            placeholder="{&quot;module&quot;:&quot;saas&quot;}"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="resourceForm.description"
            clearable
            placeholder="请输入资源描述"
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
