<script lang="ts" setup>
import type { NotificationPreference } from '~/types'
import { XhButton, XhSpinner, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'

defineOptions({ name: 'ProfileTabNotifications' })

const { apis } = useAppContext()
const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)
const pref = ref<NotificationPreference>({
  channelInApp: true,
  channelEmail: true,
  channelSms: false,
  channelPush: true,
  channelBot: false,
  typeAnnouncement: true,
  typeTask: true,
  typeApproval: true,
  typeSecurity: true,
  typeMarketing: false,
})

interface PrefItem {
  key: keyof NotificationPreference
  label: string
  desc: string
  icon: string
  marketing?: boolean
}

const channels = computed<PrefItem[]>(() => [
  { key: 'channelInApp', label: t('component.profile.notifications.channel_in_app'), desc: t('component.profile.notifications.channel_in_app_desc'), icon: 'lucide:bell' },
  { key: 'channelEmail', label: t('component.profile.notifications.channel_email'), desc: t('component.profile.notifications.channel_email_desc'), icon: 'lucide:mail' },
  { key: 'channelSms', label: t('component.profile.notifications.channel_sms'), desc: t('component.profile.notifications.channel_sms_desc'), icon: 'lucide:smartphone' },
  { key: 'channelPush', label: t('component.profile.notifications.channel_push'), desc: t('component.profile.notifications.channel_push_desc'), icon: 'lucide:radio' },
  { key: 'channelBot', label: t('component.profile.notifications.channel_bot'), desc: t('component.profile.notifications.channel_bot_desc'), icon: 'lucide:bot' },
])

const types = computed<PrefItem[]>(() => [
  { key: 'typeAnnouncement', label: t('component.profile.notifications.type_announcement'), desc: t('component.profile.notifications.type_announcement_desc'), icon: 'lucide:megaphone' },
  { key: 'typeTask', label: t('component.profile.notifications.type_task'), desc: t('component.profile.notifications.type_task_desc'), icon: 'lucide:check-square' },
  { key: 'typeApproval', label: t('component.profile.notifications.type_approval'), desc: t('component.profile.notifications.type_approval_desc'), icon: 'lucide:file-check' },
  { key: 'typeSecurity', label: t('component.profile.notifications.type_security'), desc: t('component.profile.notifications.type_security_desc'), icon: 'lucide:shield-alert' },
  { key: 'typeMarketing', label: t('component.profile.notifications.type_marketing'), desc: t('component.profile.notifications.type_marketing_desc'), icon: 'lucide:gift', marketing: true },
])

async function loadPreference() {
  loading.value = true
  try {
    pref.value = await apis.getNotificationPreferenceApi()
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.notifications.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

async function savePreference() {
  saving.value = true
  try {
    pref.value = await apis.updateNotificationPreferenceApi(pref.value)
    toast.success(t('component.profile.notifications.msg_saved'))
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.notifications.err_save_failed'))
  }
  finally {
    saving.value = false
  }
}

onMounted(loadPreference)
</script>

<template>
  <div class="pf-tab-body">
    <div class="xh-loading-stage">
      <div v-if="loading" class="xh-loading-stage__veil">
        <XhSpinner />
      </div>
      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              {{ t('component.profile.notifications.section_channels') }}
            </div>
            <div class="pf-section__desc">
              {{ t('component.profile.notifications.section_channels_desc') }}
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-list">
            <div v-for="ch in channels" :key="ch.key" class="pf-list-item">
              <div class="pf-list-icon">
                <Icon :icon="ch.icon" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ ch.label }}
                </div>
                <div class="pf-list-desc">
                  {{ ch.desc }}
                </div>
              </div>
              <XhSwitch v-model:checked="pref[ch.key]" />
            </div>
          </div>
        </div>
      </section>

      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              {{ t('component.profile.notifications.section_types') }}
            </div>
            <div class="pf-section__desc">
              {{ t('component.profile.notifications.section_types_desc') }}
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-list">
            <div v-for="item in types" :key="item.key" class="pf-list-item">
              <div class="pf-list-icon">
                <Icon :icon="item.icon" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ item.label }}
                  <XhTagRoot v-if="item.marketing" variant="subtle" size="sm">
                    <XhTagLabel>
                      {{ t('component.profile.notifications.tag_marketing') }}
                    </XhTagLabel>
                  </XhTagRoot>
                </div>
                <div class="pf-list-desc">
                  {{ item.desc }}
                </div>
              </div>
              <XhSwitch v-model:checked="pref[item.key]" />
            </div>
          </div>
        </div>
        <div class="pf-section__actions">
          <XhButton @click="loadPreference">
            {{ t('common.actions.reset') }}
          </XhButton>
          <XhButton tone="brand" :loading="saving" @click="savePreference">
            <Icon icon="lucide:save" width="16" />
            {{ t('component.profile.notifications.btn_save_preference') }}
          </XhButton>
        </div>
      </section>
    </div>
  </div>
</template>

<style src="./profile-shared.css" />
