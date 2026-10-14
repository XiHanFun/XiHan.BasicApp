<script setup lang="ts">
import type { PermissionRequestDetailDto } from '@/api'
import { XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { PERMISSION_REQUEST_STATUS_OPTIONS } from '@/constants'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

/**
 * 权限申请单据：审批中心按审批业务「permission-request」登记的视图组件加载。
 * 只读展示申请人、申请对象、理由与期望有效期；审批人没有补填字段，不暴露 getVariables。
 */
defineOptions({ name: 'PermissionRequestApproval' })

const props = defineProps<{
  businessId: string
  data: PermissionRequestDetailDto
  mode: 'approve' | 'view'
  nodeId?: string | null
}>()

const { t } = useI18n()
const requestStatusOptions = useEnumOptions('PermissionRequestStatus', PERMISSION_REQUEST_STATUS_OPTIONS)

const targetKind = computed(() => props.data.roleId
  ? t('identity.authorization.kind_role')
  : t('identity.authorization.kind_permission'))

const targetName = computed(() => {
  const name = props.data.roleName || props.data.permissionName
  const code = props.data.roleCode || props.data.permissionCode
  return [name, code && code !== name ? `(${code})` : null].filter(Boolean).join(' ') || '—'
})

const targetDescription = computed(() => props.data.roleDescription || props.data.permissionDescription)

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '—'
}
</script>

<template>
  <!-- 描边、标签在上：与各详情页同一种描述列表 -->
  <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
    <XhDescriptionsItem>
      <XhDescriptionsLabel>{{ t('identity.authorization.req_col_applicant') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>{{ data.requestUserDisplayName || data.requestUserId }}</XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem>
      <XhDescriptionsLabel>{{ t('identity.authorization.req_col_status') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>{{ getOptionLabel(requestStatusOptions, data.requestStatus) }}</XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem :span="2">
      <XhDescriptionsLabel>{{ t('identity.authorization.req_col_target') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>
        <span class="inline-flex flex-wrap items-center gap-1">
          <XhTagRoot variant="subtle" tone="neutral" size="sm">
            <XhTagLabel>{{ targetKind }}</XhTagLabel>
          </XhTagRoot>
          <span>{{ targetName }}</span>
        </span>
        <p v-if="targetDescription" class="mt-1 text-xs text-muted-foreground">
          {{ targetDescription }}
        </p>
      </XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem>
      <XhDescriptionsLabel>{{ t('identity.authorization.req_col_expected_validity') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>
        <span class="inline-flex flex-wrap items-center gap-1">
          {{ t('identity.authorization.validity_range', { from: formatNullableDate(data.expectedEffectiveTime), to: formatNullableDate(data.expectedExpirationTime) }) }}
          <XhTagRoot v-if="data.isExpectedExpired" variant="subtle" tone="warning" size="sm">
            <XhTagLabel>{{ t('identity.authorization.expected_expired') }}</XhTagLabel>
          </XhTagRoot>
        </span>
      </XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem>
      <XhDescriptionsLabel>{{ t('identity.authorization.req_col_create_time') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>{{ formatDate(data.createdTime) }}</XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem :span="2">
      <XhDescriptionsLabel>{{ t('identity.authorization.req_col_reason') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>
        <span class="whitespace-pre-wrap">{{ data.requestReason }}</span>
      </XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem v-if="data.remark" :span="2">
      <XhDescriptionsLabel>{{ t('identity.authorization.remark') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>
        <span class="whitespace-pre-wrap">{{ data.remark }}</span>
      </XhDescriptionsValue>
    </XhDescriptionsItem>
  </XhDescriptionsRoot>
</template>
