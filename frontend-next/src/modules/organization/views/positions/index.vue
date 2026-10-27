<script lang="ts" setup>
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { usePositions } from './use-positions'

defineOptions({ name: 'OrganizationPositionsView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = usePositions(20)

const columns = [
  { id: 'code', label: 'organization.positions.fields.code' },
  { id: 'name', label: 'organization.positions.fields.name' },
  { id: 'status', label: 'organization.positions.fields.status' },
  { id: 'order', label: 'organization.positions.fields.order' },
  { id: 'createdAt', label: 'organization.positions.fields.created_at' },
] as const

onMounted(() => {
  void page.load()
})

function formatTime(value: string): string {
  return new Date(value).toLocaleString()
}

// 状态值是契约枚举（字符串），文案键静态写出以便 i18n 门禁核对
function statusLabel(status: 'Active' | 'Disabled'): string {
  return status === 'Disabled' ? t('organization.positions.status.disabled') : t('organization.positions.status.active')
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('organization.positions.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('organization.positions.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 创建：按钮即操作，门控用操作的权限码；校验以后端为准，字段级 errors 落到对应字段 -->
    <form v-if="auth.can('organization.positions.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-5" data-testid="create-position" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('organization.positions.fields.code')" :error-text="page.fieldError('code')" :invalid="!!page.fieldError('code')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.code" name="code" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('organization.positions.fields.name')" :error-text="page.fieldError('name')" :invalid="!!page.fieldError('name')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.name" name="name" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('organization.positions.fields.description')" :error-text="page.fieldError('description')" :invalid="!!page.fieldError('description')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.description" name="description" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('organization.positions.fields.order')" :error-text="page.fieldError('order')" :invalid="!!page.fieldError('order')">
        <template #control>
          <XhNumberFieldRoot v-model:value="page.form.order" name="order" :step="1" />
        </template>
      </XhFieldRoot>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('organization.positions.actions.create') }}
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
            <XhTableRow v-for="row in page.items.value" :key="row.id" :value="row.id" :class="page.selected.value?.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.select(row.id)">
              <XhTableCell value="code">
                <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selected.value?.id === row.id" @click.stop="page.select(row.id)">
                  {{ row.code }}
                </button>
              </XhTableCell>
              <XhTableCell value="name">
                {{ row.name }}
              </XhTableCell>
              <XhTableCell value="status">
                <span :class="row.status === 'Disabled' ? 'text-fg-muted' : ''">{{ statusLabel(row.status) }}</span>
              </XhTableCell>
              <XhTableCell value="order">
                {{ row.order }}
              </XhTableCell>
              <XhTableCell value="createdAt">
                {{ formatTime(row.createdAt) }}
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
              {{ t('organization.positions.empty') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>
        <div class="flex items-center gap-3 text-caption text-fg-muted">
          <span>{{ t('organization.positions.total', { total: page.total.value }) }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value <= 1 || page.loading.value" @click="page.load(page.pageIndex.value - 1)">
            {{ t('common.actions.previous') }}
          </XhButton>
          <span>{{ page.pageIndex.value }} / {{ page.pageCount.value }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value >= page.pageCount.value || page.loading.value" @click="page.load(page.pageIndex.value + 1)">
            {{ t('common.actions.next') }}
          </XhButton>
        </div>
      </div>

      <!-- 详情：编辑与删除按各自权限码门控 -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="position-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ page.selected.value.name }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.code }}</span>
          <XhButton v-if="auth.can('organization.positions.delete')" class="ml-auto" size="sm" :disabled="page.working.value" @click="page.remove()">
            {{ t('organization.positions.actions.delete') }}
          </XhButton>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('organization.positions.saved') }}
        </p>
        <form class="flex flex-col gap-2" data-testid="edit-position" novalidate @submit.prevent="page.save()">
          <div class="grid gap-3 md:grid-cols-2">
            <XhFieldRoot :label="t('organization.positions.fields.name')" :error-text="page.editError('name')" :invalid="!!page.editError('name')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.name" name="name" :disabled="!auth.can('organization.positions.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('organization.positions.fields.order')" :error-text="page.editError('order')" :invalid="!!page.editError('order')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.edit.order" name="order" :step="1" :disabled="!auth.can('organization.positions.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('organization.positions.fields.description')" :error-text="page.editError('description')" :invalid="!!page.editError('description')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.description" name="description" :disabled="!auth.can('organization.positions.update')" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="position-status">
              {{ t('organization.positions.fields.status') }}
              <select id="position-status" v-model="page.edit.status" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!auth.can('organization.positions.update')">
                <option value="Active">
                  {{ t('organization.positions.status.active') }}
                </option>
                <option value="Disabled">
                  {{ t('organization.positions.status.disabled') }}
                </option>
              </select>
            </label>
          </div>
          <div v-if="auth.can('organization.positions.update')">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('organization.positions.actions.save') }}
            </XhButton>
          </div>
        </form>
      </section>
    </div>
  </section>
</template>
