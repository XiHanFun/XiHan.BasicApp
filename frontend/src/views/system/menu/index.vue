<script lang="ts" setup>
import type { VxeGridInstance } from 'vxe-table'
import type { SysMenu } from '@/api/modules/menu'
import {
  NButton,
  NCascader,
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
import { computed, onMounted, ref } from 'vue'
import { menuApi } from '@/api'
import { IconPicker } from '~/components'
import { useVxeTable } from '~/hooks'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemMenuPage' })

const message = useMessage()
const xGrid = ref<VxeGridInstance>()
const loading = ref(false)
const tableData = ref<SysMenu[]>([])

const MENU_TYPE_OPTIONS = [
  { label: '目录', value: 0 },
  { label: '菜单', value: 1 },
  { label: '按钮', value: 2 },
]

const STATUS_OPTIONS = [
  { label: '启用', value: 1 },
  { label: '禁用', value: 0 },
]

const ROOT_ID = ''

function flattenMenuTree(items: SysMenu[]): SysMenu[] {
  const result: SysMenu[] = []
  for (const item of items) {
    const { children, ...rest } = item
    result.push({ ...rest, children: undefined } as SysMenu)
    if (children?.length) {
      result.push(...flattenMenuTree(children))
    }
  }
  return result
}

const treeOptions = computed(() => {
  const items = tableData.value
  const map = new Map<string, SysMenu[]>()
  for (const item of items) {
    const pid = item.parentId || ROOT_ID
    if (!map.has(pid)) {
      map.set(pid, [])
    }
    map.get(pid)!.push(item)
  }
  function toNodes(parentId: string): any[] {
    return (map.get(parentId) ?? []).map((c) => ({
      label: c.menuName,
      value: c.basicId,
      children: map.has(c.basicId) ? toNodes(c.basicId) : undefined,
    }))
  }
  return [{ label: '顶级菜单', value: ROOT_ID, children: toNodes(ROOT_ID) }]
})

async function fetchData() {
  try {
    loading.value = true
    const list = await menuApi.list()
    const flat = flattenMenuTree(list)
    tableData.value = flat.map((item) => ({
      ...item,
      parentId: item.parentId || ROOT_ID,
    }))
  } catch {
    message.error('获取菜单列表失败')
  } finally {
    loading.value = false
  }
}

const options = useVxeTable<SysMenu>(
  {
    id: 'sys_menu',
    name: '菜单管理',
    data: [],
    columns: [
      {
        field: 'menuName',
        title: '菜单名称',
        minWidth: 200,
        treeNode: true,
        showOverflow: 'tooltip',
      },
      { field: 'menuCode', title: '权限标识', minWidth: 160, showOverflow: 'tooltip' },
      {
        field: 'menuType',
        title: '类型',
        width: 80,
        slots: { default: 'col_type' },
      },
      { field: 'icon', title: '图标', width: 80, slots: { default: 'col_icon' } },
      { field: 'path', title: '路由路径', minWidth: 180, showOverflow: 'tooltip' },
      { field: 'component', title: '组件路径', minWidth: 180, showOverflow: 'tooltip' },
      { field: 'routeName', title: '路由名称', minWidth: 130, showOverflow: 'tooltip' },
      {
        field: 'isVisible',
        title: '可见',
        width: 70,
        slots: { default: 'col_visible' },
      },
      {
        field: 'isCache',
        title: '缓存',
        width: 70,
        slots: { default: 'col_cache' },
      },
      { field: 'sort', title: '排序', width: 70 },
      {
        field: 'status',
        title: '状态',
        width: 80,
        slots: { default: 'col_status' },
      },
      {
        field: 'actions',
        title: '操作',
        width: 200,
        fixed: 'right',
        slots: { default: 'col_actions' },
      },
    ],
  },
  {
    pagerConfig: { enabled: false },
    treeConfig: {
      transform: true,
      rowField: 'basicId',
      parentField: 'parentId',
      expandAll: false,
    },
    toolbarConfig: {
      slots: { buttons: 'toolbar_buttons' },
      refresh: true,
      zoom: true,
      custom: true,
    },
  },
)

const modalVisible = ref(false)
const modalTitle = ref('新增菜单')
const submitLoading = ref(false)
const formData = ref<Partial<SysMenu>>({})

function resetForm() {
  formData.value = {
    parentId: ROOT_ID,
    menuName: '',
    menuCode: '',
    menuType: 0,
    path: '',
    component: '',
    routeName: '',
    redirect: '',
    icon: '',
    title: '',
    isExternal: false,
    isCache: true,
    isVisible: true,
    isAffix: false,
    sort: 0,
    status: 1,
    remark: '',
  }
}

function handleAdd(parentId?: string) {
  modalTitle.value = '新增菜单'
  resetForm()
  if (parentId) {
    formData.value.parentId = parentId
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
    await menuApi.delete(id)
    message.success('删除成功')
    fetchData()
  } catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  try {
    submitLoading.value = true
    if (formData.value.basicId) {
      await menuApi.update(formData.value)
    } else {
      await menuApi.create(formData.value)
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

onMounted(fetchData)
</script>

<template>
  <div class="flex flex-col h-full">
    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="options" :data="tableData" :loading="loading">
        <template #toolbar_buttons>
          <NButton type="primary" size="small" @click="handleAdd()">新增菜单</NButton>
          <NButton size="small" class="ml-2" @click="fetchData">刷新</NButton>
        </template>
        <template #col_type="{ row }">
          <NTag
            :type="row.menuType === 0 ? 'info' : row.menuType === 1 ? 'success' : 'warning'"
            size="small"
          >
            {{ getOptionLabel(MENU_TYPE_OPTIONS, row.menuType) }}
          </NTag>
        </template>
        <template #col_icon="{ row }">
          <span v-if="row.icon" class="text-lg">
            <iconify-icon :icon="row.icon" />
          </span>
          <span v-else class="text-gray-300">-</span>
        </template>
        <template #col_visible="{ row }">
          <NTag :type="row.isVisible ? 'success' : 'warning'" size="small">
            {{ row.isVisible ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_cache="{ row }">
          <NTag :type="row.isCache ? 'success' : 'default'" size="small">
            {{ row.isCache ? '是' : '否' }}
          </NTag>
        </template>
        <template #col_status="{ row }">
          <NTag :type="row.status === 1 ? 'success' : 'error'" size="small" round>
            {{ row.status === 1 ? '启用' : '禁用' }}
          </NTag>
        </template>
        <template #col_actions="{ row }">
          <NSpace size="small">
            <NButton
              v-if="row.menuType !== 2"
              size="small"
              type="info"
              text
              @click="handleAdd(row.basicId)"
            >
              新增子项
            </NButton>
            <NButton size="small" type="primary" text @click="handleEdit(row)">编辑</NButton>
            <NPopconfirm @positive-click="handleDelete(row.basicId)">
              <template #trigger>
                <NButton size="small" type="error" text>删除</NButton>
              </template>
              确认删除该菜单？子菜单也会一并删除。
            </NPopconfirm>
          </NSpace>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 700px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="90px">
        <NFormItem label="上级菜单" path="parentId">
          <NCascader
            v-model:value="formData.parentId"
            :options="treeOptions"
            check-strategy="child"
            placeholder="无则为顶级"
            clearable
            style="width: 100%"
          />
        </NFormItem>
        <NFormItem label="菜单类型" path="menuType">
          <NSelect v-model:value="formData.menuType" :options="MENU_TYPE_OPTIONS" />
        </NFormItem>
        <NFormItem label="菜单名称" path="menuName">
          <NInput v-model:value="formData.menuName" placeholder="请输入菜单名称" />
        </NFormItem>
        <NFormItem label="权限标识" path="menuCode">
          <NInput v-model:value="formData.menuCode" placeholder="如: system:user:list" />
        </NFormItem>
        <NFormItem v-if="formData.menuType !== 2" label="路由路径" path="path">
          <NInput v-model:value="formData.path" placeholder="如: /system/user" />
        </NFormItem>
        <NFormItem v-if="formData.menuType === 1" label="组件路径" path="component">
          <NInput v-model:value="formData.component" placeholder="如: system/user/index" />
        </NFormItem>
        <NFormItem v-if="formData.menuType !== 2" label="路由名称" path="routeName">
          <NInput v-model:value="formData.routeName" placeholder="路由名称" />
        </NFormItem>
        <NFormItem v-if="formData.menuType !== 2" label="重定向" path="redirect">
          <NInput v-model:value="formData.redirect" placeholder="重定向路径" />
        </NFormItem>
        <NFormItem v-if="formData.menuType !== 2" label="图标" path="icon">
          <IconPicker v-model="formData.icon" />
        </NFormItem>
        <NFormItem label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem label="状态" path="status">
          <NSelect v-model:value="formData.status" :options="STATUS_OPTIONS" />
        </NFormItem>
        <NFormItem v-if="formData.menuType !== 2" label="是否可见">
          <NSwitch v-model:value="formData.isVisible" />
        </NFormItem>
        <NFormItem v-if="formData.menuType === 1" label="是否缓存">
          <NSwitch v-model:value="formData.isCache" />
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
