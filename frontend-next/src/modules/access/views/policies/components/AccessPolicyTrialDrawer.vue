<script setup lang="ts">
import type { AccessPolicyAction, AccessPolicyConditionInput, AccessPolicyTrialResult } from '../access-policy'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldLabel, XhFieldRoot, XhFormRoot, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XCombobox, XDatePicker, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { policies as policiesApi, users as usersApi } from '~/contracts'
import { formatDate } from '~/utils'
import { attributeLabel, operatorLabel } from '../../roles/condition-labels'
import { outcomeLabel, outcomeTone } from '../../roles/condition-trial'
import { phaseLabel } from '../access-policy'

/**
 * 访问策略试运行（4.x 同）：已保存的策略或条件草稿，按模拟的请求情形求一次，看每组每个条件取到什么值、会不会拦下。
 * 只求这一条，不看有效期和其他策略；不填的情形即取不到，时刻不填即现在，不选用户即按未登录。
 */
defineOptions({ name: 'AccessPolicyTrialDrawer' })

const props = defineProps<{
  /** 标题里的策略名 */
  title: string
  /** 已保存的策略；给了它就不看 conditions */
  policyId?: null | string
  /** 条件草稿 */
  conditions?: AccessPolicyConditionInput[] | null
  /** 按哪个动作判断会不会拦下 */
  action: AccessPolicyAction
}>()

const open = defineModel<boolean>('open', { required: true })

interface TrialFormModel {
  ip: string
  time: null | number
  userAgent: string
  path: string
  method: null | string
  userId: null | string
  permissionCodes: string
}

const { t } = useI18n()
const formId = useId()
const methodOptions = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map(method => ({ value: method, label: method }))

const form = ref<TrialFormModel>(emptyForm())
const userOptions = ref<Array<{ label: string, value: string, description?: string }>>([])
const userLoading = ref(false)
const running = ref(false)
const result = ref<AccessPolicyTrialResult | null>(null)
let userSeq = 0

function emptyForm(): TrialFormModel {
  return { ip: '', time: null, userAgent: '', path: '', method: null, userId: null, permissionCodes: '' }
}

