<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  MigrationHistoryListItemDto,
  PageResult,
  VersionCreateDto,
  VersionDetailDto,
  VersionListItemDto,
  VersionUpgradeFinishDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NModal,
  NScrollbar,
  NSpace,
  NSpin,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, querySortsFromSchema, versionApi } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { formatDate } from '~/utils'

defineOptions({ name: 'SettingVersionPage' })

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadVersion() {
  void schemaPageRef.value?.reload()
}

// ── 过滤值清洗 ──────────────────────────────────────────────────
function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}
function toBool(v: unknown): boolean | undefined {
  return v == null || v === '' ? undefined : Boolean(Number(v))
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

// boolean 选项以 1/0 表达（SchemaSelectOption.value 仅 string|number），查询时 toBool 还原
const upgradingOptions = computed(() => [
  { label: t('setting.version.upgrading'), value: 1 },
  { label: t('setting.version.normal'), value: 0 },
])

// ── 字段单一事实源：列 + 常用搜索 ──────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('setting.version.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.version.keyword_placeholder'), width: 240, order: 0 },
  // 列 + 搜索
  { key: 'appVersion', title: t('setting.version.app_version'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('setting.version.app_version_placeholder'), minWidth: 130, order: 1 },
  { key: 'dbVersion', title: t('setting.version.db_version'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('setting.version.db_version_placeholder'), minWidth: 130, order: 2 },
  { key: 'minSupportVersion', title: t('setting.version.min_support_version'), dataType: 'string', sortable: true, minWidth: 130, order: 3 },
  {
    key: 'isUpgrading',
    title: t('setting.version.upgrade_status'),
    dataType: 'boolean',
    searchable: true,
    sortable: true,
    options: upgradingOptions.value,
    searchPlaceholder: t('setting.version.upgrade_status_placeholder'),
    width: 100,
    order: 4,
    render: (row) => {
      const upgrading = (row as unknown as VersionListItemDto).isUpgrading
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: upgrading ? 'warning' : 'success' },
        () => (upgrading ? t('setting.version.upgrading') : t('setting.version.normal')),
      )
    },
  },
  { key: 'upgradeNode', title: t('setting.version.upgrade_node'), dataType: 'string', sortable: true, minWidth: 140, order: 5 },
  { key: 'upgradeStartTime', title: t('setting.version.upgrade_start_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 6 },
  { key: 'createdTime', title: t('setting.version.created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 7 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.version',
  exportPermission: 'saas:version:export',
  pageName: t('setting.version.page_name'),
  batchRemovable: true,
  removePermission: 'saas:version:delete',
  rowKey: 'basicId',
  scrollX: 1200,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return versionApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts) },
        }),
        keyword: toStr(f.keyword),
        appVersion: toStr(f.appVersion),
        dbVersion: toStr(f.dbVersion),
        isUpgrading: toBool(f.isUpgrading),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => versionApi.delete(id),
  },
  actions: [
    { key: 'create', title: t('setting.version.add'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:version:create' },
    { key: 'view', title: t('setting.version.view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'startUpgrade', title: t('setting.version.start_upgrade'), scope: 'row', icon: 'lucide:rocket', permission: 'saas:version:upgrade', disabled: row => (row as unknown as VersionListItemDto).isUpgrading },
    { key: 'finishUpgrade', title: t('setting.version.finish_upgrade'), scope: 'row', icon: 'lucide:circle-check', permission: 'saas:version:upgrade', disabled: row => !(row as unknown as VersionListItemDto).isUpgrading },
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', permission: 'saas:version:delete', disabled: row => (row as unknown as VersionListItemDto).isUpgrading },
  ],
}))

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as VersionListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row) {
        void handleDetail(row)
      }
      break
    case 'startUpgrade':
      if (row) {
        handleStartUpgrade(row)
      }
      break
    case 'finishUpgrade':
      if (row) {
        handleFinishUpgrade(row)
      }
      break
    case 'delete':
      if (row) {
        handleDelete(row)
      }
      break
  }
}

// ── 详情抽屉（版本信息 + 迁移历史） ─────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<VersionDetailDto | null>(null)

