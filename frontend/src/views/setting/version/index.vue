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
import { h, ref } from 'vue'
import { createPageRequest, versionApi } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { formatDate } from '~/utils'

defineOptions({ name: 'SettingVersionPage' })

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
const upgradingOptions = [
  { label: '升级中', value: 1 },
  { label: '正常', value: 0 },
]

// ── 字段单一事实源：列 + 常用搜索 ──────────────────────────────
const fields: ListFieldSchema[] = [
  // 仅搜索（不作为列）
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索应用/数据库/最小支持版本/升级节点', width: 240, order: 0 },
  // 列 + 搜索
  { key: 'appVersion', title: '应用版本', dataType: 'string', searchable: true, searchPlaceholder: '应用版本', minWidth: 130, order: 1 },
  { key: 'dbVersion', title: '数据库版本', dataType: 'string', searchable: true, searchPlaceholder: '数据库版本', minWidth: 130, order: 2 },
  { key: 'minSupportVersion', title: '最小支持版本', dataType: 'string', minWidth: 130, order: 3 },
  {
    key: 'isUpgrading',
    title: '升级状态',
    dataType: 'boolean',
    searchable: true,
    options: upgradingOptions,
    searchPlaceholder: '升级状态',
    width: 100,
    order: 4,
    render: (row) => {
      const upgrading = (row as unknown as VersionListItemDto).isUpgrading
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: upgrading ? 'warning' : 'success' },
        () => (upgrading ? '升级中' : '正常'),
      )
    },
  },
  { key: 'upgradeNode', title: '升级节点', dataType: 'string', minWidth: 140, order: 5 },
  { key: 'upgradeStartTime', title: '升级开始时间', dataType: 'datetime', minWidth: 170, order: 6 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', minWidth: 170, order: 7 },
]

const schema: PageSchema = {
  pageCode: 'setting.version',
  exportPermission: 'saas:version:export',
  pageName: '版本管理',
  batchRemovable: true,
  removePermission: 'saas:version:delete',
  rowKey: 'basicId',
  scrollX: 1200,
  fields,
  resource: {
    page: (params) => {
      const f = params.filters
      return versionApi.page({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        keyword: toStr(f.keyword),
        appVersion: toStr(f.appVersion),
        dbVersion: toStr(f.dbVersion),
        isUpgrading: toBool(f.isUpgrading),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => versionApi.delete(id),
  },
  actions: [
    { key: 'create', title: '新增版本', scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:version:create' },
    { key: 'view', title: '查看详情', scope: 'row', icon: 'lucide:eye' },
    { key: 'startUpgrade', title: '开始升级', scope: 'row', icon: 'lucide:rocket', permission: 'saas:version:upgrade', disabled: row => (row as unknown as VersionListItemDto).isUpgrading },
    { key: 'finishUpgrade', title: '完成升级', scope: 'row', icon: 'lucide:circle-check', permission: 'saas:version:upgrade', disabled: row => !(row as unknown as VersionListItemDto).isUpgrading },
    { key: 'delete', title: '删除', scope: 'row', type: 'error', icon: 'lucide:trash-2', permission: 'saas:version:delete', disabled: row => (row as unknown as VersionListItemDto).isUpgrading },
  ],
}

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
    message.error((e as Error).message || '加载版本详情失败')
  }
  finally {
    detailLoading.value = false
  }
  // 迁移历史与版本详情并行展示（Version 为迁移脚本时间戳串，与应用版本无外键，展示全量台账）
  migrationKeyword.value = ''
  void loadMigrationHistory(1)
}

// ── 迁移历史列表（详情抽屉内） ──────────────────────────────────
const migrationColumns: DataTableColumns<MigrationHistoryListItemDto> = [
  { key: 'version', title: '版本', width: 130, ellipsis: { tooltip: true } },
  { key: 'scriptName', title: '脚本名称', minWidth: 160, ellipsis: { tooltip: true } },
  {
    key: 'success',
    title: '结果',
    width: 80,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: row.success ? 'success' : 'error' },
      () => (row.success ? '成功' : '失败'),
    ),
  },
  { key: 'nodeName', title: '节点', width: 110, ellipsis: { tooltip: true }, render: row => row.nodeName || '-' },
  { key: 'executedTime', title: '执行时间', width: 160, render: row => formatDate(row.executedTime) },
]

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
    message.error((e as Error).message || '加载迁移历史失败')
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
    message.warning('请输入应用版本')
    return
  }
  if (!createForm.value.dbVersion.trim()) {
    message.warning('请输入数据库版本')
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
    message.success('版本已创建')
    createVisible.value = false
    reloadVersion()
  }
  catch (e) {
    message.error((e as Error).message || '创建版本失败')
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
    message.warning('该版本已处于升级中')
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
    message.success('已标记为升级中')
    startVisible.value = false
    reloadVersion()
  }
  catch (e) {
    message.error((e as Error).message || '开始升级失败')
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
    message.warning('该版本未处于升级中')
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
    message.success('升级已完成')
    finishVisible.value = false
    reloadVersion()
  }
  catch (e) {
    message.error((e as Error).message || '完成升级失败')
  }
  finally {
    finishLoading.value = false
  }
}

