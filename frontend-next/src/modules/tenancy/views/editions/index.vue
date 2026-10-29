<script lang="ts" setup>
import type { EditionRow } from './use-editions'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerFooter, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, XEditModal, XInput, XNumberInput, XPermissionTransfer } from '~/components'
import { toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { formatPrice } from '../subscription/subscription'
import { editionsResource, setDefaultEdition, toWhitelistItems, useEditionEditor, useEditionPermissions } from './use-editions'

defineOptions({ name: 'TenancyEditionsView' })

const { t } = useI18n()
const auth = useAuthStore()
const resource = editionsResource()
const editor = useEditionEditor(t)
const whitelist = useEditionPermissions()

/** 白名单只读还是可改：改要 tenancy.editions.update，看只要能进本页 */
const canEditWhitelist = computed(() => auth.can('tenancy.editions.update'))

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// ── 字段单一事实源（列；editions.list 不收筛选与排序，故不登记搜索与可排序字段） ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as EditionRow

/** 名称下带一行说明（与 4.x 相同）；渲染函数节点不带页面 scope，用全局工具类 */
function renderName(row: EditionRow) {
  return h('div', { class: 'flex min-w-0 flex-col' }, [
    h('span', { class: 'font-medium' }, row.name),
    row.description ? h('span', { class: 'truncate text-caption text-fg-muted' }, row.description) : null,
  ])
}

/** 价格（4.x 同）：免费出标签，没标价为 -，其余带人民币符号与两位小数 */
function renderPrice(row: EditionRow) {
  if (row.isFree)
    return renderTag(t('tenancy.editions.free'), 'success')
  return row.price === null || row.price === undefined ? '-' : formatPrice(row.price)
}

function billingPeriodText(row: EditionRow): string {
  return row.billingPeriodMonths === null || row.billingPeriodMonths === undefined ? '-' : t('tenancy.editions.billing_period_months', { count: row.billingPeriodMonths })
}

/** 上限为空即不限 */
function userLimitText(row: EditionRow): string {
  return row.userLimit === null || row.userLimit === undefined ? t('tenancy.editions.unlimited') : String(row.userLimit)
}

function storageLimitText(row: EditionRow): string {
  return row.storageLimitMb === null || row.storageLimitMb === undefined ? t('tenancy.editions.unlimited') : t('tenancy.editions.storage_mb', { size: row.storageLimitMb })
}

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'code', title: t('tenancy.editions.fields.code'), dataType: 'string', exportable: true, minWidth: 140, order: 1 },
  { key: 'name', title: t('tenancy.editions.fields.name'), dataType: 'string', exportable: true, render: row => renderName(asRow(row)), minWidth: 180, order: 2 },
  { key: 'price', title: t('tenancy.editions.fields.price'), dataType: 'money', exportable: true, render: row => renderPrice(asRow(row)), width: 110, order: 3 },
  { key: 'billingPeriodMonths', title: t('tenancy.editions.fields.billing_period'), dataType: 'number', exportable: true, render: row => billingPeriodText(asRow(row)), width: 100, order: 4 },
  { key: 'userLimit', title: t('tenancy.editions.fields.user_limit'), dataType: 'number', exportable: true, render: row => userLimitText(asRow(row)), width: 100, order: 5 },
  { key: 'storageLimitMb', title: t('tenancy.editions.fields.storage_limit'), dataType: 'number', exportable: true, render: row => storageLimitText(asRow(row)), width: 120, order: 6 },
  { key: 'isFree', title: t('tenancy.editions.fields.is_free'), dataType: 'boolean', exportable: true, render: row => renderTag(asRow(row).isFree ? t('common.statuses.yes') : t('common.statuses.no'), asRow(row).isFree ? 'success' : 'neutral'), width: 86, order: 7 },
  { key: 'isDefault', title: t('tenancy.editions.fields.is_default'), dataType: 'boolean', exportable: true, render: row => (asRow(row).isDefault ? renderTag(t('tenancy.editions.default_badge'), 'warning') : '-'), width: 100, order: 8 },
  { key: 'permissionCount', title: t('tenancy.editions.fields.permissions'), dataType: 'number', exportable: true, width: 100, order: 9 },
  { key: 'tenantCount', title: t('tenancy.editions.fields.tenants'), dataType: 'number', exportable: true, width: 100, order: 10 },
  { key: 'order', title: t('tenancy.editions.fields.order'), dataType: 'number', exportable: true, width: 80, order: 11 },
  { key: 'description', title: t('tenancy.editions.fields.description'), dataType: 'text', visible: false, exportable: true, order: 11.5 },
  { key: 'createdAt', title: t('tenancy.editions.fields.created_at'), dataType: 'datetime', exportable: true, minWidth: 170, order: 12 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'tenancy.editions',
  pageName: t('tenancy.editions.title'),
  exportPermission: 'tenancy.editions.read',
  batchRemovable: true,
  removePermission: 'tenancy.editions.delete',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('tenancy.editions.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'tenancy.editions.create' },
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', permission: 'tenancy.editions.update' },
    { key: 'set-default', title: t('tenancy.editions.actions.set_default'), scope: 'row', icon: 'lucide:star', confirm: true, confirmText: row => t('tenancy.editions.set_default_confirm', { name: asRow(row).name }), visible: row => !asRow(row).isDefault, permission: 'tenancy.editions.update' },
    { key: 'permissions', title: t('tenancy.editions.actions.permissions'), scope: 'row', icon: 'lucide:shield-check', permission: 'tenancy.editions.read' },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', icon: 'lucide:trash-2', tone: 'danger', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), permission: 'tenancy.editions.delete' },
  ],
}))

