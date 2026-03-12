<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysDepartment } from '~/types'
import { Icon } from '~/iconify'
import {
  NButton,
  NCard,
  NDataTable,
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
  NTreeSelect,
  useMessage,
} from 'naive-ui'
import { h, onMounted, reactive, ref } from 'vue'
import {
  createDepartmentApi,
  deleteDepartmentApi,
  getDepartmentTreeApi,
  updateDepartmentApi,
} from '@/api'
import { DEPARTMENT_TYPE_OPTIONS, STATUS_OPTIONS } from '~/constants'
import { getOptionLabel, getStatusType } from '~/utils'

defineOptions({ name: 'SystemDepartmentPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysDepartment[]>([])
const treeSelectOptions = ref<any[]>([])

const queryParams = reactive({
  keyword: '',
  status: undefined as number | undefined,
  departmentType: undefined as number | undefined,
})

const modalVisible = ref(false)
const modalTitle = ref('新增部门')
const submitLoading = ref(false)

const formData = ref<Partial<SysDepartment>>({
  parentId: undefined,
  departmentName: '',
  departmentCode: '',
  departmentType: 6,
  leaderId: undefined,
  phone: '',
  email: '',
  address: '',
  sort: 100,
  status: 1,
  remark: '',
})

function buildTreeSelectOptions(list: SysDepartment[]): any[] {
  return list.map(item => ({
    label: item.departmentName,
    value: item.basicId,
    children: item.children?.length ? buildTreeSelectOptions(item.children) : undefined,
  }))
}

async function fetchData() {
  try {
    loading.value = true
    tableData.value = await getDepartmentTreeApi(queryParams)
    treeSelectOptions.value = buildTreeSelectOptions(tableData.value)
  }
  catch {
    message.error('获取部门列表失败')
  }
  finally {
    loading.value = false
  }
}

function handleAdd(parentId?: string | null) {
  modalTitle.value = '新增部门'
  formData.value = {
    parentId: parentId ?? undefined,
    departmentName: '',
    departmentCode: '',
    departmentType: 6,
    leaderId: undefined,
    phone: '',
    email: '',
    address: '',
    sort: 100,
    status: 1,
    remark: '',
  }
  modalVisible.value = true
}

function handleEdit(row: SysDepartment) {
  modalTitle.value = '编辑部门'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteDepartmentApi(id)
    message.success('删除成功')
    fetchData()
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await updateDepartmentApi(formData.value.basicId, formData.value)
    }
    else {
      await createDepartmentApi(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    fetchData()
  }
  catch {
    message.error('操作失败')
  }
  finally {
    submitLoading.value = false
  }
}

function handleReset() {
  queryParams.keyword = ''
  queryParams.status = undefined
  queryParams.departmentType = undefined
  fetchData()
}

const columns: DataTableColumns<SysDepartment> = [
  {
    title: '部门名称',
    key: 'departmentName',
    width: 220,
  },
  {
    title: '部门编码',
    key: 'departmentCode',
    width: 170,
  },
  {
    title: '部门类型',
    key: 'departmentType',
    width: 120,
    render: row => getOptionLabel(DEPARTMENT_TYPE_OPTIONS, row.departmentType ?? 6),
  },
  {
    title: '负责人ID',
    key: 'leaderId',
    width: 110,
    align: 'center',
    render: row => row.leaderId ?? '-',
  },
  {
    title: '联系电话',
    key: 'phone',
    width: 140,
  },
  {
    title: '排序',
    key: 'sort',
    width: 80,
    align: 'center',
  },
  {
    title: '状态',
    key: 'status',
    width: 90,
    render: row =>
      h(
        NTag,
        { type: getStatusType(row.status ?? 1), size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '操作',
    key: 'actions',
    width: 220,
    fixed: 'right',
    render: row =>
      h(
        NSpace,
        { size: 'small' },
        {
          default: () => [
            h(
              NButton,
              {
                size: 'small',
                ghost: true,
                onClick: () => handleAdd(row.basicId),
              },
              { default: () => '添加子项' },
            ),
            h(
              NButton,
              {
                size: 'small',
                type: 'primary',
                ghost: true,
                onClick: () => handleEdit(row),
              },
              { default: () => '编辑' },
            ),
            h(
              NPopconfirm,
              {
                onPositiveClick: () => handleDelete(row.basicId),
              },
              {
                default: () => '确认删除该部门？',
                trigger: () =>
                  h(
                    NButton,
                    { size: 'small', type: 'error', ghost: true },
                    { default: () => '删除' },
                  ),
              },
            ),
          ],
        },
      ),
  },
]

onMounted(fetchData)
</script>

<template>
  <div class="space-y-4">
    <NCard :bordered="false">
      <div class="flex flex-wrap items-center gap-3">
        <NInput
          v-model:value="queryParams.keyword"
          placeholder="搜索部门名称/编码/联系方式"
          style="width: 260px"
          clearable
          @keydown.enter="fetchData"
        />
        <NSelect
          v-model:value="queryParams.departmentType"
          :options="DEPARTMENT_TYPE_OPTIONS"
          placeholder="部门类型"
          style="width: 130px"
          clearable
        />
        <NSelect
          v-model:value="queryParams.status"
          :options="STATUS_OPTIONS"
          placeholder="状态"
          style="width: 110px"
          clearable
        />
        <NButton type="primary" @click="fetchData">
          <template #icon>
            <NIcon><Icon icon="lucide:search" width="14" /></NIcon>
          </template>
          搜索
        </NButton>
        <NButton @click="handleReset">
          重置
        </NButton>
        <NButton class="ml-auto" type="primary" @click="() => handleAdd()">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增部门
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :pagination="false"
        :default-expand-all="true"
        :row-key="(row) => row.basicId"
        :scroll-x="1300"
        size="small"
        striped
      />
    </NCard>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 620px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="上级部门" path="parentId">
          <NTreeSelect
            v-model:value="formData.parentId"
            :options="[{ label: '根部门', value: null }, ...treeSelectOptions]"
            clearable
            placeholder="不选则创建顶级部门"
          />
        </NFormItem>
        <NFormItem label="部门名称" path="departmentName">
          <NInput v-model:value="formData.departmentName" placeholder="请输入部门名称" />
        </NFormItem>
        <NFormItem label="部门编码" path="departmentCode">
          <NInput v-model:value="formData.departmentCode" placeholder="请输入部门编码" />
        </NFormItem>
        <NFormItem label="部门类型" path="departmentType">
          <NSelect v-model:value="formData.departmentType" :options="DEPARTMENT_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="负责人ID" path="leaderId">
          <NInputNumber v-model:value="formData.leaderId" :min="1" class="w-full" />
        </NFormItem>
        <NFormItem label="联系电话" path="phone">
          <NInput v-model:value="formData.phone" placeholder="请输入联系电话" />
        </NFormItem>
        <NFormItem label="邮箱" path="email">
          <NInput v-model:value="formData.email" placeholder="请输入邮箱" />
        </NFormItem>
        <NFormItem label="地址" path="address">
          <NInput
            v-model:value="formData.address"
            type="textarea"
            :rows="2"
            placeholder="请输入地址"
          />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" :max="9999" class="w-full" />
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
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">
            确认
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
