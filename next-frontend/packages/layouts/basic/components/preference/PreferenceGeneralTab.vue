<script setup lang="ts">
import type { useAppStore } from '~/stores'
import { XhSwitch } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { XNumberInput } from '~/components'
import LocaleSwitcher from '~/components/common/LocaleSwitcher.vue'
import TimezoneSwitcher from '~/components/common/TimezoneSwitcher.vue'
import PrefTip from './PrefTip.vue'

defineOptions({ name: 'PreferenceGeneralTab' })
const props = defineProps<{ appStore: ReturnType<typeof useAppStore> }>()
const appStore = props.appStore
const { t } = useI18n()
</script>

<template>
  <div class="space-y-4">
    <section class="pref-card">
      <div class="section-title">
        {{ t('preference.general.title') }}
      </div>
      <div class="pref-row">
        <span>{{ t('preference.general.language') }}</span>
        <LocaleSwitcher variant="select" apply size="sm" :select-width="130" />
      </div>
      <div class="pref-row">
        <span>{{ t('preference.general.timezone') }}</span>
        <TimezoneSwitcher variant="select" apply size="sm" :select-width="190" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.dynamic_title') }}</span>
          <PrefTip :content="t('preference.general.dynamic_title_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.dynamicTitle" />
      </div>
    </section>

    <!-- 同步 -->
    <section class="pref-card">
      <div class="section-title">
        {{ t('preference.general.sync_title') }}
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.preference_sync') }}</span>
          <PrefTip :content="t('preference.general.preference_sync_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.preferenceSyncEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.widgets_sync') }}</span>
          <PrefTip :content="t('preference.general.widgets_sync_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.widgetsSyncEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.favorites_sync') }}</span>
          <PrefTip :content="t('preference.general.favorites_sync_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.favoritesSyncEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.search_sync') }}</span>
          <PrefTip :content="t('preference.general.search_sync_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.searchSyncEnabled" />
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.table_sync') }}</span>
          <PrefTip :content="t('preference.general.table_sync_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.tableSyncEnabled" />
      </div>
    </section>

    <!-- 更新 -->
    <section class="pref-card">
      <div class="section-title">
        {{ t('preference.general.update_title') }}
      </div>
      <div class="pref-row">
        <div class="flex gap-1 items-center">
          <span>{{ t('preference.general.check_updates') }}</span>
          <PrefTip :content="t('preference.general.check_updates_tip')" />
        </div>
        <XhSwitch v-model:checked="appStore.enableCheckUpdates" />
      </div>
      <div v-if="appStore.enableCheckUpdates" class="pref-row">
        <span>{{ t('preference.general.check_updates_interval') }}</span>
        <div class="flex items-center gap-1">
          <XNumberInput
            v-model:value="appStore.checkUpdatesInterval"
            :min="10"
            :max="300"
            :step="10"
            size="sm"
            :input-props="{ style: 'text-align: center' }"
            style="width: 90px"
          />
          <span class="unit-label">{{ t('preference.general.check_updates_interval_unit') }}</span>
        </div>
      </div>
    </section>
  </div>
</template>
