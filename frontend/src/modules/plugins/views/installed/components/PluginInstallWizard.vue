<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PluginInspection, PluginRow } from '../use-plugins'
import {
  XhAlertContent,
  XhAlertDescription,
  XhAlertIndicator,
  XhAlertRoot,
  XhButton,
  XhCheckboxGroupIndicator,
  XhCheckboxGroupItem,
  XhCheckboxGroupItemText,
  XhCheckboxGroupRoot,
  XhDescriptionsItem,
  XhDescriptionsLabel,
  XhDescriptionsRoot,
  XhDescriptionsValue,
  XhDialogBody,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogFooter,
  XhDialogHeader,
  XhDialogRoot,
  XhDialogTitle,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
  XhFileUploadDropzone,
  XhFileUploadHiddenInput,
  XhFileUploadRoot,
  XhSeparator,
  XhStepsIndicator,
  XhStepsItem,
  XhStepsList,
  XhStepsRoot,
  XhStepsSeparator,
  XhStepsTitle,
  XhStepsTrigger,
} from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialogWidthStyle, Icon, XInput } from '~/components'
import { plugins as pluginsApi } from '~/contracts'
import { uploadFile } from '~/files'
import { ApiError } from '~/request'
import { formatFileSize } from '~/utils'
import { approvedGrants, grantLabel, isolationLabel } from '../use-plugins'
import PluginContributionsView from './PluginContributionsView.vue'

defineOptions({ name: 'PluginInstallWizard' })

const emit = defineEmits<{
  installed: [plugin: PluginRow]
}>()

const show = defineModel<boolean>('show', { required: true })

const { t } = useI18n()

/** 上传 → 检查报告与批准授予 → 装好 */
type Phase = 'upload' | 'review' | 'done'

const phase = ref<Phase>('upload')
const busy = ref(false)
const problem = ref<string | null>(null)
const fileId = ref<string | null>(null)
const inspection = ref<PluginInspection | null>(null)
const chosenOptional = ref<string[]>([])
const remark = ref('')
const installed = ref<PluginRow | null>(null)

const stepIndex = computed(() => ['upload', 'review', 'done'].indexOf(phase.value))
const steps = computed(() => [
  t('plugins.installed.wizard.step_upload'),
  t('plugins.installed.wizard.step_review'),
  t('plugins.installed.wizard.step_done'),
])

/** 已选文件受控为空：组件只当选择器用，重选不会因数量已满被拒 */
const NO_FILES: File[] = []

watch(show, (value) => {
  if (value) {
    phase.value = 'upload'
    busy.value = false
    problem.value = null
    fileId.value = null
    inspection.value = null
    chosenOptional.value = []
    remark.value = ''
    installed.value = null
  }
})

function messageOf(error: unknown): string {
  return error instanceof ApiError
    ? error.detail
    : (error as Error).message || t('common.messages.operation_failed')
}

/** 选中即上传并核对：核对不过留在这一步，给出后端的原因 */
async function onFileAccept(details: { files: File[] }) {
  const file = details.files[0]
  if (!file || busy.value)
    return
  busy.value = true
  problem.value = null
  try {
    const uploaded = await uploadFile(file)
    inspection.value = await pluginsApi.inspect({ fileId: uploaded.id })
    fileId.value = uploaded.id
    chosenOptional.value = []
    phase.value = 'review'
  }
  catch (error) {
    problem.value = messageOf(error)
  }
  finally {
    busy.value = false
  }
}

function onFileReject() {
  problem.value = t('plugins.installed.wizard.file_rejected')
}

async function install() {
  const report = inspection.value
  if (!report || !fileId.value)
    return
  busy.value = true
  problem.value = null
  try {
    const response = await pluginsApi.install({
      fileId: fileId.value,
      grants: approvedGrants(report.requiredGrants, report.optionalGrants, chosenOptional.value),
      remark: remark.value.trim() || null,
    })
    installed.value = response.plugin
    phase.value = 'done'
    emit('installed', response.plugin)
  }
  catch (error) {
    problem.value = messageOf(error)
  }
  finally {
    busy.value = false
  }
}
</script>

