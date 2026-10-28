<script lang="ts" setup>
import type { TenantMember, TenantMemberType, TenantRow, TenantStatus } from './use-tenants'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSpinner, XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, XCombobox, XDatePicker, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import { TENANT_MEMBER_TYPES, TENANT_STATUSES, tenantsResource, useAdminInitializer, useEditionOptions, useMemberEditor, useTenantDetail, useTenantEditor } from './use-tenants'

defineOptions({ name: 'TenancyTenantsView' })

const { t } = useI18n()
const auth = useAuthStore()
const resource = tenantsResource()
const editions = useEditionOptions()
const editor = useTenantEditor()
const admin = useAdminInitializer()
const detail = useTenantDetail(() => auth.can('tenancy.tenant-members.read'))
const memberEditor = useMemberEditor()

/** 套餐下拉要读套餐；没有这项权限时不出套餐字段（开通即缺省套餐、编辑不动套餐） */
const canReadEditions = computed(() => auth.can('tenancy.editions.read'))
const canReadMembers = computed(() => auth.can('tenancy.tenant-members.read'))
/** 添加成员要从用户里挑人：同时要能读用户 */
const canAddMember = computed(() => auth.can('tenancy.tenant-members.create') && auth.can('identity.users.read'))
const canRemoveMember = computed(() => auth.can('tenancy.tenant-members.delete'))

/** 弹窗的保存钮靠这些 id 关联到各自的表单，点它才会走整表提交 */
const editFormId = useId()
const adminFormId = useId()
const memberFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

onMounted(async () => {
  if (!canReadEditions.value)
    return
  await editions.load()
  if (editions.error.value)
    toast.danger(editions.error.value)
})

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: TenantStatus): string {
  return status === 'Suspended' ? t('tenancy.tenants.status.suspended') : t('tenancy.tenants.status.active')
}

function memberTypeLabel(type: TenantMemberType): string {
  return type === 'Owner' ? t('tenancy.tenants.members.types.owner') : t('tenancy.tenants.members.types.member')
}

/** 列表与详情里的套餐：没绑定即不受套餐限制 */
function editionLabel(id: null | string | undefined): string {
  return editions.editionName(id) ?? t('tenancy.tenants.edition_unrestricted')
}

function adminTag(row: TenantRow) {
  return row.adminUserId
    ? renderTag(t('tenancy.tenants.admin_ready'), 'success')
    : renderTag(t('tenancy.tenants.admin_none'), 'neutral')
}

const statusOptions = computed<SelectOption[]>(() => TENANT_STATUSES.map(status => ({ label: statusLabel(status), value: status })))
const memberTypeOptions = computed<SelectOption[]>(() => TENANT_MEMBER_TYPES.map(type => ({ label: memberTypeLabel(type), value: type })))
/** 列与导出用的套餐名 */
const editionColumnOptions = computed<SelectOption[]>(() => editions.editions.value.map(e => ({ label: e.name, value: e.id })))
/** 弹窗下拉：缺省套餐带标注（开通时留空即用它） */
const editionFormOptions = computed<SelectOption[]>(() => editions.editions.value.map(e => ({
  label: e.isDefault ? `${e.name}${t('tenancy.tenants.edition_default_suffix')}` : e.name,
  value: e.id,
})))

