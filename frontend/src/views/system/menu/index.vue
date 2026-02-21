<script lang="ts" setup>
import { ref, h, onMounted } from 'vue'
import {
  NCard,
  NDataTable,
  NButton,
  NSpace,
  NTag,
  NIcon,
  NModal,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NSelect,
  NSwitch,
  NPopconfirm,
  NTreeSelect,
  useMessage,
} from 'naive-ui'
import type { DataTableColumns } from 'naive-ui'
import { Icon } from '@iconify/vue'
import type { SysMenu } from '~/types'
import { MENU_TYPE, STATUS_OPTIONS } from '~/constants'
import { getMenuListApi, createMenuApi, updateMenuApi, deleteMenuApi } from '@/api'
import { listToTree } from '~/utils'

defineOptions({ name: 'SystemMenuPage' })

const message = useMessage()
const loading = ref(false)
const tableData = ref<SysMenu[]>([])
const treeSelectOptions = ref<any[]>([])

const modalVisible = ref(false)
const modalTitle = ref('新增菜单')
const submitLoading = ref(false)

const formData = ref<Partial<SysMenu>>({
  parentId: undefined,
  name: '',
  path: '',
  component: '',
  icon: '',
  type: MENU_TYPE.MENU,
  permission: '',
  sort: 100,
  status: 1,
  hidden: false,
})

const menuTypeOptions = [
  { label: '目录', value: MENU_TYPE.DIR },
  { label: '菜单', value: MENU_TYPE.MENU },
  { label: '按钮', value: MENU_TYPE.BUTTON },
]

async function fetchData() {
  try {
    loading.value = true
    const list = await getMenuListApi()
    tableData.value = listToTree(list) as SysMenu[]
    treeSelectOptions.value = buildTreeSelectOptions(tableData.value)
  } catch {
    message.error('获取菜单列表失败')
  } finally {
    loading.value = false
  }
}

function buildTreeSelectOptions(list: SysMenu[]): any[] {
  return list.map((item) => ({
    label: item.name,
    value: item.id,
    children: item.children ? buildTreeSelectOptions(item.children) : undefined,
  }))
}

function handleAdd(parentId?: string) {
  modalTitle.value = '新增菜单'
  formData.value = {
    parentId,
    name: '',
    path: '',
    component: '',
    icon: '',
    type: MENU_TYPE.MENU,
    permission: '',
    sort: 100,
    status: 1,
    hidden: false,
  }
  modalVisible.value = true
}

function handleEdit(row: SysMenu) {
  modalTitle.value = '编辑菜单'
  formData.value = { ...row }
  modalVisible.value = true
}

async function handleDelete(id: string) {
  try {
    await deleteMenuApi(id)
    message.success('删除成功')
    fetchData()
  } catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.id) {
      await updateMenuApi(formData.value.id, formData.value)
    } else {
      await createMenuApi(formData.value)
    }
    message.success('操作成功')
    modalVisible.value = false
    fetchData()
  } catch {
    message.error('操作失败')
  } finally {
    submitLoading.value = false
  }
}

