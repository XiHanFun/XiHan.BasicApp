<script setup lang="ts">
import type { ApiId, MenuCreateDto, MenuDetailDto, MenuListItemDto, MenuTreeNodeDto, MenuUpdateDto } from '@/api'
import type { PageSchema, SchemaActionPayload, SchemaQueryParams, XDataTableColumn } from '~/components'
import type { TreeSelectOption } from '~/types'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  EnableStatus,
  menuManagementApi,
  MenuType,
} from '@/api'
import { Icon, IconPicker, SchemaPage, XDataTable, XEditModal, XInput, XNumberInput, XSelect, XTreeSelect } from '~/components'
import { toast } from '~/composables'
import { useUserStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'PlatformMenuPage' })

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()
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

const statusOptions = computed(() => [
  { label: t('common.actions.enable'), value: EnableStatus.Enabled },
  { label: t('common.actions.disable'), value: EnableStatus.Disabled },
])

const menuTypeOptions = computed(() => [
  { label: t('setting.menu.type_directory'), value: MenuType.Directory },
  { label: t('setting.menu.type_menu'), value: MenuType.Menu },
  { label: t('setting.menu.type_button'), value: MenuType.Button },
])

/** 菜单类型标签配色：按层级递减强调，目录最显眼、按钮最安静（按钮数量最多，不该抢视线） */
function menuTypeTagType(menuType: MenuType) {
  if (menuType === MenuType.Directory) {
    return 'warning'
  }
  return menuType === MenuType.Menu ? 'info' : 'neutral'
}

const badgeTypeOptions = computed(() => [
  { label: t('setting.menu.badge_default'), value: 'default' },
  { label: t('setting.menu.badge_primary'), value: 'primary' },
  { label: t('setting.menu.badge_info'), value: 'info' },
  { label: t('setting.menu.badge_success'), value: 'success' },
  { label: t('setting.menu.badge_warning'), value: 'warning' },
  { label: t('setting.menu.badge_error'), value: 'error' },
])

/** 菜单徽标类型：存库取值，不随组件库词汇变动 */
type BadgeTagType = 'default' | 'primary' | 'info' | 'success' | 'warning' | 'error'

/** 存库取值 → 组件库语气 */
const BADGE_TONE: Record<BadgeTagType, 'neutral' | 'brand' | 'info' | 'success' | 'warning' | 'danger'> = {
  default: 'neutral',
  primary: 'brand',
  info: 'info',
  success: 'success',
  warning: 'warning',
  error: 'danger',
}

const badgeDotColorMap: Record<string, string> = {
  default: '#909399',
  primary: '#2080f0',
  info: '#2080f0',
  success: '#18a058',
  warning: '#f0a020',
  error: '#d03050',
}

function badgeTone(value?: string | null) {
  const stored = (value && badgeTypeOptions.value.some(o => o.value === value) ? value : 'default') as BadgeTagType
  return BADGE_TONE[stored]
}

function badgeDotColor(value?: string | null) {
  return (value && badgeDotColorMap[value]) || badgeDotColorMap.default
}

// 上级菜单树（树选择器的选项来源，独立 ref，增删改后与表格一起 reload）
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

