<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NInput, NSelect, NSlider, NSwitch } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useLocale } from '~/hooks'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceGeneralTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore
const { t } = useI18n()
const { setLocale } = useLocale()

const localeOptions = [
  { label: '简体中文', value: 'zh-CN' },
  { label: 'English', value: 'en-US' },
]
</script>

<template>
  <div class="space-y-4">
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.general.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.general.language') }}</span>
        <NSelect
          v-model:value="appStore.locale"
          :options="localeOptions"
          size="small"
          style="width: 110px"
          @update:value="(v) => setLocale(String(v))"
        />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.dynamic_title') }}</span>
          <PrefTip :content="t('preference.general.dynamic_title_tip')" />
        </div>
        <NSwitch v-model:value="appStore.dynamicTitle" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.frosted_glass') }}</span>
          <PrefTip :content="t('preference.general.frosted_glass_tip')" />
        </div>
        <NSwitch v-model:value="appStore.frostedGlassEnabled" />
      </div>
      <div v-if="appStore.frostedGlassEnabled" class="pref-row">
        <span>{{ t('preference.general.frosted_glass_intensity') }}</span>
        <NSlider
          v-model:value="appStore.frostedGlassIntensity"
          :min="1"
          :max="100"
          :step="1"
          :tooltip="true"
          style="width: 150px"
        />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.watermark') }}</span>
          <PrefTip :content="t('preference.general.watermark_tip')" />
        </div>
        <NSwitch v-model:value="appStore.watermarkEnabled" />
      </div>
      <div v-if="appStore.watermarkEnabled" class="mb-2 pref-row">
        <span>{{ t('preference.general.watermark_content') }}</span>
        <NInput
          v-model:value="appStore.watermarkText"
          size="small"
          style="width: 150px"
          :placeholder="t('preference.general.watermark_text')"
        />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.check_updates') }}</span>
          <PrefTip :content="t('preference.general.check_updates_tip')" />
        </div>
        <NSwitch v-model:value="appStore.enableCheckUpdates" />
      </div>
    </NCard>
  </div>
</template>