<template>
  <XhDialogRoot
    :open="show"
    :close-on-escape="!busy"
    :close-on-interact-outside="false"
    @update:open="(value: boolean) => (show = value)"
  >
    <XhDialogContent :style="dialogWidthStyle(760)">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('plugins.installed.wizard.title') }}</XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger v-if="!busy" />

      <XhDialogBody>
        <div class="plugin-wizard">
          <XhStepsRoot :count="steps.length" :value="stepIndex" size="sm" read-only :translations="{ list: t('plugins.installed.wizard.title') }">
            <XhStepsList>
              <XhStepsItem v-for="(step, index) in steps" :key="step" :value="index">
                <XhStepsTrigger>
                  <XhStepsIndicator>
                    {{ index + 1 }}
                  </XhStepsIndicator>
                  <XhStepsTitle>{{ step }}</XhStepsTitle>
                </XhStepsTrigger>
                <XhStepsSeparator />
              </XhStepsItem>
            </XhStepsList>
          </XhStepsRoot>

          <XhFileUploadRoot
            v-if="phase === 'upload'"
            accept=".xhplugin,.zip"
            :max-files="1"
            :files="NO_FILES"
            :disabled="busy"
            @file-accept="onFileAccept"
            @file-reject="onFileReject"
          >
            <XhFileUploadDropzone>
              <div class="plugin-wizard__dragger">
                <Icon icon="lucide:package-plus" class="plugin-wizard__dragger-icon" />
                <span>{{ busy ? t('plugins.installed.wizard.inspecting') : t('plugins.installed.wizard.dragger') }}</span>
                <span class="text-caption">{{ t('plugins.installed.wizard.dragger_hint') }}</span>
              </div>
            </XhFileUploadDropzone>
            <XhFileUploadHiddenInput />
          </XhFileUploadRoot>

          <template v-else-if="phase === 'review' && inspection">
            <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('plugins.installed.fields.name') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ inspection.name }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('plugins.installed.fields.version') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ inspection.version }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('plugins.installed.fields.manifest_id') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ inspection.manifestId }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('plugins.installed.fields.publisher') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ inspection.publisher || '-' }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('plugins.installed.fields.isolation') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ isolationLabel(t, inspection.isolation) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('plugins.installed.fields.size') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatFileSize(inspection.size) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('plugins.installed.fields.sha256') }}</XhDescriptionsLabel>
                <XhDescriptionsValue><code class="plugin-wizard__mono">{{ inspection.sha256 }}</code></XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>

            <XhSeparator>{{ t('plugins.installed.sections.contributions') }}</XhSeparator>
            <PluginContributionsView :contributions="inspection.contributions" />

            <XhSeparator>{{ t('plugins.installed.sections.grants') }}</XhSeparator>
            <XhAlertRoot v-if="inspection.contributions.callbacks.length > 0" tone="warning">
              <XhAlertIndicator>
                <Icon icon="lucide:shield-alert" width="16" height="16" />
              </XhAlertIndicator>
              <XhAlertContent>
                <XhAlertDescription>{{ t('plugins.installed.wizard.callbacks_risk', { count: inspection.contributions.callbacks.length }) }}</XhAlertDescription>
              </XhAlertContent>
            </XhAlertRoot>
            <div class="plugin-wizard__grants">
              <p class="text-caption text-fg-muted">
                {{ inspection.requiredGrants.length > 0 ? t('plugins.installed.wizard.required_grants') : t('plugins.installed.wizard.no_required_grants') }}
              </p>
              <ul v-if="inspection.requiredGrants.length > 0" class="plugin-wizard__grant-list">
                <li v-for="grant in inspection.requiredGrants" :key="grant">
                  <code class="plugin-wizard__mono">{{ grant }}</code>
                  <span class="text-caption text-fg-muted">{{ grantLabel(t, grant) }}</span>
                </li>
              </ul>
              <XhFieldRoot v-if="inspection.optionalGrants.length > 0">
                <XhFieldLabel>{{ t('plugins.installed.wizard.optional_grants') }}</XhFieldLabel>
                <XhFieldControl>
                  <XhCheckboxGroupRoot name="optionalGrants" :value="chosenOptional" @update:value="(value: string[]) => (chosenOptional = value)">
                    <XhCheckboxGroupItem v-for="grant in inspection.optionalGrants" :key="grant" :value="grant">
                      <XhCheckboxGroupIndicator />
                      <XhCheckboxGroupItemText>{{ grant }}（{{ grantLabel(t, grant) }}）</XhCheckboxGroupItemText>
                    </XhCheckboxGroupItem>
                  </XhCheckboxGroupRoot>
                </XhFieldControl>
              </XhFieldRoot>
              <XhFieldRoot>
                <XhFieldLabel>{{ t('plugins.installed.fields.remark') }}</XhFieldLabel>
                <XhFieldControl>
                  <XInput v-model:value="remark" name="remark" type="textarea" :rows="2" clearable />
                </XhFieldControl>
              </XhFieldRoot>
            </div>
          </template>

          <XhAlertRoot v-else-if="phase === 'done' && installed" tone="success">
            <XhAlertIndicator>
              <Icon icon="lucide:circle-check" width="16" height="16" />
            </XhAlertIndicator>
            <XhAlertContent>
              <XhAlertDescription>{{ t('plugins.installed.wizard.installed', { name: installed.name, version: installed.version }) }}</XhAlertDescription>
            </XhAlertContent>
          </XhAlertRoot>

          <XhAlertRoot v-if="problem" tone="danger">
            <XhAlertIndicator>
              <Icon icon="lucide:circle-alert" width="16" height="16" />
            </XhAlertIndicator>
            <XhAlertContent>
              <XhAlertDescription>{{ problem }}</XhAlertDescription>
            </XhAlertContent>
          </XhAlertRoot>
        </div>
      </XhDialogBody>

      <XhDialogFooter>
        <template v-if="phase === 'review'">
          <XhButton variant="subtle" size="sm" :disabled="busy" @click="phase = 'upload'">
            {{ t('plugins.installed.wizard.reupload') }}
          </XhButton>
          <XhButton tone="brand" size="sm" :loading="busy" @click="install">
            {{ t('plugins.installed.wizard.install') }}
          </XhButton>
        </template>
        <XhButton v-else variant="subtle" size="sm" :disabled="busy" @click="show = false">
          {{ t('common.actions.close') }}
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.plugin-wizard {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.plugin-wizard__dragger {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  align-items: center;
  padding: var(--xh-space-3) 0;
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-muted);
}

.plugin-wizard__dragger-icon {
  font-size: var(--xh-font-size-4xl);
}

.plugin-wizard__grants {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.plugin-wizard__grant-list {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  margin: 0;
  padding: 0;
  list-style: none;
}

.plugin-wizard__grant-list li {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
  align-items: baseline;
}

.plugin-wizard__mono {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-caption-size);
  word-break: break-all;
}
</style>
