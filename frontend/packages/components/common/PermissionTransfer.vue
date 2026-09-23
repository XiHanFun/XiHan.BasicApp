<script setup lang="ts" generic="T extends PermissionGrantItem">
import type { PermissionGrantItem } from './permission-grant-panel'
import {
  XhTransferGroup,
  XhTransferGroupLabel,
  XhTransferItem,
  XhTransferItemCheckbox,
  XhTransferItemText,
  XhTransferList,
  XhTransferPanelCount,
  XhTransferPanelHeader,
  XhTransferPanelTitle,
  XhTransferRoot,
  XhTransferSearch,
  XhTransferSourcePanel,
  XhTransferTargetPanel,
  XhTransferToSourceTrigger,
  XhTransferToTargetTrigger,
} from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { groupPermissions } from './permission-grant-panel'

/**
 * 权限穿梭框：二元授予（有 / 无）的权限分配用它。
 *
 * 比单栏勾选列表强的地方在右边那一栏——「已授予」是审计时要成列读的一份清单，
 * 勾选列表里得自己把勾上的挑出来。
 *
 * 不适用于三态授权（允许 / 拒绝 / 继承）和「已授予 + 有效性」这类两个正交维度的场景：
 * 穿梭框只能表达「在左还是在右」一个维度，那些仍用 XPermissionGrantPanel。
 */
defineOptions({ name: 'XPermissionTransfer' })

const props = withDefaults(defineProps<{
  /** 权限目录全集 */
  items: T[]
  /** 已授予的权限主键；两侧的归属由它决定 */
  value: (number | string)[]
  loading?: boolean
  disabled?: boolean
  sourceTitle?: string
  targetTitle?: string
  searchPlaceholder?: string
  otherGroupLabel?: string
}>(), {
  loading: false,
  disabled: false,
  sourceTitle: undefined,
  targetTitle: undefined,
  searchPlaceholder: undefined,
  otherGroupLabel: undefined,
})

const emit = defineEmits<{ 'update:value': [value: (number | string)[]] }>()

const { t } = useI18n()

/**
 * 穿梭框的值域是字符串，权限主键是 ApiId（number | string）。
 * 这里按字符串出入、回写时查回原始主键，避免把 number 主键悄悄变成串下发给后端。
 */
const idByKey = computed(() => {
  const map = new Map<string, number | string>()
  for (const item of props.items) {
    map.set(String(item.basicId), item.basicId)
  }
  return map
})

const itemByKey = computed(() => {
  const map = new Map<string, T>()
  for (const item of props.items) {
    map.set(String(item.basicId), item)
  }
  return map
})

/** label 同时是搜索取字来源：名称与权限码都拼进去，按名或按码都搜得到 */
const collection = computed(() =>
  props.items.map(item => ({
    value: String(item.basicId),
    label: `${item.permissionName} ${item.permissionCode}`,
  })),
)

const selectedKeys = computed({
  get: () => props.value.map(String),
  set: (next: string[]) => emit('update:value', next.map(key => idByKey.value.get(key) ?? key)),
})

/** 段的顺序由全集决定，两侧共用；条目在哪一侧只影响该段里还剩几条 */
const groups = computed(() => groupPermissions(props.items, props.otherGroupLabel ?? 'other'))

const keysByGroup = computed(() => {
  const map = new Map<string, Set<string>>()
  for (const group of groups.value) {
    map.set(group.key, new Set(group.items.map(item => String(item.basicId))))
  }
  return map
})

/** 面板插槽只给出本侧当前可见的条目，分组信息回全集里查 */
function inGroup(visible: readonly { value: string }[], groupKey: string): string[] {
  const keys = keysByGroup.value.get(groupKey)
  if (!keys) {
    return []
  }
  return visible.map(item => item.value).filter(key => keys.has(key))
}
</script>

<template>
  <XhTransferRoot
    v-model:value="selectedKeys"
    class="xh-perm-transfer"
    :collection="collection"
    :loading="loading"
    :disabled="disabled"
    searchable
  >
    <XhTransferSourcePanel v-slot="{ items: visible }">
      <XhTransferPanelHeader>
        <XhTransferPanelTitle>{{ sourceTitle ?? t('component.permission_transfer.source') }}</XhTransferPanelTitle>
        <XhTransferPanelCount />
      </XhTransferPanelHeader>
      <XhTransferSearch :placeholder="searchPlaceholder ?? t('component.permission_transfer.search')" />
      <XhTransferList>
        <template v-for="group in groups" :key="group.key">
          <XhTransferGroup v-if="inGroup(visible, group.key).length > 0" :value="group.key">
            <XhTransferGroupLabel>{{ group.name }}</XhTransferGroupLabel>
            <XhTransferItem v-for="key in inGroup(visible, group.key)" :key="key" :value="key">
              <XhTransferItemCheckbox />
              <XhTransferItemText>
                <span class="xh-perm-transfer__name">{{ itemByKey.get(key)?.permissionName }}</span>
                <span class="xh-perm-transfer__code">{{ itemByKey.get(key)?.permissionCode }}</span>
              </XhTransferItemText>
            </XhTransferItem>
          </XhTransferGroup>
        </template>
      </XhTransferList>
    </XhTransferSourcePanel>

    <XhTransferToTargetTrigger />
    <XhTransferToSourceTrigger />

    <XhTransferTargetPanel v-slot="{ items: visible }">
      <XhTransferPanelHeader>
        <XhTransferPanelTitle>{{ targetTitle ?? t('component.permission_transfer.target') }}</XhTransferPanelTitle>
        <XhTransferPanelCount />
      </XhTransferPanelHeader>
      <XhTransferSearch :placeholder="searchPlaceholder ?? t('component.permission_transfer.search')" />
      <XhTransferList>
        <template v-for="group in groups" :key="group.key">
          <XhTransferGroup v-if="inGroup(visible, group.key).length > 0" :value="group.key">
            <XhTransferGroupLabel>{{ group.name }}</XhTransferGroupLabel>
            <XhTransferItem v-for="key in inGroup(visible, group.key)" :key="key" :value="key">
              <XhTransferItemCheckbox />
              <XhTransferItemText>
                <span class="xh-perm-transfer__name">{{ itemByKey.get(key)?.permissionName }}</span>
                <span class="xh-perm-transfer__code">{{ itemByKey.get(key)?.permissionCode }}</span>
              </XhTransferItemText>
            </XhTransferItem>
          </XhTransferGroup>
        </template>
      </XhTransferList>
    </XhTransferTargetPanel>
  </XhTransferRoot>
</template>

<style scoped>
/*
 * 穿梭框自身的高度由内容撑出（缺省约 256px）。放进抽屉这种 flex 列里时让它吃满剩余高度，
 * 否则两栏只剩五六行、底下一大片空白，「已授予」那栏也就失去了成列审阅的意义。
 * 不在 flex 容器里时这条不起作用，仍按内容高。
 */
.xh-perm-transfer {
  flex: 1;
  min-block-size: 0;
}

/* 条目是「名称 + 权限码」两行：名称可读，码是排障与对账时要看的那一串 */
.xh-perm-transfer__name {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.xh-perm-transfer__code {
  display: block;
  overflow: hidden;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