// ── 字段单一事实源（列 + 搜索；仅搜索字段 visible:false；order 控顺序；可排序的只放后端白名单里的） ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as TenantRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('tenancy.tenants.search'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('tenancy.tenants.keyword_placeholder'), order: 0 },
  { key: 'tenantId', title: t('tenancy.tenants.fields.tenant_id'), dataType: 'string', sortable: true, exportable: true, minWidth: 140, order: 1 },
  { key: 'name', title: t('tenancy.tenants.fields.name'), dataType: 'string', sortable: true, exportable: true, minWidth: 160, order: 2 },
  { key: 'status', title: t('tenancy.tenants.fields.status'), dataType: 'enum', searchable: true, exportable: true, options: statusOptions.value, render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active' ? 'success' : 'danger'), width: 90, order: 3 },
  { key: 'editionId', title: t('tenancy.tenants.fields.edition'), dataType: 'enum', exportable: true, options: editionColumnOptions.value, render: row => editionLabel(asRow(row).editionId), ellipsis: true, minWidth: 130, order: 4 },
  { key: 'adminUserId', title: t('tenancy.tenants.fields.admin'), dataType: 'string', render: row => adminTag(asRow(row)), width: 100, order: 5 },
  { key: 'expiresAt', title: t('tenancy.tenants.fields.expires_at'), dataType: 'datetime', sortable: true, exportable: true, minWidth: 170, order: 6 },
  { key: 'order', title: t('tenancy.tenants.fields.order'), dataType: 'number', sortable: true, exportable: true, width: 80, order: 7 },
  { key: 'description', title: t('tenancy.tenants.fields.description'), dataType: 'text', visible: false, exportable: true, order: 7.5 },
  { key: 'createdAt', title: t('tenancy.tenants.fields.created_at'), dataType: 'datetime', sortable: true, exportable: true, minWidth: 170, order: 8 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'tenancy.tenants',
  pageName: t('tenancy.tenants.title'),
  exportPermission: 'tenancy.tenants.read',
  batchRemovable: true,
  removePermission: 'tenancy.tenants.delete',
  // 批量启用 / 停用即恢复 / 停用租户
  statusPermission: 'tenancy.tenants.suspend',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('tenancy.tenants.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'tenancy.tenants.create' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'tenancy.tenants.update' },
    // 每个租户只初始化一次：已有管理员就不再出这一项
    { key: 'initialize-admin', title: t('tenancy.tenants.actions.initialize_admin'), scope: 'row', icon: 'lucide:user-plus', visible: row => !asRow(row).adminUserId, permission: 'tenancy.tenants.initialize-admin' },
    { key: 'resume', title: t('tenancy.tenants.actions.resume'), scope: 'row', tone: 'success', icon: 'lucide:play', confirm: true, confirmText: row => t('tenancy.tenants.resume_confirm', { name: asRow(row).name }), visible: row => asRow(row).status === 'Suspended', permission: 'tenancy.tenants.suspend' },
    { key: 'suspend', title: t('tenancy.tenants.actions.suspend'), scope: 'row', tone: 'warning', icon: 'lucide:pause', confirm: true, confirmText: row => t('tenancy.tenants.suspend_confirm', { name: asRow(row).name }), visible: row => asRow(row).status === 'Active', permission: 'tenancy.tenants.suspend' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), permission: 'tenancy.tenants.delete' },
  ],
}))

// ── 行 / 页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'view':
      if (row)
        void detail.show(row)
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
      break
    case 'initialize-admin':
      if (row)
        admin.start(row)
      break
    case 'resume':
      if (row)
        void setStatus(row, true)
      break
    case 'suspend':
      if (row)
        void setStatus(row, false)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

async function onSubmit() {
  const creating = !editor.isEdit.value
  const saved = await editor.submit()
  if (!saved)
    return
  toast.success(t('common.messages.save_success'))
  reload()
  // 开通即接着初始化管理员（与 4.x 相同）；也可以稍后从行操作进
  if (creating && auth.can('tenancy.tenants.initialize-admin'))
    admin.start(saved)
}

async function onAdminSubmit() {
  if (await admin.submit()) {
    toast.success(t('tenancy.tenants.admin_created'))
    reload()
  }
}

