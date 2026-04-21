<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysConstraintRule } from '@/api/modules/constraint-rule'
import {
  NButton,
  NDatePicker,
  NForm,
  NFormItem,
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
import { reactive, ref } from 'vue'
import { constraintRuleApi } from '@/api'
import { XSystemQueryPanel } from '~/components'
import { CONSTRAINT_TYPE_OPTIONS, STATUS_OPTIONS, VIOLATION_ACTION_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemConstraintRulePage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const IS_ENABLED_OPTIONS = [
  { label: '启用', value: 1 },
  { label: '禁用', value: 0 },
]

const TARGET_TYPE_OPTIONS = [
  { label: '角色', value: 0 },
  { label: '权限', value: 1 },
  { label: '用户', value: 2 },
]

const queryParams = reactive({
  keyword: '',
  constraintType: undefined as number | undefined,
  isEnabled: undefined as number | undefined,
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return constraintRuleApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    constraintType: queryParams.constraintType,
    isEnabled: queryParams.isEnabled === undefined ? undefined : queryParams.isEnabled === 1,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysConstraintRule>(
  {
    id: 'sys_constraint_rule',
    name: '约束规则',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      {
        field: 'ruleName',
        title: '规则名称',
        minWidth: 160,
        showOverflow: 'tooltip',
        sortable: true,
      },
      { field: 'ruleCode', title: '规则编码', minWidth: 180, showOverflow: 'tooltip' },
      {
        field: 'constraintType',
        title: '约束类型',
        width: 120,
        formatter: ({ cellValue }) => getOptionLabel(CONSTRAINT_TYPE_OPTIONS, cellValue),
      },
      {
        field: 'targetType',
        title: '目标类型',
        width: 100,
        formatter: ({ cellValue }) => getOptionLabel(TARGET_TYPE_OPTIONS, cellValue),
      },
      {
        field: 'violationAction',
        title: '违规处理',
        width: 120,
        formatter: ({ cellValue }) => getOptionLabel(VIOLATION_ACTION_OPTIONS, cellValue),
      },
      { field: 'priority', title: '优先级', width: 90, sortable: true },
      {
        field: 'isEnabled',
        title: '规则开关',
        width: 90,
        slots: { default: 'col_enabled' },
      },
      {
        field: 'status',
        title: '状态',
        width: 80,
        slots: { default: 'col_status' },
      },
      {
        field: 'effectiveFrom',
        title: '生效时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      {
        field: 'effectiveTo',
        title: '失效时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
      },
      {
        field: 'createTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
        sortable: true,
      },
      {
        field: 'actions',
        title: '操作',
        width: 140,
        fixed: 'right',
        slots: { default: 'col_actions' },
      },
    ],
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
  queryParams.isEnabled = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增规则')
const submitLoading = ref(false)
const formData = ref<Partial<SysConstraintRule>>({})

function resetForm() {
  formData.value = {
    ruleName: '',
    ruleCode: '',
    constraintType: 0,
    targetType: 0,
    parameters: '{}',
    isEnabled: true,
    violationAction: 0,
    priority: 0,
    status: 1,
    effectiveFrom: undefined,
    effectiveTo: undefined,
    description: '',
    remark: '',
  }
}

function handleAdd() {
  modalTitle.value = '新增规则'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysConstraintRule) {
  modalTitle.value = '编辑规则'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await constraintRuleApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await constraintRuleApi.update(formData.value.basicId, formData.value)
    }
    else {
      await constraintRuleApi.create(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <vxe-input
          v-model="queryParams.keyword"
          placeholder="搜索规则名称/编码/目标类型"
          clearable
          style="width: 280px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.constraintType"
          :options="CONSTRAINT_TYPE_OPTIONS"
          placeholder="约束类型"
          clearable
          style="width: 140px"
        />
        <NSelect
          v-model:value="queryParams.isEnabled"
          :options="IS_ENABLED_OPTIONS"
          placeholder="规则开关"
          clearable
          style="width: 120px"
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="状态"
          clearable
          style="width: 120px"
        />
        <NButton type="primary" size="small" @click="handleSearch">
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options">
        <template #toolbar_buttons>
          <NButton v-access="['constraint_rule:create']" type="primary" size="small" @click="handleAdd">
            新增规则
          </NButton>
        </template>
        <template #col_enabled="{ row }">
          <NTag :type="row.isEnabled ? 'success' : 'warning'" size="small" round>
            {{ row.isEnabled ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton v-access="['constraint_rule:update']" size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm v-access="['constraint_rule:delete']" @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该规则？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 760px"
      :auto-focus="false"
    >
      <NForm class="xh-edit-form-grid" :model="formData" label-placement="top" label-width="90px">
        <NFormItem label="规则名称" path="ruleName">
          <NInput v-model:value="formData.ruleName" placeholder="请输入规则名称" />
        </NFormItem>
        <NFormItem label="规则编码" path="ruleCode">
          <NInput v-model:value="formData.ruleCode" placeholder="如: same_tenant" />
        </NFormItem>
        <NFormItem label="约束类型" path="constraintType">
          <NSelect v-model:value="formData.constraintType" :options="CONSTRAINT_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="目标类型" path="targetType">
          <NSelect v-model:value="formData.targetType" :options="TARGET_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="违规处理" path="violationAction">
          <NSelect v-model:value="formData.violationAction" :options="VIOLATION_ACTION_OPTIONS" />
        </NFormItem>
        <NFormItem label="规则开关" path="isEnabled">
          <NSwitch v-model:value="formData.isEnabled" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="formData.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="生效时间" path="effectiveFrom">
          <NDatePicker
            v-model:formatted-value="formData.effectiveFrom"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
            clearable
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="失效时间" path="effectiveTo">
          <NDatePicker
            v-model:formatted-value="formData.effectiveTo"
            type="datetime"
            value-format="yyyy-MM-dd HH:mm:ss"
            clearable
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="规则参数" path="parameters">
          <NInput
            v-model:value="formData.parameters"
            type="textarea"
            :rows="4"
            placeholder="JSON，例如 {&quot;expression&quot;:&quot;resource.tenant_id == subject.tenant_id&quot;}"
          />
        </NFormItem>
        <NFormItem label="描述" path="description">
          <NInput
            v-model:value="formData.description"
            type="textarea"
            :rows="2"
            placeholder="规则描述"
          />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="formData.remark" type="textarea" :rows="2" placeholder="备注" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton
            v-if="!formData.basicId"
            v-access="['constraint_rule:create']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
          <NButton
            v-else
            v-access="['constraint_rule:update']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
