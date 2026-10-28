<script lang="ts" setup>
import type { ConfigDataType, ConfigRow } from './use-configs'
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { CONFIG_DATA_TYPES, useConfigs } from './use-configs'

defineOptions({ name: 'SettingsConfigsView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useConfigs(() => auth.tenantId === null, 20)

// 手写结构的表格每一行各自伸缩：列宽基准写在 width 里，表头与各行才按同一基准分剩余空间；不写就按各格内容定基准，整列对不齐
const columns = [
  { id: 'key', label: 'settings.configs.fields.key', width: '10rem' },
  { id: 'name', label: 'settings.configs.fields.name', width: '7rem' },
  { id: 'group', label: 'settings.configs.fields.group', width: '5rem' },
  { id: 'dataType', label: 'settings.configs.fields.data_type', width: '4rem' },
  { id: 'owner', label: 'settings.configs.fields.owner', width: '4rem' },
  { id: 'value', label: 'settings.configs.fields.value', width: '8rem' },
  { id: 'status', label: 'settings.configs.fields.status', width: '4rem' },
] as const

onMounted(() => {
  void page.load()
})

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function dataTypeLabel(type: ConfigDataType): string {
  switch (type) {
    case 'Number': return t('settings.configs.data_types.number')
    case 'Boolean': return t('settings.configs.data_types.boolean')
    case 'Json': return t('settings.configs.data_types.json')
    case 'Array': return t('settings.configs.data_types.array')
    default: return t('settings.configs.data_types.text')
  }
}

function statusLabel(status: 'Active' | 'Disabled'): string {
  return status === 'Disabled' ? t('settings.configs.status.disabled') : t('settings.configs.status.active')
}

function ownerLabel(row: ConfigRow): string {
  return row.isPlatform ? t('settings.configs.owner.platform') : t('settings.configs.owner.tenant')
}

function valueText(row: ConfigRow): string {
  if (row.isEncrypted)
    return row.hasValue ? t('settings.configs.encrypted_value') : ''
  return row.value ?? ''
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('settings.configs.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('settings.configs.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 新建：写在调用方这一侧；租户用与平台参数相同的键即覆盖它 -->
    <form v-if="auth.can('settings.configs.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-4" data-testid="create-config" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('settings.configs.fields.key')" :error-text="page.fieldError('key')" :invalid="!!page.fieldError('key')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.key" name="key" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.configs.fields.name')" :error-text="page.fieldError('name')" :invalid="!!page.fieldError('name')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.name" name="name" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.configs.fields.group')" :error-text="page.fieldError('group')" :invalid="!!page.fieldError('group')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.group" name="group" />
        </template>
      </XhFieldRoot>
      <label class="flex flex-col gap-1 text-caption text-fg-muted" for="config-data-type">
        {{ t('settings.configs.fields.data_type') }}
        <select id="config-data-type" v-model="page.form.dataType" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
          <option v-for="type in CONFIG_DATA_TYPES" :key="type" :value="type">
            {{ dataTypeLabel(type) }}
          </option>
        </select>
      </label>
      <XhFieldRoot :label="t('settings.configs.fields.value')" :error-text="page.fieldError('value')" :invalid="!!page.fieldError('value')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.value" name="value" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.configs.fields.default_value')" :error-text="page.fieldError('defaultValue')" :invalid="!!page.fieldError('defaultValue')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.defaultValue" name="defaultValue" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.configs.fields.order')" :error-text="page.fieldError('order')" :invalid="!!page.fieldError('order')">
        <template #control>
          <XhNumberFieldRoot v-model:value="page.form.order" name="order" :step="1" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.configs.fields.description')" :error-text="page.fieldError('description')" :invalid="!!page.fieldError('description')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.description" name="description" />
        </template>
      </XhFieldRoot>
      <div class="flex flex-wrap items-center gap-4 text-body md:col-span-2">
        <label class="flex items-center gap-2">
          <input v-model="page.form.isEncrypted" type="checkbox" name="isEncrypted">
          {{ t('settings.configs.fields.encrypted') }}
        </label>
        <label v-if="auth.tenantId === null" class="flex items-center gap-2">
          <input v-model="page.form.allowTenantOverride" type="checkbox" name="allowTenantOverride">
          {{ t('settings.configs.fields.allow_tenant_override') }}
        </label>
      </div>
      <div class="flex items-end gap-2 md:col-span-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('settings.configs.actions.create') }}
        </XhButton>
        <XhButton type="button" :disabled="page.loading.value" @click="page.load()">
          {{ t('common.actions.refresh') }}
        </XhButton>
      </div>
      <p v-if="page.submitError.value" class="text-caption text-fg-danger md:col-span-4" role="alert">
        {{ page.submitError.value }}
      </p>
    </form>

    <form class="flex items-end gap-2" role="search" novalidate @submit.prevent="page.load(1)">
      <XhFieldRoot :label="t('settings.configs.search')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.keyword.value" name="keyword" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhButton type="submit" :disabled="page.loading.value">
        {{ t('settings.configs.actions.search') }}
      </XhButton>
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
            <XhTableRow v-for="row in page.items.value" :key="row.id" :value="row.id" :class="page.selected.value?.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.select(row.id)">
              <XhTableCell value="key">
                <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selected.value?.id === row.id" @click.stop="page.select(row.id)">
                  {{ row.key }}
                </button>
              </XhTableCell>
              <XhTableCell value="name">
                {{ row.name }}
              </XhTableCell>
              <XhTableCell value="group">
                {{ row.group ?? '' }}
              </XhTableCell>
              <XhTableCell value="dataType">
                {{ dataTypeLabel(row.dataType) }}
              </XhTableCell>
              <XhTableCell value="owner">
                {{ ownerLabel(row) }}
              </XhTableCell>
              <XhTableCell value="value">
                <span class="break-all" :class="row.isEncrypted ? 'text-fg-muted' : ''">{{ valueText(row) }}</span>
              </XhTableCell>
              <XhTableCell value="status">
                <span :class="row.status === 'Disabled' ? 'text-fg-muted' : ''">{{ statusLabel(row.status) }}</span>
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
              {{ t('settings.configs.empty') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>
        <div class="flex items-center gap-3 text-caption text-fg-muted">
          <span>{{ t('settings.configs.total', { total: page.total.value }) }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value <= 1 || page.loading.value" @click="page.load(page.pageIndex.value - 1)">
            {{ t('common.actions.previous') }}
          </XhButton>
          <span>{{ page.pageIndex.value }} / {{ page.pageCount.value }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value >= page.pageCount.value || page.loading.value" @click="page.load(page.pageIndex.value + 1)">
            {{ t('common.actions.next') }}
          </XhButton>
        </div>
      </div>

      <!-- 详情：本侧的参数按各自权限码可改可删；另一侧的只读，租户可一键带出覆盖表单 -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="config-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ page.selected.value.name }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.key }} · {{ ownerLabel(page.selected.value) }}</span>
          <span v-if="page.selected.value.isBuiltIn" class="text-caption text-fg-muted">{{ t('settings.configs.built_in') }}</span>
          <XhButton v-if="page.editable.value && auth.can('settings.configs.delete')" class="ml-auto" size="sm" :disabled="page.working.value" @click="page.remove()">
            {{ t('settings.configs.actions.delete') }}
          </XhButton>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('settings.configs.saved') }}
        </p>
        <div v-if="!page.editable.value" class="flex flex-wrap items-center gap-2 text-caption text-fg-muted">
          <span>{{ t('settings.configs.other_side_hint') }}</span>
          <XhButton v-if="auth.tenantId !== null && page.selected.value.allowTenantOverride && auth.can('settings.configs.create')" size="sm" @click="page.overrideFrom(page.selected.value)">
            {{ t('settings.configs.actions.override') }}
          </XhButton>
        </div>
        <form class="flex flex-col gap-2" data-testid="edit-config" novalidate @submit.prevent="page.save()">
          <div class="grid gap-3 md:grid-cols-2">
            <XhFieldRoot :label="t('settings.configs.fields.name')" :error-text="page.editError('name')" :invalid="!!page.editError('name')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.name" name="name" :disabled="!page.editable.value || !auth.can('settings.configs.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.configs.fields.group')" :error-text="page.editError('group')" :invalid="!!page.editError('group')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.group" name="group" :disabled="!page.editable.value || !auth.can('settings.configs.update')" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="config-edit-data-type">
              {{ t('settings.configs.fields.data_type') }}
              <select id="config-edit-data-type" v-model="page.edit.dataType" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!page.editable.value || page.selected.value.isBuiltIn || !auth.can('settings.configs.update')">
                <option v-for="type in CONFIG_DATA_TYPES" :key="type" :value="type">
                  {{ dataTypeLabel(type) }}
                </option>
              </select>
            </label>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="config-status">
              {{ t('settings.configs.fields.status') }}
              <select id="config-status" v-model="page.edit.status" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!page.editable.value || !auth.can('settings.configs.update')">
                <option value="Active">
                  {{ t('settings.configs.status.active') }}
                </option>
                <option value="Disabled">
                  {{ t('settings.configs.status.disabled') }}
                </option>
              </select>
            </label>
            <XhFieldRoot :label="t('settings.configs.fields.value')" :error-text="page.editError('value')" :invalid="!!page.editError('value')" :description="page.selected.value.isEncrypted ? t('settings.configs.encrypted_hint') : undefined">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.value" name="value" autocomplete="off" :disabled="!page.editable.value || !auth.can('settings.configs.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.configs.fields.default_value')" :error-text="page.editError('defaultValue')" :invalid="!!page.editError('defaultValue')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.defaultValue" name="defaultValue" autocomplete="off" :disabled="!page.editable.value || !auth.can('settings.configs.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.configs.fields.order')" :error-text="page.editError('order')" :invalid="!!page.editError('order')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.edit.order" name="order" :step="1" :disabled="!page.editable.value || !auth.can('settings.configs.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.configs.fields.description')" :error-text="page.editError('description')" :invalid="!!page.editError('description')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.description" name="description" :disabled="!page.editable.value || !auth.can('settings.configs.update')" />
              </template>
            </XhFieldRoot>
          </div>
          <div class="flex flex-wrap gap-4 text-body">
            <label class="flex items-center gap-2">
              <input v-model="page.edit.isEncrypted" type="checkbox" name="isEncrypted" :disabled="!page.editable.value || !auth.can('settings.configs.update')">
              {{ t('settings.configs.fields.encrypted') }}
            </label>
            <label v-if="page.selected.value.isPlatform" class="flex items-center gap-2">
              <input v-model="page.edit.allowTenantOverride" type="checkbox" name="allowTenantOverride" :disabled="!page.editable.value || !auth.can('settings.configs.update')">
              {{ t('settings.configs.fields.allow_tenant_override') }}
            </label>
          </div>
          <div v-if="page.editable.value && auth.can('settings.configs.update')">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('settings.configs.actions.save') }}
            </XhButton>
          </div>
        </form>
      </section>
    </div>
  </section>
</template>
