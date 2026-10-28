<script lang="ts" setup>
import type { DateFormat, ResetCycle, RuleRow } from './use-numbering-rules'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppContext, useAuthStore } from '~/stores'
import { DATE_FORMATS, RESET_CYCLES, useNumberingRules } from './use-numbering-rules'

defineOptions({ name: 'SettingsNumberingRulesView' })

const { t } = useI18n()
const auth = useAuthStore()
const page = useNumberingRules(() => auth.tenantId === null, 20)
const timeZones = ref<Array<{ id: string, displayName: string }>>([{ id: 'UTC', displayName: 'UTC' }])

// 手写结构的表格每一行各自伸缩：列宽基准写在 width 里，表头与各行才按同一基准分剩余空间；不写就按各格内容定基准，整列对不齐
const columns = [
  { id: 'code', label: 'settings.numbering_rules.fields.code', width: '7rem' },
  { id: 'name', label: 'settings.numbering_rules.fields.name', width: '7rem' },
  { id: 'template', label: 'settings.numbering_rules.fields.template', width: '9rem' },
  { id: 'resetCycle', label: 'settings.numbering_rules.fields.reset_cycle', width: '4rem' },
  { id: 'current', label: 'settings.numbering_rules.fields.current_value', width: '6rem' },
  { id: 'owner', label: 'settings.numbering_rules.fields.owner', width: '4rem' },
  { id: 'status', label: 'settings.numbering_rules.fields.status', width: '4rem' },
] as const

const allocationColumns = [
  { id: 'generatedAt', label: 'settings.numbering_rules.allocation_fields.generated_at', width: '9rem' },
  { id: 'range', label: 'settings.numbering_rules.allocation_fields.range', width: '10rem' },
  { id: 'requester', label: 'settings.numbering_rules.allocation_fields.requester', width: '5rem' },
  { id: 'key', label: 'settings.numbering_rules.allocation_fields.key', width: '6rem' },
  { id: 'business', label: 'settings.numbering_rules.allocation_fields.business', width: '6rem' },
] as const

onMounted(async () => {
  void page.load()
  const options = await useAppContext().apis.timeZoneApi.options()
  timeZones.value = [{ id: 'UTC', displayName: 'UTC' }, ...options.filter(o => o.id !== 'UTC').map(o => ({ id: o.id, displayName: o.displayName }))]
})

