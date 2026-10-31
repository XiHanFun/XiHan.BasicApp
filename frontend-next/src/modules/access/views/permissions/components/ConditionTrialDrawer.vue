<script setup lang="ts">
import type { ConditionTrialResult, ConditionTrialSet, ConditionVerdict } from '../../roles/condition-trial'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XCombobox, XDatePicker, XInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { permissionConditions as conditionsApi, permissions as permissionsApi, users as usersApi } from '~/contracts'
import { formatDate } from '~/utils'
import { attributeLabel, operatorLabel } from '../../roles/condition-labels'
import { outcomeLabel, outcomeTone, pathLabel, trialBlocks, verdictLabel, verdictTone } from '../../roles/condition-trial'

/**
 * 条件试算：选人和权限码，填模拟的请求情形，看走了哪条授予路径、每组每个子句取到什么值、结果如何。
 * 按这个人此刻的有效指派与委托判定，不看令牌；不填的情形即取不到，时刻不填即现在。
 */
defineOptions({ name: 'ConditionTrialDrawer' })

const open = defineModel<boolean>('open', { required: true })

interface TrialFormModel {
  userId: null | string
  permission: null | string
  ip: string
  time: null | number
  userAgent: string
  path: string
  method: null | string
  isImpersonating: boolean
}

const { t } = useI18n()
const formId = useId()
const methodOptions = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map(method => ({ value: method, label: method }))

const form = ref<TrialFormModel>(emptyForm())
const userOptions = ref<Array<{ label: string, value: string, description?: string }>>([])
const userLoading = ref(false)
const permissionOptions = ref<Array<{ label: string, value: string, description?: string }>>([])
const running = ref(false)
const result = ref<ConditionTrialResult | null>(null)
const blocks = computed(() => result.value ? trialBlocks(result.value) : [])
let userSeq = 0

function emptyForm(): TrialFormModel {
  return { userId: null, permission: null, ip: '', time: null, userAgent: '', path: '', method: null, isImpersonating: false }
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
    toast.danger((error as Error).message || t('access.conditions.msg_load_failed'))
  }
  finally {
    if (seq === userSeq)
      userLoading.value = false
  }
}

async function loadPermissions() {
  if (permissionOptions.value.length > 0)
    return
  try {
    permissionOptions.value = (await permissionsApi.list({})).items.map(item => ({ label: item.displayName, value: item.code, description: item.code }))
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.conditions.msg_load_failed'))
  }
}

async function run() {
  if (!form.value.userId || !form.value.permission) {
    toast.warning(t('access.conditions.msg_trial_required'))
    return
  }
  running.value = true
  try {
    result.value = await conditionsApi.trial({
      userId: form.value.userId,
      permission: form.value.permission,
      ip: form.value.ip.trim() || null,
      time: form.value.time ? new Date(form.value.time).toISOString() : null,
      userAgent: form.value.userAgent.trim() || null,
      path: form.value.path.trim() || null,
      method: form.value.method,
      isImpersonating: form.value.isImpersonating,
    })
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.conditions.msg_trial_failed'))
  }
  finally {
    running.value = false
  }
}

function setTitle(set: ConditionTrialSet): string {
  const side = set.action === 'Deny' ? t('access.conditions.deny_sets') : t('access.conditions.grant_sets')
  return set.subjectName ? `${side} · ${set.subjectName}` : side
}

function setOutcome(set: ConditionTrialSet): string {
  return set.outcome ? outcomeLabel(t, set.outcome) : t('access.conditions.missing_set')
}

function verdictText(verdict: ConditionVerdict): string {
  return verdictLabel(t, verdict)
}