async function removeRow(row: TenantRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function setStatus(row: TenantRow, enabled: boolean) {
  try {
    await resource.updateStatus?.(row.id, enabled)
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

// ── 成员 ──
async function onMemberSubmit() {
  const tenant = detail.detail.value?.tenant
  if (!tenant)
    return
  if (await memberEditor.submit(tenant.tenantId)) {
    toast.success(t('tenancy.tenants.members.added'))
    await detail.loadMembers()
  }
}

function confirmRemoveMember(member: TenantMember) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('tenancy.tenants.members.remove'),
    content: t('tenancy.tenants.members.remove_confirm', { name: member.displayName || member.userName }),
    okText: t('tenancy.tenants.members.remove'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await detail.removeMember(member)
        toast.success(t('tenancy.tenants.members.removed'))
      }
      catch (error) {
        toast.danger((error as Error).message || t('common.messages.delete_failed'))
      }
    },
  })
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value: null | string | undefined): string {
  return value ? formatDate(value) : '-'
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XhDrawerRoot v-model:open="detail.open.value" side="right">
      <XhDrawerContent style="--xh-drawer-size: 800px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('tenancy.tenants.detail_title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <div class="xh-loading-stage" :class="{ 'is-loading': detail.loading.value }">
            <div class="xh-loading-stage__veil">
              <XhSpinner />
            </div>
            <XhEmptyStateRoot v-if="!detail.loading.value && !detail.detail.value" class="tenant-detail-empty">
              <XhEmptyStateIndicator>
                <Icon icon="lucide:alert-circle" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('common.messages.load_failed') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ detail.error.value }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
            <XhTabsRoot v-else-if="detail.detail.value" default-value="overview" variant="line">
              <XhTabsList>
                <XhTabsTrigger value="overview">
                  {{ t('tenancy.tenants.tabs.overview') }}
                </XhTabsTrigger>
                <XhTabsTrigger v-if="canReadMembers" value="members">
                  {{ t('tenancy.tenants.tabs.members') }}
                </XhTabsTrigger>
                <XhTabsIndicator />
              </XhTabsList>
              <XhTabsContent value="overview">
                <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.tenant_id') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.tenant.tenantId }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.name') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.tenant.name }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.status') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>
                      <XhTagRoot variant="subtle" size="sm" :tone="detail.detail.value.tenant.status === 'Active' ? 'success' : 'danger'">
                        <XhTagLabel>{{ statusLabel(detail.detail.value.tenant.status) }}</XhTagLabel>
                      </XhTagRoot>
                    </XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.edition') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ editionLabel(detail.detail.value.tenant.editionId) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.admin') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.adminUserName ?? t('tenancy.tenants.admin_none') }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.user_count') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.userCount }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.expires_at') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatNullableDate(detail.detail.value.tenant.expiresAt) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.order') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ detail.detail.value.tenant.order }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.created_at') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatDate(detail.detail.value.tenant.createdAt) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                  <!-- 项数保持偶数：边框按子节点奇偶画竖线，单出一项会让末行只剩半边框 -->
                  <XhDescriptionsItem>
                    <XhDescriptionsLabel>{{ t('tenancy.tenants.fields.description') }}</XhDescriptionsLabel>
                    <XhDescriptionsValue>{{ formatNullable(detail.detail.value.tenant.description) }}</XhDescriptionsValue>
                  </XhDescriptionsItem>
                </XhDescriptionsRoot>
              </XhTabsContent>
              <XhTabsContent v-if="canReadMembers" value="members">
                <div class="tenant-member-toolbar">
                  <span class="text-caption text-fg-muted">{{ t('tenancy.tenants.members.hint') }}</span>
                  <XhButton v-if="canAddMember" variant="subtle" size="sm" tone="brand" @click="memberEditor.start()">
                    {{ t('tenancy.tenants.members.add') }}
                  </XhButton>
                </div>
                <div class="xh-loading-stage" :class="{ 'is-loading': detail.membersLoading.value }">
                  <div class="xh-loading-stage__veil">
                    <XhSpinner />
                  </div>
                  <XhEmptyStateRoot v-if="detail.membersError.value" class="tenant-detail-empty">
                    <XhEmptyStateIndicator>
                      <Icon icon="lucide:alert-circle" />
                    </XhEmptyStateIndicator>
                    <XhEmptyStateTitle>{{ t('tenancy.tenants.members.load_failed') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ detail.membersError.value }}</XhEmptyStateDescription>
                    <XhEmptyStateAction>
                      <XhButton variant="subtle" size="sm" @click="detail.loadMembers()">
                        {{ t('common.actions.refresh') }}
                      </XhButton>
                    </XhEmptyStateAction>
                  </XhEmptyStateRoot>
                  <XhEmptyStateRoot v-else-if="!detail.membersLoading.value && detail.members.value.length === 0" class="tenant-detail-empty">
                    <XhEmptyStateIndicator>
                      <Icon icon="lucide:inbox" />
                    </XhEmptyStateIndicator>
                    <XhEmptyStateTitle>{{ t('common.empty') }}</XhEmptyStateTitle>
                    <XhEmptyStateDescription>{{ t('tenancy.tenants.members.empty') }}</XhEmptyStateDescription>
                  </XhEmptyStateRoot>
                  <table v-else class="xh-detail-table" data-testid="tenant-members">
                    <thead>
                      <tr>
                        <th>{{ t('tenancy.tenants.members.user_name') }}</th>
                        <th>{{ t('tenancy.tenants.members.display_name') }}</th>
                        <th>{{ t('tenancy.tenants.members.home_tenant') }}</th>
                        <th>{{ t('tenancy.tenants.members.type') }}</th>
                        <th>{{ t('tenancy.tenants.members.joined_at') }}</th>
                        <th v-if="canRemoveMember">
                          {{ t('tenancy.tenants.members.operation') }}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="member in detail.members.value" :key="member.id">
                        <td>{{ member.userName }}</td>
                        <td>{{ formatNullable(member.displayName) }}</td>
                        <td>{{ member.homeTenantId }}</td>
                        <td>
                          <XhTagRoot variant="subtle" size="sm" :tone="member.memberType === 'Owner' ? 'warning' : 'neutral'">
                            <XhTagLabel>{{ memberTypeLabel(member.memberType) }}</XhTagLabel>
                          </XhTagRoot>
                        </td>
                        <td>{{ formatDate(member.createdAt) }}</td>
                        <td v-if="canRemoveMember">
                          <XhButton variant="subtle" size="sm" tone="danger" @click="confirmRemoveMember(member)">
                            {{ t('tenancy.tenants.members.remove') }}
                          </XhButton>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </XhTabsContent>
            </XhTabsRoot>
          </div>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <XEditModal
      v-model:show="editor.open.value"
      :title="editor.isEdit.value ? t('tenancy.tenants.edit_title') : t('tenancy.tenants.create_title')"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="tenant-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('tenantId')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.tenant_id') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.tenantId" name="tenantId" clearable autocomplete="off" :disabled="editor.isEdit.value" :placeholder="t('tenancy.tenants.tenant_id_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('tenantId')">
            {{ editor.fieldError('tenantId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('tenancy.tenants.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="canReadEditions" :invalid="!!editor.fieldError('editionId')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.edition') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect
              v-model:value="editor.form.editionId"
              name="editionId"
              clearable
              :options="editionFormOptions"
              :placeholder="editor.isEdit.value ? t('tenancy.tenants.edition_unrestricted') : t('tenancy.tenants.edition_default')"
            />
          </XhFieldControl>
          <XhFieldDescription v-if="editor.isEdit.value">
            {{ t('tenancy.tenants.edition_hint') }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="editor.fieldError('editionId')">
            {{ editor.fieldError('editionId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('expiresAt')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.expires_at') }}</XhFieldLabel>
          <XhFieldControl>
            <XDatePicker v-model:value="editor.form.expiresAt" name="expiresAt" clearable show-time :placeholder="t('tenancy.tenants.expires_never')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('expiresAt')">
            {{ editor.fieldError('expiresAt') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('description')">
          <XhFieldLabel>{{ t('tenancy.tenants.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" type="textarea" :rows="3" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('description')">
            {{ editor.fieldError('description') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="!editor.isEdit.value && auth.can('tenancy.tenants.initialize-admin')" class="xh-span-2 text-caption text-fg-muted">
          {{ t('tenancy.tenants.create_admin_hint') }}
        </p>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="admin.open.value"
      :title="t('tenancy.tenants.admin_form.title', { name: admin.tenant.value?.name ?? '' })"
      :loading="admin.submitting.value"
      :form-id="adminFormId"
    >
      <XhFormRoot :id="adminFormId" class="xh-edit-form-grid" data-testid="admin-form" @submit="onAdminSubmit">
        <XhFieldRoot :invalid="!!admin.fieldError('userName')" required>
          <XhFieldLabel>{{ t('tenancy.tenants.admin_form.user_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="admin.form.userName" name="userName" clearable autocomplete="off" :placeholder="t('tenancy.tenants.admin_form.user_name_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="admin.fieldError('userName')">
            {{ admin.fieldError('userName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!admin.fieldError('displayName')">
          <XhFieldLabel>{{ t('tenancy.tenants.admin_form.display_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="admin.form.displayName" name="displayName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="admin.fieldError('displayName')">
            {{ admin.fieldError('displayName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!admin.fieldError('email')">
          <XhFieldLabel>{{ t('tenancy.tenants.admin_form.email') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="admin.form.email" name="email" type="email" clearable autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="admin.fieldError('email')">
            {{ admin.fieldError('email') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!admin.fieldError('password')" required>
          <XhFieldLabel>{{ t('tenancy.tenants.admin_form.password') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="admin.form.password" name="password" type="password" autocomplete="new-password" :placeholder="t('tenancy.tenants.admin_form.password_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="admin.fieldError('password')">
            {{ admin.fieldError('password') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="admin.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ admin.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <XEditModal
      v-model:show="memberEditor.open.value"
      :title="t('tenancy.tenants.members.add_title', { name: detail.detail.value?.tenant.name ?? '' })"
      :loading="memberEditor.submitting.value"
      :save-disabled="!memberEditor.form.userId"
      :form-id="memberFormId"
    >
      <XhFormRoot :id="memberFormId" class="xh-edit-form-grid" data-testid="member-form" @submit="onMemberSubmit">
        <XhFieldRoot class="xh-span-2" :invalid="!!(memberEditor.fieldError('userId') || memberEditor.userError.value)" required>
          <XhFieldLabel>{{ t('tenancy.tenants.members.user') }}</XhFieldLabel>
          <XhFieldControl>
            <XCombobox
              v-model:value="memberEditor.form.userId"
              name="userId"
              remote
              clearable
              :loading="memberEditor.userLoading.value"
              :options="memberEditor.userOptions.value"
              :placeholder="t('tenancy.tenants.members.user_placeholder')"
              @search="memberEditor.searchUsers"
            />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('userId') || memberEditor.userError.value">
            {{ memberEditor.fieldError('userId') ?? memberEditor.userError.value }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!memberEditor.fieldError('memberType')">
          <XhFieldLabel>{{ t('tenancy.tenants.members.type') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="memberEditor.form.memberType" name="memberType" :options="memberTypeOptions" />
          </XhFieldControl>
          <XhFieldErrorText v-if="memberEditor.fieldError('memberType')">
            {{ memberEditor.fieldError('memberType') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="memberEditor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ memberEditor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
.tenant-detail-empty {
  padding-block: var(--xh-space-8);
}

.tenant-member-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--xh-space-2);
  margin-block-end: var(--xh-space-3);
}
</style>
