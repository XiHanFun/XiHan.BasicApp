<script lang="ts" setup>
import type { Tone } from '@xihan-ui/core'
import type { MembershipRow } from './use-user-detail'
import type { RoleGrantItem } from './use-user-roles'
import type { UserRow, UserStatus } from './use-users'
import type { GrantTransferGroup, ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance, XDataTableColumn } from '~/components'
import type { SelectOption } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhClipboardControl, XhClipboardCopyTrigger, XhClipboardIndicator, XhClipboardInput, XhClipboardLabel, XhClipboardRoot, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerFooter, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, XDataTable, XEditModal, XGrantTransfer, XInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { PLATFORM_TENANT_ID } from '~/constants'
import { useAuthStore, useUserStore } from '~/stores'
import { formatDate } from '~/utils'
import { useUserDepartments } from './use-user-departments'
import { useUserDetail } from './use-user-detail'
import { useUserRoles } from './use-user-roles'
import { displayNameOf, setUserLocked, usePasswordReset, USER_STATUSES, usersResource, useUserEditor } from './use-users'

defineOptions({ name: 'IdentityUsersView' })

const { t } = useI18n()
const auth = useAuthStore()
const userStore = useUserStore()
const resource = usersResource()
const editor = useUserEditor()
const passwordReset = usePasswordReset()
const detail = useUserDetail()
const roleAssign = useUserRoles()
const departments = useUserDepartments()

/** 弹窗的保存钮靠这些 id 关联到各自的表单，点它才会走整表提交 */
const editFormId = useId()
const resetFormId = useId()
const memberFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

const isPlatformCaller = () => auth.tenantId === null
/** 用户的注册地是不是调用方所在的这一侧：角色只能指派给同一侧的用户 */
const isOwnSide = (row: UserRow) => row.homeTenantId === (auth.tenantId ?? PLATFORM_TENANT_ID)
/** 后端拒绝对自己锁定、删除与模仿，入口随之收起 */
const isSelf = (row: UserRow) => row.id === auth.user?.id

// 状态值是契约字面量，文案键静态写出以便 i18n 门禁核对
function statusLabel(status: UserStatus): string {
  switch (status) {
    case 'Locked': return t('identity.users.status.locked')
    case 'Disabled': return t('identity.users.status.disabled')
    default: return t('identity.users.status.active')
  }
}

function statusTone(status: UserStatus): Tone {
  switch (status) {
    case 'Locked': return 'warning'
    case 'Disabled': return 'danger'
    default: return 'success'
  }
}

function homeTenantLabel(homeTenantId: string): string {
  return homeTenantId === PLATFORM_TENANT_ID ? t('identity.users.home.platform') : homeTenantId
}

const statusOptions = computed<SelectOption[]>(() => USER_STATUSES.map(status => ({ label: statusLabel(status), value: status })))

// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as UserRow

/** 用户信息列：显示名加粗在上，@用户名在下（与 4.x 用户页同一副两行单元格） */
function renderUserInfo(row: UserRow) {
  return h('div', { class: 'min-w-0 leading-snug' }, [
    h('div', { class: 'truncate text-body font-semibold text-fg' }, displayNameOf(row)),
    h('div', { class: 'truncate text-caption text-fg-muted' }, `@${row.userName}`),
  ])
}