watch(open, (value) => {
  if (value) {
    void loadPermissions()
    if (userOptions.value.length === 0)
      void searchUsers()
  }
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 1100px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('access.conditions.trial_title') }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body trial">
        <p class="trial__tip">
          {{ t('access.conditions.trial_tip') }}
        </p>
        <XhFormRoot :id="formId" class="xh-edit-form-grid" data-testid="condition-trial-form" @submit="run">
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('access.conditions.label_user') }}</XhFieldLabel>
            <XhFieldControl>
              <XCombobox
                class="trial__combobox"
                remote
                :options="userOptions"
                :value="form.userId"
                :loading="userLoading"
                :placeholder="t('access.conditions.ph_user')"
                @search="searchUsers"
                @update:value="value => (form.userId = typeof value === 'string' ? value : null)"
              />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot required>
            <XhFieldLabel>{{ t('access.conditions.label_permission') }}</XhFieldLabel>
            <XhFieldControl>
              <XCombobox
                class="trial__combobox"
                :options="permissionOptions"
                :value="form.permission"
                :placeholder="t('access.conditions.ph_permission')"
                @update:value="value => (form.permission = typeof value === 'string' ? value : null)"
              />
            </XhFieldControl>
          </XhFieldRoot>
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
            <XhFieldLabel>{{ t('access.conditions.label_user_agent') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.userAgent" name="userAgent" clearable />
            </XhFieldControl>
          </XhFieldRoot>
          <XhFieldRoot>
            <XhFieldLabel>{{ t('access.conditions.label_impersonating') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="form.isImpersonating" name="isImpersonating" />
            </XhFieldControl>
            <XhFieldDescription>{{ t('access.conditions.hint_impersonating') }}</XhFieldDescription>
          </XhFieldRoot>
        </XhFormRoot>
        <div class="trial__actions">
          <XhButton variant="subtle" tone="brand" :loading="running" type="submit" :form="formId">
            <XhButtonIndicator>
              <Icon icon="lucide:play" width="14" />
            </XhButtonIndicator>
            <XhButtonLabel>{{ t('access.conditions.action_run') }}</XhButtonLabel>
          </XhButton>
        </div>

        <section v-if="result" class="trial__result" :aria-label="t('access.conditions.result_title')">
          <header class="trial__verdict">
            <XhTagRoot variant="subtle" :tone="verdictTone(result.verdict)">
              <XhTagLabel>{{ verdictText(result.verdict) }}</XhTagLabel>
            </XhTagRoot>
            <span class="trial__meta">
              {{ t('access.conditions.trial_context', { user: result.userName, ip: result.ip ?? '-', time: formatDate(result.time), zone: result.timeZone }) }}
            </span>
          </header>
          <article v-for="block in blocks" :key="block.key" class="trial-path">
            <header class="trial-path__header">
              <XhTagRoot v-if="block.holds !== null" variant="subtle" size="sm" :tone="block.holds ? 'success' : 'danger'">
                <XhTagLabel>{{ block.holds ? t('access.conditions.path_holds') : t('access.conditions.path_fails') }}</XhTagLabel>
              </XhTagRoot>
              <span class="trial-path__title">{{ pathLabel(t, block.source, block.roleName) }}</span>
              <span v-if="block.hasUnconditionalGrant" class="trial__meta">{{ t('access.conditions.path_unconditional') }}</span>
            </header>
            <div v-for="(set, setIndex) in block.sets" :key="setIndex" class="trial-set">
              <p class="trial-set__title">
                {{ setTitle(set) }}
                <XhTagRoot variant="outline" size="sm" :tone="outcomeTone(set.outcome)">
                  <XhTagLabel>{{ setOutcome(set) }}</XhTagLabel>
                </XhTagRoot>
                <code v-if="set.binding === '*'">*</code>
              </p>
              <p v-for="error in set.errors" :key="error" class="trial__error">
                {{ error }}
              </p>
              <table v-for="group in set.groups" :key="group.key ?? ''" class="xh-detail-table">
                <caption class="trial-set__caption">
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
                  <tr v-for="clause in group.clauses" :key="clause.key ?? clause.attributeName">
                    <td>
                      <span class="trial-clause__attribute">{{ attributeLabel(t, clause.attributeName) }}</span>
                      <code class="trial__meta">{{ clause.attributeName }}</code>
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
            </div>
          </article>
          <p v-if="result.paths.length === 0 && result.denies.length === 0" class="trial__tip">
            {{ t('access.conditions.trial_no_paths') }}
          </p>
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

.trial__error {
  margin: 0;
  color: var(--xh-fg-danger);
  font-size: var(--xh-text-caption-size);
}

.trial__combobox {
  inline-size: 100%;
}

.trial__actions {
  display: flex;
  justify-content: flex-end;
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
  border-radius: var(--xh-shape-inset);
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
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
  margin: 0;
}

.trial-set__caption {
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
  text-align: start;
}

.trial-clause__attribute {
  display: block;
}
</style>
