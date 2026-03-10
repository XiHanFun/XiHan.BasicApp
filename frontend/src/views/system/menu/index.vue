<script lang="ts" setup>
import type { DataTableColumns } from 'naive-ui'
import type { SysMenu } from '~/types'
import { Icon } from '@iconify/vue'
import {
  NButton,
  NCard,
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
  NTreeSelect,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import { createMenuApi, deleteMenuApi, getMenuListApi, updateMenuApi } from '@/api'
import { CrudProTable } from '~/components'
import { MENU_TYPE, STATUS_OPTIONS } from '~/constants'
import { filterTree } from '~/utils'

defineOptions({ name: 'SystemMenuPage' })

const message = useMessage()
const loading = ref(false)
const treeData = ref<SysMenu[]>([])
const keyword = ref('')

const modalVisible = ref(false)
const modalTitle = ref('新增菜单')
const submitLoading = ref(false)

function defaultForm(): Partial<SysMenu> {
  return {
  parentId: undefined,
  menuName: '',
  menuCode: '',
  menuType: MENU_TYPE.MENU,
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
  sort: 100,
  status: 1,
  remark: '',
  }
}

const formData = ref<Partial<SysMenu>>(defaultForm())

const menuTypeOptions = [
  { label: '目录', value: MENU_TYPE.DIR },
  { label: '菜单', value: MENU_TYPE.MENU },
  { label: '按钮', value: MENU_TYPE.BUTTON },
]

const statusOptions = STATUS_OPTIONS

async function fetchData() {
  try {
    loading.value = true
    treeData.value = await getMenuListApi()
  }
  catch {
    message.error('获取菜单列表失败')
  }
  finally {
    loading.value = false
  }
}

function buildTreeSelectOptions(list: SysMenu[]): any[] {
  return list
    .filter(item => item.menuType !== MENU_TYPE.BUTTON)
    .map(item => ({
      label: item.menuName,
      value: item.basicId,
      children: item.children ? buildTreeSelectOptions(item.children) : undefined,
    }))
}

const treeSelectOptions = computed(() => buildTreeSelectOptions(treeData.value))

const displayTableData = computed(() => {
  return filterTree(treeData.value, keyword.value, (node, kw) => {
    return [node.menuName, node.menuCode, node.path]
      .filter(Boolean)
      .some(text => String(text).toLowerCase().includes(kw))
  })
})

function resolveIcon(icon: string) {
  if (!icon)
    return icon
  return icon.includes(':') ? icon : `lucide:${icon}`
}

function handleAdd(parentId?: string) {
  modalTitle.value = '新增菜单'
  formData.value = { ...defaultForm(), parentId }
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
  }
  catch {
    message.error('删除失败')
  }
}

