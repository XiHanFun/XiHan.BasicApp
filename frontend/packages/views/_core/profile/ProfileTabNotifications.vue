<script lang="ts" setup>
import type { NotificationPreference } from '~/types'
import { NButton, NSpin, NSwitch, NTag, useMessage } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'

defineOptions({ name: 'ProfileTabNotifications' })

const { apis } = useAppContext()
const message = useMessage()

const loading = ref(false)
const saving = ref(false)
const pref = ref<NotificationPreference>({
  channelInApp: true,
  channelEmail: true,
  channelSms: false,
  channelPush: true,
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

const channels: PrefItem[] = [
  { key: 'channelInApp', label: '站内信', desc: '系统内消息中心通知', icon: 'lucide:bell' },
  { key: 'channelEmail', label: '邮箱通知', desc: '通过邮件接收重要消息', icon: 'lucide:mail' },
  { key: 'channelSms', label: '短信通知', desc: '通过短信接收高优先级提醒', icon: 'lucide:smartphone' },
  { key: 'channelPush', label: '推送通知', desc: '浏览器/客户端实时推送', icon: 'lucide:radio' },
]

const types: PrefItem[] = [
  { key: 'typeAnnouncement', label: '系统公告', desc: '平台维护、版本更新等公告', icon: 'lucide:megaphone' },
  { key: 'typeTask', label: '任务提醒', desc: '待办任务与到期提醒', icon: 'lucide:check-square' },
  { key: 'typeApproval', label: '审批通知', desc: '审批待办与结果通知', icon: 'lucide:file-check' },
  { key: 'typeSecurity', label: '安全告警', desc: '异常登录、密码变更等安全事件', icon: 'lucide:shield-alert' },
  { key: 'typeMarketing', label: '营销消息', desc: '产品推广与优惠活动（可随时关闭）', icon: 'lucide:gift', marketing: true },
]

async function loadPreference() {
  loading.value = true
  try {
    pref.value = await apis.getNotificationPreferenceApi()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载通知偏好失败')
  }
  finally {
    loading.value = false
  }
}

async function savePreference() {
  saving.value = true
  try {
    pref.value = await apis.updateNotificationPreferenceApi(pref.value)
    message.success('通知偏好已保存')
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '保存失败')
  }
  finally {
    saving.value = false
  }
}

onMounted(loadPreference)
</script>

<template>
  <div class="pf-tab-body">
    <NSpin :show="loading">
      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              接收渠道
            </div>
            <div class="pf-section__desc">
              选择接收通知的方式
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
              <NSwitch v-model:value="pref[ch.key]" />
            </div>
          </div>
        </div>
      </section>

      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              通知类型
            </div>
            <div class="pf-section__desc">
              按消息类型订阅或退订
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-list">
            <div v-for="t in types" :key="t.key" class="pf-list-item">
              <div class="pf-list-icon">
                <Icon :icon="t.icon" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ t.label }}
                  <NTag v-if="t.marketing" size="tiny" :bordered="false">
                    营销
                  </NTag>
                </div>
                <div class="pf-list-desc">
                  {{ t.desc }}
                </div>
              </div>
              <NSwitch v-model:value="pref[t.key]" />
            </div>
          </div>
        </div>
        <div class="pf-section__actions">
          <NButton @click="loadPreference">
            重置
          </NButton>
          <NButton type="primary" :loading="saving" @click="savePreference">
            <template #icon>
              <Icon icon="lucide:save" width="16" />
            </template>
            保存偏好
          </NButton>
        </div>
      </section>
    </NSpin>
  </div>
</template>

<style src="./profile-shared.css" />
