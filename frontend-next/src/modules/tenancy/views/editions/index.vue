<script lang="ts" setup>
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { useEditions } from './use-editions'

defineOptions({ name: 'TenancyEditionsView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useEditions()

// 手写结构的表格每一行各自伸缩：列宽基准写在 width 里，表头与各行才按同一基准分剩余空间；不写就按各格内容定基准，整列对不齐
const columns = [
  { id: 'code', label: 'tenancy.editions.fields.code', width: '6rem' },
  { id: 'name', label: 'tenancy.editions.fields.name', width: '8rem' },
  { id: 'permissions', label: 'tenancy.editions.fields.permissions', width: '5rem' },
  { id: 'tenants', label: 'tenancy.editions.fields.tenants', width: '5rem' },
  { id: 'order', label: 'tenancy.editions.fields.order', width: '4rem' },
] as const

onMounted(() => {
  void page.load()
})

function groupChecked(pkg: string): boolean {
  return page.catalog.value.filter(i => i.package === pkg).every(i => page.draft.value.has(i.code))
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('tenancy.editions.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('tenancy.editions.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 新建：白名单建好后在详情里勾选；校验以后端为准，字段级 errors 落到对应字段 -->
    <form v-if="auth.can('tenancy.editions.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-6" data-testid="create-edition" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('tenancy.editions.fields.code')" :error-text="page.fieldError('code')" :invalid="!!page.fieldError('code')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.code" name="code" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('tenancy.editions.fields.name')" :error-text="page.fieldError('name')" :invalid="!!page.fieldError('name')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.name" name="name" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('tenancy.editions.fields.description')" :error-text="page.fieldError('description')" :invalid="!!page.fieldError('description')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.description" name="description" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('tenancy.editions.fields.order')" :error-text="page.fieldError('order')" :invalid="!!page.fieldError('order')">
        <template #control>
          <XhNumberFieldRoot v-model:value="page.form.order" name="order" :step="1" />
        </template>
      </XhFieldRoot>
      <label class="flex items-end gap-2 pb-2 text-body">
        <input v-model="page.form.isDefault" type="checkbox" name="isDefault">
        {{ t('tenancy.editions.fields.is_default') }}
      </label>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('tenancy.editions.actions.create') }}
        </XhButton>
        <XhButton type="button" :disabled="page.loading.value" @click="page.load()">
          {{ t('common.actions.refresh') }}
        </XhButton>
      </div>
      <p v-if="page.submitError.value" class="text-caption text-fg-danger md:col-span-6" role="alert">
        {{ page.submitError.value }}
      </p>
    </form>

    <p v-if="page.listError.value" class="text-caption text-fg-danger" role="alert">
      {{ page.listError.value }}
    </p>

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
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
          <XhTableRow v-for="row in page.items.value" :key="row.id" :value="row.id" :class="page.selected.value?.edition.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.select(row.id)">
            <XhTableCell value="code">
              <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selected.value?.edition.id === row.id" @click.stop="page.select(row.id)">
                {{ row.code }}
              </button>
            </XhTableCell>
            <XhTableCell value="name">
              {{ row.name }}
              <span v-if="row.isDefault" class="ml-1 rounded-control bg-subtle px-1 text-caption text-fg-muted">{{ t('tenancy.editions.default_badge') }}</span>
            </XhTableCell>
            <XhTableCell value="permissions">
              {{ row.permissionCount }}
            </XhTableCell>
            <XhTableCell value="tenants">
              {{ row.tenantCount }}
            </XhTableCell>
            <XhTableCell value="order">
              {{ row.order }}
            </XhTableCell>
          </XhTableRow>
          <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
            {{ t('tenancy.editions.empty') }}
          </XhTableEmpty>
        </XhTableBody>
      </XhTableRoot>

      <!-- 详情：编辑、白名单、删除；每个动作按自己的权限码门控 -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="edition-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ page.selected.value.edition.name }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.edition.code }} · {{ t('tenancy.editions.fields.tenants') }} {{ page.selected.value.edition.tenantCount }}</span>
          <XhButton v-if="auth.can('tenancy.editions.delete')" class="ml-auto" size="sm" :disabled="page.working.value" @click="page.remove()">
            {{ t('tenancy.editions.actions.delete') }}
          </XhButton>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('tenancy.editions.saved') }}
        </p>
        <p v-if="page.notice.value === 'permissions_saved'" class="text-caption text-fg-success" role="status">
          {{ t('tenancy.editions.permissions_saved') }}
        </p>

        <form class="flex flex-col gap-2" data-testid="edit-edition" novalidate @submit.prevent="page.save()">
          <h3 class="text-body font-semibold">
            {{ t('tenancy.editions.sections.detail') }}
          </h3>
          <div class="grid gap-3 md:grid-cols-3">
            <XhFieldRoot :label="t('tenancy.editions.fields.name')" :error-text="page.editError('name')" :invalid="!!page.editError('name')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.name" name="name" :disabled="!auth.can('tenancy.editions.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('tenancy.editions.fields.description')" :error-text="page.editError('description')" :invalid="!!page.editError('description')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.description" name="description" :disabled="!auth.can('tenancy.editions.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('tenancy.editions.fields.order')" :error-text="page.editError('order')" :invalid="!!page.editError('order')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.edit.order" name="order" :step="1" :disabled="!auth.can('tenancy.editions.update')" />
              </template>
            </XhFieldRoot>
          </div>
          <label class="flex items-center gap-2 text-body">
            <input v-model="page.edit.isDefault" type="checkbox" name="isDefault" :disabled="!auth.can('tenancy.editions.update')">
            {{ t('tenancy.editions.fields.is_default') }}
          </label>
          <div v-if="auth.can('tenancy.editions.update')">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('tenancy.editions.actions.save') }}
            </XhButton>
          </div>
        </form>

        <div class="flex flex-col gap-2" data-testid="edition-permissions">
          <h3 class="text-body font-semibold">
            {{ t('tenancy.editions.sections.permissions') }}
            <span class="ml-1 text-caption font-normal text-fg-muted">{{ t('tenancy.editions.selected', { count: page.draft.value.size, total: page.catalog.value.length }) }}</span>
          </h3>
          <p class="text-caption text-fg-muted">
            {{ t('tenancy.editions.permissions_hint') }}
          </p>
          <fieldset v-for="group in page.groups.value" :key="group.package" class="flex flex-col gap-1 rounded-control border border-border-subtle p-2">
            <legend class="flex items-center gap-2 px-1 text-caption text-fg-muted">
              <label class="flex items-center gap-1">
                <input type="checkbox" :checked="groupChecked(group.package)" :disabled="!auth.can('tenancy.editions.update') || page.working.value" @change="page.setGroup(group.package, !groupChecked(group.package))">
                {{ group.package }}
              </label>
            </legend>
            <label v-for="item in group.items" :key="item.code" class="flex items-start gap-2 text-body">
              <input
                type="checkbox"
                class="mt-1"
                :checked="page.draft.value.has(item.code)"
                :disabled="!auth.can('tenancy.editions.update') || page.working.value"
                @change="page.togglePermission(item.code)"
              >
              <span>
                <span class="font-medium">{{ item.displayName }}</span>
                <span class="text-caption text-fg-muted"> {{ item.code }}</span>
                <span v-if="item.description" class="block text-caption text-fg-muted">{{ item.description }}</span>
              </span>
            </label>
          </fieldset>
          <div v-if="auth.can('tenancy.editions.update')">
            <XhButton size="sm" variant="solid" :disabled="!page.dirty.value || page.working.value" :loading="page.working.value" @click="page.savePermissions()">
              {{ t('tenancy.editions.actions.save_permissions') }}
            </XhButton>
          </div>
        </div>
      </section>
    </div>
  </section>
</template>