async function handleSubmit() {
  if (!formData.value.menuName?.trim()) {
    message.warning('请输入菜单名称')
    return
  }
  if (!formData.value.menuCode?.trim()) {
    message.warning('请输入菜单编码')
    return
  }
  try {
    submitLoading.value = true
    const payload = { ...formData.value }
    if (payload.title === undefined || payload.title === '') {
      payload.title = payload.menuName
    }
    if (payload.basicId) {
      await updateMenuApi(payload.basicId, payload)
    }
    else {
      await createMenuApi(payload)
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

const columns: DataTableColumns<SysMenu> = [
  {
    title: '菜单名称',
    key: 'menuName',
    width: 220,
    tree: true,
    render: (row) => {
      const children = []
      if (row.icon) {
        children.push(
          h(Icon, {
            icon: resolveIcon(row.icon),
            width: 16,
            class: 'mr-1.5 flex-shrink-0 opacity-70',
          }),
        )
      }
      children.push(h('span', {}, row.menuName))
      return h('div', { class: 'inline-flex items-center' }, children)
    },
  },
  {
    title: '菜单编码',
    key: 'menuCode',
    width: 140,
    ellipsis: { tooltip: true },
    render: row =>
      h(
        NTag,
        { size: 'small', bordered: false, type: 'info' },
        { default: () => row.menuCode },
      ),
  },
  {
    title: '类型',
    key: 'menuType',
    width: 80,
    align: 'center',
    render: row => {
      const map: Record<number, { label: string, type: 'default' | 'info' | 'success' }> = {
        [MENU_TYPE.DIR]: { label: '目录', type: 'default' },
        [MENU_TYPE.MENU]: { label: '菜单', type: 'info' },
        [MENU_TYPE.BUTTON]: { label: '按钮', type: 'success' },
      }
      const t = map[row.menuType] ?? { label: '未知', type: 'default' }
      return h(NTag, { type: t.type, size: 'small', bordered: false }, { default: () => t.label })
    },
  },
  {
    title: '路由路径',
    key: 'path',
    width: 160,
    ellipsis: { tooltip: true },
    render: row =>
      row.path
        ? h('span', { class: 'text-xs font-mono' }, row.path)
        : h('span', { class: 'text-gray-300' }, '-'),
  },
  {
    title: '组件',
    key: 'component',
    width: 180,
    ellipsis: { tooltip: true },
    render: row =>
      row.component
        ? h('span', { class: 'text-xs font-mono' }, row.component)
        : h('span', { class: 'text-gray-300' }, '-'),
  },
  {
    title: '排序',
    key: 'sort',
    width: 70,
    align: 'center',
  },
  {
    title: '可见',
    key: 'isVisible',
    width: 70,
    align: 'center',
    render: row =>
      h(
        NTag,
        {
          type: row.isVisible ? 'success' : 'default',
          size: 'small',
          round: true,
          bordered: false,
        },
        { default: () => (row.isVisible ? '是' : '否') },
      ),
  },
  {
    title: '状态',
    key: 'status',
    width: 70,
    align: 'center',
    render: row =>
      h(
        NTag,
        { type: row.status === 1 ? 'success' : 'error', size: 'small', round: true },
        { default: () => (row.status === 1 ? '启用' : '禁用') },
      ),
  },
  {
    title: '操作',
    key: 'actions',
    width: 200,
    fixed: 'right',
    render: row =>
      h(
        NSpace,
        { size: 'small' },
        {
          default: () =>
            [
              row.menuType !== MENU_TYPE.BUTTON
              && h(
                NButton,
                { size: 'small', ghost: true, onClick: () => handleAdd(row.basicId) },
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
                { onPositiveClick: () => handleDelete(row.basicId) },
                {
                  default: () => '确认删除该菜单？',
                  trigger: () =>
                    h(
                      NButton,
                      { size: 'small', type: 'error', ghost: true },
                      { default: () => '删除' },
                    ),
                },
              ),
            ].filter(Boolean),
        },
      ),
  },
]

onMounted(fetchData)
</script>

<template>
  <div class="space-y-3">
    <NCard :bordered="false">
      <div class="flex items-center gap-3">
        <NButton type="primary" @click="() => handleAdd()">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" width="14" /></NIcon>
          </template>
          新增菜单
        </NButton>
        <NButton @click="fetchData">
          <template #icon>
            <NIcon><Icon icon="lucide:refresh-cw" width="14" /></NIcon>
          </template>
          刷新
        </NButton>
        <NInput
          v-model:value="keyword"
          class="ml-auto max-w-[280px]"
          placeholder="搜索菜单名称/编码/路径"
          clearable
        />
      </div>
    </NCard>

    <CrudProTable
      :columns="columns"
      :data="displayTableData"
      :loading="loading"
      :row-key="(row: SysMenu) => row.basicId"
      :scroll-x="1200"
      max-height="calc(100vh - 280px)"
      :show-toolbar="false"
      :show-pagination="false"
      :default-expand-all="true"
    />

    <NModal
      v-model:show="modalVisible"
      :title="modalTitle"
      preset="card"
      style="width: 620px"
      :auto-focus="false"
    >
      <NForm :model="formData" label-placement="left" label-width="90px" size="small">
        <NFormItem label="上级菜单" path="parentId">
          <NTreeSelect
            v-model:value="formData.parentId"
            :options="[{ label: '根目录', value: null }, ...treeSelectOptions]"
            clearable
            placeholder="不选则为顶级菜单"
          />
        </NFormItem>
        <NFormItem label="菜单类型" path="menuType">
          <NSelect v-model:value="formData.menuType" :options="menuTypeOptions" />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem label="菜单名称" path="menuName">
            <NInput v-model:value="formData.menuName" placeholder="如: 账号管理" />
          </NFormItem>
          <NFormItem label="菜单编码" path="menuCode">
            <NInput v-model:value="formData.menuCode" placeholder="如: user" />
          </NFormItem>
        </div>
        <div v-if="formData.menuType !== MENU_TYPE.BUTTON" class="grid grid-cols-2 gap-x-4">
          <NFormItem label="路由路径" path="path">
            <NInput v-model:value="formData.path" placeholder="如: /system/user" />
          </NFormItem>
          <NFormItem v-if="formData.menuType === MENU_TYPE.MENU" label="组件路径" path="component">
            <NInput v-model:value="formData.component" placeholder="如: System/User/Index" />
          </NFormItem>
          <NFormItem v-if="formData.menuType === MENU_TYPE.DIR" label="重定向" path="redirect">
            <NInput v-model:value="formData.redirect" placeholder="如: /system/user" />
          </NFormItem>
        </div>
        <div v-if="formData.menuType !== MENU_TYPE.BUTTON" class="grid grid-cols-2 gap-x-4">
          <NFormItem label="路由名称" path="routeName">
            <NInput v-model:value="formData.routeName" placeholder="如: SystemUser" />
          </NFormItem>
          <NFormItem label="菜单标题" path="title">
            <NInput v-model:value="formData.title" placeholder="如: 账号管理" />
          </NFormItem>
        </div>
        <div v-if="formData.menuType !== MENU_TYPE.BUTTON" class="grid grid-cols-2 gap-x-4">
          <NFormItem label="图标" path="icon">
            <div class="flex items-center gap-2 w-full">
              <NInput v-model:value="formData.icon" placeholder="如: user" class="flex-1" />
              <Icon
                v-if="formData.icon"
                :icon="resolveIcon(formData.icon)"
                width="20"
                class="flex-shrink-0 text-[var(--primary-color)]"
              />
            </div>
          </NFormItem>
          <NFormItem label="排序" path="sort">
            <NInputNumber v-model:value="formData.sort" :min="0" :max="9999" class="w-full" />
          </NFormItem>
        </div>
        <NFormItem v-if="formData.menuType === MENU_TYPE.BUTTON" label="排序" path="sort">
          <NInputNumber v-model:value="formData.sort" :min="0" :max="9999" class="w-full" />
        </NFormItem>
        <div class="grid grid-cols-2 gap-x-4">
          <NFormItem label="状态" path="status">
            <NSelect v-model:value="formData.status" :options="statusOptions" />
          </NFormItem>
          <NFormItem v-if="formData.menuType !== MENU_TYPE.BUTTON" label="是否可见" path="isVisible">
            <NSwitch v-model:value="formData.isVisible" />
          </NFormItem>
        </div>
        <div v-if="formData.menuType === MENU_TYPE.MENU" class="grid grid-cols-2 gap-x-4">
          <NFormItem label="是否缓存" path="isCache">
            <NSwitch v-model:value="formData.isCache" />
          </NFormItem>
          <NFormItem label="是否固定" path="isAffix">
            <NSwitch v-model:value="formData.isAffix" />
          </NFormItem>
        </div>
        <NFormItem v-if="formData.menuType !== MENU_TYPE.BUTTON" label="是否外链" path="isExternal">
          <NSwitch v-model:value="formData.isExternal" />
        </NFormItem>
        <NFormItem v-if="formData.isExternal" label="外链地址" path="externalUrl">
          <NInput v-model:value="formData.externalUrl" placeholder="https://..." />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="formData.remark" type="textarea" :rows="2" placeholder="备注信息" />
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
