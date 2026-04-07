<script lang="ts" setup>
import { NAlert, NCard, NSwitch, NTag } from 'naive-ui'
import { ref } from 'vue'
import { Icon } from '~/iconify'

interface NotifyChannel { key: string, label: string, desc: string, icon: string, enabled: boolean, marketing?: boolean }

const notifyChannels = ref<NotifyChannel[]>([
  { key: 'email_security', label: '安全警报邮件', desc: '登录异常、密码修改等安全事件通知', icon: 'lucide:shield-alert', enabled: true },
  { key: 'email_system', label: '系统通知邮件', desc: '账户变更、服务状态等系统消息', icon: 'lucide:mail', enabled: true },
  { key: 'sms_security', label: '安全短信通知', desc: '异地登录、高危操作短信提醒', icon: 'lucide:smartphone', enabled: false },
  { key: 'sms_system', label: '系统短信通知', desc: '重要系统公告短信推送', icon: 'lucide:message-square', enabled: false },
  { key: 'in_app', label: '站内消息', desc: '系统内消息中心通知', icon: 'lucide:bell', enabled: true },
  { key: 'email_marketing', label: '营销与推广', desc: '产品更新、优惠活动等推广信息', icon: 'lucide:megaphone', enabled: false, marketing: true },
])
</script>

<template>
  <div class="pf-tab-body">
    <NAlert type="info" :bordered="false">通知偏好设置将在后端接口就绪后生效。营销类通知您可随时关闭（GDPR 合规）。</NAlert>
    <NCard :bordered="false" size="small" class="pf-card">
      <template #header>
        <div class="pf-card-header"><Icon icon="lucide:bell-ring" width="16" /><span>通知渠道</span></div>
      </template>
      <div class="pf-list">
        <div v-for="ch in notifyChannels" :key="ch.key" class="pf-list-item">
          <div class="pf-list-icon"><Icon :icon="ch.icon" width="16" /></div>
          <div class="pf-list-body">
            <div class="pf-list-title">{{ ch.label }} <NTag v-if="ch.marketing" size="tiny" :bordered="false">营销</NTag></div>
            <div class="pf-list-desc">{{ ch.desc }}</div>
          </div>
          <NSwitch v-model:value="ch.enabled" />
        </div>
      </div>
    </NCard>
  </div>
</template>

<style src="./profile-shared.css" />
