<script setup lang="ts">
import type { LoginAutoBanSettings } from '../access-policy'
import type { ConfigDetailDto, LoginBanDto } from '@/api'
import type { XDataTableColumn } from '~/components'
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
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhSwitch,
} from '@xihan-ui/vue'
import { computed, h, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { accessPolicyApi, configApi, createPageRequest } from '@/api'
import { Icon, XDataTable, XNumberInput } from '~/components'
import { dialog, toast } from '~/composables'
import { formatDate } from '~/utils'
import { LOGIN_AUTO_BAN_CONFIG_KEY, readAutoBanSettings, writeAutoBanSettings } from '../access-policy'

/**
 * 自动封禁：登录失败过多被暂时封禁的 IP，可手动解封；有参数配置编辑权限时在这里改封禁参数。只在平台上下文打开
 */
defineOptions({ name: 'LoginBanDrawer' })

const props = defineProps<{
  canUnban: boolean
  canEditSettings: boolean
}>()

const open = defineModel<boolean>('open', { required: true })

/** 表单里的数值项清空时为 null */
type AutoBanForm = { [K in keyof LoginAutoBanSettings]: LoginAutoBanSettings[K] extends number ? number | null : LoginAutoBanSettings[K] }

const { t } = useI18n()
const settingsFormId = useId()
const bans = ref<LoginBanDto[]>([])
const loading = ref(false)
const config = ref<ConfigDetailDto | null>(null)
const settings = ref<AutoBanForm | null>(null)
const settingsUnreadable = ref(false)
const settingsLoading = ref(false)
const settingsSaving = ref(false)

const columns = computed<XDataTableColumn<LoginBanDto>[]>(() => [
  { key: 'ip', title: t('setting.access_policy.bans.th_ip'), render: row => h('code', row.ip) },
  { key: 'until', title: t('setting.access_policy.bans.th_until'), render: row => formatDate(row.until) },
  { key: 'remainingSeconds', title: t('setting.access_policy.bans.th_remaining'), render: row => t('setting.access_policy.bans.remaining', { minutes: Math.ceil(row.remainingSeconds / 60) }) },
  ...(props.canUnban
    ? [{
        key: 'actions',
        title: t('setting.access_policy.bans.th_actions'),
        render: (row: LoginBanDto) => h(XhButton, { variant: 'ghost', size: 'sm', tone: 'danger', onClick: () => confirmUnban(row) }, () => h(XhButtonLabel, () => t('setting.access_policy.bans.unban'))),
      }]
    : []),
])

async function load() {
  loading.value = true
  try {
    bans.value = await accessPolicyApi.bans()
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.bans.load_failed'))
  }
  finally {
    loading.value = false
  }
}

async function loadSettings() {
  settingsLoading.value = true
  try {
    const page = await configApi.page({ ...createPageRequest({ page: { pageIndex: 1, pageSize: 20 } }), keyword: LOGIN_AUTO_BAN_CONFIG_KEY })
    const item = page.items.find(row => row.configKey === LOGIN_AUTO_BAN_CONFIG_KEY)
    config.value = item ? await configApi.detail(item.basicId) : null
    settings.value = config.value ? readAutoBanSettings(config.value.configValue, config.value.defaultValue) : null
    settingsUnreadable.value = settings.value === null
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.bans.settings_load_failed'))
  }
  finally {
    settingsLoading.value = false
  }
}

async function saveSettings() {
  const current = config.value
  const form = settings.value
  if (!current || !form) {
    return
  }
  const { windowMinutes, maxFailures, banMinutes, maxBanMinutes } = form
  if (windowMinutes === null || maxFailures === null || banMinutes === null || maxBanMinutes === null) {
    toast.warning(t('setting.access_policy.bans.settings_incomplete'))
    return
  }
  settingsSaving.value = true
  try {
    config.value = await configApi.update({
      basicId: current.basicId,
      configDescription: current.configDescription,
      configGroup: current.configGroup,
      configName: current.configName,
      configType: current.configType,
      configValue: writeAutoBanSettings({ isEnabled: form.isEnabled, windowMinutes, maxFailures, banMinutes, maxBanMinutes }),
      dataType: current.dataType,
      defaultValue: current.defaultValue,
      isEncrypted: current.isEncrypted,
      remark: current.remark,
      sort: current.sort,
    })
    toast.success(t('setting.access_policy.bans.settings_saved'))
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.bans.settings_save_failed'))
  }
  finally {
    settingsSaving.value = false
  }
}

function confirmUnban(row: LoginBanDto) {
  void dialog.confirm({
    badge: 'warning',
    title: t('setting.access_policy.bans.unban'),
    content: t('setting.access_policy.bans.confirm_unban', { ip: row.ip }),
    okText: t('setting.access_policy.bans.unban'),
    cancelText: t('setting.access_policy.cancel'),
    onOk: async () => {
      try {
        await accessPolicyApi.unban({ ip: row.ip })
        toast.success(t('setting.access_policy.bans.unbanned', { ip: row.ip }))
        await load()
      }
      catch (error) {
        toast.danger((error as Error).message || t('setting.access_policy.bans.unban_failed'))
      }
    },
  })
}

watch(open, (value) => {
  if (value) {
    void load()
    if (props.canEditSettings) {
      void loadSettings()
    }
  }
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 640px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('setting.access_policy.bans.title') }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body login-bans">
        <p class="login-bans__tip">
          {{ t('setting.access_policy.bans.tip') }}
        </p>
        <section class="login-bans__section">
          <h3 class="login-bans__heading">
            {{ t('setting.access_policy.bans.settings_title') }}
          </h3>
          <p v-if="!canEditSettings" class="login-bans__tip">
            {{ t('setting.access_policy.bans.settings_readonly_hint', { key: LOGIN_AUTO_BAN_CONFIG_KEY }) }}
          </p>
          <p v-else-if="settingsUnreadable" class="login-bans__tip">
            {{ t('setting.access_policy.bans.settings_unreadable', { key: LOGIN_AUTO_BAN_CONFIG_KEY }) }}
          </p>
          <XhFormRoot v-else-if="settings" :id="settingsFormId" class="xh-edit-form-grid" @submit="saveSettings">
            <XhFormFieldGroup name="isEnabled">
              <XhFieldRoot>
                <XhFieldLabel>{{ t('setting.access_policy.bans.is_enabled') }}</XhFieldLabel>
                <XhFieldControl>
                  <XhSwitch v-model:checked="settings.isEnabled" />
                </XhFieldControl>
              </XhFieldRoot>
            </XhFormFieldGroup>
            <XhFormFieldGroup name="windowMinutes">
              <XhFieldRoot>
                <XhFieldLabel>{{ t('setting.access_policy.bans.window_minutes') }}</XhFieldLabel>
                <XhFieldControl>
                  <XNumberInput v-model:value="settings.windowMinutes" :min="1" />
                </XhFieldControl>
              </XhFieldRoot>
            </XhFormFieldGroup>
            <XhFormFieldGroup name="maxFailures">
              <XhFieldRoot>
                <XhFieldLabel>{{ t('setting.access_policy.bans.max_failures') }}</XhFieldLabel>
                <XhFieldControl>
                  <XNumberInput v-model:value="settings.maxFailures" :min="1" />
                </XhFieldControl>
              </XhFieldRoot>
            </XhFormFieldGroup>
            <XhFormFieldGroup name="banMinutes">
              <XhFieldRoot>
                <XhFieldLabel>{{ t('setting.access_policy.bans.ban_minutes') }}</XhFieldLabel>
                <XhFieldControl>
                  <XNumberInput v-model:value="settings.banMinutes" :min="1" />
                </XhFieldControl>
              </XhFieldRoot>
            </XhFormFieldGroup>
            <XhFormFieldGroup name="maxBanMinutes">
              <XhFieldRoot>
                <XhFieldLabel>{{ t('setting.access_policy.bans.max_ban_minutes') }}</XhFieldLabel>
                <XhFieldControl>
                  <XNumberInput v-model:value="settings.maxBanMinutes" :min="1" />
                </XhFieldControl>
              </XhFieldRoot>
            </XhFormFieldGroup>
          </XhFormRoot>
          <div v-if="canEditSettings && settings">
            <XhButton type="submit" :form="settingsFormId" size="sm" :loading="settingsSaving || settingsLoading">
              <XhButtonLabel>{{ t('setting.access_policy.bans.settings_save') }}</XhButtonLabel>
            </XhButton>
          </div>
        </section>
        <section class="login-bans__section">
          <h3 class="login-bans__heading">
            {{ t('setting.access_policy.bans.current_title') }}
          </h3>
          <div>
            <XhButton variant="outline" size="sm" :loading="loading" @click="load">
              <XhButtonIndicator>
                <Icon icon="lucide:refresh-cw" width="14" />
              </XhButtonIndicator>
              <XhButtonLabel>{{ t('setting.access_policy.bans.refresh') }}</XhButtonLabel>
            </XhButton>
          </div>
          <XDataTable
            :columns="columns"
            :data="bans"
            row-key="ip"
            size="sm"
            :loading="loading"
            :empty-text="t('setting.access_policy.bans.empty')"
          />
        </section>
      </XhDrawerBody>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
.login-bans,
.login-bans__section {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.login-bans__tip {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.login-bans__heading {
  margin: 0;
  font-size: var(--xh-text-body-size);
  font-weight: var(--xh-font-weight-semibold);
}
</style>
