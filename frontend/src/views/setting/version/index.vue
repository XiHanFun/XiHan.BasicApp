<script setup lang="ts">
import type { PageResult, VersionDetailDto, VersionListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NIcon,
  NScrollbar,
  NSpin,
  NTag,
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

// 只读页：版本行由升级引擎写入并把 IsUpgrading 用作分布式锁，页面不再提供任何写操作
const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.version',
  exportPermission: 'saas:version:export',
  pageName: t('setting.version.page_name'),
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
  },
  actions: [
    { key: 'view', title: t('setting.version.view'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as VersionListItemDto | undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

// ── 详情抽屉（版本信息） ─────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<VersionDetailDto | null>(null)

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
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <!-- 页面无写操作，工具栏位置改为说明数据来源 -->
    <template #toolbar>
      <span class="xh-version-hint">
        <NIcon size="14"><Icon icon="lucide:info" /></NIcon>
        {{ t('setting.version.engine_managed_hint') }}
      </span>
    </template>

    <!-- 详情抽屉：版本信息 -->
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
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>
  </SchemaPage>
</template>

<style scoped>
.xh-detail-empty {
  padding: 48px 0;
}

.xh-version-hint {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  opacity: 0.65;
}
</style>