function formatTime(value: string): string {
  return new Date(value).toLocaleString()
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function cycleLabel(cycle: ResetCycle): string {
  switch (cycle) {
    case 'Daily': return t('settings.numbering_rules.cycles.daily')
    case 'Monthly': return t('settings.numbering_rules.cycles.monthly')
    case 'Yearly': return t('settings.numbering_rules.cycles.yearly')
    default: return t('settings.numbering_rules.cycles.never')
  }
}

function dateFormatLabel(format: DateFormat): string {
  return format === 'None' ? t('settings.numbering_rules.no_date') : format
}

function statusLabel(status: 'Active' | 'Disabled'): string {
  return status === 'Disabled' ? t('settings.numbering_rules.status.disabled') : t('settings.numbering_rules.status.active')
}

function ownerLabel(row: RuleRow): string {
  return row.isPlatform ? t('settings.numbering_rules.owner.platform') : t('settings.numbering_rules.owner.tenant')
}

const canWrite = (permission: string) => page.editable.value && auth.can(permission)
</script>

<template>
  <section class="flex flex-col gap-4">
    <XhPageHeaderRoot size="sm">
      <XhPageHeaderTitle>{{ t('settings.numbering_rules.title') }}</XhPageHeaderTitle>
      <XhPageHeaderDescription>{{ t('settings.numbering_rules.description') }}</XhPageHeaderDescription>
    </XhPageHeaderRoot>

    <!-- 新建：日期格式要能区分重置周期；只有平台规则能供租户使用 -->
    <form v-if="auth.can('settings.numbering-rules.create')" class="grid gap-3 rounded-surface border border-border bg-surface p-4 md:grid-cols-4" data-testid="create-numbering-rule" novalidate @submit.prevent="page.submit()">
      <XhFieldRoot :label="t('settings.numbering_rules.fields.code')" :error-text="page.fieldError('code')" :invalid="!!page.fieldError('code')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.code" name="code" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.numbering_rules.fields.name')" :error-text="page.fieldError('name')" :invalid="!!page.fieldError('name')" required>
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.name" name="name" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.numbering_rules.fields.prefix')" :error-text="page.fieldError('prefix')" :invalid="!!page.fieldError('prefix')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.prefix" name="prefix" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <XhFieldRoot :label="t('settings.numbering_rules.fields.separator')" :error-text="page.fieldError('separator')" :invalid="!!page.fieldError('separator')">
        <template #control>
          <XhTextFieldRoot v-model:value="page.form.separator" name="separator" autocomplete="off" />
        </template>
      </XhFieldRoot>
      <label class="flex flex-col gap-1 text-caption text-fg-muted" for="rule-date-format">
        {{ t('settings.numbering_rules.fields.date_format') }}
        <select id="rule-date-format" v-model="page.form.dateFormat" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
          <option v-for="format in DATE_FORMATS" :key="format" :value="format">
            {{ dateFormatLabel(format) }}
          </option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-caption text-fg-muted" for="rule-reset-cycle">
        {{ t('settings.numbering_rules.fields.reset_cycle') }}
        <select id="rule-reset-cycle" v-model="page.form.resetCycle" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
          <option v-for="cycle in RESET_CYCLES" :key="cycle" :value="cycle">
            {{ cycleLabel(cycle) }}
          </option>
        </select>
      </label>
      <XhFieldRoot :label="t('settings.numbering_rules.fields.serial_length')" :error-text="page.fieldError('serialLength')" :invalid="!!page.fieldError('serialLength')">
        <template #control>
          <XhNumberFieldRoot v-model:value="page.form.serialLength" name="serialLength" :step="1" />
        </template>
      </XhFieldRoot>
      <label class="flex flex-col gap-1 text-caption text-fg-muted" for="rule-time-zone">
        {{ t('settings.numbering_rules.fields.time_zone') }}
        <select id="rule-time-zone" v-model="page.form.timeZoneId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg">
          <option v-for="zone in timeZones" :key="zone.id" :value="zone.id">
            {{ zone.displayName }}
          </option>
        </select>
      </label>
      <p v-if="page.fieldError('timeZoneId') || page.fieldError('dateFormat')" class="text-caption text-fg-danger md:col-span-4" role="alert">
        {{ page.fieldError('timeZoneId') ?? page.fieldError('dateFormat') }}
      </p>
      <div class="flex flex-wrap items-end gap-4 md:col-span-4">
        <label v-if="auth.tenantId === null" class="flex items-center gap-2 text-body">
          <input v-model="page.form.allowTenantUse" type="checkbox" name="allowTenantUse">
          {{ t('settings.numbering_rules.fields.allow_tenant_use') }}
        </label>
        <XhButton type="submit" variant="solid" :loading="page.submitting.value" :disabled="page.submitting.value">
          {{ t('settings.numbering_rules.actions.create') }}
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
            <XhTableRow v-for="row in page.items.value" :key="row.id" :value="row.id" :class="page.selected.value?.id === row.id ? 'bg-brand-subtle' : 'cursor-pointer'" @click="page.select(row.id)">
              <XhTableCell value="code">
                <button type="button" class="text-fg-brand underline-offset-2 hover:underline" :aria-pressed="page.selected.value?.id === row.id" @click.stop="page.select(row.id)">
                  {{ row.code }}
                </button>
              </XhTableCell>
              <XhTableCell value="name">
                {{ row.name }}
              </XhTableCell>
              <XhTableCell value="template">
                <span class="font-mono">{{ row.template }}</span>
              </XhTableCell>
              <XhTableCell value="resetCycle">
                {{ cycleLabel(row.resetCycle) }}
              </XhTableCell>
              <XhTableCell value="current">
                {{ row.currentPeriod ? `${row.currentPeriod} · ${row.currentValue}` : String(row.currentValue) }}
              </XhTableCell>
              <XhTableCell value="owner">
                {{ ownerLabel(row) }}
              </XhTableCell>
              <XhTableCell value="status">
                <span :class="row.status === 'Disabled' ? 'text-fg-muted' : ''">{{ statusLabel(row.status) }}</span>
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.loading.value && page.items.value.length === 0">
              {{ t('settings.numbering_rules.empty') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>
        <div class="flex items-center gap-3 text-caption text-fg-muted">
          <span>{{ t('settings.numbering_rules.total', { total: page.total.value }) }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value <= 1 || page.loading.value" @click="page.load(page.pageIndex.value - 1)">
            {{ t('common.actions.previous') }}
          </XhButton>
          <span>{{ page.pageIndex.value }} / {{ page.pageCount.value }}</span>
          <XhButton size="sm" :disabled="page.pageIndex.value >= page.pageCount.value || page.loading.value" @click="page.load(page.pageIndex.value + 1)">
            {{ t('common.actions.next') }}
          </XhButton>
        </div>
      </div>

      <!-- 详情：编辑（发过号后格式冻结）、预览、安全重置、发号记录；另一侧的规则只读 -->
      <section v-if="page.selected.value" class="flex flex-col gap-4 rounded-surface border border-border bg-surface p-4" data-testid="numbering-rule-detail">
        <header class="flex flex-wrap items-center gap-2">
          <h2 class="text-label font-semibold">
            {{ page.selected.value.name }}
          </h2>
          <span class="text-caption text-fg-muted">{{ page.selected.value.code }} · {{ ownerLabel(page.selected.value) }}</span>
          <XhButton v-if="canWrite('settings.numbering-rules.delete') && !page.formatFrozen.value" class="ml-auto" size="sm" :disabled="page.working.value" @click="page.remove()">
            {{ t('settings.numbering_rules.actions.delete') }}
          </XhButton>
        </header>
        <p v-if="page.detailError.value" class="text-caption text-fg-danger" role="alert">
          {{ page.detailError.value }}
        </p>
        <p v-if="page.notice.value === 'saved'" class="text-caption text-fg-success" role="status">
          {{ t('settings.numbering_rules.saved') }}
        </p>
        <p v-if="page.notice.value === 'reset'" class="text-caption text-fg-success" role="status">
          {{ t('settings.numbering_rules.reset_done') }}
        </p>
        <p v-if="!page.editable.value" class="text-caption text-fg-muted">
          {{ t('settings.numbering_rules.other_side_hint') }}
        </p>
        <p v-else-if="page.formatFrozen.value" class="text-caption text-fg-muted">
          {{ t('settings.numbering_rules.frozen_hint') }}
        </p>

        <form class="flex flex-col gap-2" data-testid="edit-numbering-rule" novalidate @submit.prevent="page.save()">
          <div class="grid gap-3 md:grid-cols-2">
            <XhFieldRoot :label="t('settings.numbering_rules.fields.name')" :error-text="page.editError('name')" :invalid="!!page.editError('name')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.name" name="name" :disabled="!canWrite('settings.numbering-rules.update')" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="rule-status">
              {{ t('settings.numbering_rules.fields.status') }}
              <select id="rule-status" v-model="page.edit.status" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!canWrite('settings.numbering-rules.update')">
                <option value="Active">
                  {{ t('settings.numbering_rules.status.active') }}
                </option>
                <option value="Disabled">
                  {{ t('settings.numbering_rules.status.disabled') }}
                </option>
              </select>
            </label>
            <XhFieldRoot :label="t('settings.numbering_rules.fields.prefix')" :error-text="page.editError('prefix')" :invalid="!!page.editError('prefix')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.prefix" name="prefix" :disabled="!canWrite('settings.numbering-rules.update') || page.formatFrozen.value" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.numbering_rules.fields.separator')" :error-text="page.editError('separator')" :invalid="!!page.editError('separator')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.separator" name="separator" :disabled="!canWrite('settings.numbering-rules.update') || page.formatFrozen.value" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="rule-edit-date-format">
              {{ t('settings.numbering_rules.fields.date_format') }}
              <select id="rule-edit-date-format" v-model="page.edit.dateFormat" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!canWrite('settings.numbering-rules.update') || page.formatFrozen.value">
                <option v-for="format in DATE_FORMATS" :key="format" :value="format">
                  {{ dateFormatLabel(format) }}
                </option>
              </select>
            </label>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="rule-edit-reset-cycle">
              {{ t('settings.numbering_rules.fields.reset_cycle') }}
              <select id="rule-edit-reset-cycle" v-model="page.edit.resetCycle" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!canWrite('settings.numbering-rules.update') || page.formatFrozen.value">
                <option v-for="cycle in RESET_CYCLES" :key="cycle" :value="cycle">
                  {{ cycleLabel(cycle) }}
                </option>
              </select>
            </label>
            <XhFieldRoot :label="t('settings.numbering_rules.fields.serial_length')" :error-text="page.editError('serialLength')" :invalid="!!page.editError('serialLength')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.edit.serialLength" name="serialLength" :step="1" :disabled="!canWrite('settings.numbering-rules.update') || page.formatFrozen.value" />
              </template>
            </XhFieldRoot>
            <label class="flex flex-col gap-1 text-caption text-fg-muted" for="rule-edit-time-zone">
              {{ t('settings.numbering_rules.fields.time_zone') }}
              <select id="rule-edit-time-zone" v-model="page.edit.timeZoneId" class="rounded-control border border-border bg-surface px-2 py-1 text-body text-fg" :disabled="!canWrite('settings.numbering-rules.update') || page.formatFrozen.value">
                <option v-if="!timeZones.some(zone => zone.id === page.edit.timeZoneId)" :value="page.edit.timeZoneId">
                  {{ page.edit.timeZoneId }}
                </option>
                <option v-for="zone in timeZones" :key="zone.id" :value="zone.id">
                  {{ zone.displayName }}
                </option>
              </select>
            </label>
            <XhFieldRoot class="md:col-span-2" :label="t('settings.numbering_rules.fields.description')" :error-text="page.editError('description')" :invalid="!!page.editError('description')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.edit.description" name="description" :disabled="!canWrite('settings.numbering-rules.update')" />
              </template>
            </XhFieldRoot>
          </div>
          <label v-if="page.selected.value.isPlatform" class="flex items-center gap-2 text-body">
            <input v-model="page.edit.allowTenantUse" type="checkbox" name="allowTenantUse" :disabled="!canWrite('settings.numbering-rules.update')">
            {{ t('settings.numbering_rules.fields.allow_tenant_use') }}
          </label>
          <div v-if="canWrite('settings.numbering-rules.update')">
            <XhButton type="submit" size="sm" variant="solid" :disabled="page.working.value" :loading="page.working.value">
              {{ t('settings.numbering_rules.actions.save') }}
            </XhButton>
          </div>
        </form>

        <!-- 预览：按上面的格式在当前时刻拼号，不发号 -->
        <form class="flex flex-col gap-2" data-testid="preview-numbering" novalidate @submit.prevent="page.runPreview()">
          <h3 class="text-label font-semibold">
            {{ t('settings.numbering_rules.sections.preview') }}
          </h3>
          <div class="flex flex-wrap items-end gap-3">
            <XhFieldRoot :label="t('settings.numbering_rules.fields.start_value')">
              <template #control>
                <XhTextFieldRoot v-model:value="page.preview.startValue" name="startValue" autocomplete="off" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.numbering_rules.fields.count')">
              <template #control>
                <XhNumberFieldRoot v-model:value="page.preview.count" name="count" :step="1" />
              </template>
            </XhFieldRoot>
            <XhButton type="submit" size="sm">
              {{ t('settings.numbering_rules.actions.preview') }}
            </XhButton>
          </div>
          <p v-if="page.previewError.value" class="text-caption text-fg-danger" role="alert">
            {{ page.previewError.value }}
          </p>
          <ul v-if="page.previewNumbers.value.length > 0" class="flex list-none flex-wrap gap-2 p-0 font-mono text-body" data-testid="preview-numbers">
            <li v-for="number in page.previewNumbers.value" :key="number" class="rounded-control border border-border px-2 py-1">
              {{ number }}
            </li>
          </ul>
        </form>

        <!-- 安全重置：下一个值要大于本周期已发出的最大值；平台规则回填编码确认 -->
        <form v-if="canWrite('settings.numbering-rules.reset')" class="flex flex-col gap-2" data-testid="reset-numbering-rule" novalidate @submit.prevent="page.runReset()">
          <h3 class="text-label font-semibold">
            {{ t('settings.numbering_rules.sections.reset') }}
          </h3>
          <div class="grid gap-3 md:grid-cols-3">
            <XhFieldRoot :label="t('settings.numbering_rules.fields.next_value')" :error-text="page.resetError('nextValue')" :invalid="!!page.resetError('nextValue')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.reset.nextValue" name="nextValue" autocomplete="off" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot :label="t('settings.numbering_rules.fields.reason')" :error-text="page.resetError('reason')" :invalid="!!page.resetError('reason')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.reset.reason" name="reason" />
              </template>
            </XhFieldRoot>
            <XhFieldRoot v-if="page.selected.value.isPlatform" :label="t('settings.numbering_rules.fields.confirm_code', { code: page.selected.value.code })" :error-text="page.resetError('confirmCode')" :invalid="!!page.resetError('confirmCode')" required>
              <template #control>
                <XhTextFieldRoot v-model:value="page.reset.confirmCode" name="confirmCode" autocomplete="off" />
              </template>
            </XhFieldRoot>
          </div>
          <div>
            <XhButton type="submit" size="sm" :disabled="page.working.value">
              {{ t('settings.numbering_rules.actions.reset') }}
            </XhButton>
          </div>
        </form>

        <!-- 发号记录：租户对平台规则只看到自己发起的 -->
        <h3 class="text-label font-semibold">
          {{ t('settings.numbering_rules.sections.allocations') }}
        </h3>
        <XhTableRoot
          :columns="allocationColumns.map(c => ({ ...c, label: t(c.label) }))"
          :rows="page.allocations.value.map(row => ({ id: row.id }))"
          :loading="page.allocationsLoading.value"
          size="sm"
          ruled
        >
          <XhTableHeader>
            <XhTableRow>
              <XhTableColumnHeader v-for="column in allocationColumns" :key="column.id" :value="column.id">
                <XhTableColumnLabel>{{ t(column.label) }}</XhTableColumnLabel>
              </XhTableColumnHeader>
            </XhTableRow>
          </XhTableHeader>
          <XhTableBody>
            <XhTableRow v-for="row in page.allocations.value" :key="row.id" :value="row.id">
              <XhTableCell value="generatedAt">
                {{ formatTime(row.generatedAt) }}
              </XhTableCell>
              <XhTableCell value="range">
                <span class="font-mono">{{ row.count > 1 ? `${row.firstNumber} … ${row.lastNumber}` : row.firstNumber }}</span>
              </XhTableCell>
              <XhTableCell value="requester">
                {{ row.requestTenantId }}
              </XhTableCell>
              <XhTableCell value="key">
                {{ row.idempotencyKey }}
              </XhTableCell>
              <XhTableCell value="business">
                {{ [row.businessType, row.businessId].filter(Boolean).join(' · ') }}
              </XhTableCell>
            </XhTableRow>
            <XhTableEmpty v-if="!page.allocationsLoading.value && page.allocations.value.length === 0">
              {{ t('settings.numbering_rules.no_allocations') }}
            </XhTableEmpty>
          </XhTableBody>
        </XhTableRoot>
        <div v-if="page.allocationTotal.value > 10" class="flex items-center gap-3 text-caption text-fg-muted">
          <XhButton size="sm" :disabled="page.allocationPage.value <= 1 || page.allocationsLoading.value" @click="page.loadAllocations(page.allocationPage.value - 1)">
            {{ t('common.actions.previous') }}
          </XhButton>
          <span>{{ page.allocationPage.value }} / {{ Math.ceil(page.allocationTotal.value / 10) }}</span>
          <XhButton size="sm" :disabled="page.allocationPage.value * 10 >= page.allocationTotal.value || page.allocationsLoading.value" @click="page.loadAllocations(page.allocationPage.value + 1)">
            {{ t('common.actions.next') }}
          </XhButton>
        </div>
      </section>
    </div>
  </section>
</template>
