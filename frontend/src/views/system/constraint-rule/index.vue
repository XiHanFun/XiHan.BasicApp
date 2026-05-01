<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ConstraintRuleCreateDto,
  ConstraintRuleItemInputDto,
  ConstraintRuleListItemDto,
  ConstraintRuleUpdateDto,
} from '@/api'
import {
  NButton,
  NDatePicker,
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
  constraintRuleApi,
  ConstraintTargetType,
  ConstraintType,
  createPageRequest,
  EnableStatus,
  ViolationAction,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { CONSTRAINT_TYPE_OPTIONS, STATUS_OPTIONS, VIOLATION_ACTION_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemConstraintRulePage' })

interface ConstraintRuleGridResult {
  items: ConstraintRuleListItemDto[]
  total: number
}

interface ConstraintRuleFormModel {
  basicId?: ConstraintRuleListItemDto['basicId']
  constraintType: ConstraintType
  description?: string | null
  effectiveTime?: string | null
  expirationTime?: string | null
  itemsJson: string
  parameters?: string | null
  priority: number
  remark?: string | null
  ruleCode: string
  ruleName: string
  status: EnableStatus
  targetType: ConstraintTargetType
  violationAction: ViolationAction
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<ConstraintRuleListItemDto>>()
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const ruleForm = ref<ConstraintRuleFormModel>(createDefaultForm())

const queryParams = reactive({
  constraintType: undefined as ConstraintType | undefined,
  isGlobal: undefined as number | undefined,
  keyword: '',
  status: undefined as EnableStatus | undefined,
  targetType: undefined as ConstraintTargetType | undefined,
  violationAction: undefined as ViolationAction | undefined,
})

const targetTypeOptions = [
  { label: '角色', value: ConstraintTargetType.Role },
  { label: '权限', value: ConstraintTargetType.Permission },
  { label: '用户', value: ConstraintTargetType.User },
]

const globalOptions = [
  { label: '全局规则', value: 1 },
  { label: '租户规则', value: 0 },
]

const modalTitle = computed(() => (ruleForm.value.basicId ? '编辑约束规则' : '新增约束规则'))

function createDefaultForm(): ConstraintRuleFormModel {
  return {
    constraintType: ConstraintType.Ssd,
    description: null,
    effectiveTime: null,
    expirationTime: null,
    itemsJson: '[]',
    parameters: '{}',
    priority: 0,
    remark: null,
    ruleCode: '',
    ruleName: '',
    status: EnableStatus.Enabled,
    targetType: ConstraintTargetType.Role,
    violationAction: ViolationAction.Deny,
  }
}

function toOptionalBoolean(value: number | undefined) {
  if (value === undefined) {
    return undefined
  }

  return value === 1
}

function canMaintainRule(row: ConstraintRuleListItemDto) {
  return !row.isGlobal
}

function parseRuleItems(): ConstraintRuleItemInputDto[] | null {
  try {
    const parsed = JSON.parse(ruleForm.value.itemsJson)
    if (!Array.isArray(parsed)) {
      message.warning('规则项必须是数组')
      return null
    }

    return parsed.map(item => ({
      constraintGroup: Number(item.constraintGroup ?? 0),
      remark: typeof item.remark === 'string' ? item.remark : null,
      targetId: Number(item.targetId),
      targetType: Number(item.targetType ?? ruleForm.value.targetType),
    }))
  }
  catch {
    message.warning('规则项 JSON 格式无效')
    return null
  }
}

function validateForm(items: ConstraintRuleItemInputDto[] | null) {
  if (!ruleForm.value.ruleName.trim()) {
    message.warning('请输入规则名称')
    return false
  }

  if (!ruleForm.value.basicId && !ruleForm.value.ruleCode.trim()) {
    message.warning('请输入规则编码')
    return false
  }

  if (!items || items.length === 0) {
    message.warning('请维护至少一个规则项')
    return false
  }

  if (items.some(item => !Number.isFinite(item.targetId) || item.targetId <= 0)) {
    message.warning('规则项目标主键必须大于 0')
    return false
  }

  return true
}

function toItemsJson(items: ConstraintRuleItemInputDto[]) {
  return JSON.stringify(
    items.map(item => ({
      constraintGroup: item.constraintGroup,
      remark: item.remark ?? null,
      targetId: item.targetId,
      targetType: item.targetType,
    })),
    null,
    2,
  )
}

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<ConstraintRuleGridResult> {
  return constraintRuleApi
    .page({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      constraintType: queryParams.constraintType,
      isGlobal: toOptionalBoolean(queryParams.isGlobal),
      keyword: queryParams.keyword.trim() || null,
      status: queryParams.status,
      targetType: queryParams.targetType,
      violationAction: queryParams.violationAction,
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询约束规则失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const tableOptions = useVxeTable<ConstraintRuleListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'ruleName', minWidth: 160, showOverflow: 'tooltip', sortable: true, title: '规则名称' },
      { field: 'ruleCode', minWidth: 180, showOverflow: 'tooltip', title: '规则编码' },
      {
        field: 'constraintType',
        formatter: ({ cellValue }) => getOptionLabel(CONSTRAINT_TYPE_OPTIONS, cellValue),
        minWidth: 130,
        title: '约束类型',
      },
      {
        field: 'targetType',
        formatter: ({ cellValue }) => getOptionLabel(targetTypeOptions, cellValue),
        minWidth: 100,
        title: '目标类型',
      },
      {
        field: 'violationAction',
        formatter: ({ cellValue }) => getOptionLabel(VIOLATION_ACTION_OPTIONS, cellValue),
        minWidth: 120,
        title: '违规处理',
      },
      { field: 'priority', minWidth: 90, sortable: true, title: '优先级' },
      { field: 'itemCount', minWidth: 90, title: '规则项' },
      {
        field: 'isGlobal',
        slots: { default: 'col_global' },
        title: '全局',
        width: 82,
      },
      {
        field: 'isActive',
        slots: { default: 'col_active' },
        title: '生效',
        width: 82,
      },
      {
        field: 'status',
        slots: { default: 'col_status' },
        title: '状态',
        width: 82,
      },
      {
        field: 'effectiveTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '生效时间',
      },
      {
        field: 'expirationTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '失效时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 190,
      },
    ],
    id: 'sys_constraint_rule',
    name: '约束规则',
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
  queryParams.constraintType = undefined
  queryParams.targetType = undefined
  queryParams.violationAction = undefined
  queryParams.isGlobal = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  editingStatus.value = null
  ruleForm.value = createDefaultForm()
  modalVisible.value = true
}

async function handleEdit(row: ConstraintRuleListItemDto) {
  const detail = await constraintRuleApi.detail(row.basicId)
  if (!detail) {
    message.error('约束规则不存在')
    return
  }

  editingStatus.value = detail.status
  ruleForm.value = {
    basicId: detail.basicId,
    constraintType: detail.constraintType,
    description: detail.description,
    effectiveTime: detail.effectiveTime ?? null,
    expirationTime: detail.expirationTime ?? null,
    itemsJson: toItemsJson(detail.items),
    parameters: detail.parameters ?? '{}',
    priority: detail.priority,
    remark: detail.remark,
    ruleCode: detail.ruleCode,
    ruleName: detail.ruleName,
    status: detail.status,
    targetType: detail.targetType,
    violationAction: detail.violationAction,
  }
  modalVisible.value = true
}

async function handleSubmit() {
  const items = parseRuleItems()
  if (!validateForm(items)) {
    return
  }

  submitLoading.value = true

  try {
    if (ruleForm.value.basicId) {
      const updateInput: ConstraintRuleUpdateDto = {
        basicId: ruleForm.value.basicId,
        constraintType: ruleForm.value.constraintType,
        description: ruleForm.value.description,
        effectiveTime: ruleForm.value.effectiveTime,
        expirationTime: ruleForm.value.expirationTime,
        items: items ?? [],
        parameters: ruleForm.value.parameters,
        priority: ruleForm.value.priority,
        remark: ruleForm.value.remark,
        ruleName: ruleForm.value.ruleName.trim(),
        targetType: ruleForm.value.targetType,
        violationAction: ruleForm.value.violationAction,
      }

      await constraintRuleApi.update(updateInput)
      if (editingStatus.value !== ruleForm.value.status) {
        await constraintRuleApi.updateStatus({
          basicId: ruleForm.value.basicId,
          remark: ruleForm.value.remark,
          status: ruleForm.value.status,
        })
      }
    }
    else {
      const createInput: ConstraintRuleCreateDto = {
        constraintType: ruleForm.value.constraintType,
        description: ruleForm.value.description,
        effectiveTime: ruleForm.value.effectiveTime,
        expirationTime: ruleForm.value.expirationTime,
        items: items ?? [],
        parameters: ruleForm.value.parameters,
        priority: ruleForm.value.priority,
        remark: ruleForm.value.remark,
        ruleCode: ruleForm.value.ruleCode.trim(),
        ruleName: ruleForm.value.ruleName.trim(),
        status: ruleForm.value.status,
        targetType: ruleForm.value.targetType,
        violationAction: ruleForm.value.violationAction,
      }

      await constraintRuleApi.create(createInput)
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

async function handleDelete(row: ConstraintRuleListItemDto) {
  await constraintRuleApi.delete(row.basicId)
  message.success('删除成功')
  xGrid.value?.commitProxy('query')
}

async function handleToggleStatus(row: ConstraintRuleListItemDto) {
  await constraintRuleApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? '前端停用约束规则' : '前端启用约束规则',
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
          placeholder="搜索规则名称/编码"
          style="width: 240px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.constraintType"
          :options="CONSTRAINT_TYPE_OPTIONS"
          clearable
          placeholder="约束类型"
          style="width: 140px"
        />
        <NSelect
          v-model:value="queryParams.targetType"
          :options="targetTypeOptions"
          clearable
          placeholder="目标类型"
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.violationAction"
          :options="VIOLATION_ACTION_OPTIONS"
          clearable
          placeholder="违规处理"
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
            新增规则
          </NButton>
        </template>

        <template #col_global="{ row }">
          <NTag :type="row.isGlobal ? 'warning' : 'default'" round size="small">
            {{ row.isGlobal ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_active="{ row }">
          <NTag :type="row.isActive ? 'success' : 'default'" round size="small">
            {{ row.isActive ? '生效' : '未生效' }}
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
              :disabled="!canMaintainRule(row)"
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

            <NPopconfirm :disabled="!canMaintainRule(row)" @positive-click="handleToggleStatus(row)">
              <template #trigger>
                <NButton :disabled="!canMaintainRule(row)" size="small" text type="warning">
                  <template #icon>
                    <NIcon>
                      <Icon :icon="row.status === EnableStatus.Enabled ? 'lucide:ban' : 'lucide:circle-check'" />
                    </NIcon>
                  </template>
                  {{ row.status === EnableStatus.Enabled ? '停用' : '启用' }}
                </NButton>
              </template>
              确认更新约束规则状态？
            </NPopconfirm>

            <NPopconfirm :disabled="!canMaintainRule(row)" @positive-click="handleDelete(row)">
              <template #trigger>
                <NButton :disabled="!canMaintainRule(row)" size="small" text type="error">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  删除
                </NButton>
              </template>
              确认删除该约束规则？
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
      style="width: 820px; max-width: 94vw"
    >
      <NForm :model="ruleForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="规则名称" path="ruleName">
          <NInput v-model:value="ruleForm.ruleName" clearable placeholder="请输入规则名称" />
        </NFormItem>
        <NFormItem label="规则编码" path="ruleCode">
          <NInput
            v-model:value="ruleForm.ruleCode"
            clearable
            :disabled="Boolean(ruleForm.basicId)"
            placeholder="如: ssd_role_conflict"
          />
        </NFormItem>
        <NFormItem label="约束类型" path="constraintType">
          <NSelect v-model:value="ruleForm.constraintType" :options="CONSTRAINT_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="目标类型" path="targetType">
          <NSelect v-model:value="ruleForm.targetType" :options="targetTypeOptions" />
        </NFormItem>
        <NFormItem label="违规处理" path="violationAction">
          <NSelect v-model:value="ruleForm.violationAction" :options="VIOLATION_ACTION_OPTIONS" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="ruleForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="ruleForm.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="生效时间" path="effectiveTime">
          <NDatePicker
            v-model:formatted-value="ruleForm.effectiveTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem label="失效时间" path="expirationTime">
          <NDatePicker
            v-model:formatted-value="ruleForm.expirationTime"
            clearable
            style="width: 100%"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
          />
        </NFormItem>
        <NFormItem label="约束参数" path="parameters">
          <NInput v-model:value="ruleForm.parameters" clearable placeholder="{}" />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="ruleForm.description"
            clearable
            placeholder="请输入规则描述"
            :rows="3"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="规则项 JSON" path="itemsJson">
          <NInput
            v-model:value="ruleForm.itemsJson"
            placeholder="[]"
            :rows="6"
            type="textarea"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="ruleForm.remark" clearable placeholder="请输入备注" />
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
