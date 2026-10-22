<script setup lang="ts">
import type { AccessPolicyAction, AccessPolicyConditionDto, AccessPolicyTrialResultDto, ApiId } from '@/api'
import {
  XhAlertContent,
  XhAlertDescription,
  XhAlertIndicator,
  XhAlertRoot,
  XhButton,
  XhButtonIndicator,
  XhButtonLabel,
  XhDrawerBody,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerHeader,
  XhDrawerRoot,
  XhDrawerTitle,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { accessPolicyApi, createPageRequest, userManagementApi } from '@/api'
import { ACCESS_POLICY_PHASE_OPTIONS, CONDITION_OPERATOR_OPTIONS } from '@/constants'
import { Icon, XCombobox, XDatePicker, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'
import { outcomeTone } from '../../../identity/components/permission-condition'
import { trialUserLabel } from '../access-policy'

/**
 * 访问策略试运行：已保存的策略或条件草稿，按模拟的请求上下文求一次，看每组每个子句取到什么值、会不会拦下。
 * 只求这一条，不看生效期和其他策略；不填的项取当前请求与当前用户
 */
defineOptions({ name: 'AccessPolicyTrialDrawer' })

const props = defineProps<{
  /** 标题里的策略名 */
  title: string
  /** 已保存的策略主键；给了它就不看 conditions */
  policyId?: ApiId | null
  /** 条件草稿 */
  conditions?: AccessPolicyConditionDto[] | null
  /** 按哪个动作判断会不会拦下 */
  action: AccessPolicyAction
}>()

const open = defineModel<boolean>('open', { required: true })

interface TrialFormModel {
  ip: string
  time: number | null
  userAgent: string
  path: string
  method: string | null
  userId: ApiId | null
  permissionCodes: string
}

const { t } = useI18n()
const operatorOptions = useEnumOptions('ConditionOperator', CONDITION_OPERATOR_OPTIONS)
const phaseOptions = useEnumOptions('AccessPolicyPhase', ACCESS_POLICY_PHASE_OPTIONS)
const formId = useId()
const methodOptions = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map(method => ({ value: method, label: method }))

const form = ref<TrialFormModel>(emptyForm())
const userOptions = ref<Array<{ label: string, value: ApiId }>>([])
const userLoading = ref(false)
const running = ref(false)
const result = ref<AccessPolicyTrialResultDto | null>(null)
const resultUser = ref('')

function emptyForm(): TrialFormModel {
  return { ip: '', time: null, userAgent: '', path: '', method: null, userId: null, permissionCodes: '' }
}

async function searchUsers(keyword = '') {
  userLoading.value = true
  try {
    const page = await userManagementApi.page({ ...createPageRequest({ page: { pageIndex: 1, pageSize: 20 } }), keyword: keyword || null })
    userOptions.value = page.items.map(user => ({ label: `${user.realName || user.nickName || user.userName} (@${user.userName})`, value: user.basicId }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.trial.load_users_failed'))
  }
  finally {
    userLoading.value = false
  }
}

async function run() {
  running.value = true
  try {
    const codes = form.value.permissionCodes.split(/[\s,，]+/).map(code => code.trim()).filter(Boolean)
    const userLabel = trialUserLabel(form.value.userId, userOptions.value, t('setting.access_policy.trial.current_user'))
    result.value = await accessPolicyApi.trial({
      policyId: props.policyId ?? null,
      conditions: props.policyId ? null : (props.conditions ?? []),
      action: props.action,
      context: {
        ip: form.value.ip.trim() || null,
        time: form.value.time ? new Date(form.value.time).toISOString() : null,
        userAgent: form.value.userAgent.trim() || null,
        path: form.value.path.trim() || null,
        method: form.value.method,
        userId: form.value.userId,
        permissionCodes: codes.length ? codes : null,
      },
    })
    resultUser.value = userLabel
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.trial.failed'))
  }
  finally {
    running.value = false
  }
}

watch(open, (value) => {
  if (value) {
    form.value = emptyForm()
    result.value = null
    if (!userOptions.value.length) {
      void searchUsers()
    }
  }
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 960px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('setting.access_policy.trial.title', { name: title }) }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body policy-trial">
        <p class="policy-trial__tip">
          {{ t('setting.access_policy.trial.tip') }}
        </p>
        <p v-if="!policyId" class="policy-trial__tip">
          {{ t('setting.access_policy.trial.draft_tip') }}
        </p>
        <XhFormRoot :id="formId" v-model:values="form" class="xh-edit-form-grid" @submit="run">
          <XhFormFieldGroup name="ip">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_ip') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.ip" clearable :placeholder="t('identity.condition.ph_default_current')" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="time">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_time') }}</XhFieldLabel>
              <XhFieldControl>
                <XDatePicker v-model:value="form.time" show-time :placeholder="t('identity.condition.ph_default_now')" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="userId">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_user') }}</XhFieldLabel>
              <XhFieldControl>
                <XCombobox
                  remote
                  :options="userOptions"
                  :value="form.userId"
                  :loading="userLoading"
                  :placeholder="t('setting.access_policy.trial.ph_user')"
                  @search="searchUsers"
                  @update:value="value => (form.userId = (value as ApiId | null))"
                />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="userAgent">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_user_agent') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.userAgent" clearable :placeholder="t('identity.condition.ph_default_current')" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="path">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_path') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.path" clearable />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="method">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_method') }}</XhFieldLabel>
              <XhFieldControl>
                <XSelect v-model:value="form.method" clearable :options="methodOptions" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="permissionCodes" class="xh-span-2">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('setting.access_policy.trial.label_permission_codes') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.permissionCodes" clearable :placeholder="t('setting.access_policy.trial.ph_permission_codes')" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
        </XhFormRoot>
        <div class="policy-trial__toolbar">
          <XhButton variant="subtle" tone="brand" :loading="running" @click="run">
            <XhButtonIndicator>
              <Icon icon="lucide:play" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('identity.condition.run_trial') }}</XhButtonLabel>
          </XhButton>
        </div>

        <section v-if="result" class="policy-trial__result" :aria-label="t('setting.access_policy.trial.result_title')">
          <XhAlertRoot v-if="result.errors.length" tone="danger" variant="subtle">
            <XhAlertIndicator>
              <Icon icon="lucide:circle-alert" width="16" />
            </XhAlertIndicator>
            <XhAlertContent>
              <XhAlertDescription>{{ t('setting.access_policy.trial.errors', { errors: result.errors.join('；') }) }}</XhAlertDescription>
            </XhAlertContent>
          </XhAlertRoot>
          <header v-else class="policy-trial__verdict">
            <XhTagRoot variant="subtle" :tone="outcomeTone(result.outcome)">
              <XhTagLabel>{{ result.outcome ? t(`identity.condition.outcome.${result.outcome}`) : '-' }}</XhTagLabel>
            </XhTagRoot>
            <XhTagRoot variant="subtle" :tone="result.wouldBlock ? 'danger' : 'success'">
              <XhTagLabel>{{ result.wouldBlock ? t('setting.access_policy.trial.would_block') : t('setting.access_policy.trial.would_pass') }}</XhTagLabel>
            </XhTagRoot>
            <XhTagRoot variant="outline">
              <XhTagLabel>{{ getOptionLabel(phaseOptions, result.phase) }}</XhTagLabel>
            </XhTagRoot>
          </header>
          <p class="policy-trial__meta">
            {{ t('setting.access_policy.trial.context', {
              ip: result.ip ?? '-',
              location: result.location ?? '-',
              time: formatDate(result.time),
              zone: result.timeZone,
              user: resultUser,
            }) }}
          </p>
          <table v-for="group in result.groups" :key="group.group ?? ''" class="xh-detail-table">
            <caption class="policy-trial__caption">
              {{ t('identity.condition.group_title', { group: group.group ?? '' }) }} · {{ t(`identity.condition.outcome.${group.outcome}`) }}
            </caption>
            <thead>
              <tr>
                <th>{{ t('identity.condition.th_attribute') }}</th>
                <th>{{ t('identity.condition.th_operator') }}</th>
                <th>{{ t('identity.condition.th_value') }}</th>
                <th>{{ t('identity.condition.attribute_values') }}</th>
                <th>{{ t('identity.condition.th_outcome') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="clause in group.clauses" :key="clause.conditionId ?? clause.attributeName">
                <td><code>{{ clause.attributeName }}</code></td>
                <td>{{ clause.isNegated ? t('identity.condition.negated_prefix') : '' }}{{ getOptionLabel(operatorOptions, clause.operator) }}</td>
                <td><code>{{ clause.value }}</code></td>
                <td>{{ clause.attributeValues.join('、') || '-' }}</td>
                <td :title="clause.message ?? undefined">
                  <XhTagRoot variant="subtle" size="sm" :tone="outcomeTone(clause.outcome)">
                    <XhTagLabel>{{ t(`identity.condition.outcome.${clause.outcome}`) }}</XhTagLabel>
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

.policy-trial__toolbar,
.policy-trial__verdict {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.policy-trial__result {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.policy-trial__caption {
  padding-block-end: var(--xh-space-1);
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
  text-align: start;
}
</style>