// ── 迁移历史列表（详情抽屉内）：先于 handleDetail 声明，handleDetail 内会重置 keyword 并加载 ──
const migrationLoading = ref(false)
const migrationItems = ref<MigrationHistoryListItemDto[]>([])
const migrationKeyword = ref('')
const migrationPagination = ref({ itemCount: 0, page: 1, pageSize: 10 })

async function handleDetail(row: VersionListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await versionApi.detail(row.basicId) ?? null
  }
  catch (e) {
    message.error((e as Error).message || t('setting.version.load_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
  // 迁移历史与版本详情并行展示（Version 为迁移脚本时间戳串，与应用版本无外键，展示全量台账）
  migrationKeyword.value = ''
  void loadMigrationHistory(1)
}

// ── 迁移历史列表（详情抽屉内） ──────────────────────────────────
const migrationColumns = computed<DataTableColumns<MigrationHistoryListItemDto>>(() => [
  { key: 'version', title: t('setting.version.migration_version'), width: 130, ellipsis: { tooltip: true } },
  { key: 'scriptName', title: t('setting.version.migration_script'), minWidth: 160, ellipsis: { tooltip: true } },
  {
    key: 'success',
    title: t('setting.version.migration_result'),
    width: 80,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: row.success ? 'success' : 'error' },
      () => (row.success ? t('setting.version.result_success') : t('setting.version.result_failed')),
    ),
  },
  { key: 'nodeName', title: t('setting.version.migration_node'), width: 110, ellipsis: { tooltip: true }, render: row => row.nodeName || '-' },
  { key: 'executedTime', title: t('setting.version.migration_executed_time'), width: 160, render: row => formatDate(row.executedTime) },
])

async function loadMigrationHistory(page?: number) {
  if (page) {
    migrationPagination.value.page = page
  }
  migrationLoading.value = true
  try {
    const result = await versionApi.migrationHistoryPage({
      ...createPageRequest({
        page: {
          pageIndex: migrationPagination.value.page,
          pageSize: migrationPagination.value.pageSize,
        },
      }),
      keyword: migrationKeyword.value.trim() || undefined,
    })
    migrationItems.value = result.items
    migrationPagination.value.itemCount = result.page.totalCount
  }
  catch (e) {
    message.error((e as Error).message || t('setting.version.load_migration_failed'))
  }
  finally {
    migrationLoading.value = false
  }
}

// ── 新增版本 ────────────────────────────────────────────────────
interface VersionFormModel {
  appVersion: string
  dbVersion: string
  minSupportVersion: string
}

const createVisible = ref(false)
const createLoading = ref(false)
const createForm = ref<VersionFormModel>(createDefaultVersionForm())

function createDefaultVersionForm(): VersionFormModel {
  return {
    appVersion: '',
    dbVersion: '0.0.0',
    minSupportVersion: '',
  }
}

function handleAdd() {
  createForm.value = createDefaultVersionForm()
  createVisible.value = true
}

async function handleCreateSubmit() {
  if (!createForm.value.appVersion.trim()) {
    message.warning(t('setting.version.validate_app_version'))
    return
  }
  if (!createForm.value.dbVersion.trim()) {
    message.warning(t('setting.version.validate_db_version'))
    return
  }
  createLoading.value = true
  try {
    const input: VersionCreateDto = {
      appVersion: createForm.value.appVersion.trim(),
      dbVersion: createForm.value.dbVersion.trim(),
      isUpgrading: false,
      minSupportVersion: createForm.value.minSupportVersion.trim() || null,
    }
    await versionApi.create(input)
    message.success(t('setting.version.version_created'))
    createVisible.value = false
    reloadVersion()
  }
  catch (e) {
    message.error((e as Error).message || t('setting.version.create_failed'))
  }
  finally {
    createLoading.value = false
  }
}

// ── 开始升级 ────────────────────────────────────────────────────
const startVisible = ref(false)
const startLoading = ref(false)
const startTarget = ref<VersionListItemDto | null>(null)
const startUpgradeNode = ref('')