// ── 行 / 页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
      break
    case 'set-default':
      if (row)
        void setDefault(row)
      break
    case 'permissions':
      if (row)
        void whitelist.start(row)
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

async function onSubmit() {
  const creating = !editor.isEdit.value
  const saved = await editor.submit()
  if (!saved)
    return
  toast.success(t('common.messages.save_success'))
  reload()
  // 新套餐的白名单是空的（什么都不放行）：建好接着勾选
  if (creating && canEditWhitelist.value)
    void whitelist.start(saved)
}

async function setDefault(row: EditionRow) {
  try {
    await setDefaultEdition(row.id)
    toast.success(t('tenancy.editions.set_default_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.operation_failed'))
  }
}

async function removeRow(row: EditionRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

// ── 白名单抽屉 ──
const whitelistItems = computed(() => toWhitelistItems(whitelist.catalog.value, whitelist.whitelist.value, t('tenancy.editions.permissions_outside')))
const outsideCodes = computed(() => new Set(whitelistItems.value.filter(item => item.outside).map(item => item.basicId)))

async function saveWhitelist() {
  if (await whitelist.save()) {
    toast.success(t('tenancy.editions.permissions_saved'))
    reload()
  }
  else if (whitelist.saveError.value) {
    toast.danger(whitelist.saveError.value)
  }
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <XEditModal
      v-model:show="editor.open.value"
      :title="editor.isEdit.value ? t('tenancy.editions.edit_title') : t('tenancy.editions.create_title')"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="edition-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('code')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('tenancy.editions.fields.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable autocomplete="off" :disabled="editor.isEdit.value" :placeholder="t('tenancy.editions.code_placeholder')">
              <!-- 随机生成只在新建时出现；type="button" 防止把整表提交掉 -->
              <template v-if="!editor.isEdit.value" #suffix>
                <button
                  type="button"
                  class="edition-code-random"
                  :aria-label="t('tenancy.editions.code_random')"
                  :title="t('tenancy.editions.code_random')"
                  @click="editor.generateCode()"
                >
                  <Icon width="14" height="14" icon="lucide:dices" />
                </button>
              </template>
            </XInput>
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('tenancy.editions.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <!-- 免费套餐不能标价（4.x 同）：打开免费即清空价格、价格框禁用 -->
        <XhFieldRoot :invalid="!!editor.fieldError('price')">
          <XhFieldLabel>{{ t('tenancy.editions.fields.price') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.price" name="price" :min="0" :max="99999999" :precision="2" :disabled="editor.form.isFree" :placeholder="t('tenancy.editions.price_placeholder')" />
          </XhFieldControl>
          <XhFieldDescription v-if="editor.form.isFree">
            {{ t('tenancy.editions.price_free_hint') }}
          </XhFieldDescription>
          <XhFieldErrorText v-if="editor.fieldError('price')">
            {{ editor.fieldError('price') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('billingPeriodMonths')">
          <XhFieldLabel>{{ t('tenancy.editions.billing_period_form') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.billingPeriodMonths" name="billingPeriodMonths" :min="1" :max="120" :precision="0" :placeholder="t('tenancy.editions.billing_period_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('billingPeriodMonths')">
            {{ editor.fieldError('billingPeriodMonths') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('userLimit')">
          <XhFieldLabel>{{ t('tenancy.editions.fields.user_limit') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.userLimit" name="userLimit" :min="0" :max="10000000" :precision="0" :placeholder="t('tenancy.editions.limit_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('userLimit')">
            {{ editor.fieldError('userLimit') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('storageLimitMb')">
          <XhFieldLabel>{{ t('tenancy.editions.storage_limit_form') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.storageLimitMb" name="storageLimitMb" :min="0" :max="1000000000" :precision="0" :placeholder="t('tenancy.editions.limit_placeholder')" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('storageLimitMb')">
            {{ editor.fieldError('storageLimitMb') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('tenancy.editions.fields.is_free') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch :checked="editor.form.isFree" name="isFree" @update:checked="editor.setFree" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('tenancy.editions.fields.is_default') }}</XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.isDefault" name="isDefault" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('tenancy.editions.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('description')">
          <XhFieldLabel>{{ t('tenancy.editions.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" type="textarea" :rows="2" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('description')">
            {{ editor.fieldError('description') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="!editor.isEdit.value" class="xh-span-2 text-caption text-fg-muted">
          {{ t('tenancy.editions.create_hint') }}
        </p>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <XhDrawerRoot v-model:open="whitelist.open.value" side="right">
      <XhDrawerContent style="--xh-drawer-size: 980px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('tenancy.editions.permissions_title', { name: whitelist.edition.value?.name ?? '' }) }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body" data-testid="edition-permissions">
          <p class="drawer-tip">
            {{ t('tenancy.editions.permissions_hint') }}
          </p>
          <p v-if="whitelist.loadError.value" class="drawer-tip text-fg-danger" role="alert">
            {{ t('tenancy.editions.permissions_load_failed', { reason: whitelist.loadError.value }) }}
          </p>
          <p v-if="outsideCodes.size > 0" class="drawer-tip">
            {{ t('tenancy.editions.permissions_outside_tip') }}
          </p>
          <XPermissionTransfer
            :items="whitelistItems"
            :value="whitelist.checked.value"
            :loading="whitelist.loading.value"
            :disabled="whitelist.loading.value || whitelist.saving.value || !!whitelist.loadError.value || !canEditWhitelist"
            :source-title="t('tenancy.editions.permissions_available')"
            :target-title="t('tenancy.editions.permissions_granted')"
            :search-placeholder="t('tenancy.editions.permissions_search')"
            :other-group-label="t('tenancy.editions.permissions_outside')"
            @update:value="whitelist.setChecked"
          >
            <template #suffix="{ item }">
              <!-- 目录外的码（权限已下线或改了作用侧）：后端不再接受，移到左栏保存即去掉 -->
              <XhTagRoot v-if="outsideCodes.has(item.basicId)" variant="subtle" size="sm" tone="warning">
                <XhTagLabel>{{ t('tenancy.editions.permissions_outside') }}</XhTagLabel>
              </XhTagRoot>
            </template>
          </XPermissionTransfer>
        </XhDrawerBody>
        <XhDrawerFooter>
          <XhButton variant="subtle" @click="whitelist.open.value = false">
            {{ t('common.actions.close') }}
          </XhButton>
          <XhButton
            v-if="canEditWhitelist"
            variant="subtle"
            tone="brand"
            :loading="whitelist.saving.value"
            :disabled="!whitelist.dirty.value || whitelist.loading.value || !!whitelist.loadError.value"
            @click="saveWhitelist"
          >
            <XhButtonIndicator />
            <XhButtonLabel>{{ t('common.actions.save') }}</XhButtonLabel>
          </XhButton>
        </XhDrawerFooter>
      </XhDrawerContent>
    </XhDrawerRoot>
  </SchemaPage>
</template>

<style scoped>
.drawer-tip {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.drawer-tip.text-fg-danger {
  color: var(--xh-fg-danger);
}

/* 编码随机生成触发器：排在输入框盒内，与清除钮同段；焦点环交给浏览器默认样式 */
.edition-code-random {
  display: inline-flex;
  flex: none;
  align-items: center;
  justify-content: center;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--xh-fg-muted);
  cursor: pointer;
}

.edition-code-random:hover {
  color: var(--xh-fg-default);
}
</style>