const columns: DataTableColumns<SysMenu> = [
  {
    title: '菜单名称',
    key: 'name',
    width: 200,
  },
  {
    title: '类型',
    key: 'type',
    width: 80,
    align: 'center',
    render: (row) => {
      const typeMap: Record<number, { label: string; type: 'default' | 'info' | 'success' | 'warning' | 'error' }> = {
        [MENU_TYPE.DIR]: { label: '目录', type: 'default' },
        [MENU_TYPE.MENU]: { label: '菜单', type: 'info' },
        [MENU_TYPE.BUTTON]: { label: '按钮', type: 'success' },
      }
      const t = typeMap[row.type] ?? { label: '未知', type: 'default' }
      return h(NTag, { type: t.type, size: 'small', bordered: false }, { default: () => t.label })
    },
  },
  {
    title: '路由路径',
    key: 'path',
    width: 180,
    ellipsis: { tooltip: true },
  },
  {
    title: '权限标识',
    key: 'permission',
    width: 180,
    ellipsis: { tooltip: true },
    render: (row) =>
      row.permission
        ? h(NTag, { type: 'warning', size: 'small', bordered: false }, { default: () => row.permission })
        : h('span', { class: 'text-gray-300' }, '-'),
  },
  {
    title: '排序',
    key: 'sort',
    width: 70,
    align: 'center',
  },
  {
    title: '状态',
    key: 'status',
    width: 80,
    render: (row) =>
      h(NTag, { type: row.status === 1 ? 'success' : 'error', size: 'small', round: true }, {
        default: () => (row.status === 1 ? '启用' : '禁用'),
      }),
  },
  {
    title: '操作',
    key: 'actions',
    width: 200,
    fixed: 'right',
    render: (row) =>
      h(NSpace, { size: 'small' }, {
        default: () => [
          row.type !== MENU_TYPE.BUTTON &&
            h(NButton, {
              size: 'small',
              ghost: true,
              onClick: () => handleAdd(row.id),
            }, { default: () => '添加子项' }),
          h(NButton, {
            size: 'small',
            type: 'primary',
            ghost: true,
            onClick: () => handleEdit(row),
          }, { default: () => '编辑' }),
          h(NPopconfirm, {
            onPositiveClick: () => handleDelete(row.id),
          }, {
            default: () => '确认删除该菜单？',
            trigger: () =>
              h(NButton, { size: 'small', type: 'error', ghost: true }, { default: () => '删除' }),
          }),
        ].filter(Boolean),
      }),
  },
]

onMounted(fetchData)
</script>

<template>
  <div class="space-y-4">
    <NCard :bordered="false">
      <div class="flex items-center gap-3">
        <NButton type="primary" @click="() => handleAdd()">
          <template #icon><NIcon><Icon icon="lucide:plus" width="14" /></NIcon></template>
          新增菜单
        </NButton>
        <NButton @click="fetchData">
          <template #icon><NIcon><Icon icon="lucide:refresh-cw" width="14" /></NIcon></template>
          刷新
        </NButton>
      </div>
    </NCard>

    <NCard :bordered="false">
      <NDataTable
        :columns="columns"
        :data="tableData"
        :loading="loading"
        :row-key="(row) => row.id"
        :scroll-x="1000"
        :pagination="false"
        size="small"
        :default-expand-all="true"
      />
    </NCard>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 560px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="80px">
        <NFormItem label="上级菜单" path="parentId">
          <NTreeSelect
            v-model:value="formData.parentId"
            :options="[{ label: '根目录', value: null }, ...treeSelectOptions]"
            clearable
            placeholder="请选择上级菜单（不选则为顶级）"
          />
        </NFormItem>
        <NFormItem label="类型" path="type">
          <NSelect v-model:value="formData.type" :options="menuTypeOptions" />
        </NFormItem>
        <NFormItem label="菜单名称" path="name">
          <NInput v-model:value="formData.name" placeholder="请输入菜单名称" />
        </NFormItem>
        <NFormItem v-if="formData.type !== MENU_TYPE.BUTTON" label="路由路径" path="path">
          <NInput v-model:value="formData.path" placeholder="如: /system/user" />
        </NFormItem>
        <NFormItem v-if="formData.type === MENU_TYPE.MENU" label="组件路径" path="component">
          <NInput v-model:value="formData.component" placeholder="如: system/user/index" />
        </NFormItem>
        <NFormItem v-if="formData.type !== MENU_TYPE.DIR" label="权限标识" path="permission">
          <NInput v-model:value="formData.permission" placeholder="如: system:user:list" />
        </NFormItem>
        <NFormItem v-if="formData.type !== MENU_TYPE.BUTTON" label="图标" path="icon">
          <NInput v-model:value="formData.icon" placeholder="如: lucide:users" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" :max="9999" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem v-if="formData.type !== MENU_TYPE.BUTTON" label="是否隐藏" path="hidden">
          <NSwitch v-model:value="formData.hidden" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitLoading" @click="handleSubmit">确认</NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>