// --- 上级菜单树选择器 ---
function buildTreeSelectOptions(nodes: MenuTreeNodeDto[]): TreeSelectOption[] {
  return nodes.map(node => ({
    value: node.basicId,
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
  exportPermission: 'setting.menu.export',
  pageName: t('setting.menu.page_name'),
  batchRemovable: true,
  removePermission: 'setting.menu.delete',
  statusPermission: 'setting.menu.status',
  rowKey: 'basicId',
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
      render: (row) => {
        const menuType = (row as unknown as MenuListItemDto).menuType
        return h(XhTagRoot, { variant: 'outline', tone: menuTypeTagType(menuType) }, () => h(XhTagLabel, () => getOptionLabel(menuTypeOptions.value, menuType)))
      },
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
          return h(XhTagRoot, { variant: 'outline', tone: badgeTone(item.badgeType) }, () => h(XhTagLabel, () => item.badge))
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
        h(XhTagRoot, { variant: 'outline', tone: (row as unknown as MenuListItemDto).isVisible ? 'success' : 'neutral' }, () => h(XhTagLabel, () => ((row as unknown as MenuListItemDto).isVisible ? t('common.statuses.yes') : t('common.statuses.no')))),
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
        h(XhTagRoot, { variant: 'outline', tone: (row as unknown as MenuListItemDto).status === EnableStatus.Enabled ? 'success' : 'danger' }, () => h(XhTagLabel, () => getOptionLabel(statusOptions.value, (row as unknown as MenuListItemDto).status))),
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
  catch (error) {
    toast.error((error as Error)?.message || t('setting.menu.load_detail_failed'))
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
      toast.warning(t('setting.menu.detail_not_found'))
    }
  }
  catch (error) {
    toast.error((error as Error)?.message || t('setting.menu.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

async function toggleStatus(row: MenuListItemDto) {
  const next = row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled
  try {
    await menuManagementApi.updateStatus({ basicId: row.basicId, status: next })
    toast.success(t('setting.menu.status_update_success'))
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('setting.menu.status_update_failed'))
  }
}

async function removeRow(row: MenuListItemDto) {
  try {
    await menuManagementApi.delete(row.basicId)
    toast.success(t('common.messages.delete_success'))
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.delete_failed'))
  }
}

function validateForm() {
  const form = menuForm.value
  if (!form.menuName.trim()) {
    toast.warning(t('setting.menu.validate_menu_name'))
    return false
  }
  if (!form.basicId && !form.menuCode.trim()) {
    toast.warning(t('setting.menu.validate_menu_code'))
    return false
  }
  // 按钮无路由，目录/菜单需要路由路径
  if (form.menuType !== MenuType.Button && !form.path.trim()) {
    toast.warning(t('setting.menu.validate_path'))
    return false
  }
  // 非外链菜单需要组件路径（与后端校验一致）
  if (form.menuType === MenuType.Menu && !form.isExternal && !form.component?.trim()) {
    toast.warning(t('setting.menu.validate_component'))
    return false
  }
  // 外链菜单需要外链地址
  if (form.isExternal && !form.externalUrl?.trim()) {
    toast.warning(t('setting.menu.validate_external_url'))
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

    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    schemaPageRef.value?.reload()
    void loadTree()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

// 详情弹窗子菜单（从菜单树定位当前节点的 children 展示）
const childMenuColumns = computed<XDataTableColumn<MenuTreeNodeDto>[]>(() => [
  { title: t('setting.menu.child_menu_name'), key: 'menuName', minWidth: 120, ellipsis: true },
  { title: t('setting.menu.child_code'), key: 'menuCode', width: 110, ellipsis: true },
  {
    title: t('setting.menu.child_type'),
    key: 'menuType',
    width: 80,
    render: row =>
      h(XhTagRoot, { variant: 'outline', tone: menuTypeTagType(row.menuType) }, () => h(XhTagLabel, () => getOptionLabel(menuTypeOptions.value, row.menuType))),
  },
  {
    title: t('setting.menu.child_path'),
    key: 'path',
    minWidth: 120,
    ellipsis: true,
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

    <XhDialogRoot v-model:open="detailVisible">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 720px">
        <XhDialogTitle v-if="currentDetail">
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
        </XhDialogTitle>
        <XhDialogCloseTrigger />

        <div v-if="detailLoading" class="modal-loading">
          {{ t('common.statuses.loading') }}
        </div>
        <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
        <XhTabsRoot v-else-if="currentDetail" default-value="overview" variant="line">
          <XhTabsList>
            <XhTabsTrigger value="overview">
              {{ t('setting.menu.overview') }}
            </XhTabsTrigger>
            <XhTabsTrigger value="children">
              {{ t('setting.menu.children_tab', { count: childMenus.length }) }}
            </XhTabsTrigger>
          </XhTabsList>
          <XhTabsContent value="overview">
            <XhDescriptionsRoot :columns="2" bordered size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.menu_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ getOptionLabel(menuTypeOptions, currentDetail.menuType) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhTagRoot variant="subtle" size="sm" :tone="currentDetail.status === EnableStatus.Enabled ? 'success' : 'danger'">
                    <XhTagLabel>
                      {{ formatStatus(currentDetail.status) }}
                    </XhTagLabel>
                  </XhTagRoot>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.route_path') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.path) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.component_path') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.component) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.route_name') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.routeName) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.icon') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <span v-if="currentDetail.icon" style="display: inline-flex; align-items: center; gap: 6px">
                    <Icon :icon="currentDetail.icon" width="16" />
                    <span>{{ currentDetail.icon }}</span>
                  </span>
                  <span v-else>-</span>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.title') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.title) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.i18n_key') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.i18nKey) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.badge') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhTagRoot v-if="currentDetail.badge" variant="subtle" size="sm" :tone="badgeTone(currentDetail.badgeType)">
                    <XhTagLabel>
                      {{ currentDetail.badge }}
                    </XhTagLabel>
                  </XhTagRoot>
                  <span
                    v-else-if="currentDetail.badgeDot"
                    :style="{ display: 'inline-block', width: '8px', height: '8px', borderRadius: '50%', backgroundColor: badgeDotColor(currentDetail.badgeType) }"
                  />
                  <span v-else>-</span>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.is_external') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatBoolean(currentDetail.isExternal) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem v-if="currentDetail.isExternal" style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('setting.menu.external_url') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.externalUrl) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.is_cache') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatBoolean(currentDetail.isCache) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.is_visible') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatBoolean(currentDetail.isVisible) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.is_affix') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatBoolean(currentDetail.isAffix) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.sort') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ currentDetail.sort }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.permission_id') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullable(currentDetail.permissionId) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.menu.created_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(currentDetail.createdTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem v-if="currentDetail.remark" style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('setting.menu.remark') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ currentDetail.remark }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </XhTabsContent>
          <XhTabsContent value="children">
            <div class="xh-detail-table-wrap">
              <XDataTable
                v-if="childMenus.length"
                :columns="childMenuColumns"
                :data="childMenus"
                size="sm"
                :row-key="(row: MenuTreeNodeDto) => row.basicId"
              />
              <XhEmptyStateRoot v-else size="sm" style="padding: 32px 0">
                <XhEmptyStateIcon>
                  <Icon icon="lucide:inbox" width="24" />
                </XhEmptyStateIcon>
                <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
                <XhEmptyStateDescription>{{ t('setting.menu.no_children') }}</XhEmptyStateDescription>
              </XhEmptyStateRoot>
            </div>
          </XhTabsContent>
        </XhTabsRoot>

        <div class="xh-dialog-footer">
          <XhFlex justify="end">
            <XhButton size="sm" @click="detailVisible = false">
              {{ t('common.actions.close') }}
            </XhButton>
            <XhButton
              v-if="currentDetail"
              size="sm"
              tone="brand"
              @click="detailVisible = false; openEdit(currentDetail as unknown as MenuListItemDto)"
            >
              {{ t('common.actions.edit') }}
            </XhButton>
          </XhFlex>
        </div>
      </XhDialogContent>
    </XhDialogRoot>

    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="menuForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup value="menuName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.menu_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.menuName" clearable :placeholder="t('setting.menu.menu_name_input_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="menuCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.menu_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput
                v-model:value="menuForm.menuCode"
                :disabled="Boolean(menuForm.basicId)"
                clearable
                :placeholder="t('setting.menu.menu_code_input_placeholder')"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="parentId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.parent_menu') }}</XhFieldLabel>
            <XhFieldControl>
              <XTreeSelect v-model:value="menuForm.parentId" :options="treeSelectOptions" clearable :placeholder="t('setting.menu.parent_menu_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="menuType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.menu_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="menuForm.menuType" :options="menuTypeOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="path">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.route_path') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.path" clearable :placeholder="t('setting.menu.path_input_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="routeName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.route_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.routeName" clearable :placeholder="t('setting.menu.route_name_input_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="component">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.component_path') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.component" clearable :placeholder="t('setting.menu.component_input_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="redirect">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.redirect') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.redirect" clearable :placeholder="t('setting.menu.redirect_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="icon">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.icon') }}</XhFieldLabel>
            <XhFieldControl :as-child="false">
              <IconPicker v-model="menuForm.icon" :placeholder="t('setting.menu.icon_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="title">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.title') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.title" clearable :placeholder="t('setting.menu.title_input_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="i18nKey">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.i18n_key') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.i18nKey" clearable :placeholder="t('setting.menu.i18n_key_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="sort">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.sort') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="menuForm.sort" :min="0" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="badge">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.badge_content') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.badge" clearable :placeholder="t('setting.menu.badge_content_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="badgeType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.badge_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="menuForm.badgeType" :options="badgeTypeOptions" clearable :placeholder="t('setting.menu.badge_type_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('setting.menu.badge_dot') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="menuForm.badgeDot" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
        <XhFormFieldGroup v-if="!menuForm.basicId" value="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="menuForm.status" :options="statusOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('setting.menu.visible') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="menuForm.isVisible" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('setting.menu.cache') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="menuForm.isCache" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('setting.menu.affix') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="menuForm.isAffix" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('setting.menu.external') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="menuForm.isExternal" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
        <XhFormFieldGroup v-if="menuForm.isExternal" value="externalUrl" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.external_url') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.externalUrl" clearable :placeholder="t('setting.menu.external_url_placeholder')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup value="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.menu.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="menuForm.remark" clearable :placeholder="t('setting.menu.remark_placeholder')" :rows="3" type="textarea" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </div>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}
</style>
