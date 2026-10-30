<script setup lang="ts">
import type { VersionRow } from './version'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { formatDate } from '~/utils'
import { errorText } from '../jobs/job'
import { databaseLabel, getVersion, upgradingLabel, upgradingOptions, upgradingTone, versionResource } from './version'

defineOptions({ name: 'MaintenanceVersionsView' })

const { t } = useI18n()
const resource = versionResource()

/** 行对象在 SchemaPage 边界为宽松类型，这里收敛回台账行 */
const asRow = (row: unknown) => row as VersionRow

// ── 字段单一事实源：列 + 常用搜索（可排序的只放后端白名单：应用版本、数据库版本、最近升级时间） ──
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('maintenance.versions.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('maintenance.versions.keyword_placeholder'), width: 240, order: 0 },
  // 列 + 搜索
  { key: 'database', title: t('maintenance.versions.database'), dataType: 'string', minWidth: 120, order: 1, render: row => databaseLabel(t, asRow(row).database) },
  { key: 'appVersion', title: t('maintenance.versions.app_version'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('maintenance.versions.app_version_placeholder'), minWidth: 130, order: 2 },
  { key: 'dbVersion', title: t('maintenance.versions.db_version'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('maintenance.versions.db_version_placeholder'), minWidth: 130, order: 3 },
  {
    key: 'isUpgrading',
    title: t('maintenance.versions.upgrade_status'),
    dataType: 'enum',
    searchable: true,
    options: upgradingOptions(t),
    searchPlaceholder: t('maintenance.versions.upgrade_status_placeholder'),
    width: 100,
    order: 4,
    render: row => renderTag(upgradingLabel(t, asRow(row).isUpgrading), upgradingTone(asRow(row).isUpgrading)),
  },
  { key: 'upgradeNode', title: t('maintenance.versions.upgrade_node'), dataType: 'string', minWidth: 140, order: 5 },
  { key: 'upgradeStartedAt', title: t('maintenance.versions.upgrade_start_time'), dataType: 'datetime', minWidth: 170, order: 6 },
  { key: 'upgradedAt', title: t('maintenance.versions.upgraded_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 7 },
])

// 只读页：版本行由升级执行器写入并把 IsUpgrading 用作分布式锁，页面不提供任何写操作
const schema = computed<PageSchema>(() => ({
  pageCode: 'maintenance.versions',
  pageName: t('maintenance.versions.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'view', title: t('maintenance.versions.view'), scope: 'row', icon: 'lucide:eye' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  if (payload.key === 'view' && row) {
    void handleDetail(row)
  }
}

// ── 详情抽屉（版本信息） ─────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailData = ref<VersionRow | null>(null)

async function handleDetail(row: VersionRow) {
  detailVisible.value = true
  detailLoading.value = true
  detailData.value = null
  try {
    detailData.value = await getVersion(row.id)
  }
  catch (error) {
    toast.danger(errorText(error) || t('maintenance.versions.load_detail_failed'))
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
        {{ t('maintenance.versions.engine_managed_hint') }}
      </span>
    </template>

    <!-- 详情抽屉：版本信息 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 720px">
        <XhDrawerTitle>{{ t('maintenance.versions.detail_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <div class="xh-loading-stage" :class="{ 'is-loading': detailLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="!detailLoading && !detailData" class="xh-detail-empty">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('maintenance.versions.detail_empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
          <div v-else-if="detailData" class="xh-scroll-area" style="max-height: calc(100vh - 120px)">
            <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.database') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ databaseLabel(t, detailData.database) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.app_version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.appVersion }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.db_version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.dbVersion || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.upgrade_status') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <XhTagRoot variant="subtle" :tone="upgradingTone(detailData.isUpgrading)" size="sm">
                    <XhTagLabel>
                      {{ upgradingLabel(t, detailData.isUpgrading) }}
                    </XhTagLabel>
                  </XhTagRoot>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.upgrade_node') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ detailData.upgradeNode || '-' }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.upgrade_start_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatDate(detailData.upgradeStartedAt) }}
                </XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('maintenance.versions.upgraded_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  {{ formatDate(detailData.upgradedAt) }}
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
  padding-block: var(--xh-space-8);
}

.xh-version-hint {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}
</style>
