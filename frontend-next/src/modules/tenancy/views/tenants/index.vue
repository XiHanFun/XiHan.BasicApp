<script lang="ts" setup>
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { useTenants } from './use-tenants'

defineOptions({ name: 'TenancyTenantsView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useTenants(20)

const columns = [
  { id: 'tenantId', label: 'tenancy.tenants.fields.tenant_id' },
  { id: 'name', label: 'tenancy.tenants.fields.name' },
  { id: 'status', label: 'tenancy.tenants.fields.status' },
  { id: 'admin', label: 'tenancy.tenants.fields.admin' },
  { id: 'expiresAt', label: 'tenancy.tenants.fields.expires_at' },
  { id: 'createdAt', label: 'tenancy.tenants.fields.created_at' },
] as const

onMounted(() => {
  void page.load()
})

function formatTime(value: string | null | undefined): string {
  return value ? new Date(value).toLocaleString() : '-'
}

// 状态值是契约枚举（字符串），文案键静态写出以便 i18n 门禁核对
function statusLabel(status: 'Active' | 'Suspended'): string {
  return status === 'Suspended' ? t('tenancy.tenants.status.suspended') : t('tenancy.tenants.status.active')
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('tenancy.tenants.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('tenancy.tenants.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 开通：按钮即操作，门控用操作的权限码；校验以后端为准，字段级 errors 落到对应字段 -->
    <form v-if="auth.can('tenancy.tenants.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-5" data-testid="create-tenant" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('tenancy.tenants.fields.tenant_id')" :error-text="page.fieldError('tenantId')" :invalid="!!page.fieldError('tenantId')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.tenantId" name="tenantId" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('tenancy.tenants.fields.name')" :error-text="page.fieldError('name')" :invalid="!!page.fieldError('name')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.name" name="name" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('tenancy.tenants.fields.description')" :error-text="page.fieldError('description')" :invalid="!!page.fieldError('description')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.description" name="description" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('tenancy.tenants.fields.order')" :error-text="page.fieldError('order')" :invalid="!!page.fieldError('order')">
        <template #control>
          <XhNumberFieldRoot v-model:value="page.form.order" name="order" :step="1" />
        </template>
      </XhFieldRoot>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('tenancy.tenants.actions.create') }}
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
            <XhTableRow v-for="row in page.items.value" :key="row.id" :value="row.id" :class="page.selected.value?.tenant.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.select(row.id)">
              <XhTableCell value="tenantId">
                <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selected.value?.tenant.id === row.id" @click.stop="page.select(row.id)">
                  {{ row.tenantId }}
                </button>
              </XhTableCell>
              <XhTableCell value="name">
                {{ row.name }}
              </XhTableCell>
              <XhTableCell value="status">
                <span :class="row.status === 'Suspended' ? 'text-fg-danger' : ''">{{ statusLabel(row.status) }}</span>
              </XhTableCell>
              <XhTableCell value="admin">
                {{ row.adminUserId ? '✓' : t('tenancy.tenants.no_admin') }}
              </XhTableCell>
              <XhTableCell value="expiresAt">
                {{ formatTime(row.expiresAt) }}
              </XhTableCell>
              <XhTableCell value="createdAt">
                {{ formatTime(row.createdAt) }}
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
              {{ t('tenancy.tenants.empty') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>
        <div class="flex items-center gap-3 text-caption text-fg-muted">
          <span>{{ t('tenancy.tenants.total', { total: page.total.value }) }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value <= 1 || page.loading.value" @click="page.load(page.pageIndex.value - 1)">
            {{ t('common.actions.previous') }}
          </XhButton>
          <span>{{ page.pageIndex.value }} / {{ page.pageCount.value }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value >= page.pageCount.value || page.loading.value" @click="page.load(page.pageIndex.value + 1)">
            {{ t('common.actions.next') }}
          </XhButton>
        </div>
      </div>

      <!-- 详情：编辑、管理员初始化（一次）、停用 / 恢复、删除；每个动作按自己的权限码门控 -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="tenant-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ page.selected.value.tenant.name }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.tenant.tenantId }} · {{ statusLabel(page.selected.value.tenant.status) }} · {{ t('tenancy.tenants.fields.user_count') }} {{ page.selected.value.userCount }}</span>
          <div class="ml-auto flex gap-2">
            <XhButton v-if="auth.can('tenancy.tenants.suspend') && page.selected.value.tenant.status === 'Active'" size="sm" :disabled="page.working.value" @click="page.suspend()">
              {{ t('tenancy.tenants.actions.suspend') }}
            </XhButton>
            <XhButton v-if="auth.can('tenancy.tenants.suspend') && page.selected.value.tenant.status === 'Suspended'" size="sm" :disabled="page.working.value" @click="page.resume()">
              {{ t('tenancy.tenants.actions.resume') }}
            </XhButton>
            <XhButton v-if="auth.can('tenancy.tenants.delete')" size="sm" :disabled="page.working.value" @click="page.remove()">
              {{ t('tenancy.tenants.actions.delete') }}
            </XhButton>
          </div>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('tenancy.tenants.saved') }}
        </p>
        <p v-if="page.notice.value === 'admin_created'" class="text-caption text-fg-success" role="status">
          {{ t('tenancy.tenants.admin_created') }}
        </p>

        <form class="flex flex-col gap-2" data-testid="edit-tenant" novalidate @submit.prevent="page.save()">
          <h3 class="text-body font-semibold">
            {{ t('tenancy.tenants.sections.detail') }}
          </h3>
          <div class="grid gap-3 md:grid-cols-3">
            <XhFieldRoot :label="t('tenancy.tenants.fields.name')" :error-text="page.editError('name')" :invalid="!!page.editError('name')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.name" name="name" :disabled="!auth.can('tenancy.tenants.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('tenancy.tenants.fields.description')" :error-text="page.editError('description')" :invalid="!!page.editError('description')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.description" name="description" :disabled="!auth.can('tenancy.tenants.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('tenancy.tenants.fields.order')" :error-text="page.editError('order')" :invalid="!!page.editError('order')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.edit.order" name="order" :step="1" :disabled="!auth.can('tenancy.tenants.update')" />
              </template>
            </XhFieldRoot>
          </div>
          <div v-if="auth.can('tenancy.tenants.update')">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('tenancy.tenants.actions.save') }}
            </XhButton>
          </div>
        </form>

        <div class="flex flex-col gap-2">
          <h3 class="text-body font-semibold">
            {{ t('tenancy.tenants.sections.admin') }}
          </h3>
          <p v-if="page.selected.value.adminUserName" class="text-body">
            {{ page.selected.value.adminUserName }}
          </p>
          <p v-else-if="!auth.can('tenancy.tenants.initialize-admin')" class="text-caption text-fg-muted">
            {{ t('tenancy.tenants.no_admin') }}
          </p>
          <form v-else class="grid gap-3 md:grid-cols-4" data-testid="initialize-admin" novalidate @submit.prevent="page.initializeAdmin()">
            <XhFieldRoot :label="t('tenancy.tenants.admin_form.user_name')" :error-text="page.adminError('userName')" :invalid="!!page.adminError('userName')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.adminForm.userName" name="userName" autocomplete="off" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('tenancy.tenants.admin_form.password')" :error-text="page.adminError('password')" :invalid="!!page.adminError('password')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.adminForm.password" name="password" type="password" autocomplete="new-password" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('tenancy.tenants.admin_form.display_name')" :error-text="page.adminError('displayName')" :invalid="!!page.adminError('displayName')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.adminForm.displayName" name="displayName" />
              </template>
            </XhFieldRoot>
            <div class="flex items-end">
              <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
                {{ t('tenancy.tenants.actions.initialize_admin') }}
              </XhButton>
            </div>
          </form>
        </div>
      </section>
    </div>
  </section>
</template>
