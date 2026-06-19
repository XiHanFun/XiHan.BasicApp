<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { NCard, NInputNumber, NSelect, NSwitch } from 'naive-ui'
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
    </NCard>

    <!-- 同步 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.general.sync_title') }}
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.preference_sync') }}</span>
          <PrefTip :content="t('preference.general.preference_sync_tip')" />
        </div>
        <NSwitch v-model:value="appStore.preferenceSyncEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.favorites_sync') }}</span>
          <PrefTip :content="t('preference.general.favorites_sync_tip')" />
        </div>
        <NSwitch v-model:value="appStore.favoritesSyncEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.search_sync') }}</span>
          <PrefTip :content="t('preference.general.search_sync_tip')" />
        </div>
        <NSwitch v-model:value="appStore.searchSyncEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.table_sync') }}</span>
          <PrefTip :content="t('preference.general.table_sync_tip')" />
        </div>
        <NSwitch v-model:value="appStore.tableSyncEnabled" />
      </div>
    </NCard>

    <!-- 更新 -->
    <NCard size="small" :bordered="false">
      <div class="section-title">
        {{ t('preference.general.update_title') }}
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.check_updates') }}</span>
          <PrefTip :content="t('preference.general.check_updates_tip')" />
        </div>
        <NSwitch v-model:value="appStore.enableCheckUpdates" />
      </div>
      <div v-if="appStore.enableCheckUpdates" class="pref-row">
        <span>{{ t('preference.general.check_updates_interval') }}</span>
        <div class="flex items-center gap-1">
          <NInputNumber
            v-model:value="appStore.checkUpdatesInterval"
            :min="10"
            :max="300"
            :step="10"
            size="small"
            :input-props="{ style: 'text-align: center' }"
            style="width: 90px"
          />
          <span class="unit-label">{{ t('preference.general.check_updates_interval_unit') }}</span>
        </div>
      </div>
    </NCard>
  </div>
</template>
