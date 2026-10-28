<script lang="ts" setup>
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { useRoles } from './use-roles'

defineOptions({ name: 'AccessRolesView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useRoles(20)
const memberToAdd = ref('')

// 手写结构的表格每一行各自伸缩：列宽基准写在 width 里，表头与各行才按同一基准分剩余空间；不写就按各格内容定基准，整列对不齐
const columns = [
  { id: 'code', label: 'access.roles.fields.code', width: '6rem' },
  { id: 'name', label: 'access.roles.fields.name', width: '7rem' },
  { id: 'status', label: 'access.roles.fields.status', width: '4rem' },
  { id: 'builtIn', label: 'access.roles.fields.built_in', width: '4rem' },
  { id: 'createdAt', label: 'access.roles.fields.created_at', width: '10rem' },
] as const

onMounted(() => {
  void page.load()
})

// 选中角色后装候选用户（只有能读用户的主体）与候选部门（租户侧、能读部门的主体；平台没有部门也没有数据范围）
watch(() => page.selected.value?.role.id, (id) => {
  memberToAdd.value = ''
  if (id && auth.can('identity.users.read') && auth.can('access.roles.assign'))
    void page.loadUserOptions()
  if (id && auth.tenantId !== null && auth.can('organization.departments.read'))
    void page.loadDepartmentOptions()
})

// 数据范围的下拉项：契约枚举值 → 静态文案键（i18n 门禁核对）
const scopeOptions = [
  { value: 'SelfOnly', label: 'access.roles.data_scope.self_only' },
  { value: 'DepartmentOnly', label: 'access.roles.data_scope.department_only' },
  { value: 'DepartmentAndChildren', label: 'access.roles.data_scope.department_and_children' },
  { value: 'All', label: 'access.roles.data_scope.all' },
  { value: 'Custom', label: 'access.roles.data_scope.custom' },
] as const

function formatTime(value: string): string {
  return new Date(value).toLocaleString()
}

async function addMember() {
  if (await page.addMember(memberToAdd.value))
    memberToAdd.value = ''
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('access.roles.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('access.roles.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 创建：按钮即操作，门控用操作的权限码；校验以后端为准，字段级 errors 落到对应字段 -->
    <form v-if="auth.can('access.roles.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-4" data-testid="create-role" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('access.roles.fields.code')" :error-text="page.fieldError('code')" :invalid="!!page.fieldError('code')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.code" name="code" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('access.roles.fields.name')" :error-text="page.fieldError('name')" :invalid="!!page.fieldError('name')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.name" name="name" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('access.roles.fields.description')" :error-text="page.fieldError('description')" :invalid="!!page.fieldError('description')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.description" name="description" />
        </template>
      </XhFieldRoot>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('access.roles.actions.create') }}
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

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div class="flex flex-col gap-3">
        <XhTableRoot
          :columns="columns.map(c => ({ ...c, label: t(c.label) }))"
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
            <XhTableRow v-for="row in page.items.value" :key="row.id" :value="row.id" :class="page.selected.value?.role.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.select(row.id)">
              <XhTableCell value="code">
                <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selected.value?.role.id === row.id" @click.stop="page.select(row.id)">
                  {{ row.code }}
                </button>
              </XhTableCell>
              <XhTableCell value="name">
                {{ row.name }}
              </XhTableCell>
              <XhTableCell value="status">
                {{ row.status }}
              </XhTableCell>
              <XhTableCell value="builtIn">
                {{ row.isBuiltIn ? t('access.roles.built_in_badge') : '-' }}
              </XhTableCell>
              <XhTableCell value="createdAt">
                {{ formatTime(row.createdAt) }}
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
              {{ t('access.roles.empty') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>
        <div class="flex items-center gap-3 text-caption text-fg-muted">
          <span>{{ t('access.roles.total', { total: page.total.value }) }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value <= 1 || page.loading.value" @click="page.load(page.pageIndex.value - 1)">
            {{ t('common.actions.previous') }}
          </XhButton>
          <span>{{ page.pageIndex.value }} / {{ page.pageCount.value }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value >= page.pageCount.value || page.loading.value" @click="page.load(page.pageIndex.value + 1)">
            {{ t('common.actions.next') }}
          </XhButton>
        </div>
      </div>

      <!-- 详情：授予草稿按包分组，保存按差异授予 / 撤销；成员增删；删除（内建不可删） -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="role-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ page.selected.value.role.name }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.role.code }} · {{ page.selected.value.role.ownerTenantId }}</span>
          <span v-if="page.selected.value.role.isBuiltIn" class="rounded-control bg-subtle px-2 py-1 text-caption text-fg-muted">{{ t('access.roles.built_in_badge') }}</span>
          <XhButton v-if="auth.can('access.roles.delete') && !page.selected.value.role.isBuiltIn" class="ml-auto" size="sm" :disabled="page.working.value" @click="page.remove()">
            {{ t('access.roles.actions.delete') }}
          </XhButton>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('access.roles.saved') }}
        </p>
        <p v-if="page.notice.value === 'data_scope_saved'" class="text-caption text-fg-success" role="status">
          {{ t('access.roles.data_scope_saved') }}
        </p>

        <!-- 数据范围：只在租户侧（平台角色固定为全部）；自定义时勾部门并可含子部门 -->
        <div v-if="auth.tenantId !== null" class="flex flex-col gap-2" data-testid="role-data-scope">
          <h3 class="text-body font-semibold">
            {{ t('access.roles.sections.data_scope') }}
          </h3>
          <label class="flex flex-col gap-1 text-caption text-fg-muted" for="role-data-scope">
            {{ t('access.roles.sections.data_scope') }}
            <select id="role-data-scope" v-model="page.scopeDraft.value" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!auth.can('access.roles.update') || page.working.value">
              <option v-for="option in scopeOptions" :key="option.value" :value="option.value">
                {{ t(option.label) }}
              </option>
            </select>
          </label>
          <fieldset v-if="page.scopeDraft.value === 'Custom'" class="flex flex-col gap-1 rounded-control border border-border-subtle p-2">
            <legend class="px-1 text-caption text-fg-muted">
              {{ t('access.roles.custom_departments') }}
            </legend>
            <div v-for="department in page.departmentOptions.value" :key="department.id" class="flex flex-wrap items-center gap-3 text-body">
              <label class="flex items-center gap-2">
                <input type="checkbox" :checked="page.customDraft.value.has(department.id)" :disabled="!auth.can('access.roles.update') || page.working.value" @change="page.toggleCustomDepartment(department.id)">
                <span>{{ '· '.repeat(department.depth) }}{{ department.name }}</span>
              </label>
              <label v-if="page.customDraft.value.has(department.id)" class="flex items-center gap-2 text-caption text-fg-muted">
                <input type="checkbox" :checked="page.customDraft.value.get(department.id) === true" :disabled="!auth.can('access.roles.update') || page.working.value" @change="page.toggleIncludeChildren(department.id)">
                {{ t('access.roles.include_children') }}
              </label>
            </div>
          </fieldset>
          <div v-if="auth.can('access.roles.update')">
            <XhButton size="sm" variant="solid" :disabled="!page.scopeDirty.value || page.working.value" :loading="page.working.value" @click="page.saveDataScope()">
              {{ t('access.roles.actions.save_data_scope') }}
            </XhButton>
          </div>
        </div>

        <div class="flex flex-col gap-2">
          <h3 class="text-body font-semibold">
            {{ t('access.roles.sections.permissions') }}
          </h3>
          <fieldset v-for="group in page.groups.value" :key="group.package" class="flex flex-col gap-1 rounded-control border border-border-subtle p-2">
            <legend class="px-1 text-caption text-fg-muted">
              {{ group.package }}
            </legend>
            <label v-for="item in group.items" :key="item.code" class="flex items-start gap-2 text-body">
              <input
                type="checkbox"
                class="mt-1"
                :checked="page.draft.value.has(item.code)"
                :disabled="!auth.can('access.roles.grant') || page.working.value"
                @change="page.togglePermission(item.code)"
              >
              <span>
                <span class="font-medium">{{ item.displayName }}</span>
                <span class="text-caption text-fg-muted"> {{ item.code }}</span>
                <span v-if="item.description" class="block text-caption text-fg-muted">{{ item.description }}</span>
              </span>
            </label>
          </fieldset>
          <div v-if="auth.can('access.roles.grant')">
            <XhButton size="sm" variant="solid" :disabled="!page.dirty.value || page.working.value" :loading="page.working.value" @click="page.savePermissions()">
              {{ t('access.roles.actions.save_permissions') }}
            </XhButton>
          </div>
        </div>

        <div class="flex flex-col gap-2">
          <h3 class="text-body font-semibold">
            {{ t('access.roles.sections.members') }} · {{ page.members.value.length }}
          </h3>
          <ul v-if="page.members.value.length > 0" class="flex flex-col divide-y divide-border-subtle">
            <li v-for="member in page.members.value" :key="member.userId" class="flex items-center gap-2 py-1 text-body">
              <span>{{ member.displayName }}</span>
              <span class="text-caption text-fg-muted">{{ member.userName }}</span>
              <XhButton v-if="auth.can('access.roles.assign')" class="ml-auto" size="sm" :disabled="page.working.value" @click="page.removeMember(member.userId)">
                {{ t('access.roles.actions.remove_member') }}
              </XhButton>
            </li>
          </ul>
          <p v-else class="text-caption text-fg-muted">
            {{ t('access.roles.no_members') }}
          </p>
          <form v-if="auth.can('access.roles.assign') && auth.can('identity.users.read')" class="flex items-end gap-2" novalidate @submit.prevent="addMember">
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="member-to-add">
              {{ t('access.roles.select_user') }}
              <select id="member-to-add" v-model="memberToAdd" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
                <option value="">
                  -
                </option>
                <option v-for="user in page.userOptions.value" :key="user.id" :value="user.id">
                  {{ user.displayName }} ({{ user.userName }})
                </option>
              </select>
            </label>
            <XhButton type="submit" size="sm" :disabled="!memberToAdd || page.working.value">
              {{ t('access.roles.actions.add_member') }}
            </XhButton>
          </form>
        </div>
      </section>
    </div>
  </section>
</template>
