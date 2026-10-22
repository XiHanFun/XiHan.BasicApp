<script setup lang="ts">
import type { ApiId, PermissionConditionTrialResultDto, PermissionConditionTrialSetDto } from '@/api'
import {
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
  XhFieldDescription,
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhSwitch,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, permissionApi, permissionConditionInsightApi, userManagementApi } from '@/api'
import { CONDITION_OPERATOR_OPTIONS } from '@/constants'
import { Icon, XCombobox, XDatePicker, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { useUserStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'
import { outcomeTone, parseKeyValueLines, verdictTone } from '../../components/permission-condition'

/**
 * 条件试算：选人和权限码，填模拟的请求上下文，看走了哪条授予路径、每组每个子句取到什么值、结果如何。
 * 不填的项取当前请求的 IP、User-Agent 与当前时刻
 */
defineOptions({ name: 'ConditionTrialDrawer' })

const open = defineModel<boolean>('open', { required: true })

interface TrialFormModel {
  userId: ApiId | null
  permissionCode: string | null
  tenantId: string
  ip: string
  time: number | null
  userAgent: string
  path: string
  method: string | null
  queryText: string
  routeText: string
  resourceId: string
  isImpersonating: boolean
}

const { t } = useI18n()
const userStore = useUserStore()
const operatorOptions = useEnumOptions('ConditionOperator', CONDITION_OPERATOR_OPTIONS)
const formId = useId()
const isPlatformContext = computed(() => userStore.userInfo?.isPlatform ?? false)
const methodOptions = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map(method => ({ value: method, label: method }))

const form = ref<TrialFormModel>(emptyForm())
const userOptions = ref<Array<{ label: string, value: ApiId }>>([])
const userLoading = ref(false)
const permissionOptions = ref<Array<{ label: string, value: string, description?: string }>>([])
const running = ref(false)
const result = ref<PermissionConditionTrialResultDto | null>(null)

function emptyForm(): TrialFormModel {
  return {
    userId: null,
    permissionCode: null,
    tenantId: '',
    ip: '',
    time: null,
    userAgent: '',
    path: '',
    method: null,
    queryText: '',
    routeText: '',
    resourceId: '',
    isImpersonating: false,
  }
}

async function searchUsers(keyword = '') {
  userLoading.value = true
  try {
    const page = await userManagementApi.page({ ...createPageRequest({ page: { pageIndex: 1, pageSize: 20 } }), keyword: keyword || null })
    userOptions.value = page.items.map(user => ({ label: `${user.realName || user.nickName || user.userName} (@${user.userName})`, value: user.basicId }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.condition.msg_load_failed'))
  }
  finally {
    userLoading.value = false
  }
}

async function loadPermissions() {
  if (permissionOptions.value.length) {
    return
  }
  try {
    permissionOptions.value = (await permissionApi.catalog()).map(permission => ({
      label: permission.permissionName,
      value: permission.permissionCode,
      description: permission.permissionCode,
    }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.condition.msg_load_failed'))
  }
}

async function run() {
  if (!form.value.userId || !form.value.permissionCode) {
    toast.warning(t('identity.condition.msg_trial_required'))
    return
  }
  running.value = true
  try {
    const query = parseKeyValueLines(form.value.queryText)
    const route = parseKeyValueLines(form.value.routeText)
    result.value = await permissionConditionInsightApi.trial({
      userId: form.value.userId,
      permissionCode: form.value.permissionCode,
      tenantId: isPlatformContext.value && form.value.tenantId.trim() ? form.value.tenantId.trim() : null,
      ip: form.value.ip.trim() || null,
      time: form.value.time ? new Date(form.value.time).toISOString() : null,
      userAgent: form.value.userAgent.trim() || null,
      path: form.value.path.trim() || null,
      method: form.value.method,
      queryValues: Object.keys(query).length ? query : null,
      routeValues: Object.keys(route).length ? route : null,
      resourceId: form.value.resourceId.trim() || null,
      isImpersonating: form.value.isImpersonating,
    })
  }
  catch (error) {
    toast.danger((error as Error).message || t('identity.condition.msg_trial_failed'))
  }
  finally {
    running.value = false
  }
}

function setOutcomeLabel(set: PermissionConditionTrialSetDto) {
  return set.outcome ? t(`identity.condition.outcome.${set.outcome}`) : t('identity.condition.missing_set')
}

watch(open, (value) => {
  if (value) {
    void loadPermissions()
    if (!userOptions.value.length) {
      void searchUsers()
    }
  }
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 1100px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('identity.condition.trial_title') }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body trial">
        <p class="trial__tip">
          {{ t('identity.condition.trial_tip') }}
        </p>
        <XhFormRoot :id="formId" v-model:values="form" class="xh-edit-form-grid trial__form" @submit="run">
          <XhFormFieldGroup name="userId">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_user') }}</XhFieldLabel>
              <XhFieldControl>
                <XCombobox
                  remote
                  :options="userOptions"
                  :value="form.userId"
                  :loading="userLoading"
                  :placeholder="t('identity.condition.ph_user')"
                  @search="searchUsers"
                  @update:value="value => (form.userId = (value as ApiId | null))"
                />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="permissionCode">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_permission') }}</XhFieldLabel>
              <XhFieldControl>
                <XCombobox
                  :options="permissionOptions"
                  :value="form.permissionCode"
                  :placeholder="t('identity.condition.ph_permission')"
                  @update:value="value => (form.permissionCode = (value as string | null))"
                />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup v-if="isPlatformContext" name="tenantId">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_tenant') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.tenantId" clearable :placeholder="t('identity.condition.ph_tenant')" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
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
          <XhFormFieldGroup name="queryText">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_query') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.queryText" type="textarea" :rows="2" :placeholder="t('identity.condition.ph_key_values')" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="routeText">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_route') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.routeText" type="textarea" :rows="2" :placeholder="t('identity.condition.ph_key_values')" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="resourceId">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_resource') }}</XhFieldLabel>
              <XhFieldControl>
                <XInput v-model:value="form.resourceId" clearable />
              </XhFieldControl>
              <XhFieldDescription>{{ t('identity.condition.resource_hint') }}</XhFieldDescription>
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="isImpersonating">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.condition.label_impersonating') }}</XhFieldLabel>
              <XhFieldControl>
                <XhSwitch :checked="form.isImpersonating" @update:checked="(value: boolean) => (form.isImpersonating = value)" />
              </XhFieldControl>
            </XhFieldRoot>
          </XhFormFieldGroup>
        </XhFormRoot>
        <div class="trial__toolbar">
          <XhButton variant="subtle" tone="brand" :loading="running" @click="run">
            <XhButtonIndicator>
              <Icon icon="lucide:play" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('identity.condition.run_trial') }}</XhButtonLabel>
          </XhButton>
        </div>

        <section v-if="result" class="trial__result" :aria-label="t('identity.condition.result_title')">
          <header class="trial__verdict">
            <XhTagRoot variant="subtle" :tone="verdictTone(result.verdict)">
              <XhTagLabel>{{ t(`identity.condition.verdict.${result.verdict}`) }}</XhTagLabel>
            </XhTagRoot>
            <span class="trial__meta">
              {{ t('identity.condition.trial_context', { user: result.userName ?? result.userId, ip: result.ip ?? '-', time: formatDate(result.time), zone: result.timeZone }) }}
            </span>
          </header>

          <article v-for="path in result.paths" :key="path.source" class="trial-path">
            <header class="trial-path__header">
              <XhTagRoot variant="subtle" size="sm" :tone="outcomeTone(path.outcome)">
                <XhTagLabel>{{ t(`identity.condition.outcome.${path.outcome}`) }}</XhTagLabel>
              </XhTagRoot>
              <span class="trial-path__title">{{ path.sourceLabel || path.source }}</span>
              <span v-if="path.hasUnconditionalGrant" class="trial__meta">{{ t('identity.condition.path_unconditional') }}</span>
            </header>
            <template v-for="(sets, side) in { grant: path.grantSets, deny: path.denySets }" :key="side">
              <div v-for="set in sets" :key="`${side}-${set.key}`" class="trial-set">
                <p class="trial-set__title">
                  {{ side === 'grant' ? t('identity.condition.grant_sets') : t('identity.condition.deny_sets') }}
                  <XhTagRoot variant="outline" size="sm" :tone="outcomeTone(set.outcome)">
                    <XhTagLabel>{{ setOutcomeLabel(set) }}</XhTagLabel>
                  </XhTagRoot>
                </p>
                <table v-for="group in set.groups" :key="group.group ?? ''" class="xh-detail-table">
                  <caption class="trial-set__caption">
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
              </div>
            </template>
          </article>

          <article v-if="result.denies.length" class="trial-path">
            <header class="trial-path__header">
              <span class="trial-path__title">{{ t('identity.condition.user_denies') }}</span>
            </header>
            <div v-for="set in result.denies" :key="set.key" class="trial-set">
              <p class="trial-set__title">
                <XhTagRoot variant="outline" size="sm" :tone="outcomeTone(set.outcome)">
                  <XhTagLabel>{{ setOutcomeLabel(set) }}</XhTagLabel>
                </XhTagRoot>
              </p>
              <table v-for="group in set.groups" :key="group.group ?? ''" class="xh-detail-table">
                <tbody>
                  <tr v-for="clause in group.clauses" :key="clause.conditionId ?? clause.attributeName">
                    <td><code>{{ clause.attributeName }}</code></td>
                    <td>{{ clause.isNegated ? t('identity.condition.negated_prefix') : '' }}{{ getOptionLabel(operatorOptions, clause.operator) }}</td>
                    <td><code>{{ clause.value }}</code></td>
                    <td>{{ clause.attributeValues.join('、') || '-' }}</td>
                    <td>
                      <XhTagRoot variant="subtle" size="sm" :tone="outcomeTone(clause.outcome)">
                        <XhTagLabel>{{ t(`identity.condition.outcome.${clause.outcome}`) }}</XhTagLabel>
                      </XhTagRoot>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </article>
        </section>
      </XhDrawerBody>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
.trial {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.trial__tip,
.trial__meta {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.trial__toolbar {
  display: flex;
  gap: var(--xh-space-2);
}

.trial__result {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.trial__verdict,
.trial-path__header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.trial-path {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}

.trial-path__title {
  font-weight: var(--xh-font-weight-semibold);
}

.trial-set {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
}

.trial-set__title {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  margin: 0;
}

.trial-set__caption {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
  text-align: start;
}
</style>