function handleStartUpgrade(row: VersionListItemDto) {
  if (row.isUpgrading) {
    message.warning(t('setting.version.already_upgrading'))
    return
  }
  startTarget.value = row
  startUpgradeNode.value = row.upgradeNode ?? ''
  startVisible.value = true
}

async function handleStartSubmit() {
  if (!startTarget.value) {
    return
  }
  startLoading.value = true
  try {
    // 升级开始时间留空由后端取当前时间
    await versionApi.startUpgrade({
      basicId: startTarget.value.basicId,
      upgradeNode: startUpgradeNode.value.trim() || null,
    })
    message.success(t('setting.version.marked_upgrading'))
    startVisible.value = false
    reloadVersion()
  }
  catch (e) {
    message.error((e as Error).message || t('setting.version.start_failed'))
  }
  finally {
    startLoading.value = false
  }
}

// ── 完成升级 ────────────────────────────────────────────────────
const finishVisible = ref(false)
const finishLoading = ref(false)
const finishTarget = ref<VersionListItemDto | null>(null)
const finishForm = ref<VersionFormModel>(createDefaultVersionForm())

function handleFinishUpgrade(row: VersionListItemDto) {
  if (!row.isUpgrading) {
    message.warning(t('setting.version.not_upgrading'))
    return
  }
  finishTarget.value = row
  finishForm.value = {
    appVersion: row.appVersion,
    dbVersion: row.dbVersion,
    minSupportVersion: row.minSupportVersion ?? '',
  }
  finishVisible.value = true
}

async function handleFinishSubmit() {
  if (!finishTarget.value) {
    return
  }
  finishLoading.value = true
  try {
    const input: VersionUpgradeFinishDto = {
      basicId: finishTarget.value.basicId,
      // 留空表示沿用原值（后端仅在非空时覆盖）
      appVersion: finishForm.value.appVersion.trim() || null,
      dbVersion: finishForm.value.dbVersion.trim() || null,
      minSupportVersion: finishForm.value.minSupportVersion.trim() || null,
    }
    await versionApi.finishUpgrade(input)
    message.success(t('setting.version.upgrade_finished'))
    finishVisible.value = false
    reloadVersion()
  }
  catch (e) {
    message.error((e as Error).message || t('setting.version.finish_failed'))
  }
  finally {
    finishLoading.value = false
  }
}

