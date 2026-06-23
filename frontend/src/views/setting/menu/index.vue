<script setup lang="ts">
import type { DataTableColumns, TreeSelectOption } from 'naive-ui'
import type { ApiId, MenuCreateDto, MenuDetailDto, MenuListItemDto, MenuTreeNodeDto, MenuUpdateDto } from '@/api'
import type { PageSchema, SchemaActionPayload, SchemaQueryParams } from '~/components'
import {
  NButton,
  NConfigProvider,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NSelect,
  NSpace,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  NTreeSelect,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  EnableStatus,
  menuManagementApi,
  MenuType,
} from '@/api'
import { Icon, IconPicker, SchemaPage } from '~/components'
import { useUserStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformMenuPage' })

const { t } = useI18n()
const userStore = useUserStore()

/**
 * 全局菜单(TenantId=0)仅平台运维态可维护；非平台态隐藏编辑/启停/删除入口，
 * 避免点击后撞后端「平台级全局菜单仅平台运维态可维护」错误。
 */
function canMaintainMenu(row: unknown): boolean {
  const menu = row as MenuListItemDto
  return !menu.isGlobal || (userStore.userInfo?.isPlatform ?? false)
}

interface MenuFormModel extends MenuCreateDto {
  basicId?: ApiId
}

interface MenuTreeItem extends MenuListItemDto {
  children?: MenuTreeItem[]
}

const message = useMessage()

const statusOptions = computed(() => [
  { label: t('common.actions.enable'), value: EnableStatus.Enabled },
  { label: t('common.actions.disable'), value: EnableStatus.Disabled },
])

const menuTypeOptions = computed(() => [
  { label: t('setting.menu.type_directory'), value: MenuType.Directory },
  { label: t('setting.menu.type_menu'), value: MenuType.Menu },
  { label: t('setting.menu.type_button'), value: MenuType.Button },
])

const badgeTypeOptions = computed(() => [
  { label: t('setting.menu.badge_default'), value: 'default' },
  { label: t('setting.menu.badge_primary'), value: 'primary' },
  { label: t('setting.menu.badge_info'), value: 'info' },
  { label: t('setting.menu.badge_success'), value: 'success' },
  { label: t('setting.menu.badge_warning'), value: 'warning' },
  { label: t('setting.menu.badge_error'), value: 'error' },
])

type BadgeTagType = 'default' | 'primary' | 'info' | 'success' | 'warning' | 'error'

const badgeDotColorMap: Record<string, string> = {
  default: '#909399',
  primary: '#2080f0',
  info: '#2080f0',
  success: '#18a058',
  warning: '#f0a020',
  error: '#d03050',
}

function badgeTagType(value?: string | null): BadgeTagType {
  return (value && badgeTypeOptions.value.some(o => o.value === value) ? value : 'default') as BadgeTagType
}

function badgeDotColor(value?: string | null) {
  return (value && badgeDotColorMap[value]) || badgeDotColorMap.default
}

// 上级菜单树（NTreeSelect 选项来源，独立 ref，增删改后与表格一起 reload）
const treeNodes = ref<MenuTreeNodeDto[]>([])

const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<MenuDetailDto | null>(null)

const modalVisible = ref(false)
const submitLoading = ref(false)
const menuForm = ref<MenuFormModel>(createDefaultForm())

const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)

const modalTitle = computed(() => (menuForm.value.basicId ? t('setting.menu.edit_title') : t('setting.menu.add_title')))

