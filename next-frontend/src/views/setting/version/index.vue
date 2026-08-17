<script setup lang="ts">
import type { PageResult, VersionDetailDto, VersionListItemDto } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhBadge, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateRoot, XhSpinner } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, querySortsFromSchema, versionApi } from '@/api'
import { Icon, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { formatDate } from '~/utils'

defineOptions({ name: 'SettingVersionPage' })

const { t } = useI18n()

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
      return h(XhBadge, { variant: 'subtle', size: 'sm', tone: upgrading ? 'warning' : 'success' }, () => (upgrading ? t('setting.version.upgrading') : t('setting.version.normal')))
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
    toast.error((e as Error).message || t('setting.version.load_detail_failed'))
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
        <Icon width="14" height="14" icon="lucide:info" />
        {{ t('setting.version.engine_managed_hint') }}
      </span>
    </template>

    <!-- 详情抽屉：版本信息 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 720px">
        <XhDrawerTitle>{{ t('setting.version.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger>✕</XhDrawerCloseTrigger>
        <div class="xh-loading-stage">
          <div v-if="detailLoading" class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!detailLoading && !detailData" class="xh-detail-empty">
            <XhEmptyStateDescription>{{ t('setting.version.detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="detailData" class="xh-scroll-area" style="max-height: calc(100vh - 120px)">
            <XhDescriptionsRoot :columns="2" bordered>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.version.app_version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.appVersion }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.version.db_version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.dbVersion }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.version.min_support_version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.minSupportVersion || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.version.upgrade_status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhBadge variant="subtle" :tone="detailData.isUpgrading ? 'warning' : 'success'" size="sm">
                    {{ detailData.isUpgrading ? t('setting.version.upgrading') : t('setting.version.normal') }}
                  </XhBadge>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.version.upgrade_node') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.upgradeNode || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.version.upgrade_start_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detailData.upgradeStartTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.version.created_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatNullableDate(detailData.createdTime) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('setting.version.created_by') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.createdBy || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </div>
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>
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
