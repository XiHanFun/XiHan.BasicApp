<script setup lang="ts">
import type { ResponseOf } from '~/contracts'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton, XhButtonIndicator, XhButtonLabel, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerBody, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerHeader, XhDrawerRoot, XhDrawerTitle, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/components'
import { dialog, toast } from '~/composables'
import { loginBans as loginBansApi } from '~/contracts'
import { formatDate } from '~/utils'

/**
 * 登录失败自动封禁（只在平台，4.x 同）：此刻生效的参数（在参数配置里改，缺省关）与当前封禁的地址，可解封。
 */
defineOptions({ name: 'LoginBanDrawer' })

defineProps<{
  /** 是否能解封 */
  canLift: boolean
}>()

const open = defineModel<boolean>('open', { required: true })

const { t } = useI18n()
const loading = ref(false)
const result = ref<ResponseOf<'login-bans.list'> | null>(null)

async function load() {
  loading.value = true
  try {
    result.value = await loginBansApi.list({})
  }
  catch (error) {
    toast.danger((error as Error).message || t('access.policies.bans.load_failed'))
  }
  finally {
    loading.value = false
  }
}

function lift(ip: string) {
  void dialog.confirm({
    badge: 'warning',
    title: t('access.policies.bans.lift'),
    content: t('access.policies.bans.confirm_lift', { ip }),
    okText: t('access.policies.bans.lift'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await loginBansApi.lift({ ip })
        toast.success(t('access.policies.bans.lifted', { ip }))
        await load()
      }
      catch (error) {
        toast.danger((error as Error).message || t('access.policies.bans.lift_failed'))
      }
    },
  })
}

/** 剩余时间按分钟向上取整 */
function remainingMinutes(seconds: number) {
  return Math.max(1, Math.ceil(seconds / 60))
}

watch(open, (value) => {
  if (value)
    void load()
})
</script>

<template>
  <XhDrawerRoot v-model:open="open" side="right">
    <XhDrawerContent style="--xh-drawer-size: 720px">
      <XhDrawerHeader>
        <XhDrawerTitle>{{ t('access.policies.bans.title') }}</XhDrawerTitle>
      </XhDrawerHeader>
      <XhDrawerCloseTrigger />
      <XhDrawerBody class="xh-overlay-body policy-bans" data-testid="login-ban-drawer">
        <p class="policy-bans__tip">
          {{ t('access.policies.bans.tip') }}
        </p>
        <template v-if="result">
          <XhAlertRoot v-if="result.settings.error" tone="danger" variant="subtle">
            <XhAlertIndicator>
              <Icon icon="lucide:circle-alert" width="16" />
            </XhAlertIndicator>
            <XhAlertContent>
              <XhAlertDescription>{{ result.settings.error }}</XhAlertDescription>
            </XhAlertContent>
          </XhAlertRoot>
          <section class="policy-bans__section" :aria-label="t('access.policies.bans.settings_title')">
            <header class="policy-bans__head">
              <span class="policy-bans__title">{{ t('access.policies.bans.settings_title') }}</span>
              <XhTagRoot variant="subtle" size="sm" :tone="result.settings.isEnabled ? 'success' : 'neutral'">
                <XhTagLabel>{{ result.settings.isEnabled ? t('access.policies.bans.enabled') : t('access.policies.bans.disabled') }}</XhTagLabel>
              </XhTagRoot>
            </header>
            <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('access.policies.bans.window_minutes') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ result.settings.windowMinutes }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('access.policies.bans.max_failures') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ result.settings.maxFailures }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('access.policies.bans.ban_minutes') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ result.settings.banMinutes }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('access.policies.bans.max_ban_minutes') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ result.settings.maxBanMinutes }}</XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
            <p class="policy-bans__tip">
              {{ t('access.policies.bans.settings_hint', { key: result.settings.configKey }) }}
            </p>
          </section>
          <section class="policy-bans__section" :aria-label="t('access.policies.bans.current_title')">
            <header class="policy-bans__head">
              <span class="policy-bans__title">{{ t('access.policies.bans.current_title') }}</span>
              <XhButton variant="ghost" size="sm" :loading="loading" @click="load">
                <XhButtonIndicator>
                  <Icon icon="lucide:refresh-cw" width="14" />
                </XhButtonIndicator>
                <XhButtonLabel>{{ t('access.policies.bans.refresh') }}</XhButtonLabel>
              </XhButton>
            </header>
            <table v-if="result.items.length > 0" class="xh-detail-table">
              <thead>
                <tr>
                  <th>{{ t('access.policies.bans.th_ip') }}</th>
                  <th>{{ t('access.policies.bans.th_until') }}</th>
                  <th>{{ t('access.policies.bans.th_remaining') }}</th>
                  <th v-if="canLift">
                    {{ t('access.policies.bans.th_actions') }}
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="ban in result.items" :key="ban.ip">
                  <td><code>{{ ban.ip }}</code></td>
                  <td>{{ formatDate(ban.until) }}</td>
                  <td>{{ t('access.policies.bans.remaining', { minutes: remainingMinutes(ban.remainingSeconds) }) }}</td>
                  <td v-if="canLift">
                    <XhButton variant="ghost" size="sm" tone="danger" @click="lift(ban.ip)">
                      <XhButtonLabel>{{ t('access.policies.bans.lift') }}</XhButtonLabel>
                    </XhButton>
                  </td>
                </tr>
              </tbody>
            </table>
            <p v-else class="policy-bans__tip">
              {{ t('access.policies.bans.empty') }}
            </p>
          </section>
        </template>
      </XhDrawerBody>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>

<style scoped>
.policy-bans,
.policy-bans__section {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.policy-bans__tip {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.policy-bans__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.policy-bans__title {
  flex: 1;
  font-weight: var(--xh-font-weight-medium);
}
</style>
