<script lang="ts" setup>
import type { DictRow } from './use-dicts'
import { onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import { useDicts } from './use-dicts'

defineOptions({ name: 'SettingsDictsView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useDicts(() => auth.tenantId === null, 20)

// 手写结构的表格每一行各自伸缩：列宽基准写在 width 里，表头与各行才按同一基准分剩余空间；不写就按各格内容定基准，整列对不齐
const columns = [
  { id: 'code', label: 'settings.dicts.fields.code', width: '8rem' },
  { id: 'name', label: 'settings.dicts.fields.name', width: '7rem' },
  { id: 'category', label: 'settings.dicts.fields.category', width: '5rem' },
  { id: 'owner', label: 'settings.dicts.fields.owner', width: '4rem' },
  { id: 'status', label: 'settings.dicts.fields.status', width: '4rem' },
] as const

const itemColumns = [
  { id: 'code', label: 'settings.dicts.item_fields.code', width: '8rem' },
  { id: 'name', label: 'settings.dicts.item_fields.name', width: '7rem' },
  { id: 'value', label: 'settings.dicts.item_fields.value', width: '5rem' },
  { id: 'isDefault', label: 'settings.dicts.item_fields.default', width: '3rem' },
  { id: 'status', label: 'settings.dicts.item_fields.status', width: '4rem' },
] as const

onMounted(() => {
  void page.load()
})

function statusLabel(status: 'Active' | 'Disabled'): string {
  return status === 'Disabled' ? t('settings.dicts.status.disabled') : t('settings.dicts.status.active')
}

function ownerLabel(row: DictRow): string {
  return row.isPlatform ? t('settings.dicts.owner.platform') : t('settings.dicts.owner.tenant')
}

const canWriteItems = (permission: string) => page.editable.value && auth.can(permission)
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('settings.dicts.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('settings.dicts.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 新建字典：写在调用方这一侧；编码平台与租户之间也不重 -->
    <form v-if="auth.can('settings.dicts.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-5" data-testid="create-dict" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('settings.dicts.fields.code')" :error-text="page.fieldError('code')" :invalid="!!page.fieldError('code')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.code" name="code" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.dicts.fields.name')" :error-text="page.fieldError('name')" :invalid="!!page.fieldError('name')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.name" name="name" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.dicts.fields.category')" :error-text="page.fieldError('category')" :invalid="!!page.fieldError('category')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.category" name="category" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.dicts.fields.order')" :error-text="page.fieldError('order')" :invalid="!!page.fieldError('order')">
        <template #control>
          <XhNumberFieldRoot v-model:value="page.form.order" name="order" :step="1" />
        </template>
      </XhFieldRoot>
      <div class="flex items-end gap-2">
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('settings.dicts.actions.create') }}
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
              <XhTableCell value="category">
                {{ row.category ?? '' }}
              </XhTableCell>
              <XhTableCell value="owner">
                {{ ownerLabel(row) }}
              </XhTableCell>
              <XhTableCell value="status">
                <span :class="row.status === 'Disabled' ? 'text-fg-muted' : ''">{{ statusLabel(row.status) }}</span>
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
              {{ t('settings.dicts.empty') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>
        <div class="flex items-center gap-3 text-caption text-fg-muted">
          <span>{{ t('settings.dicts.total', { total: page.total.value }) }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value <= 1 || page.loading.value" @click="page.load(page.pageIndex.value - 1)">
            {{ t('common.actions.previous') }}
          </XhButton>
          <span>{{ page.pageIndex.value }} / {{ page.pageCount.value }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value >= page.pageCount.value || page.loading.value" @click="page.load(page.pageIndex.value + 1)">
            {{ t('common.actions.next') }}
          </XhButton>
        </div>
      </div>

      <!-- 选中字典：编辑字典，下面是它的项（深度优先，按层级缩进）；另一侧的字典只读 -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="dict-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ page.selected.value.name }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.code }} · {{ ownerLabel(page.selected.value) }}</span>
          <XhButton v-if="page.editable.value && auth.can('settings.dicts.delete')" class="ml-auto" size="sm" :disabled="page.working.value" @click="page.remove()">
            {{ t('settings.dicts.actions.delete') }}
          </XhButton>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('settings.dicts.saved') }}
        </p>
        <p v-if="page.notice.value === 'item_saved'" class="text-caption text-fg-success" role="status">
          {{ t('settings.dicts.item_saved') }}
        </p>
        <p v-if="!page.editable.value" class="text-caption text-fg-muted">
          {{ t('settings.dicts.other_side_hint') }}
        </p>
        <form class="flex flex-col gap-2" data-testid="edit-dict" novalidate @submit.prevent="page.save()">
          <div class="grid gap-3 md:grid-cols-2">
            <XhFieldRoot :label="t('settings.dicts.fields.name')" :error-text="page.editError('name')" :invalid="!!page.editError('name')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.name" name="name" :disabled="!canWriteItems('settings.dicts.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.dicts.fields.category')" :error-text="page.editError('category')" :invalid="!!page.editError('category')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.category" name="category" :disabled="!canWriteItems('settings.dicts.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.dicts.fields.order')" :error-text="page.editError('order')" :invalid="!!page.editError('order')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.edit.order" name="order" :step="1" :disabled="!canWriteItems('settings.dicts.update')" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="dict-status">
              {{ t('settings.dicts.fields.status') }}
              <select id="dict-status" v-model="page.edit.status" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!canWriteItems('settings.dicts.update')">
                <option value="Active">
                  {{ t('settings.dicts.status.active') }}
                </option>
                <option value="Disabled">
                  {{ t('settings.dicts.status.disabled') }}
                </option>
              </select>
            </label>
            <XhFieldRoot class="md:col-span-2" :label="t('settings.dicts.fields.description')" :error-text="page.editError('description')" :invalid="!!page.editError('description')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.description" name="description" :disabled="!canWriteItems('settings.dicts.update')" />
              </template>
            </XhFieldRoot>
          </div>
          <div v-if="canWriteItems('settings.dicts.update')">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('settings.dicts.actions.save') }}
            </XhButton>
          </div>
        </form>

        <h3 class="text-label font-semibold">
          {{ t('settings.dicts.sections.items') }}
        </h3>
        <XhTableRoot
          v-if="auth.can('settings.dict-items.read')"
          :columns="itemColumns.map(c => ({ ...c, label: t(c.label) }))"
          :rows="page.dictItems.value.map(row => ({ id: row.id }))"
          :loading="page.itemsLoading.value"
          size="sm"
          ruled
        >
          <XhTableHeader>
            <XhTableRow>
              <XhTableColumnHeader v-for="column in itemColumns" :key="column.id" :value="column.id">
                <XhTableColumnLabel>{{ t(column.label) }}</XhTableColumnLabel>
              </XhTableColumnHeader>
            </XhTableRow>
          </XhTableHeader>
          <XhTableBody>
            <XhTableRow v-for="row in page.dictItems.value" :key="row.id" :value="row.id" :class="page.selectedItem.value?.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.selectItem(row.id)">
              <XhTableCell value="code">
                <span class="flex items-center gap-1">
                  <span v-for="level in row.depth" :key="level" class="inline-block w-4 shrink-0" aria-hidden="true" />
                  <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selectedItem.value?.id === row.id" @click.stop="page.selectItem(row.id)">
                    {{ row.code }}
                  </button>
                </span>
              </XhTableCell>
              <XhTableCell value="name">
                {{ row.name }}
              </XhTableCell>
              <XhTableCell value="value">
                {{ row.value ?? '' }}
              </XhTableCell>
              <XhTableCell value="isDefault">
                {{ row.isDefault ? t('settings.dicts.default_badge') : '' }}
              </XhTableCell>
              <XhTableCell value="status">
                <span :class="row.status === 'Disabled' ? 'text-fg-muted' : ''">{{ statusLabel(row.status) }}</span>
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.itemsLoading.value && page.dictItems.value.length === 0">
              {{ t('settings.dicts.no_items') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>

        <!-- 编辑选中的项 -->
        <form v-if="page.selectedItem.value" class="flex flex-col gap-2 rounded-surface border border-border p-3" data-testid="edit-dict-item" novalidate @submit.prevent="page.saveItem()">
          <header class="flex flex-wrap items-center gap-2">
            <span class="text-label font-semibold">{{ page.selectedItem.value.code }}</span>
            <XhButton v-if="canWriteItems('settings.dict-items.delete')" class="ml-auto" size="sm" type="button" :disabled="page.working.value" @click="page.removeItem()">
              {{ t('settings.dicts.actions.delete_item') }}
            </XhButton>
          </header>
          <div class="grid gap-3 md:grid-cols-2">
            <XhFieldRoot :label="t('settings.dicts.item_fields.name')" :error-text="page.itemEditError('name')" :invalid="!!page.itemEditError('name')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.itemEdit.name" name="name" :disabled="!canWriteItems('settings.dict-items.update')" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="dict-item-parent">
              {{ t('settings.dicts.item_fields.parent') }}
              <select id="dict-item-parent" v-model="page.itemEdit.parentId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!canWriteItems('settings.dict-items.update')">
                <option value="">
                  {{ t('settings.dicts.top_level') }}
                </option>
                <option v-for="choice in page.parentChoices.value" :key="choice.id" :value="choice.id">
                  {{ choice.code }} · {{ choice.name }}
                </option>
              </select>
            </label>
            <XhFieldRoot :label="t('settings.dicts.item_fields.value')" :error-text="page.itemEditError('value')" :invalid="!!page.itemEditError('value')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.itemEdit.value" name="value" :disabled="!canWriteItems('settings.dict-items.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.dicts.item_fields.order')" :error-text="page.itemEditError('order')" :invalid="!!page.itemEditError('order')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.itemEdit.order" name="order" :step="1" :disabled="!canWriteItems('settings.dict-items.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.dicts.item_fields.description')" :error-text="page.itemEditError('description')" :invalid="!!page.itemEditError('description')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.itemEdit.description" name="description" :disabled="!canWriteItems('settings.dict-items.update')" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.dicts.item_fields.metadata')" :error-text="page.itemEditError('metadata')" :invalid="!!page.itemEditError('metadata')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.itemEdit.metadata" name="metadata" autocomplete="off" :disabled="!canWriteItems('settings.dict-items.update')" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="dict-item-status">
              {{ t('settings.dicts.item_fields.status') }}
              <select id="dict-item-status" v-model="page.itemEdit.status" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!canWriteItems('settings.dict-items.update')">
                <option value="Active">
                  {{ t('settings.dicts.status.active') }}
                </option>
                <option value="Disabled">
                  {{ t('settings.dicts.status.disabled') }}
                </option>
              </select>
            </label>
            <label class="flex items-center gap-2 text-body">
              <input v-model="page.itemEdit.isDefault" type="checkbox" name="isDefault" :disabled="!canWriteItems('settings.dict-items.update')">
              {{ t('settings.dicts.item_fields.default') }}
            </label>
          </div>
          <div v-if="canWriteItems('settings.dict-items.update')">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('settings.dicts.actions.save_item') }}
            </XhButton>
          </div>
        </form>

        <!-- 新建项：字典要在本侧且启用 -->
        <form v-if="canWriteItems('settings.dict-items.create')" class="grid gap-3 md:grid-cols-3" data-testid="create-dict-item" novalidate @submit.prevent="page.submitItem()">
          <XhFieldRoot :label="t('settings.dicts.item_fields.code')" :error-text="page.itemError('code')" :invalid="!!page.itemError('code')" required>
            <template #control>
              <XhTextFieldRoot v-model:value="page.itemForm.code" name="code" autocomplete="off" />
            </template>
          </XhFieldRoot>
          <XhFieldRoot :label="t('settings.dicts.item_fields.name')" :error-text="page.itemError('name')" :invalid="!!page.itemError('name')" required>
            <template #control>
              <XhTextFieldRoot v-model:value="page.itemForm.name" name="name" />
            </template>
          </XhFieldRoot>
          <label class="flex flex-col gap-1 text-caption text-fg-muted" for="dict-item-new-parent">
            {{ t('settings.dicts.item_fields.parent') }}
            <select id="dict-item-new-parent" v-model="page.itemForm.parentId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
              <option value="">
                {{ t('settings.dicts.top_level') }}
              </option>
              <option v-for="choice in page.dictItems.value" :key="choice.id" :value="choice.id">
                {{ choice.code }} · {{ choice.name }}
              </option>
            </select>
          </label>
          <XhFieldRoot :label="t('settings.dicts.item_fields.value')" :error-text="page.itemError('value')" :invalid="!!page.itemError('value')">
            <template #control>
              <XhTextFieldRoot v-model:value="page.itemForm.value" name="value" />
            </template>
          </XhFieldRoot>
          <XhFieldRoot :label="t('settings.dicts.item_fields.order')" :error-text="page.itemError('order')" :invalid="!!page.itemError('order')">
            <template #control>
              <XhNumberFieldRoot v-model:value="page.itemForm.order" name="order" :step="1" />
            </template>
          </XhFieldRoot>
          <div class="flex items-end gap-3">
            <label class="flex items-center gap-2 text-body">
              <input v-model="page.itemForm.isDefault" type="checkbox" name="isDefault">
              {{ t('settings.dicts.item_fields.default') }}
            </label>
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value">
              {{ t('settings.dicts.actions.create_item') }}
            </XhButton>
          </div>
        </form>
      </section>
    </div>
  </section>
</template>
