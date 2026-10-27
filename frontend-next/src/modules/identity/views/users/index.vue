<script lang="ts" setup>
import { onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { useUserDetail } from './use-user-detail'
import { useUsers } from './use-users'

defineOptions({ name: 'IdentityUsersView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useUsers(10)
const detail = useUserDetail()

onMounted(() => {
  void page.load()
})

const columns = [
  { id: 'userName', label: 'identity.users.fields.user_name' },
  { id: 'displayName', label: 'identity.users.fields.display_name' },
  { id: 'email', label: 'identity.users.fields.email' },
  { id: 'status', label: 'identity.users.fields.status' },
  { id: 'createdAt', label: 'identity.users.fields.created_at' },
] as const

function formatTime(value: string | null | undefined): string {
  // 后端一律 UTC；按浏览器区域显示（用户偏好时区随 N1 的个人设置接入）
  return value ? new Date(value).toLocaleString() : '-'
}

// 状态值是契约枚举（字符串），文案键静态写出以便 i18n 门禁核对
function statusLabel(status: 'Active' | 'Disabled' | 'Locked'): string {
  if (status === 'Locked')
    return t('identity.users.status.locked')
  return status === 'Disabled' ? t('identity.users.status.disabled') : t('identity.users.status.active')
}

// 详情面板装哪些区块由权限码决定（安全边界仍在后端）：角色要能读角色；部门只在租户侧且能读归属
const canRoles = () => auth.can('access.roles.read')
const canDepartments = () => auth.tenantId !== null && auth.can('organization.memberships.read')

function select(id: string) {
  void detail.select(id, { roles: canRoles(), departments: canDepartments() })
}

// 选中后装加入部门的候选（能读部门与岗位的主体）
watch(() => detail.user.value?.id, (id) => {
  if (id && canDepartments() && auth.can('organization.memberships.create') && auth.can('organization.departments.read') && auth.can('organization.positions.read'))
    void detail.loadDepartmentOptions()
})

async function act(action: 'lock' | 'unlock' | 'remove') {
  const ok = action === 'lock' ? await detail.lock() : action === 'unlock' ? await detail.unlock() : await detail.remove()
  if (ok)
    await page.load()
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('identity.users.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('identity.users.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 创建：按钮即操作，门控用操作的权限码（安全边界在后端）；每个字段有可见标签；校验以后端 Schema 与自校验为准（novalidate 关掉浏览器原生校验，否则 type=email 不合法时 submit 根本不触发），字段级 errors 由 ApiError 交到对应字段 -->
    <form v-if="auth.can('identity.users.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-4" data-testid="create-user" novalidate @submit.prevent="page.submit()">
      <!-- 字段部件不写默认插槽：标题、错误文案由 label / error-text 属性铺出，控件放 control 插槽，标签的 for 与控件的 id、aria-* 由它生成 -->
      <XhFieldRoot :label="t('identity.users.fields.user_name')" :error-text="page.fieldError('userName')" :invalid="!!page.fieldError('userName')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.userName" name="userName" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.users.fields.display_name')" :error-text="page.fieldError('displayName')" :invalid="!!page.fieldError('displayName')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.displayName" name="displayName" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('identity.users.fields.email')" :error-text="page.fieldError('email')" :invalid="!!page.fieldError('email')">
        <template #control>
          <XhTextFieldRoot :value="page.form.email ?? ''" name="email" type="email" @update:value="page.form.email = $event" />
        </template>
      </XhFieldRoot>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('identity.users.actions.create') }}
        </XhButton>
        <XhButton type="button" :disabled="page.loading.value" @click="page.load()">
          {{ t('common.actions.refresh') }}
        </XhButton>
      </div>
      <p v-if="page.submitError.value" class="text-caption text-fg-danger md:col-span-4" role="alert">
        {{ page.submitError.value }}
      </p>
    </form>

    <p v-if="page.listError.value" class="text-caption text-fg-danger" role="alert">
      {{ page.listError.value }}
    </p>

    <div class="grid gap-4 lg:grid-cols-2">
      <div class="flex flex-col gap-3">
        <XhTableRoot
          :columns="columns.map(c => ({ id: c.id, label: t(c.label) }))"
          :rows="page.items.value.map(row => ({ id: row.id }))"
          :loading="page.loading.value"
          size="sm"
          ruled
        >
          <XhTableHeader>
            <XhTableRow>
              <XhTableColumnHeader v-for="column in columns" :key="column.id" :value="column.id">
                <XhTableColumnLabel>{{ t(column.label) }}</XhTableColumnLabel>
              </XhTableColumnHeader>
            </XhTableRow>
          </XhTableHeader>
          <XhTableBody>
            <XhTableRow v-for="row in page.items.value" :key="row.id" :value="row.id" :class="detail.user.value?.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="select(row.id)">
              <XhTableCell value="userName">
                <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="detail.user.value?.id === row.id" @click.stop="select(row.id)">
                  {{ row.userName }}
                </button>
              </XhTableCell>
              <XhTableCell value="displayName">
                {{ row.displayName }}
              </XhTableCell>
              <XhTableCell value="email">
                {{ row.email ?? '-' }}
              </XhTableCell>
              <XhTableCell value="status">
                <span :class="row.status === 'Active' ? '' : 'text-fg-danger'">{{ statusLabel(row.status) }}</span>
              </XhTableCell>
              <XhTableCell value="createdAt">
                {{ formatTime(row.createdAt) }}
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
              {{ t('identity.users.empty') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>

        <div class="flex items-center gap-3 text-caption text-fg-muted">
          <span>{{ t('identity.users.total', { total: page.total.value }) }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value <= 1 || page.loading.value" @click="page.load(page.pageIndex.value - 1)">
            {{ t('common.actions.previous') }}
          </XhButton>
          <span>{{ page.pageIndex.value }} / {{ page.pageCount.value }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value >= page.pageCount.value || page.loading.value" @click="page.load(page.pageIndex.value + 1)">
            {{ t('common.actions.next') }}
          </XhButton>
        </div>
      </div>

      <!-- 详情：账号动作、角色勾选（按差异指派 / 解除）、部门归属；每个动作按自己的权限码门控 -->
      <section v-if="detail.user.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="user-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ detail.user.value.displayName }}
          </h2>
          <span class="text-caption text-fg-muted">{{ detail.user.value.userName }} · {{ detail.user.value.homeTenantId }} · {{ statusLabel(detail.user.value.status) }}</span>
          <div class="ml-auto flex flex-wrap gap-2">
            <XhButton v-if="auth.can('identity.users.lock') && detail.user.value.status !== 'Locked'" size="sm" :disabled="detail.working.value" @click="act('lock')">
              {{ t('identity.users.actions.lock') }}
            </XhButton>
            <XhButton v-if="auth.can('identity.users.lock') && detail.user.value.status === 'Locked'" size="sm" :disabled="detail.working.value" @click="act('unlock')">
              {{ t('identity.users.actions.unlock') }}
            </XhButton>
            <XhButton v-if="auth.can('identity.users.delete')" size="sm" :disabled="detail.working.value" @click="act('remove')">
              {{ t('identity.users.actions.delete') }}
            </XhButton>
          </div>
        </header>
        <p v-if="detail.error.value" class="text-caption text-fg-danger" role="alert">
          {{ detail.error.value }}
        </p>
        <p v-if="detail.notice.value === 'roles_saved'" class="text-caption text-fg-success" role="status">
          {{ t('identity.users.roles_saved') }}
        </p>
        <p v-if="detail.notice.value === 'password_reset'" class="text-caption text-fg-success" role="status">
          {{ t('identity.users.password_reset') }}
        </p>
        <p v-if="detail.notice.value === 'department_added'" class="text-caption text-fg-success" role="status">
          {{ t('identity.users.department_added') }}
        </p>
        <p v-if="detail.notice.value === 'locked'" class="text-caption text-fg-success" role="status">
          {{ t('identity.users.locked') }}
        </p>

        <div class="flex flex-col gap-2">
          <h3 class="text-body font-semibold">
            {{ t('identity.users.sections.account') }}
          </h3>
          <dl class="grid grid-cols-2 gap-x-4 gap-y-1 text-body">
            <dt class="text-caption text-fg-muted">
              {{ t('identity.users.fields.email') }}
            </dt>
            <dd>{{ detail.user.value.email ?? '-' }}</dd>
            <dt class="text-caption text-fg-muted">
              {{ t('identity.users.fields.phone') }}
            </dt>
            <dd>{{ detail.user.value.phoneNumber ?? '-' }}</dd>
            <dt class="text-caption text-fg-muted">
              {{ t('identity.users.fields.last_login_at') }}
            </dt>
            <dd>{{ formatTime(detail.user.value.lastLoginAt) }}</dd>
          </dl>
          <form v-if="auth.can('identity.users.reset-password')" class="flex flex-wrap items-end gap-2" data-testid="reset-password" novalidate @submit.prevent="detail.resetPassword()">
            <XhFieldRoot :label="t('identity.users.reset_form.new_password')" :error-text="detail.resetError('newPassword')" :invalid="!!detail.resetError('newPassword')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="detail.resetForm.newPassword" name="newPassword" type="password" autocomplete="new-password" />
              </template>
            </XhFieldRoot>
            <XhButton type="submit" size="sm" :disabled="!detail.resetForm.newPassword || detail.working.value" :loading="detail.working.value">
              {{ t('identity.users.actions.reset_password') }}
            </XhButton>
          </form>
        </div>

        <div v-if="detail.sections.value.roles" class="flex flex-col gap-2" data-testid="user-roles">
          <h3 class="text-body font-semibold">
            {{ t('identity.users.sections.roles') }}
          </h3>
          <p v-if="detail.roleOptions.value.length === 0" class="text-caption text-fg-muted">
            {{ t('identity.users.no_roles') }}
          </p>
          <label v-for="role in detail.roleOptions.value" :key="role.id" class="flex items-center gap-2 text-body">
            <input type="checkbox" :checked="detail.roleDraft.value.has(role.id)" :disabled="!auth.can('access.roles.assign') || detail.working.value" @change="detail.toggleRole(role.id)">
            <span>{{ role.name }}</span>
            <span class="text-caption text-fg-muted">{{ role.code }}</span>
          </label>
          <div v-if="auth.can('access.roles.assign')">
            <XhButton size="sm" variant="solid" :disabled="!detail.rolesDirty.value || detail.working.value" :loading="detail.working.value" @click="detail.saveRoles()">
              {{ t('identity.users.actions.save_roles') }}
            </XhButton>
          </div>
        </div>

        <div v-if="detail.sections.value.departments" class="flex flex-col gap-2" data-testid="user-departments">
          <h3 class="text-body font-semibold">
            {{ t('identity.users.sections.departments') }}
          </h3>
          <ul v-if="detail.memberships.value.length > 0" class="flex flex-col divide-y divide-border-subtle">
            <li v-for="membership in detail.memberships.value" :key="membership.id" class="flex flex-wrap items-center gap-2 py-1 text-body">
              <span>{{ membership.departmentName }}</span>
              <span v-if="membership.positionName" class="text-caption text-fg-muted">· {{ membership.positionName }}</span>
              <span v-if="membership.isPrimary" class="rounded-control bg-subtle px-2 py-1 text-caption text-fg-muted">{{ t('identity.users.primary_badge') }}</span>
              <XhButton v-if="auth.can('organization.memberships.delete')" class="ml-auto" size="sm" :disabled="detail.working.value" @click="detail.removeDepartment(membership.id)">
                {{ t('identity.users.actions.remove_department') }}
              </XhButton>
            </li>
          </ul>
          <p v-else class="text-caption text-fg-muted">
            {{ t('identity.users.no_departments') }}
          </p>
          <form v-if="auth.can('organization.memberships.create') && auth.can('organization.departments.read')" class="flex flex-wrap items-end gap-2" data-testid="add-department" novalidate @submit.prevent="detail.addDepartment()">
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="user-department">
              {{ t('identity.users.member_form.department') }}
              <select id="user-department" v-model="detail.memberForm.departmentId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
                <option value="">
                  -
                </option>
                <option v-for="department in detail.departmentOptions.value" :key="department.id" :value="department.id">
                  {{ '· '.repeat(department.depth) }}{{ department.name }}
                </option>
              </select>
            </label>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="user-position">
              {{ t('identity.users.member_form.position') }}
              <select id="user-position" v-model="detail.memberForm.positionId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
                <option value="">
                  -
                </option>
                <option v-for="position in detail.positionOptions.value" :key="position.id" :value="position.id">
                  {{ position.name }}
                </option>
              </select>
            </label>
            <XhButton type="submit" size="sm" :disabled="!detail.memberForm.departmentId || detail.working.value" :loading="detail.working.value">
              {{ t('identity.users.actions.add_department') }}
            </XhButton>
          </form>
        </div>
      </section>
    </div>
  </section>
</template>
