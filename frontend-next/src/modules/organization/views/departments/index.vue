<script lang="ts" setup>
import { onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { useDepartments } from './use-departments'

defineOptions({ name: 'OrganizationDepartmentsView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useDepartments()

// 手写结构的表格每一行各自伸缩：列宽基准写在 width 里，表头与各行才按同一基准分剩余空间；不写就按各格内容定基准，整列对不齐
const columns = [
  { id: 'code', label: 'organization.departments.fields.code', width: '8rem' },
  { id: 'name', label: 'organization.departments.fields.name', width: '7rem' },
  { id: 'status', label: 'organization.departments.fields.status', width: '4rem' },
  { id: 'memberCount', label: 'organization.departments.fields.member_count', width: '5rem' },
  { id: 'order', label: 'organization.departments.fields.order', width: '4rem' },
] as const

// 树的缩进：按深度取固定间距类（门禁不收裸 px）
const indents = ['', 'pl-4', 'pl-8', 'pl-12', 'pl-16', 'pl-20'] as const

onMounted(() => {
  void page.load()
})

// 选中部门后装候选用户与岗位（只有能读用户与岗位的主体）
watch(() => page.selected.value?.department.id, (id) => {
  if (id && auth.can('organization.memberships.create') && auth.can('identity.users.read') && auth.can('organization.positions.read'))
    void page.loadOptions()
})

function indentClass(depth: number): string {
  return indents[Math.min(depth, indents.length - 1)]!
}

// 状态值是契约枚举（字符串），文案键静态写出以便 i18n 门禁核对
function statusLabel(status: 'Active' | 'Disabled'): string {
  return status === 'Disabled' ? t('organization.departments.status.disabled') : t('organization.departments.status.active')
}

function pathOf(): string {
  const detail = page.selected.value
  if (!detail)
    return ''
  return [...detail.ancestors].reverse().map(a => a.name).concat(detail.department.name).join(' / ')
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('organization.departments.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('organization.departments.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 创建：按钮即操作，门控用操作的权限码；校验以后端为准，字段级 errors 落到对应字段 -->
    <form v-if="auth.can('organization.departments.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-5" data-testid="create-department" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('organization.departments.fields.code')" :error-text="page.fieldError('code')" :invalid="!!page.fieldError('code')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.code" name="code" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('organization.departments.fields.name')" :error-text="page.fieldError('name')" :invalid="!!page.fieldError('name')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.name" name="name" />
        </template>
      </XhFieldRoot>
      <label class="flex flex-col gap-1 text-caption text-fg-muted" for="department-parent">
        {{ t('organization.departments.fields.parent') }}
        <select id="department-parent" v-model="page.form.parentId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
          <option value="">
            {{ t('organization.departments.top_level') }}
          </option>
          <option v-for="row in page.flat.value" :key="row.id" :value="row.id">
            {{ '· '.repeat(row.depth) }}{{ row.name }}
          </option>
        </select>
      </label>
      <XhFieldRoot :label="t('organization.departments.fields.order')" :error-text="page.fieldError('order')" :invalid="!!page.fieldError('order')">
        <template #control>
          <XhNumberFieldRoot v-model:value="page.form.order" name="order" :step="1" />
        </template>
      </XhFieldRoot>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('organization.departments.actions.create') }}
        </XhButton>
        <XhButton type="button" :disabled="page.loading.value" @click="page.load()">
          {{ t('common.actions.refresh') }}
        </XhButton>
      </div>
      <p v-if="page.submitError.value" class="text-caption text-fg-danger md:col-span-5" role="alert">
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
          :rows="page.flat.value.map(row => ({ id: row.id }))"
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
            <XhTableRow v-for="row in page.flat.value" :key="row.id" :value="row.id" :class="page.selected.value?.department.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.select(row.id)">
              <XhTableCell value="code">
                <span class="block" :class="indentClass(row.depth)">
                  <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selected.value?.department.id === row.id" @click.stop="page.select(row.id)">
                    {{ row.code }}
                  </button>
                </span>
              </XhTableCell>
              <XhTableCell value="name">
                {{ row.name }}
              </XhTableCell>
              <XhTableCell value="status">
                <span :class="row.status === 'Disabled' ? 'text-fg-muted' : ''">{{ statusLabel(row.status) }}</span>
              </XhTableCell>
              <XhTableCell value="memberCount">
                {{ row.memberCount }}
              </XhTableCell>
              <XhTableCell value="order">
                {{ row.order }}
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.loading.value && page.flat.value.length === 0">
              {{ t('organization.departments.empty') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>
        <div class="text-caption text-fg-muted">
          {{ t('organization.departments.total', { total: page.total.value }) }}
        </div>
      </div>

      <!-- 详情：编辑、移动、删除、成员；每个动作按自己的权限码门控 -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="department-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ page.selected.value.department.name }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.department.code }} · {{ t('organization.departments.fields.path') }} {{ pathOf() }}</span>
          <XhButton v-if="auth.can('organization.departments.delete')" class="ml-auto" size="sm" :disabled="page.working.value" @click="page.remove()">
            {{ t('organization.departments.actions.delete') }}
          </XhButton>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('organization.departments.saved') }}
        </p>
        <p v-if="page.notice.value === 'moved'" class="text-caption text-fg-success" role="status">
          {{ t('organization.departments.moved') }}
        </p>
        <p v-if="page.notice.value === 'member_added'" class="text-caption text-fg-success" role="status">
          {{ t('organization.departments.member_added') }}
        </p>

        <form class="flex flex-col gap-2" data-testid="edit-department" novalidate @submit.prevent="page.save()">
          <h3 class="text-body font-semibold">
            {{ t('organization.departments.sections.detail') }}
          </h3>
          <div class="grid gap-3 md:grid-cols-2">
            <XhFieldRoot :label="t('organization.departments.fields.name')" :error-text="page.editError('name')" :invalid="!!page.editError('name')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.name" name="name" :disabled="!auth.can('organization.departments.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('organization.departments.fields.order')" :error-text="page.editError('order')" :invalid="!!page.editError('order')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.edit.order" name="order" :step="1" :disabled="!auth.can('organization.departments.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('organization.departments.fields.description')" :error-text="page.editError('description')" :invalid="!!page.editError('description')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.description" name="description" :disabled="!auth.can('organization.departments.update')" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="department-status">
              {{ t('organization.departments.fields.status') }}
              <select id="department-status" v-model="page.edit.status" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!auth.can('organization.departments.update')">
                <option value="Active">
                  {{ t('organization.departments.status.active') }}
                </option>
                <option value="Disabled">
                  {{ t('organization.departments.status.disabled') }}
                </option>
              </select>
            </label>
          </div>
          <div v-if="auth.can('organization.departments.update')" class="flex flex-wrap items-end gap-2">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('organization.departments.actions.save') }}
            </XhButton>
            <label class="ml-auto flex flex-col gap-1 text-caption text-fg-muted" for="department-move-target">
              {{ t('organization.departments.fields.parent') }}
              <select id="department-move-target" v-model="page.moveTarget.value" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
                <option value="">
                  {{ t('organization.departments.top_level') }}
                </option>
                <option v-for="row in page.moveOptions.value" :key="row.id" :value="row.id">
                  {{ '· '.repeat(row.depth) }}{{ row.name }}
                </option>
              </select>
            </label>
            <XhButton type="button" size="sm" :disabled="page.working.value" @click="page.move()">
              {{ t('organization.departments.actions.move') }}
            </XhButton>
          </div>
        </form>

        <div class="flex flex-col gap-2">
          <h3 class="text-body font-semibold">
            {{ t('organization.departments.sections.members') }} · {{ page.members.value.length }}
          </h3>
          <ul v-if="page.members.value.length > 0" class="flex flex-col divide-y divide-border-subtle">
            <li v-for="member in page.members.value" :key="member.id" class="flex flex-wrap items-center gap-2 py-1 text-body">
              <span>{{ member.displayName }}</span>
              <span class="text-caption text-fg-muted">{{ member.userName }}</span>
              <span v-if="member.positionName" class="text-caption text-fg-muted">· {{ member.positionName }}</span>
              <span v-if="member.isPrimary" class="rounded-control bg-subtle px-2 py-1 text-caption text-fg-muted">{{ t('organization.departments.primary_badge') }}</span>
              <span class="ml-auto flex gap-2">
                <XhButton v-if="auth.can('organization.memberships.update') && !member.isPrimary" size="sm" :disabled="page.working.value" @click="page.setPrimary(member)">
                  {{ t('organization.departments.actions.set_primary') }}
                </XhButton>
                <XhButton v-if="auth.can('organization.memberships.delete')" size="sm" :disabled="page.working.value" @click="page.removeMember(member.id)">
                  {{ t('organization.departments.actions.remove_member') }}
                </XhButton>
              </span>
            </li>
          </ul>
          <p v-else class="text-caption text-fg-muted">
            {{ t('organization.departments.no_members') }}
          </p>
          <form v-if="auth.can('organization.memberships.create') && auth.can('identity.users.read')" class="grid gap-3 md:grid-cols-4" data-testid="add-member" novalidate @submit.prevent="page.addMember()">
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="member-user">
              {{ t('organization.departments.member_form.user') }}
              <select id="member-user" v-model="page.memberForm.userId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
                <option value="">
                  -
                </option>
                <option v-for="user in page.userOptions.value" :key="user.id" :value="user.id">
                  {{ user.displayName }} ({{ user.userName }})
                </option>
              </select>
            </label>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="member-position">
              {{ t('organization.departments.member_form.position') }}
              <select id="member-position" v-model="page.memberForm.positionId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
                <option value="">
                  -
                </option>
                <option v-for="position in page.positionOptions.value" :key="position.id" :value="position.id">
                  {{ position.name }}
                </option>
              </select>
            </label>
            <XhFieldRoot :label="t('organization.departments.member_form.employee_number')" :error-text="page.memberError('employeeNumber')" :invalid="!!page.memberError('employeeNumber')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.memberForm.employeeNumber" name="employeeNumber" autocomplete="off" />
              </template>
            </XhFieldRoot>
            <div class="flex items-end gap-2">
              <label class="flex items-center gap-2 text-body">
                <input v-model="page.memberForm.isPrimary" type="checkbox">
                {{ t('organization.departments.member_form.primary') }}
              </label>
              <XhButton type="submit" size="sm" variant="solid" :disabled="!page.memberForm.userId || page.working.value" :loading="page.working.value">
                {{ t('organization.departments.actions.add_member') }}
              </XhButton>
            </div>
          </form>
        </div>
      </section>
    </div>
  </section>
</template>
