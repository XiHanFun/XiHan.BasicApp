<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PluginDetail, PluginVersion } from '../use-plugins'
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
  XhDialogBody,
  XhDialogCloseTrigger,
  XhDialogContent,
  XhDialogDescription,
  XhDialogFooter,
  XhDialogHeader,
  XhDialogRoot,
  XhDialogTitle,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialogWidthStyle, Icon } from '~/components'
import { toast } from '~/composables'
import { plugins as pluginsApi } from '~/contracts'
import { ApiError } from '~/request'
import { approvedGrants, carriedOptional, grantLabel, signatureLabel, signatureTone } from '../use-plugins'

defineOptions({ name: 'PluginRollbackDialog' })

const props = defineProps<{
  detail: PluginDetail | null
  version: PluginVersion | null
}>()

const emit = defineEmits<{
  switched: []
}>()

const show = defineModel<boolean>('show', { required: true })

const { t } = useI18n()

const busy = ref(false)
const problem = ref<string | null>(null)
const chosenOptional = ref<string[]>([])

watch(show, (value) => {
  if (value && props.detail) {
    busy.value = false
    problem.value = null
    chosenOptional.value = carriedOptional(props.detail.optionalGrants, props.detail.grants)
  }
})

async function confirm() {
  const detail = props.detail
  const version = props.version
  if (!detail || !version)
    return
  busy.value = true
  problem.value = null
  try {
    await pluginsApi.rollback({
      id: detail.plugin.id,
      concurrencyStamp: detail.plugin.concurrencyStamp,
      versionId: version.id,
      grants: approvedGrants(version.requiredGrants, detail.optionalGrants, chosenOptional.value),
    })
    toast.success(t('plugins.installed.rollback.switched', { version: version.version }))
    show.value = false
    emit('switched')
  }
  catch (error) {
    problem.value = error instanceof ApiError
      ? error.detail
      : (error as Error).message || t('common.messages.operation_failed')
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
    <XhDialogContent :style="dialogWidthStyle(560)">
      <XhDialogHeader>
        <XhDialogTitle>{{ t('plugins.installed.rollback.title', { version: version?.version ?? '' }) }}</XhDialogTitle>
        <XhDialogDescription>{{ t('plugins.installed.rollback.summary', { version: version?.version ?? '' }) }}</XhDialogDescription>
      </XhDialogHeader>
      <XhDialogCloseTrigger v-if="!busy" />

      <XhDialogBody>
        <div v-if="detail && version" class="plugin-rollback">
          <div class="plugin-rollback__signature">
            <span class="text-caption text-fg-muted">{{ t('plugins.installed.fields.signature') }}</span>
            <XhTagRoot variant="subtle" size="sm" :tone="signatureTone(version.signature.status)">
              <XhTagLabel>{{ signatureLabel(t, version.signature.status) }}</XhTagLabel>
            </XhTagRoot>
          </div>
          <p class="text-caption text-fg-muted">
            {{ version.requiredGrants.length > 0 ? t('plugins.installed.wizard.required_grants') : t('plugins.installed.wizard.no_required_grants') }}
          </p>
          <ul v-if="version.requiredGrants.length > 0" class="plugin-rollback__grants">
            <li v-for="grant in version.requiredGrants" :key="grant">
              <code>{{ grant }}</code>
              <span class="text-caption text-fg-muted">{{ grantLabel(t, grant) }}</span>
            </li>
          </ul>
          <XhFieldRoot v-if="detail.optionalGrants.length > 0">
            <XhFieldLabel>{{ t('plugins.installed.wizard.optional_grants') }}</XhFieldLabel>
            <XhFieldControl>
              <XhCheckboxGroupRoot name="optionalGrants" :value="chosenOptional" :disabled="busy" @update:value="(value: string[]) => (chosenOptional = value)">
                <XhCheckboxGroupItem v-for="grant in detail.optionalGrants" :key="grant" :value="grant">
                  <XhCheckboxGroupIndicator />
                  <XhCheckboxGroupItemText>{{ grant }}（{{ grantLabel(t, grant) }}）</XhCheckboxGroupItemText>
                </XhCheckboxGroupItem>
              </XhCheckboxGroupRoot>
            </XhFieldControl>
          </XhFieldRoot>
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
        <XhButton variant="subtle" size="sm" :disabled="busy" @click="show = false">
          {{ t('common.actions.cancel') }}
        </XhButton>
        <XhButton tone="brand" size="sm" :loading="busy" @click="confirm">
          {{ t('plugins.installed.rollback.confirm') }}
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.plugin-rollback {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
}

.plugin-rollback__signature {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
}

.plugin-rollback__grants {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  margin: 0;
  padding: 0;
  list-style: none;
}

.plugin-rollback__grants li {
  display: flex;
  flex-wrap: wrap;
  gap: var(--xh-space-2);
  align-items: baseline;
}

.plugin-rollback__grants code {
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-caption-size);
  word-break: break-all;
}
</style>