// ── 删除 ────────────────────────────────────────────────────────
function handleDelete(row: VersionListItemDto) {
  if (row.isUpgrading) {
    message.warning('升级中的版本不能删除')
    return
  }
  dialog.warning({
    title: '删除版本',
    content: `确定删除版本「${row.appVersion}」吗？删除后不可恢复。`,
    positiveText: '删除',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await versionApi.delete(row.basicId)
        message.success('版本已删除')
        reloadVersion()
      }
      catch (e) {
        message.error((e as Error).message || '删除版本失败')
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
      <NDrawerContent closable title="版本详情">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !detailData" class="xh-detail-empty" description="暂无版本详情">
            <template #icon>
              <NIcon><Icon icon="lucide:inbox" /></NIcon>
            </template>
          </NEmpty>
          <NScrollbar v-else-if="detailData" style="max-height: calc(100vh - 120px)">
            <NDescriptions :column="2" bordered label-placement="left" size="small">
              <NDescriptionsItem label="应用版本">
                {{ detailData.appVersion }}
              </NDescriptionsItem>
              <NDescriptionsItem label="数据库版本">
                {{ detailData.dbVersion }}
              </NDescriptionsItem>
              <NDescriptionsItem label="最小支持版本">
                {{ detailData.minSupportVersion || '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem label="升级状态">
                <NTag :type="detailData.isUpgrading ? 'warning' : 'success'" round size="small">
                  {{ detailData.isUpgrading ? '升级中' : '正常' }}
                </NTag>
              </NDescriptionsItem>
              <NDescriptionsItem label="升级节点">
                {{ detailData.upgradeNode || '-' }}
              </NDescriptionsItem>
              <NDescriptionsItem label="升级开始时间">
                {{ formatNullableDate(detailData.upgradeStartTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="创建时间">
                {{ formatNullableDate(detailData.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem label="创建者">
                {{ detailData.createdBy || '-' }}
              </NDescriptionsItem>
            </NDescriptions>

            <div class="xh-migration-header">
              <span class="xh-migration-title">迁移历史</span>
              <NInput
                v-model:value="migrationKeyword"
                clearable
                placeholder="搜索版本/脚本/节点"
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
      title="新增版本"
    >
      <NForm :model="createForm" label-placement="top">
        <NFormItem label="应用版本" path="appVersion">
          <NInput v-model:value="createForm.appVersion" clearable placeholder="如: 1.4.0" />
        </NFormItem>
        <NFormItem label="数据库版本" path="dbVersion">
          <NInput v-model:value="createForm.dbVersion" clearable placeholder="如: 1.4.0" />
        </NFormItem>
        <NFormItem label="最小支持版本" path="minSupportVersion">
          <NInput v-model:value="createForm.minSupportVersion" clearable placeholder="低于该版本的客户端将被要求升级（可空）" />
        </NFormItem>
      </NForm>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="createVisible = false">
            取消
          </NButton>
          <NButton :loading="createLoading" type="primary" @click="handleCreateSubmit">
            保存
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
      :title="`开始升级 ${startTarget?.appVersion ?? ''}`"
    >
      <NForm label-placement="top">
        <NFormItem label="升级节点">
          <NInput v-model:value="startUpgradeNode" clearable placeholder="执行升级的节点标识（可空）" />
        </NFormItem>
      </NForm>
      <span class="xh-modal-tip">确认后该版本将标记为「升级中」，升级开始时间取服务器当前时间。</span>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="startVisible = false">
            取消
          </NButton>
          <NButton :loading="startLoading" type="warning" @click="handleStartSubmit">
            开始升级
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
      :title="`完成升级 ${finishTarget?.appVersion ?? ''}`"
    >
      <NForm :model="finishForm" label-placement="top">
        <NFormItem label="应用版本">
          <NInput v-model:value="finishForm.appVersion" clearable placeholder="留空沿用原值" />
        </NFormItem>
        <NFormItem label="数据库版本">
          <NInput v-model:value="finishForm.dbVersion" clearable placeholder="留空沿用原值" />
        </NFormItem>
        <NFormItem label="最小支持版本">
          <NInput v-model:value="finishForm.minSupportVersion" clearable placeholder="留空沿用原值" />
        </NFormItem>
      </NForm>
      <span class="xh-modal-tip">确认后该版本将取消「升级中」标记，并按上方非空字段更新版本号。</span>
      <template #footer>
        <NSpace justify="end">
          <NButton @click="finishVisible = false">
            取消
          </NButton>
          <NButton :loading="finishLoading" type="primary" @click="handleFinishSubmit">
            完成升级
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