// ── 删除 ────────────────────────────────────────────────────────
function handleDelete(row: VersionListItemDto) {
  if (row.isUpgrading) {
    message.warning(t('setting.version.upgrading_cannot_delete'))
    return
  }
  dialog.warning({
    title: t('setting.version.delete_title'),
    content: t('setting.version.delete_content', { version: row.appVersion }),
    positiveText: t('common.actions.delete'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await versionApi.delete(row.basicId)
        message.success(t('setting.version.version_deleted'))
        reloadVersion()
      }
      catch (e) {
        message.error((e as Error).message || t('setting.version.delete_failed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <!-- 详情抽屉：版本信息 + 迁移历史台账 -->
    <NDrawer v-model:show="detailVisible" :width="720">
      <NDrawerContent closable :title="t('setting.version.detail_title')">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !detailData" class="xh-detail-empty" :description="t('setting.version.detail_empty')">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="detailData" style="max-height: calc(100vh - 120px)">
            <NDescriptions :column="2" bordered label-placement="left" size="small">
              <NDescriptionsItem :label="t('setting.version.app_version')">
                {{ detailData.appVersion }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.version.db_version')">
                {{ detailData.dbVersion }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.version.min_support_version')">
                {{ detailData.minSupportVersion || '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.version.upgrade_status')">
                <NTag :type="detailData.isUpgrading ? 'warning' : 'success'" round size="small">
                  {{ detailData.isUpgrading ? t('setting.version.upgrading') : t('setting.version.normal') }}
                </NTag>
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.version.upgrade_node')">
                {{ detailData.upgradeNode || '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.version.upgrade_start_time')">
                {{ formatNullableDate(detailData.upgradeStartTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.version.created_time')">
                {{ formatNullableDate(detailData.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('setting.version.created_by')">
                {{ detailData.createdBy || '-' }}
              </NDescriptionsItem>
            </NDescriptions>

            <div class="xh-migration-header">
              <span class="xh-migration-title">{{ t('setting.version.migration_history') }}</span>
              <NInput
                v-model:value="migrationKeyword"
                clearable
                :placeholder="t('setting.version.migration_search_placeholder')"
                size="small"
                style="width: 220px"
                @clear="loadMigrationHistory(1)"
                @keyup.enter="loadMigrationHistory(1)"
              >
                <template #prefix>
                  <NIcon><Icon icon="lucide:search" /></NIcon>
                </template>
              </NInput>
            </div>
            <NDataTable
              :columns="migrationColumns"
              :data="migrationItems"
              :loading="migrationLoading"
              :pagination="{
                page: migrationPagination.page,
                pageSize: migrationPagination.pageSize,
                itemCount: migrationPagination.itemCount,
                onUpdatePage: (p: number) => loadMigrationHistory(p),
              }"
              :row-key="(row: MigrationHistoryListItemDto) => row.basicId"
              remote
              size="small"
            />
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <!-- 新增版本 -->
    <NModal
      v-model:show="createVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 520px; max-width: 92vw"
      :title="t('setting.version.add_title')"
    >
      <NForm :model="createForm" label-placement="top">
        <NFormItem :label="t('setting.version.app_version')" path="appVersion">
          <NInput v-model:value="createForm.appVersion" clearable :placeholder="t('setting.version.app_version_input_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.version.db_version')" path="dbVersion">
          <NInput v-model:value="createForm.dbVersion" clearable :placeholder="t('setting.version.db_version_input_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.version.min_support_version')" path="minSupportVersion">
          <NInput v-model:value="createForm.minSupportVersion" clearable :placeholder="t('setting.version.min_support_input_placeholder')" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="createVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="createLoading" type="primary" @click="handleCreateSubmit">
            {{ t('common.actions.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- 开始升级 -->
    <NModal
      v-model:show="startVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 480px; max-width: 92vw"
      :title="t('setting.version.start_title', { version: startTarget?.appVersion ?? '' })"
    >
      <NForm label-placement="top">
        <NFormItem :label="t('setting.version.upgrade_node')">
          <NInput v-model:value="startUpgradeNode" clearable :placeholder="t('setting.version.upgrade_node_placeholder')" />
        </NFormItem>
      </NForm>
      <span class="xh-modal-tip">{{ t('setting.version.start_tip') }}</span>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="startVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="startLoading" type="warning" @click="handleStartSubmit">
            {{ t('setting.version.start_upgrade') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>

    <!-- 完成升级 -->
    <NModal
      v-model:show="finishVisible"
      :auto-focus="false"
      :bordered="false"
      preset="card"
      style="width: 520px; max-width: 92vw"
      :title="t('setting.version.finish_title', { version: finishTarget?.appVersion ?? '' })"
    >
      <NForm :model="finishForm" label-placement="top">
        <NFormItem :label="t('setting.version.app_version')">
          <NInput v-model:value="finishForm.appVersion" clearable :placeholder="t('setting.version.keep_original_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.version.db_version')">
          <NInput v-model:value="finishForm.dbVersion" clearable :placeholder="t('setting.version.keep_original_placeholder')" />
        </NFormItem>
        <NFormItem :label="t('setting.version.min_support_version')">
          <NInput v-model:value="finishForm.minSupportVersion" clearable :placeholder="t('setting.version.keep_original_placeholder')" />
        </NFormItem>
      </NForm>
      <span class="xh-modal-tip">{{ t('setting.version.finish_tip') }}</span>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="finishVisible = false">
            {{ t('common.actions.cancel') }}
          </NButton>
          <NButton :loading="finishLoading" type="primary" @click="handleFinishSubmit">
            {{ t('setting.version.finish_upgrade') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.xh-migration-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 20px 0 10px;
}

.xh-migration-title {
  font-size: 14px;
  font-weight: 600;
}

.xh-modal-tip {
  display: block;
  margin-top: 4px;
  font-size: 12px;
  opacity: 0.65;
}
</style>
