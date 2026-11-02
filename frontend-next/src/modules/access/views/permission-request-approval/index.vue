<script setup lang="ts">
import type { PermissionRequestRow } from '../delegations/use-permission-requests'
import { XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { formatDate } from '~/utils'
import { expectedValidity, requestStatusLabel } from '../delegations/use-permission-requests'

/**
 * 授权申请单据（照 4.x，设计稿 D4）：审批中心按审批业务 permission-request 登记的组件路径 access/permission-request-approval/index 加载，
 * data 是与 permission-requests.get 同一形态的响应。只读展示申请人、申请对象、理由与期望有效期；审批人没有补填字段，不暴露 getVariables。
 */
defineOptions({ name: 'AccessPermissionRequestApproval' })

const props = defineProps<{
  businessId: string
  data: PermissionRequestRow
  mode: 'approve' | 'view'
  nodeId?: string | null
}>()

const { t } = useI18n()

/** 名字（编码）：编码与名字相同时不重复写 */
function nameWithCode(name?: null | string, code?: null | string): string {
  return [name, code && code !== name ? `(${code})` : null].filter(Boolean).join(' ') || '-'
}

/** 申请的对象：6.0 可以同时申请角色与权限，各占一行 */
const targets = computed(() => {
  const items: { kind: string, name: string, description?: null | string }[] = []
  if (props.data.roleId)
    items.push({ kind: t('access.permission_requests.kind_role'), name: nameWithCode(props.data.roleName, props.data.roleCode), description: props.data.roleDescription })
  if (props.data.permissionCode)
    items.push({ kind: t('access.permission_requests.kind_permission'), name: nameWithCode(props.data.permissionName, props.data.permissionCode), description: props.data.permissionDescription })
  return items
})
</script>

<template>
  <!-- 描边、标签在上：与各详情页同一种描述列表 -->
  <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
    <XhDescriptionsItem>
      <XhDescriptionsLabel>{{ t('access.permission_requests.fields.applicant') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>{{ data.requestUserName || data.requestUserId }}</XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem>
      <XhDescriptionsLabel>{{ t('access.permission_requests.fields.status') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>{{ requestStatusLabel(t, data.status) }}</XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem :span="2">
      <XhDescriptionsLabel>{{ t('access.permission_requests.fields.target') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>
        <!-- 对象下面是它的说明（照 4.x） -->
        <div v-for="target in targets" :key="target.kind">
          <span class="flex flex-wrap items-center gap-1">
            <XhTagRoot variant="subtle" tone="neutral" size="sm">
              <XhTagLabel>{{ target.kind }}</XhTagLabel>
            </XhTagRoot>
            <span>{{ target.name }}</span>
          </span>
          <p v-if="target.description" class="mt-1 text-xs text-muted-foreground">
            {{ target.description }}
          </p>
        </div>
        <template v-if="targets.length === 0">
          -
        </template>
      </XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem>
      <XhDescriptionsLabel>{{ t('access.permission_requests.fields.expected_validity') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>
        <span class="inline-flex flex-wrap items-center gap-1">
          {{ expectedValidity(t, data) }}
          <XhTagRoot v-if="data.isExpectedExpired" variant="subtle" tone="warning" size="sm">
            <XhTagLabel>{{ t('access.permission_requests.expected_expired') }}</XhTagLabel>
          </XhTagRoot>
        </span>
      </XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem>
      <XhDescriptionsLabel>{{ t('access.permission_requests.fields.created_at') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>{{ formatDate(data.createdAt) }}</XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem :span="2">
      <XhDescriptionsLabel>{{ t('access.permission_requests.fields.reason') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>
        <span class="whitespace-pre-wrap">{{ data.reason }}</span>
      </XhDescriptionsValue>
    </XhDescriptionsItem>
    <XhDescriptionsItem v-if="data.remark" :span="2">
      <XhDescriptionsLabel>{{ t('access.permission_requests.fields.remark') }}</XhDescriptionsLabel>
      <XhDescriptionsValue>
        <span class="whitespace-pre-wrap">{{ data.remark }}</span>
      </XhDescriptionsValue>
    </XhDescriptionsItem>
  </XhDescriptionsRoot>
</template>