// ── 字段单一事实源（列 + 搜索；仅搜索 / 仅导出字段 visible:false；order 控顺序；可排序与可筛选的只放后端白名单里的） ──
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('identity.users.search'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.users.keyword_placeholder'), order: 0 },
  { key: 'userName', title: t('identity.users.fields.user_info'), dataType: 'string', sortable: true, exportable: true, render: row => renderUserInfo(asRow(row)), minWidth: 180, order: 1 },
  { key: 'displayName', title: t('identity.users.fields.display_name'), dataType: 'string', visible: false, exportable: true, order: 1.5 },
  { key: 'email', title: t('identity.users.fields.email'), dataType: 'email', advancedSearch: true, exportable: true, minWidth: 180, order: 2 },
  { key: 'phoneNumber', title: t('identity.users.fields.phone'), dataType: 'phone', exportable: true, width: 140, order: 3 },
  // 平台看得见全部用户，注册地一列才有区分意义；租户只看得到本租户注册的用户
  ...(isPlatformCaller()
    ? [{ key: 'homeTenantId', title: t('identity.users.fields.home_tenant'), dataType: 'string' as const, exportable: true, render: (row: object) => homeTenantLabel(asRow(row).homeTenantId), ellipsis: true, width: 120, order: 4 }]
    : []),
  { key: 'status', title: t('identity.users.fields.status'), dataType: 'enum', searchable: true, searchPlaceholder: t('identity.users.status_placeholder'), options: statusOptions.value, exportable: true, render: row => renderTag(statusLabel(asRow(row).status), statusTone(asRow(row).status)), width: 100, order: 5 },
  { key: 'lastLoginAt', title: t('identity.users.fields.last_login_at'), dataType: 'datetime', exportable: true, minWidth: 170, order: 6 },
  { key: 'createdAt', title: t('identity.users.fields.created_at'), dataType: 'datetime', sortable: true, exportable: true, minWidth: 170, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'identity.users',
  pageName: t('identity.users.title'),
  exportPermission: 'identity.users.read',
  batchRemovable: true,
  removePermission: 'identity.users.delete',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('identity.users.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'identity.users.create' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'identity.users.update' },
    // 回显要读角色；指派只对同一侧的用户成立（平台角色给平台账号、租户角色给本租户用户）
    { key: 'assignRoles', title: t('identity.users.actions.assign_roles'), scope: 'row', icon: 'lucide:users-round', visible: row => isOwnSide(asRow(row)) && auth.can('access.roles.read'), permission: 'access.roles.assign' },
    // 部门与岗位是租户侧数据：平台态不出这个入口
    { key: 'departments', title: t('identity.users.actions.departments'), scope: 'row', icon: 'lucide:building-2', visible: () => !isPlatformCaller(), permission: 'organization.memberships.read' },
    { key: 'lock', title: t('identity.users.actions.lock'), scope: 'row', tone: 'warning', icon: 'lucide:lock', confirm: true, confirmText: row => t('identity.users.confirm_lock', { name: displayNameOf(asRow(row)) }), visible: row => asRow(row).status !== 'Locked' && !isSelf(asRow(row)), permission: 'identity.users.lock' },
    { key: 'unlock', title: t('identity.users.actions.unlock'), scope: 'row', icon: 'lucide:lock-open', confirm: true, confirmText: row => t('identity.users.confirm_unlock', { name: displayNameOf(asRow(row)) }), visible: row => asRow(row).status === 'Locked', permission: 'identity.users.lock' },
    { key: 'resetPassword', title: t('identity.users.actions.reset_password'), scope: 'row', tone: 'danger', icon: 'lucide:key-square', permission: 'identity.users.reset-password' },
    // 模仿态里不能再模仿；目标须是正常状态的他人
    { key: 'impersonate', title: t('identity.users.actions.impersonate'), scope: 'row', icon: 'lucide:user-round-cog', confirm: true, confirmText: row => t('identity.users.impersonate_content', { name: displayNameOf(asRow(row)) }), visible: row => userStore.userInfo?.canImpersonate === true && asRow(row).status === 'Active' && !isSelf(asRow(row)), permission: 'identity.impersonation.start' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', confirm: true, confirmText: row => deleteConfirmText(t, displayNameOf(asRow(row))), visible: row => !isSelf(asRow(row)), permission: 'identity.users.delete' },
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
        openDetail(row)
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
      break
    case 'assignRoles':
      if (row)
        void openRoles(row)
      break
    case 'departments':
      if (row)
        void openDepartments(row)
      break
    case 'lock':
    case 'unlock':
      if (row)
        void toggleLock(row, payload.key === 'lock')
      break
    case 'resetPassword':
      if (row)
        passwordReset.start(row)
      break
    case 'impersonate':
      if (row)
        void impersonate(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

// ── 新建 / 编辑 ──
const modalTitle = computed(() => editor.isEdit.value
  ? t('identity.users.edit_title', { name: editor.form.userName })
  : t('identity.users.create_title'))

async function onSubmit() {
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
}

// ── 账号动作 ──
async function toggleLock(row: UserRow, locked: boolean) {
  try {
    await setUserLocked(row.id, locked)
    toast.success(locked ? t('identity.users.locked') : t('identity.users.unlocked'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.operation_failed'))
  }
}

async function removeRow(row: UserRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

/** 平台里模仿进目标的注册地（平台账号即平台）；租户里不传，沿用当前租户。成功后整页重建 */
async function impersonate(row: UserRow) {
  try {
    await auth.startImpersonation({ targetUserId: row.id, tenantId: isPlatformCaller() ? row.homeTenantId : null })
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.users.impersonate_failed'))
  }
}

// ── 重置密码：预填随机密码，提交后出一次性的复制框 ──
const resetTitle = computed(() => t('identity.users.reset_title', { name: passwordReset.target.value ? displayNameOf(passwordReset.target.value) : '' }))

async function onResetSubmit() {
  const row = passwordReset.target.value
  const password = await passwordReset.submit()
  if (!row || password === null)
    return
  void dialog.success({
    title: t('identity.users.reset_done_title'),
    okText: t('common.actions.close'),
    // 正文摆一个只读的新密码与复制钮，复制态由剪贴板部件自己反馈
    content: () => [
      h(XhClipboardRoot, { value: password }, () => [
        h(XhClipboardLabel, () => t('identity.users.reset_done_content', { name: displayNameOf(row) })),
        h(XhClipboardControl, null, () => [
          h(XhClipboardInput),
          h(XhClipboardCopyTrigger, { 'aria-label': t('identity.users.reset_copy') }, () => [
            h(XhClipboardIndicator),
            h(XhClipboardIndicator, { copied: true }),
          ]),
        ]),
      ]),
    ],
  })
}

// ── 查看详情：角色要能读角色且同一侧；部门只在租户侧且能读归属 ──
function openDetail(row: UserRow) {
  void detail.show(row, {
    roles: isOwnSide(row) && auth.can('access.roles.read'),
    departments: !isPlatformCaller() && auth.can('organization.memberships.read'),
  })
}

function formatNullable(value: null | string | undefined): string {
  return value === null || value === undefined || value === '' ? '-' : value
}

/** 部门归属的标签文案：部门 · 岗位，主部门另加标注 */
function membershipLabel(membership: MembershipRow): string {
  const text = membership.positionName ? `${membership.departmentName} · ${membership.positionName}` : membership.departmentName
  return membership.isPrimary ? t('identity.users.primary_label', { name: text }) : text
}

// ── 分配角色抽屉 ──
const roleGroups = computed<GrantTransferGroup<RoleGrantItem>[]>(() => [{ key: 'roles', name: '', items: roleAssign.items.value }])

async function openRoles(row: UserRow) {
  try {
    await roleAssign.start(row)
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.users.roles_load_failed'))
  }
}

async function saveRoles() {
  if (!roleAssign.dirty.value) {
    toast.info(t('identity.users.roles_no_change'))
    return
  }
  try {
    const result = await roleAssign.save()
    toast.success(t('identity.users.roles_saved', { assign: result.assigned, unassign: result.unassigned }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
}

// ── 所属部门抽屉 ──
const canAddDepartment = computed(() => auth.can('organization.memberships.create') && auth.can('organization.departments.read'))
const canRemoveDepartment = computed(() => auth.can('organization.memberships.delete'))
const canPickPosition = computed(() => canAddDepartment.value && auth.can('organization.positions.read'))

async function openDepartments(row: UserRow) {
  try {
    await departments.start(row, { departments: canAddDepartment.value, positions: canPickPosition.value })
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.users.departments_load_failed'))
  }
}

async function onAddDepartment() {
  try {
    if (await departments.add())
      toast.success(t('identity.users.department_added'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.save_failed'))
  }
}

function confirmRemoveDepartment(membership: MembershipRow) {
  void dialog.confirm({
    title: t('identity.users.actions.remove_department'),
    content: t('identity.users.confirm_remove_department', { name: membership.departmentName }),
    badge: 'warning',
    tone: 'danger',
    onOk: async () => {
      try {
        await departments.remove(membership.id)
        toast.success(t('identity.users.department_removed'))
      }
      catch (error) {
        toast.danger((error as Error).message || t('common.messages.delete_failed'))
      }
    },
  })
}

const membershipColumns = computed<XDataTableColumn<MembershipRow>[]>(() => [
  { key: 'departmentName', title: t('identity.users.member_form.department'), minWidth: 160, ellipsis: true },
  { key: 'positionName', title: t('identity.users.member_form.position'), minWidth: 120, ellipsis: true, render: membership => membership.positionName ?? '-' },
  { key: 'isPrimary', title: t('identity.users.fields.primary'), width: 110, render: membership => membership.isPrimary ? renderTag(t('common.statuses.yes'), 'brand') : '-' },
  ...(canRemoveDepartment.value
    ? [{
        key: 'actions',
        title: t('component.schema_page.actions_column'),
        width: 90,
        render: (membership: MembershipRow) => h(XhButton, { size: 'sm', variant: 'outline', tone: 'danger', disabled: departments.working.value, onClick: () => confirmRemoveDepartment(membership) }, () => t('identity.users.actions.remove_department')),
      }]
    : []),
])
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <!-- 新建 / 编辑 -->
    <XEditModal
      v-model:show="editor.open.value"
      :title="modalTitle"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="user-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('userName')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('identity.users.fields.user_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.userName" name="userName" clearable autocomplete="off" :disabled="editor.isEdit.value" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('userName')">
            {{ editor.fieldError('userName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('displayName')" required>
          <XhFieldLabel>{{ t('identity.users.fields.display_name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.displayName" name="displayName" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('displayName')">
            {{ editor.fieldError('displayName') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('email')">
          <XhFieldLabel>{{ t('identity.users.fields.email') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.email" name="email" type="email" clearable autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('email')">
            {{ editor.fieldError('email') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('phoneNumber')">
          <XhFieldLabel>{{ t('identity.users.fields.phone') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.phoneNumber" name="phoneNumber" type="tel" clearable autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('phoneNumber')">
            {{ editor.fieldError('phoneNumber') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="!editor.isEdit.value" :invalid="!!editor.fieldError('password')">
          <XhFieldLabel>{{ t('identity.users.fields.password') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.password" name="password" type="password" autocomplete="new-password" :placeholder="t('identity.users.password_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('password')">
            {{ editor.fieldError('password') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <!-- 重置密码 -->
    <XEditModal
      v-model:show="passwordReset.open.value"
      :title="resetTitle"
      :width="480"
      :loading="passwordReset.submitting.value"
      :form-id="resetFormId"
      :save-text="t('identity.users.reset_confirm')"
      save-tone="danger"
    >
      <XhFormRoot :id="resetFormId" class="xh-edit-form-grid" data-testid="reset-password-form" @submit="onResetSubmit">
        <p class="xh-span-2 text-caption text-fg-muted">
          {{ t('identity.users.reset_hint') }}
        </p>
        <XhFieldRoot class="xh-span-2" :invalid="!!passwordReset.fieldError('newPassword')" required>
          <XhFieldLabel>{{ t('identity.users.fields.new_password') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="passwordReset.form.newPassword" name="newPassword" type="password" autocomplete="new-password" />
          </XhFieldControl>
          <XhFieldErrorText v-if="passwordReset.fieldError('newPassword')">
            {{ passwordReset.fieldError('newPassword') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <div class="xh-span-2">
          <XhButton type="button" variant="subtle" size="sm" @click="passwordReset.regenerate()">
            <Icon icon="lucide:refresh-cw" width="14" height="14" />
            {{ t('identity.users.actions.generate_password') }}
          </XhButton>
        </div>
        <p v-if="passwordReset.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ passwordReset.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <!-- 详情 -->
    <XhDialogRoot v-model:open="detail.open.value">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 640px">
        <XhDialogHeader>
          <XhDialogTitle>
            <div v-if="detail.user.value" class="det-hd-entity">
              <div class="det-hd-ico">
                <Icon icon="lucide:user-round" width="22" height="22" />
              </div>
              <div class="min-w-0">
                <div class="det-hd-name">
                  {{ displayNameOf(detail.user.value) }}
                </div>
                <div class="det-hd-sub">
                  @{{ detail.user.value.userName }}
                </div>
              </div>
            </div>
          </XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody class="xh-overlay-body">
          <p v-if="detail.error.value" class="text-caption text-fg-danger" role="alert">
            {{ detail.error.value }}
          </p>
          <XhDescriptionsRoot v-if="detail.user.value" :columns="2" variant="outline" size="sm" data-testid="user-detail">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('identity.users.fields.email') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.user.value.email) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('identity.users.fields.phone') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.user.value.phoneNumber) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('identity.users.fields.home_tenant') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ homeTenantLabel(detail.user.value.homeTenantId) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('identity.users.fields.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <XhTagRoot variant="subtle" size="sm" :tone="statusTone(detail.user.value.status)">
                  <XhTagLabel>{{ statusLabel(detail.user.value.status) }}</XhTagLabel>
                </XhTagRoot>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('identity.users.fields.last_login_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(detail.user.value.lastLoginAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('identity.users.fields.created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(detail.user.value.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
            <XhDescriptionsItem v-if="detail.sections.value.roles" style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('identity.users.fields.roles') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <span v-if="detail.loading.value" class="text-caption text-fg-muted">{{ t('common.statuses.loading') }}</span>
                <span v-else-if="!detail.roles.value?.length">-</span>
                <div v-else class="flex flex-wrap gap-1">
                  <XhTagRoot v-for="role in detail.roles.value" :key="role.id" variant="subtle" size="sm" tone="info">
                    <XhTagLabel>{{ role.name }}</XhTagLabel>
                  </XhTagRoot>
                </div>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem v-if="detail.sections.value.departments" style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('identity.users.fields.departments') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <span v-if="detail.loading.value" class="text-caption text-fg-muted">{{ t('common.statuses.loading') }}</span>
                <span v-else-if="!detail.memberships.value?.length">-</span>
                <div v-else class="flex flex-wrap gap-1">
                  <XhTagRoot v-for="membership in detail.memberships.value" :key="membership.id" variant="subtle" size="sm" :tone="membership.isPrimary ? 'brand' : 'neutral'">
                    <XhTagLabel>{{ membershipLabel(membership) }}</XhTagLabel>
                  </XhTagRoot>
                </div>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
        </XhDialogBody>
        <XhDialogFooter>
          <XhButton variant="subtle" size="sm" @click="detail.open.value = false">
            {{ t('common.actions.close') }}
          </XhButton>
        </XhDialogFooter>
      </XhDialogContent>
    </XhDialogRoot>

    <!-- 分配角色抽屉（穿梭框，挪好后一次提交） -->
    <XhDrawerRoot v-model:open="roleAssign.open.value" side="right">
      <XhDrawerContent style="--xh-drawer-size: 720px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('identity.users.roles_title', { name: roleAssign.user.value ? displayNameOf(roleAssign.user.value) : '' }) }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <p class="m-0 text-caption text-fg-muted">
            {{ t('identity.users.roles_tip') }}
          </p>
          <XGrantTransfer
            :items="roleAssign.items.value"
            :value="roleAssign.draft.value"
            :groups="roleGroups"
            :get-label="role => role.name"
            :get-description="role => role.code"
            :loading="roleAssign.loading.value"
            :disabled="roleAssign.loading.value"
            :source-title="t('identity.users.roles_source')"
            :target-title="t('identity.users.roles_target')"
            :search-placeholder="t('identity.users.roles_search')"
            @update:value="roleAssign.change"
          >
            <template #suffix="{ item }">
              <XhTagRoot v-if="item.disabled" variant="subtle" size="sm" tone="neutral">
                <XhTagLabel>{{ t('common.statuses.disabled') }}</XhTagLabel>
              </XhTagRoot>
            </template>
          </XGrantTransfer>
        </XhDrawerBody>
        <XhDrawerFooter>
          <XhButton variant="subtle" @click="roleAssign.open.value = false">
            {{ t('common.actions.cancel') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="roleAssign.loading.value" :disabled="!roleAssign.dirty.value" @click="saveRoles">
            <XhButtonIndicator />
            <XhButtonLabel>{{ t('common.actions.save') }}</XhButtonLabel>
          </XhButton>
        </XhDrawerFooter>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 所属部门抽屉（租户侧）：归属一览，加入与移出即时生效 -->
    <XhDrawerRoot v-model:open="departments.open.value" side="right">
      <XhDrawerContent style="--xh-drawer-size: 640px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('identity.users.departments_title', { name: departments.user.value ? displayNameOf(departments.user.value) : '' }) }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <XDataTable
            :columns="membershipColumns"
            :data="departments.memberships.value"
            row-key="id"
            :loading="departments.loading.value"
            :empty-text="t('identity.users.no_departments')"
          />
          <XhFormRoot v-if="canAddDepartment" :id="memberFormId" class="xh-edit-form-grid" data-testid="add-department" @submit="onAddDepartment">
            <XhFieldRoot :invalid="!!departments.fieldError('departmentId')" required>
              <XhFieldLabel>{{ t('identity.users.member_form.department') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="departments.form.departmentId" name="departmentId" :options="departments.departmentOptions.value" :loading="departments.loading.value" clearable />
              </XhFieldControl>
              <XhFieldErrorText v-if="departments.fieldError('departmentId')">
                {{ departments.fieldError('departmentId') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <XhFieldRoot v-if="canPickPosition" :invalid="!!departments.fieldError('positionId')">
              <XhFieldLabel>{{ t('identity.users.member_form.position') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="departments.form.positionId" name="positionId" :options="departments.positionOptions.value" :loading="departments.loading.value" clearable />
              </XhFieldControl>
              <XhFieldErrorText v-if="departments.fieldError('positionId')">
                {{ departments.fieldError('positionId') }}
              </XhFieldErrorText>
            </XhFieldRoot>
            <div class="xh-span-2">
              <XhButton type="submit" variant="subtle" tone="brand" size="sm" :loading="departments.working.value" :disabled="!departments.form.departmentId || departments.working.value">
                <XhButtonIndicator />
                <XhButtonLabel>{{ t('identity.users.actions.add_department') }}</XhButtonLabel>
              </XhButton>
            </div>
          </XhFormRoot>
        </XhDrawerBody>
        <XhDrawerFooter>
          <XhButton variant="subtle" @click="departments.open.value = false">
            {{ t('common.actions.close') }}
          </XhButton>
        </XhDrawerFooter>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>