async function searchUsers(keyword = '') {
  const seq = ++userSeq
  userLoading.value = true
  try {
    const page = await usersApi.list({ page: { index: 1, size: 50 }, filters: [], sorts: [], keyword: keyword.trim() || null })
    if (seq === userSeq)
      userOptions.value = page.items.map(user => ({ label: user.displayName, value: user.id, description: `@${user.userName}` }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.policies.trial.load_users_failed'))
  }
  finally {
    if (seq === userSeq)
      userLoading.value = false
  }
}

async function run() {
  running.value = true
  try {
    const codes = form.value.permissionCodes.split(/[\s,，]+/).map(code => code.trim()).filter(Boolean)
    result.value = await policiesApi.trial({
      policyId: props.policyId ?? null,
      conditions: props.policyId ? null : (props.conditions ?? []),
      action: props.action,
      ip: form.value.ip.trim() || null,
      time: form.value.time ? new Date(form.value.time).toISOString() : null,
      userAgent: form.value.userAgent.trim() || null,
      path: form.value.path.trim() || null,
      method: form.value.method,
      userId: form.value.userId,
      permissionCodes: codes.length > 0 ? codes : null,
    })
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.policies.trial.failed'))
  }
  finally {
    running.value = false
  }
}

watch(open, (value) => {
  if (value) {
    form.value = emptyForm()
    result.value = null
    if (userOptions.value.length === 0)
      void searchUsers()
  }
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 960px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('access.policies.trial.title', { name: title }) }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body policy-trial">
        <p class="policy-trial__tip">
          {{ t('access.policies.trial.tip') }}
        </p>
        <p v-if="!policyId" class="policy-trial__tip">
          {{ t('access.policies.trial.draft_tip') }}
        </p>
        <XhFormRoot :id="formId" class="xh-edit-form-grid" data-testid="policy-trial-form" @submit="run">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.conditions.label_ip') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.ip" name="ip" clearable :placeholder="t('access.conditions.ph_ip')" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.conditions.label_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker v-model:value="form.time" clearable show-time :placeholder="t('access.conditions.ph_time')" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.conditions.label_user') }}</XhFieldLabel>
            <XhFieldControl>
              <XCombobox
                class="policy-trial__combobox"
                remote
                :options="userOptions"
                :value="form.userId"
                :loading="userLoading"
                :placeholder="t('access.policies.trial.ph_user')"
                @search="searchUsers"
                @update:value="value => (form.userId = typeof value === 'string' ? value : null)"
              />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.conditions.label_user_agent') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.userAgent" name="userAgent" clearable />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.conditions.label_path') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.path" name="path" clearable placeholder="/api/users:list" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.conditions.label_method') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="form.method" name="method" clearable :options="methodOptions" />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot class="xh-span-2">
            <XhFieldLabel>{{ t('access.policies.trial.label_permission_codes') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.permissionCodes" name="permissionCodes" clearable :placeholder="t('access.policies.trial.ph_permission_codes')" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormRoot>
        <div class="policy-trial__actions">
          <XhButton variant="subtle" tone="brand" :loading="running" type="submit" :form="formId">
            <XhButtonIndicator>
              <Icon icon="lucide:play" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('access.conditions.action_run') }}</XhButtonLabel>
          </XhButton>
        </div>

        <section v-if="result" class="policy-trial__result" :aria-label="t('access.policies.trial.result_title')">
          <p v-for="error in result.errors" :key="error" class="policy-trial__error" role="alert">
            {{ t('access.policies.trial.errors', { errors: error }) }}
          </p>
          <header v-if="result.errors.length === 0" class="policy-trial__verdict">
            <XhTagRoot variant="subtle" :tone="outcomeTone(result.outcome)">
              <XhTagLabel>{{ result.outcome ? outcomeLabel(t, result.outcome) : '-' }}</XhTagLabel>
            </XhTagRoot>
            <XhTagRoot variant="subtle" :tone="result.wouldBlock ? 'danger' : 'success'">
              <XhTagLabel>{{ result.wouldBlock ? t('access.policies.trial.would_block') : t('access.policies.trial.would_pass') }}</XhTagLabel>
            </XhTagRoot>
            <XhTagRoot variant="outline">
              <XhTagLabel>{{ phaseLabel(t, result.phase) }}</XhTagLabel>
            </XhTagRoot>
          </header>
          <p class="policy-trial__meta">
            {{ t('access.policies.trial.context', {
              ip: result.ip ?? '-',
              location: result.location ?? '-',
              time: formatDate(result.time),
              zone: result.timeZone,
              user: result.userName ?? t('access.policies.trial.anonymous'),
            }) }}
          </p>
          <table v-for="group in result.groups" :key="group.key ?? ''" class="xh-detail-table">
            <caption class="policy-trial__caption">
              {{ t('access.conditions.group_title', { group: group.key ?? '' }) }} · {{ outcomeLabel(t, group.outcome) }}
            </caption>
            <thead>
              <tr>
                <th>{{ t('access.conditions.th_attribute') }}</th>
                <th>{{ t('access.conditions.th_operator') }}</th>
                <th>{{ t('access.conditions.th_value') }}</th>
                <th>{{ t('access.conditions.attribute_values') }}</th>
                <th>{{ t('access.conditions.th_outcome') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(clause, clauseIndex) in group.clauses" :key="clause.key ?? clauseIndex">
                <td>
                  <span class="policy-trial__attribute">{{ attributeLabel(t, clause.attributeName) }}</span>
                  <code class="policy-trial__meta">{{ clause.attributeName }}</code>
                </td>
                <td>{{ clause.isNegated ? t('access.conditions.negated_prefix') : '' }}{{ operatorLabel(t, clause.operator) }}</td>
                <td><code>{{ clause.value }}</code></td>
                <td>{{ clause.attributeValues.join('、') || '-' }}</td>
                <td :title="clause.reason ?? undefined">
                  <XhTagRoot variant="subtle" size="sm" :tone="outcomeTone(clause.outcome)">
                    <XhTagLabel>{{ outcomeLabel(t, clause.outcome) }}</XhTagLabel>
                  </XhTagRoot>
                </td>
              </tr>
            </tbody>
          </table>
        </section>
      </XhDrawerBody>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
.policy-trial {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.policy-trial__tip,
.policy-trial__meta {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.policy-trial__error {
  margin: 0;
  color: var(--xh-fg-danger);
  font-size: var(--xh-text-caption-size);
}

.policy-trial__combobox {
  inline-size: 100%;
}

.policy-trial__actions,
.policy-trial__verdict {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.policy-trial__actions {
  justify-content: flex-end;
}

.policy-trial__result {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.policy-trial__attribute {
  display: block;
}

.policy-trial__caption {
  padding-block-end: var(--xh-space-1);
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
  text-align: start;
}
</style>
