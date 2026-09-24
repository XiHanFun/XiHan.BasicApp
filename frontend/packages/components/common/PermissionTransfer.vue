<script setup lang="ts" generic="T extends PermissionGrantItem">
import type { GrantTransferSide } from './grant-transfer'
import type { PermissionGrantItem } from './permission-grant-panel'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import GrantTransfer from './GrantTransfer.vue'
import { groupPermissions } from './permission-grant-panel'

/**
 * 权限穿梭框：按资源分段、条目为「权限名 + 权限码」的授权穿梭框。
 *
 * 只表达「授予 / 未授予」一个维度。三态直授（允许 / 拒绝）用两个本组件各管一态，
 * 行尾一格（suffix 插槽）用来标出条目在另一态里的归属。
 */
defineOptions({ name: 'XPermissionTransfer' })

const props = withDefaults(defineProps<{
  /** 权限目录全集 */
  items: T[]
  /** 已授予的权限主键；两侧的归属由它决定 */
  value: T['basicId'][]
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

const emit = defineEmits<{ 'update:value': [value: T['basicId'][]] }>()

defineSlots<{
  /** 行尾一格：只放徽标、计数这类非交互内容 */
  suffix?: (props: { item: T, side: GrantTransferSide }) => unknown
}>()

const { t } = useI18n()

/** 段的顺序由全集决定，两侧共用；与勾选面板同一份分组口径 */
const groups = computed(() => groupPermissions(props.items, props.otherGroupLabel ?? 'other'))
</script>

<template>
  <GrantTransfer
    :items="items"
    :value="value"
    :groups="groups"
    :get-label="item => item.permissionName"
    :get-description="item => item.permissionCode"
    :loading="loading"
    :disabled="disabled"
    :source-title="sourceTitle ?? t('component.permission_transfer.source')"
    :target-title="targetTitle ?? t('component.permission_transfer.target')"
    :search-placeholder="searchPlaceholder ?? t('component.permission_transfer.search')"
    @update:value="next => emit('update:value', next)"
  >
    <template v-if="$slots.suffix" #suffix="slotProps">
      <slot name="suffix" v-bind="slotProps" />
    </template>
  </GrantTransfer>
</template>
