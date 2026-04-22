<script lang="ts" setup>
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { SysFieldLevelSecurity } from '@/api/modules/field-level-security'
import {
  NButton,
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
import { fieldLevelSecurityApi } from '@/api'
import { XSystemQueryPanel } from '~/components'
import { STATUS_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemFieldLevelSecurityPage' })

const TARGET_TYPE_OPTIONS = [
  { label: '角色', value: 0 },
  { label: '用户', value: 1 },
  { label: '权限', value: 2 },
]

const MASK_STRATEGY_OPTIONS = [
  { label: '不脱敏', value: 0 },
  { label: '完全隐藏', value: 1 },
  { label: '全部星号', value: 2 },
  { label: '部分脱敏', value: 3 },
  { label: '哈希', value: 4 },
  { label: '固定替换', value: 5 },
  { label: '自定义', value: 99 },
]

const message = useMessage()
const xGrid = ref<VxeGridInstance>()

const queryParams = reactive({
  keyword: '',
  targetType: undefined as number | undefined,
  status: undefined as number | undefined,
})

function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  return fieldLevelSecurityApi.page({
    page: page.currentPage,
    pageSize: page.pageSize,
    keyword: queryParams.keyword,
    targetType: queryParams.targetType,
    status: queryParams.status,
  })
}

const options = useVxeTable<SysFieldLevelSecurity>(
  {
    id: 'sys_field_level_security',
    name: '字段级安全',
    columns: [
      { type: 'seq', title: '序号', width: 60, fixed: 'left' },
      {
        field: 'fieldName',
        title: '字段名',
        minWidth: 180,
        showOverflow: 'tooltip',
      },
      {
        field: 'targetType',
        title: '目标类型',
        width: 100,
        formatter: ({ cellValue }) => getOptionLabel(TARGET_TYPE_OPTIONS, cellValue),
      },
      { field: 'targetId', title: '目标 ID', minWidth: 120, showOverflow: 'tooltip' },
      { field: 'resourceId', title: '资源 ID', minWidth: 120, showOverflow: 'tooltip' },
      {
        field: 'isReadable',
        title: '可读',
        width: 80,
        slots: { default: 'col_readable' },
      },
      {
        field: 'isEditable',
        title: '可编辑',
        width: 80,
        slots: { default: 'col_editable' },
      },
      {
        field: 'maskStrategy',
        title: '脱敏策略',
        width: 110,
        formatter: ({ cellValue }) => getOptionLabel(MASK_STRATEGY_OPTIONS, cellValue),
      },
      { field: 'priority', title: '优先级', width: 90 },
      {
        field: 'status',
        title: '状态',
        width: 80,
        slots: { default: 'col_status' },
      },
      {
        field: 'createTime',
        title: '创建时间',
        width: 170,
        formatter: ({ cellValue }) => formatDate(cellValue),
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
  queryParams.targetType = undefined
  queryParams.status = undefined
  xGrid.value?.commitProxy('reload')
}

const modalVisible = ref(false)
const modalTitle = ref('新增字段级安全策略')
const submitLoading = ref(false)
const formData = ref<Partial<SysFieldLevelSecurity>>({})

function resetForm() {
  formData.value = {
    targetType: 0,
    targetId: '',
    resourceId: '',
    fieldName: '',
    isReadable: true,
    isEditable: true,
    maskStrategy: 0,
    maskPattern: '',
    priority: 0,
    status: 1,
    description: '',
    remark: '',
  }
}

function handleAdd() {
  modalTitle.value = '新增字段级安全策略'
  resetForm()
  modalVisible.value = true
}

function handleEdit(row: SysFieldLevelSecurity) {
  modalTitle.value = '编辑字段级安全策略'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await fieldLevelSecurityApi.delete(id)
    message.success('删除成功')
    xGrid.value?.commitProxy('query')
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    if (!formData.value.targetId || !formData.value.resourceId || !formData.value.fieldName) {
      message.warning('目标 ID、资源 ID、字段名不能为空')
      return
    }

    submitLoading.value = true
    if (formData.value.basicId) {
      await fieldLevelSecurityApi.update(formData.value.basicId, formData.value)
    }
    else {
      await fieldLevelSecurityApi.create(formData.value)
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
          placeholder="搜索字段名/描述/脱敏模式"
          clearable
          style="width: 280px"
          @keyup.enter="handleSearch"
        />
        <NSelect
          v-model:value="queryParams.targetType"
          :options="TARGET_TYPE_OPTIONS"
          placeholder="目标类型"
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
          <NButton v-access="['field_level_security:create']" type="primary" size="small" @click="handleAdd">
            新增策略
          </NButton>
        </template>
        <template #col_readable="{ row }">
          <NTag :type="row.isReadable ? 'success' : 'error'" size="small" round>
            {{ row.isReadable ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_editable="{ row }">
          <NTag :type="row.isEditable ? 'success' : 'warning'" size="small" round>
            {{ row.isEditable ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton v-access="['field_level_security:update']" size="small" type="primary" text @click="handleEdit(row)">
              编辑
            </NButton>
            <NPopconfirm v-access="['field_level_security:delete']" @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>
                  删除
                </NButton>
              </template>
              确认删除该策略？
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal v-model:show="modalVisible" :title="modalTitle" preset="card" style="width: 720px" :auto-focus="false">
      <NForm class="xh-edit-form-grid" :model="formData" label-placement="top" label-width="90px">
        <NFormItem label="目标类型" path="targetType">
          <NSelect v-model:value="formData.targetType" :options="TARGET_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="目标 ID" path="targetId">
          <NInput v-model:value="formData.targetId" placeholder="角色/用户/权限 ID" />
        </NFormItem>
        <NFormItem label="资源 ID" path="resourceId">
          <NInput v-model:value="formData.resourceId" placeholder="资源 ID" />
        </NFormItem>
        <NFormItem label="字段名" path="fieldName">
          <NInput v-model:value="formData.fieldName" placeholder="如: Salary、Phone" />
        </NFormItem>
        <NFormItem label="可读" path="isReadable">
          <NSwitch v-model:value="formData.isReadable" />
        </NFormItem>
        <NFormItem label="可编辑" path="isEditable">
          <NSwitch v-model:value="formData.isEditable" />
        </NFormItem>
        <NFormItem label="脱敏策略" path="maskStrategy">
          <NSelect v-model:value="formData.maskStrategy" :options="MASK_STRATEGY_OPTIONS" />
        </NFormItem>
        <NFormItem label="优先级" path="priority">
          <NInputNumber v-model:value="formData.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="脱敏模式" path="maskPattern" class="xh-form-full-row">
          <NInput v-model:value="formData.maskPattern" placeholder='如: {"keepLeft":3,"keepRight":4,"maskChar":"*"}' />
        </NFormItem>
        <NFormItem label="描述" path="description" class="xh-form-full-row">
          <NInput v-model:value="formData.description" type="textarea" :rows="2" placeholder="策略说明" />
        </NFormItem>
        <NFormItem label="备注" path="remark" class="xh-form-full-row">
          <NInput v-model:value="formData.remark" type="textarea" :rows="2" placeholder="备注" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton
            v-if="!formData.basicId"
            v-access="['field_level_security:create']"
            type="primary"
            :loading="submitLoading"
            @click="handleSubmit"
          >
            确认
          </NButton>
          <NButton
            v-else
            v-access="['field_level_security:update']"
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
