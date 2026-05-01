<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiId, OperationCreateDto, OperationListItemDto, OperationUpdateDto } from '@/api'
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
import {
  createPageRequest,
  EnableStatus,
  HttpMethodType,
  operationApi,
  OperationCategory,
  OperationTypeCode,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemOperationPage' })

interface OperationGridResult {
  items: OperationListItemDto[]
  total: number
}

interface OperationFormModel extends OperationCreateDto {
  basicId?: ApiId
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<OperationListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const operationForm = ref<OperationFormModel>(createDefaultForm())

const queryParams = reactive({
  category: undefined as OperationCategory | undefined,
  httpMethod: undefined as HttpMethodType | undefined,
  isDangerous: undefined as number | undefined,
  isGlobal: undefined as number | undefined,
  isRequireAudit: undefined as number | undefined,
  keyword: '',
  operationTypeCode: undefined as OperationTypeCode | undefined,
  status: undefined as EnableStatus | undefined,
})

const operationTypeOptions = [
  { label: '创建', value: OperationTypeCode.Create },
  { label: '读取', value: OperationTypeCode.Read },
  { label: '更新', value: OperationTypeCode.Update },
  { label: '删除', value: OperationTypeCode.Delete },
  { label: '查看详情', value: OperationTypeCode.View },
  { label: '审批', value: OperationTypeCode.Approve },
  { label: '执行', value: OperationTypeCode.Execute },
  { label: '导入', value: OperationTypeCode.Import },
  { label: '导出', value: OperationTypeCode.Export },
  { label: '上传', value: OperationTypeCode.Upload },
  { label: '下载', value: OperationTypeCode.Download },
  { label: '打印', value: OperationTypeCode.Print },
  { label: '分享', value: OperationTypeCode.Share },
  { label: '授权', value: OperationTypeCode.Grant },
  { label: '撤销', value: OperationTypeCode.Revoke },
  { label: '启用', value: OperationTypeCode.Enable },
  { label: '禁用', value: OperationTypeCode.Disable },
  { label: '自定义', value: OperationTypeCode.Custom },
]

const categoryOptions = [
  { label: 'CRUD', value: OperationCategory.Crud },
  { label: '业务', value: OperationCategory.Business },
  { label: '管理', value: OperationCategory.Admin },
  { label: '系统', value: OperationCategory.System },
  { label: '自定义', value: OperationCategory.Custom },
]

const httpMethodOptions = [
  { label: 'GET', value: HttpMethodType.GET },
  { label: 'POST', value: HttpMethodType.POST },
  { label: 'PUT', value: HttpMethodType.PUT },
  { label: 'DELETE', value: HttpMethodType.DELETE },
  { label: 'PATCH', value: HttpMethodType.PATCH },
  { label: 'HEAD', value: HttpMethodType.HEAD },
  { label: 'OPTIONS', value: HttpMethodType.OPTIONS },
  { label: 'ALL', value: HttpMethodType.ALL },
]

const yesNoOptions = [
  { label: '是', value: 1 },
  { label: '否', value: 0 },
]

const globalOptions = [
  { label: '全局操作', value: 1 },
  { label: '租户操作', value: 0 },
]

const modalTitle = computed(() => (operationForm.value.basicId ? '编辑操作' : '新增操作'))

function createDefaultForm(): OperationFormModel {
  return {
    category: OperationCategory.Crud,
    color: null,
    description: null,
    httpMethod: HttpMethodType.GET,
    icon: null,
    isDangerous: false,
    isRequireAudit: false,
    operationCode: '',
    operationName: '',
    operationTypeCode: OperationTypeCode.Read,
    remark: null,
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

function canMaintainOperation(row: OperationListItemDto) {
  return !row.isGlobal
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<OperationGridResult> {
  return operationApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      category: queryParams.category,
      httpMethod: queryParams.httpMethod,
      isDangerous: toOptionalBoolean(queryParams.isDangerous),
      isGlobal: toOptionalBoolean(queryParams.isGlobal),
      isRequireAudit: toOptionalBoolean(queryParams.isRequireAudit),
      keyword: normalizeNullable(queryParams.keyword),
      operationTypeCode: queryParams.operationTypeCode,
      status: queryParams.status,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询操作失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<OperationListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'operationName', minWidth: 140, showOverflow: 'tooltip', sortable: true, title: '操作名称' },
      { field: 'operationCode', minWidth: 140, showOverflow: 'tooltip', title: '操作编码' },
      {
        field: 'operationTypeCode',
        formatter: ({ cellValue }) => getOptionLabel(operationTypeOptions, cellValue),
        minWidth: 110,
        title: '操作类型',
      },
      {
        field: 'category',
        formatter: ({ cellValue }) => getOptionLabel(categoryOptions, cellValue),
        minWidth: 100,
        title: '分类',
      },
      {
        field: 'httpMethod',
        formatter: ({ cellValue }) => getOptionLabel(httpMethodOptions, cellValue),
        minWidth: 100,
        title: 'HTTP',
      },
      {
        field: 'isDangerous',
        slots: { default: 'col_dangerous' },
        title: '高危',
        width: 82,
      },
      {
        field: 'isRequireAudit',
        slots: { default: 'col_audit' },
        title: '审计',
        width: 82,
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
        width: 180,
      },
    ],
    id: 'sys_operation',
    name: '操作管理',
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
  queryParams.category = undefined
  queryParams.httpMethod = undefined
  queryParams.isDangerous = undefined
  queryParams.isGlobal = undefined
  queryParams.isRequireAudit = undefined
  queryParams.operationTypeCode = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  operationForm.value = createDefaultForm()
  modalVisible.value = true
}

function handleEdit(row: OperationListItemDto) {
  operationForm.value = {
    category: row.category,
    color: row.color ?? null,
    basicId: row.basicId,
    description: row.description ?? null,
    httpMethod: row.httpMethod ?? null,
    icon: row.icon ?? null,
    isDangerous: row.isDangerous,
    isRequireAudit: row.isRequireAudit,
    operationCode: row.operationCode,
    operationName: row.operationName,
    operationTypeCode: row.operationTypeCode,
    remark: null,
    sort: row.sort,
    status: row.status,
  }
  modalVisible.value = true
}

function validateForm() {
  if (!operationForm.value.operationName.trim()) {
    message.warning('请输入操作名称')
    return false
  }

  if (!operationForm.value.basicId && !operationForm.value.operationCode.trim()) {
    message.warning('请输入操作编码')
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
    if (operationForm.value.basicId) {
      const updateInput: OperationUpdateDto = {
        category: operationForm.value.category,
        color: normalizeNullable(operationForm.value.color),
        basicId: operationForm.value.basicId,
        description: normalizeNullable(operationForm.value.description),
        httpMethod: operationForm.value.httpMethod,
        icon: normalizeNullable(operationForm.value.icon),
        isDangerous: operationForm.value.isDangerous,
        isRequireAudit: operationForm.value.isRequireAudit,
        operationName: operationForm.value.operationName.trim(),
        operationTypeCode: operationForm.value.operationTypeCode,
        remark: normalizeNullable(operationForm.value.remark),
        sort: operationForm.value.sort,
      }

      await operationApi.update(updateInput)
    }
    else {
      const createInput: OperationCreateDto = {
        category: operationForm.value.category,
        color: normalizeNullable(operationForm.value.color),
        description: normalizeNullable(operationForm.value.description),
        httpMethod: operationForm.value.httpMethod,
        icon: normalizeNullable(operationForm.value.icon),
        isDangerous: operationForm.value.isDangerous,
        isRequireAudit: operationForm.value.isRequireAudit,
        operationCode: operationForm.value.operationCode.trim(),
        operationName: operationForm.value.operationName.trim(),
        operationTypeCode: operationForm.value.operationTypeCode,
        remark: normalizeNullable(operationForm.value.remark),
        sort: operationForm.value.sort,
        status: operationForm.value.status,
      }

      await operationApi.create(createInput)
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

async function handleDelete(row: OperationListItemDto) {
  await operationApi.delete(row.basicId)
  message.success('删除成功')
  xGrid.value?.commitProxy('query')
}

async function handleToggleStatus(row: OperationListItemDto) {
  await operationApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用操作' : '前端启用操作',
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
          placeholder="搜索操作名称/编码"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.operationTypeCode"
          :options="operationTypeOptions"
          clearable
          placeholder="操作类型"
          style="width: 130px"
        />
        <NSelect
          v-model:value="queryParams.category"
          :options="categoryOptions"
          clearable
          placeholder="分类"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.httpMethod"
          :options="httpMethodOptions"
          clearable
          placeholder="HTTP"
          style="width: 110px"
        />
        <NSelect
          v-model:value="queryParams.isRequireAudit"
          :options="yesNoOptions"
          clearable
          placeholder="审计"
          style="width: 100px"
        />
        <NSelect
          v-model:value="queryParams.isDangerous"
          :options="yesNoOptions"
          clearable
          placeholder="高危"
          style="width: 100px"
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
            新增操作
          </NButton>
        </template>

        <template #col_dangerous="{ row }">
          <NTag :type="row.isDangerous ? 'error' : 'default'" round size="small">
            {{ row.isDangerous ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_audit="{ row }">
          <NTag :type="row.isRequireAudit ? 'warning' : 'default'" round size="small">
            {{ row.isRequireAudit ? '是' : '否' }}
          </NTag>
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
            <NButton
              :disabled="!canMaintainOperation(row)"
              size="small"
              text
              type="primary"
              @click="handleEdit(row)"
            >
              <template #icon>
                <NIcon><Icon icon="lucide:pencil" /></NIcon>
              </template>
              编辑
            </NButton>

            <NPopconfirm
              :disabled="!canMaintainOperation(row)"
              @positive-click="handleToggleStatus(row)"
            >
              <template #trigger>
                <NButton :disabled="!canMaintainOperation(row)" size="small" text type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                  {{ row.status === EnableStatus.Enabled ? '停用' : '启用' }}
                </NButton>
              </template>
              确认更新操作状态？
            </NPopconfirm>

            <NPopconfirm :disabled="!canMaintainOperation(row)" @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton :disabled="!canMaintainOperation(row)" size="small" text type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  删除
                </NButton>
              </template>
              确认删除该操作？
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
      <NForm :model="operationForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="操作名称" path="operationName">
          <NInput v-model:value="operationForm.operationName" clearable placeholder="请输入操作名称" />
        </NFormItem>
        <NFormItem label="操作编码" path="operationCode">
          <NInput
            v-model:value="operationForm.operationCode"
            :disabled="Boolean(operationForm.basicId)"
            clearable
            placeholder="如: read"
          />
        </NFormItem>
        <NFormItem label="操作类型" path="operationTypeCode">
          <NSelect v-model:value="operationForm.operationTypeCode" :options="operationTypeOptions" />
        </NFormItem>
        <NFormItem label="分类" path="category">
          <NSelect v-model:value="operationForm.category" :options="categoryOptions" />
        </NFormItem>
        <NFormItem label="HTTP 方法" path="httpMethod">
          <NSelect
            v-model:value="operationForm.httpMethod"
            :options="httpMethodOptions"
            clearable
            placeholder="可为空"
          />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="operationForm.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="高危操作" path="isDangerous">
          <NSwitch v-model:value="operationForm.isDangerous" />
        </NFormItem>
        <NFormItem label="需要审计" path="isRequireAudit">
          <NSwitch v-model:value="operationForm.isRequireAudit" />
        </NFormItem>
        <NFormItem v-if="!operationForm.basicId" label="状态" path="status">
          <NSelect v-model:value="operationForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="图标" path="icon">
          <NInput v-model:value="operationForm.icon" clearable placeholder="如: lucide:eye" />
        </NFormItem>
        <NFormItem label="颜色" path="color">
          <NInput v-model:value="operationForm.color" clearable placeholder="如: #18a058" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="operationForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="operationForm.description"
            clearable
            placeholder="请输入操作描述"
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