function toStr(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

function formatBoolean(value?: boolean | null) {
  if (value === undefined || value === null) {
    return '-'
  }
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

function formatStatus(value?: EnableStatus | null) {
  return getOptionLabel(statusOptions.value, value)
}

function createDefaultForm(): MenuFormModel {
  return {
    badge: null,
    badgeDot: false,
    badgeType: null,
    component: null,
    externalUrl: null,
    icon: null,
    isAffix: false,
    isCache: false,
    isExternal: false,
    isVisible: true,
    menuCode: '',
    menuName: '',
    menuType: MenuType.Menu,
    metadata: null,
    parentId: null,
    path: '',
    permissionId: null,
    redirect: null,
    remark: null,
    routeName: null,
    sort: 100,
    status: EnableStatus.Enabled,
    title: null,
    i18nKey: null,
  }
}

// --- 上级菜单 NTreeSelect（key-field=key / label-field=label） ---
function buildTreeSelectOptions(nodes: MenuTreeNodeDto[]): TreeSelectOption[] {
  return nodes.map(node => ({
    key: node.basicId,
    label: t('setting.menu.tree_label', { name: node.menuName, path: node.path }),
    children: node.children?.length ? buildTreeSelectOptions(node.children) : undefined,
  }))
}

const treeSelectOptions = computed(() => buildTreeSelectOptions(treeNodes.value))

async function loadTree() {
  try {
    treeNodes.value = await menuManagementApi.tree({ includeButtons: false, keyword: null, limit: 3000, onlyEnabled: false })
  }
  catch {
    treeNodes.value = []
  }
}

// --- 客户端组树（保留全部列字段：状态/可见/创建时间等，tree() DTO 不含这些字段，故由 page() 组树） ---
function buildTree(items: MenuListItemDto[]): MenuTreeItem[] {
  const map = new Map<ApiId, MenuTreeItem>()
  const roots: MenuTreeItem[] = []

  for (const item of items) {
    map.set(item.basicId, { ...item, children: [] })
  }

  for (const item of items) {
    const node = map.get(item.basicId)!
    if (item.parentId && map.has(item.parentId)) {
      map.get(item.parentId)!.children!.push(node)
    }
    else {
      roots.push(node)
    }
  }

  return roots
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'platform.menu',
  exportPermission: 'saas:menu:export',
  pageName: t('setting.menu.page_name'),
  batchRemovable: true,
  removePermission: 'saas:menu:delete',
  statusPermission: 'saas:menu:status',
  rowKey: 'basicId',
  scrollX: 2000,
  tree: { childrenKey: 'children', defaultExpandAll: false },
  resource: {
    tree: (params: SchemaQueryParams) => {
      const result = menuManagementApi.list({
        keyword: toStr(params.filters.keyword as string | undefined),
        menuType: (params.filters.menuType as MenuType | undefined) || undefined,
        status: (params.filters.status as EnableStatus | undefined) || undefined,
      })
      return result.then(items => buildTree(items)) as unknown as Promise<Record<string, unknown>[]>
    },
    remove: (id: ApiId) => menuManagementApi.delete(id),
    updateStatus: (id, enabled) => menuManagementApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
  },
  fields: [
    {
      key: 'menuName',
      title: t('setting.menu.menu_name'),
      dataType: 'string',
      treeColumn: true,
      searchable: true,
      searchPlaceholder: t('setting.menu.menu_name_placeholder'),
      minWidth: 200,
      order: 0,
      render: (row) => {
        const item = row as unknown as MenuListItemDto
        return h('span', { style: 'display:inline-flex;align-items:center;gap:6px' }, [
          item.icon
            ? h(Icon, { icon: item.icon, width: 16 })
            : h('span', { style: 'display:inline-block;width:16px' }),
          h('span', item.menuName),
        ])
      },
    },
    {
      key: 'menuCode',
      title: t('setting.menu.menu_code'),
      dataType: 'string',
      minWidth: 150,
      order: 1,
    },
    {
      key: 'menuType',
      title: t('setting.menu.type'),
      dataType: 'enum',
      options: menuTypeOptions.value,
      searchable: true,
      searchPlaceholder: t('setting.menu.type_placeholder'),
      width: 90,
      order: 2,
      render: row =>
        h(NTag, { size: 'small', round: true, bordered: false }, () => getOptionLabel(menuTypeOptions.value, (row as unknown as MenuListItemDto).menuType)),
    },
    {
      key: 'path',
      title: t('setting.menu.path'),
      dataType: 'string',
      minWidth: 200,
      order: 3,
    },
    {
      key: 'icon',
      title: t('setting.menu.icon'),
      dataType: 'string',
      minWidth: 170,
      order: 4,
      render: (row) => {
        const item = row as unknown as MenuListItemDto
        if (!item.icon) {
          return '-'
        }
        return h('span', { style: 'display:inline-flex;align-items:center;gap:6px' }, [
          h(Icon, { icon: item.icon, width: 16 }),
          h('span', item.icon),
        ])
      },
    },
    {
      key: 'badge',
      title: t('setting.menu.badge'),
      dataType: 'string',
      width: 110,
      order: 5,
      render: (row) => {
        const item = row as unknown as MenuListItemDto
        if (item.badgeDot) {
          return h('span', {
            style: {
              display: 'inline-block',
              width: '8px',
              height: '8px',
              borderRadius: '50%',
              backgroundColor: badgeDotColor(item.badgeType),
            },
          })
        }
        if (item.badge) {
          return h(NTag, { size: 'small', round: true, bordered: false, type: badgeTagType(item.badgeType) }, () => item.badge)
        }
        return '-'
      },
    },
    {
      key: 'isVisible',
      title: t('setting.menu.visible'),
      dataType: 'boolean',
      width: 80,
      order: 6,
      render: row =>
        h(NTag, { size: 'small', round: true, type: (row as unknown as MenuListItemDto).isVisible ? 'success' : 'default' }, () => ((row as unknown as MenuListItemDto).isVisible ? t('common.statuses.yes') : t('common.statuses.no'))),
    },
    {
      key: 'status',
      title: t('setting.menu.status'),
      dataType: 'enum',
      dictionaryCode: 'EnableStatus',
      options: statusOptions.value,
      searchable: true,
      searchPlaceholder: t('setting.menu.status_placeholder'),
      width: 90,
      order: 7,
      render: row =>
        h(NTag, { size: 'small', round: true, bordered: false, type: (row as unknown as MenuListItemDto).status === EnableStatus.Enabled ? 'success' : 'error' }, () => getOptionLabel(statusOptions.value, (row as unknown as MenuListItemDto).status)),
    },
    {
      key: 'sort',
      title: t('setting.menu.sort'),
      dataType: 'number',
      width: 80,
      order: 8,
    },
    {
      key: 'createdTime',
      title: t('setting.menu.created_time'),
      dataType: 'datetime',
      minWidth: 170,
      order: 9,
      render: row => formatDate((row as unknown as MenuListItemDto).createdTime),
    },
  ],
  actions: [
    { key: 'create', title: t('setting.menu.add'), scope: 'page', type: 'primary', icon: 'lucide:plus' },
    { key: 'addChild', title: t('setting.menu.add_child'), scope: 'row', icon: 'lucide:plus', visible: row => (row as unknown as MenuListItemDto).menuType !== MenuType.Button },
    { key: 'view', title: t('setting.menu.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pen', visible: canMaintainMenu },
    { key: 'toggle', title: t('setting.menu.toggle'), scope: 'row', icon: 'lucide:power', visible: canMaintainMenu },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', confirm: true, confirmText: t('setting.menu.confirm_delete'), visible: canMaintainMenu },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const { key, scope } = payload
  const row = payload.row as unknown as MenuListItemDto | undefined

  if (scope === 'page' && key === 'create') {
    openCreate()
    return
  }
  if (scope === 'row' && row) {
    if (key === 'addChild')
      openCreate(row.basicId)
    else if (key === 'view')
      void openDetail(row)
    else if (key === 'edit')
      void openEdit(row)
    else if (key === 'toggle')
      void toggleStatus(row)
    else if (key === 'delete')
      void removeRow(row)
  }
}

function openCreate(parentId?: ApiId | null) {
  menuForm.value = createDefaultForm()
  menuForm.value.parentId = parentId ?? null
  modalVisible.value = true
}

function buildFormModel(row: MenuDetailDto | MenuListItemDto): MenuFormModel {
  return {
    ...createDefaultForm(),
    badge: 'badge' in row ? row.badge ?? null : null,
    badgeDot: 'badgeDot' in row ? row.badgeDot : false,
    badgeType: 'badgeType' in row ? row.badgeType ?? null : null,
    basicId: row.basicId,
    component: row.component ?? null,
    externalUrl: 'externalUrl' in row ? row.externalUrl ?? null : null,
    icon: row.icon ?? null,
    isAffix: row.isAffix,
    isCache: row.isCache,
    isExternal: row.isExternal,
    isVisible: row.isVisible,
    menuCode: row.menuCode,
    menuName: row.menuName,
    menuType: row.menuType,
    metadata: 'metadata' in row ? row.metadata ?? null : null,
    parentId: row.parentId ?? null,
    path: row.path,
    permissionId: row.permissionId ?? null,
    redirect: row.redirect ?? null,
    remark: 'remark' in row ? row.remark ?? null : null,
    routeName: row.routeName ?? null,
    sort: row.sort,
    status: row.status,
    title: row.title ?? null,
    i18nKey: row.i18nKey ?? null,
  }
}

async function openEdit(row: MenuListItemDto) {
  try {
    const detail = await menuManagementApi.detail(row.basicId)
    menuForm.value = buildFormModel(detail ?? row)
  }
  catch {
    message.error(t('setting.menu.load_detail_failed'))
    menuForm.value = buildFormModel(row)
  }
  modalVisible.value = true
}

async function openDetail(row: MenuListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null

  try {
    currentDetail.value = await menuManagementApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning(t('setting.menu.detail_not_found'))
    }
  }
  catch {
    message.error(t('setting.menu.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

async function toggleStatus(row: MenuListItemDto) {
  const next = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await menuManagementApi.updateStatus({ basicId: row.basicId, status: next })
    message.success(t('setting.menu.status_update_success'))
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch {
    message.error(t('setting.menu.status_update_failed'))
  }
}

async function removeRow(row: MenuListItemDto) {
  try {
    await menuManagementApi.delete(row.basicId)
    message.success(t('common.messages.delete_success'))
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch {
    message.error(t('common.messages.delete_failed'))
  }
}

function validateForm() {
  const form = menuForm.value
  if (!form.menuName.trim()) {
    message.warning(t('setting.menu.validate_menu_name'))
    return false
  }
  if (!form.basicId && !form.menuCode.trim()) {
    message.warning(t('setting.menu.validate_menu_code'))
    return false
  }
  // 按钮无路由，目录/菜单需要路由路径
  if (form.menuType !== MenuType.Button && !form.path.trim()) {
    message.warning(t('setting.menu.validate_path'))
    return false
  }
  // 非外链菜单需要组件路径（与后端校验一致）
  if (form.menuType === MenuType.Menu && !form.isExternal && !form.component?.trim()) {
    message.warning(t('setting.menu.validate_component'))
    return false
  }
  // 外链菜单需要外链地址
  if (form.isExternal && !form.externalUrl?.trim()) {
    message.warning(t('setting.menu.validate_external_url'))
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validateForm())
    return

  submitLoading.value = true
  try {
    if (menuForm.value.basicId) {
      const updateInput: MenuUpdateDto = {
        badge: toStr(menuForm.value.badge),
        badgeDot: menuForm.value.badgeDot,
        badgeType: toStr(menuForm.value.badgeType),
        basicId: menuForm.value.basicId,
        component: toStr(menuForm.value.component),
        externalUrl: toStr(menuForm.value.externalUrl),
        icon: toStr(menuForm.value.icon),
        isAffix: menuForm.value.isAffix,
        isCache: menuForm.value.isCache,
        isExternal: menuForm.value.isExternal,
        isVisible: menuForm.value.isVisible,
        menuName: menuForm.value.menuName.trim(),
        menuType: menuForm.value.menuType,
        metadata: toStr(menuForm.value.metadata),
        parentId: menuForm.value.parentId,
        path: menuForm.value.path.trim(),
        permissionId: menuForm.value.permissionId,
        redirect: toStr(menuForm.value.redirect),
        remark: toStr(menuForm.value.remark),
        routeName: toStr(menuForm.value.routeName),
        sort: menuForm.value.sort,
        title: toStr(menuForm.value.title),
        i18nKey: toStr(menuForm.value.i18nKey),
      }
      await menuManagementApi.update(updateInput)
    }
    else {
      const createInput: MenuCreateDto = {
        badge: toStr(menuForm.value.badge),
        badgeDot: menuForm.value.badgeDot,
        badgeType: toStr(menuForm.value.badgeType),
        component: toStr(menuForm.value.component),
        externalUrl: toStr(menuForm.value.externalUrl),
        icon: toStr(menuForm.value.icon),
        isAffix: menuForm.value.isAffix,
        isCache: menuForm.value.isCache,
        isExternal: menuForm.value.isExternal,
        isVisible: menuForm.value.isVisible,
        menuCode: menuForm.value.menuCode.trim(),
        menuName: menuForm.value.menuName.trim(),
        menuType: menuForm.value.menuType,
        metadata: toStr(menuForm.value.metadata),
        parentId: menuForm.value.parentId,
        path: menuForm.value.path.trim(),
        permissionId: menuForm.value.permissionId,
        redirect: toStr(menuForm.value.redirect),
        remark: toStr(menuForm.value.remark),
        routeName: toStr(menuForm.value.routeName),
        sort: menuForm.value.sort,
        status: menuForm.value.status,
        title: toStr(menuForm.value.title),
        i18nKey: toStr(menuForm.value.i18nKey),
      }
      await menuManagementApi.create(createInput)
    }

    message.success(t('common.messages.save_success'))
    modalVisible.value = false
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch {
    message.error(t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

// 详情弹窗子菜单（从菜单树定位当前节点的 children 展示）
const childMenuColumns = computed<DataTableColumns<MenuTreeNodeDto>>(() => [
  { title: t('setting.menu.child_menu_name'), key: 'menuName', minWidth: 120, ellipsis: { tooltip: true } },
  { title: t('setting.menu.child_code'), key: 'menuCode', width: 110, ellipsis: { tooltip: true } },
  {
    title: t('setting.menu.child_type'),
    key: 'menuType',
    width: 80,
    render: row => getOptionLabel(menuTypeOptions.value, row.menuType),
  },
  {
    title: t('setting.menu.child_path'),
    key: 'path',
    minWidth: 120,
    ellipsis: { tooltip: true },
    render: row => formatNullable(row.path),
  },
])

function findNodeChildren(nodes: MenuTreeNodeDto[], id: ApiId): MenuTreeNodeDto[] {
  for (const node of nodes) {
    if (node.basicId === id)
      return node.children ?? []
    if (node.children?.length) {
      const found = findNodeChildren(node.children, id)
      if (found.length)
        return found
    }
  }
  return []
}

const childMenus = computed(() => {
  if (!currentDetail.value)
    return []
  return findNodeChildren(treeNodes.value, currentDetail.value.basicId)
})

onMounted(() => {
  void loadTree()
})
</script>

<template>
  <div class="flex overflow-hidden flex-col h-full">
    <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction" />

    <NModal
      v-model:show="detailVisible"
      class="xh-mgmt-detail-modal"
      preset="card"
      :bordered="false"
      :mask-closable="true"
      style="width: 720px; max-width: calc(100vw - 32px);"
    >
      <template v-if="currentDetail" #header>
        <div class="det-hd-entity">
          <div class="det-hd-ico">
            <Icon icon="tabler:menu-2" :size="22" />
          </div>
          <div class="min-w-0">
            <div class="det-hd-name">
              {{ currentDetail.menuName }}
            </div>
            <div class="det-hd-sub">
              {{ currentDetail.menuCode }}
            </div>
          </div>
        </div>
      </template>

      <div v-if="detailLoading" class="modal-loading">
        {{ t('common.statuses.loading') }}
      </div>
      <NTabs v-else-if="currentDetail" type="line" animated size="small">
        <NTabPane name="overview" :tab="t('setting.menu.overview')">
          <NDescriptions :column="2" bordered size="small">
            <NDescriptionsItem :label="t('setting.menu.menu_type')">
              {{ getOptionLabel(menuTypeOptions, currentDetail.menuType) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.status')">
              <NTag size="small" :type="currentDetail.status === EnableStatus.Enabled ? 'success' : 'error'" :bordered="false">
                {{ formatStatus(currentDetail.status) }}
              </NTag>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.route_path')">
              {{ formatNullable(currentDetail.path) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.component_path')">
              {{ formatNullable(currentDetail.component) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.route_name')">
              {{ formatNullable(currentDetail.routeName) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.icon')">
              <span v-if="currentDetail.icon" style="display: inline-flex; align-items: center; gap: 6px">
                <Icon :icon="currentDetail.icon" width="16" />
                <span>{{ currentDetail.icon }}</span>
              </span>
              <span v-else>-</span>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.title')">
              {{ formatNullable(currentDetail.title) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.i18n_key')">
              {{ formatNullable(currentDetail.i18nKey) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.badge')">
              <NTag v-if="currentDetail.badge" size="small" round :bordered="false" :type="badgeTagType(currentDetail.badgeType)">
                {{ currentDetail.badge }}
              </NTag>
              <span
                v-else-if="currentDetail.badgeDot"
                :style="{ display: 'inline-block', width: '8px', height: '8px', borderRadius: '50%', backgroundColor: badgeDotColor(currentDetail.badgeType) }"
              />
              <span v-else>-</span>
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.is_external')">
              {{ formatBoolean(currentDetail.isExternal) }}
            </NDescriptionsItem>
            <NDescriptionsItem v-if="currentDetail.isExternal" :label="t('setting.menu.external_url')" :span="2">
              {{ formatNullable(currentDetail.externalUrl) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.is_cache')">
              {{ formatBoolean(currentDetail.isCache) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.is_visible')">
              {{ formatBoolean(currentDetail.isVisible) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.is_affix')">
              {{ formatBoolean(currentDetail.isAffix) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.sort')">
              {{ currentDetail.sort }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.permission_id')">
              {{ formatNullable(currentDetail.permissionId) }}
            </NDescriptionsItem>
            <NDescriptionsItem :label="t('setting.menu.created_time')">
              {{ formatNullableDate(currentDetail.createdTime) }}
            </NDescriptionsItem>
            <NDescriptionsItem v-if="currentDetail.remark" :label="t('setting.menu.remark')" :span="2">
              {{ currentDetail.remark }}
            </NDescriptionsItem>
          </NDescriptions>
        </NTabPane>
        <NTabPane name="children" :tab="t('setting.menu.children_tab', { count: childMenus.length })">
          <div class="xh-detail-table-wrap">
            <NDataTable
              v-if="childMenus.length"
              :columns="childMenuColumns"
              :data="childMenus"
              :bordered="false"
              size="small"
              :row-key="(row: MenuTreeNodeDto) => row.basicId"
            />
            <NEmpty v-else :description="t('setting.menu.no_children')" style="padding: 32px 0" />
          </div>
        </NTabPane>
      </NTabs>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="detailVisible = false">
            {{ t('common.actions.close') }}
          </NButton>
          <NButton
            v-if="currentDetail"
            size="small"
            type="primary"
            @click="detailVisible = false; openEdit(currentDetail as unknown as MenuListItemDto)"
          >
            {{ t('common.actions.edit') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 800px; max-width: 92vw"
    >
      <NConfigProvider size="small" abstract>
        <NForm :model="menuForm" size="small" class="xh-edit-form-grid" label-placement="top">
          <NFormItem :label="t('setting.menu.menu_name')" path="menuName">
            <NInput v-model:value="menuForm.menuName" clearable :placeholder="t('setting.menu.menu_name_input_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.menu_code')" path="menuCode">
            <NInput
              v-model:value="menuForm.menuCode"
              :disabled="Boolean(menuForm.basicId)"
              clearable
              :placeholder="t('setting.menu.menu_code_input_placeholder')"
            />
          </NFormItem>
          <NFormItem :label="t('setting.menu.parent_menu')" path="parentId">
            <NTreeSelect
              v-model:value="menuForm.parentId"
              :options="treeSelectOptions"
              key-field="key"
              label-field="label"
              clearable
              :placeholder="t('setting.menu.parent_menu_placeholder')"
            />
          </NFormItem>
          <NFormItem :label="t('setting.menu.menu_type')" path="menuType">
            <NSelect v-model:value="menuForm.menuType" :options="menuTypeOptions" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.route_path')" path="path">
            <NInput v-model:value="menuForm.path" clearable :placeholder="t('setting.menu.path_input_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.route_name')" path="routeName">
            <NInput v-model:value="menuForm.routeName" clearable :placeholder="t('setting.menu.route_name_input_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.component_path')" path="component">
            <NInput v-model:value="menuForm.component" clearable :placeholder="t('setting.menu.component_input_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.redirect')" path="redirect">
            <NInput v-model:value="menuForm.redirect" clearable :placeholder="t('setting.menu.redirect_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.icon')" path="icon">
            <IconPicker v-model="menuForm.icon" :placeholder="t('setting.menu.icon_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.title')" path="title">
            <NInput v-model:value="menuForm.title" clearable :placeholder="t('setting.menu.title_input_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.i18n_key')" path="i18nKey">
            <NInput v-model:value="menuForm.i18nKey" clearable :placeholder="t('setting.menu.i18n_key_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.sort')" path="sort">
            <NInputNumber v-model:value="menuForm.sort" :min="0" style="width: 100%" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.badge_content')" path="badge">
            <NInput v-model:value="menuForm.badge" clearable :placeholder="t('setting.menu.badge_content_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.badge_type')" path="badgeType">
            <NSelect v-model:value="menuForm.badgeType" :options="badgeTypeOptions" clearable :placeholder="t('setting.menu.badge_type_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.badge_dot')">
            <NSwitch v-model:value="menuForm.badgeDot" />
          </NFormItem>
          <NFormItem v-if="!menuForm.basicId" :label="t('setting.menu.status')" path="status">
            <NSelect v-model:value="menuForm.status" :options="statusOptions" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.visible')">
            <NSwitch v-model:value="menuForm.isVisible" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.cache')">
            <NSwitch v-model:value="menuForm.isCache" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.affix')">
            <NSwitch v-model:value="menuForm.isAffix" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.external')">
            <NSwitch v-model:value="menuForm.isExternal" />
          </NFormItem>
          <NFormItem v-if="menuForm.isExternal" :label="t('setting.menu.external_url')" path="externalUrl">
            <NInput v-model:value="menuForm.externalUrl" clearable :placeholder="t('setting.menu.external_url_placeholder')" />
          </NFormItem>
          <NFormItem :label="t('setting.menu.remark')" path="remark">
            <NInput v-model:value="menuForm.remark" clearable :placeholder="t('setting.menu.remark_placeholder')" :rows="3" type="textarea" />
          </NFormItem>
        </NForm>
      </NConfigProvider>

      <template #footer>
        <NSpace justify="end">
          <NButton size="small" @click="modalVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton size="small" :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}
</style>
