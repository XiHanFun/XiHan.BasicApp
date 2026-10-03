来源：https://basicapp.docs.xihanfun.com/preview

# 功能预览

<script setup>
const previewImages = import.meta.glob('../assets/preview/*.png', { eager: true, query: '?url', import: 'default' })
</script>

<table class="feature-preview-table">
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/dashboard.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/dashboard.png']" alt="工作台" loading="lazy" /></a><br/>工作台</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/user.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/user.png']" alt="用户管理" loading="lazy" /></a><br/>用户管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-trace.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-trace.png']" alt="日志链路追踪" loading="lazy" /></a><br/>日志链路追踪</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/printing.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/printing.png']" alt="打印模板" loading="lazy" /></a><br/>打印模板</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/control-center.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/control-center.png']" alt="控制中心" loading="lazy" /></a><br/>控制中心</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/login.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/login.png']" alt="登录认证" loading="lazy" /></a><br/>登录认证</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/profile.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/profile.png']" alt="个人中心" loading="lazy" /></a><br/>个人中心</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/profile-security.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/profile-security.png']" alt="个人中心（安全设置）" loading="lazy" /></a><br/>个人中心（安全设置）</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/online-user.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/online-user.png']" alt="在线用户" loading="lazy" /></a><br/>在线用户</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/role.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/role.png']" alt="角色管理" loading="lazy" /></a><br/>角色管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/org.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/org.png']" alt="组织机构" loading="lazy" /></a><br/>组织机构</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/position.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/position.png']" alt="岗位管理" loading="lazy" /></a><br/>岗位管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/permission.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/permission.png']" alt="权限管理" loading="lazy" /></a><br/>权限管理</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/menu.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/menu.png']" alt="菜单管理" loading="lazy" /></a><br/>菜单管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/field-security.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/field-security.png']" alt="字段安全" loading="lazy" /></a><br/>字段安全</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/authorization.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/authorization.png']" alt="授权申请与委托" loading="lazy" /></a><br/>授权申请与委托</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/review.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/review.png']" alt="审批中心" loading="lazy" /></a><br/>审批中心</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/constraint.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/constraint.png']" alt="审批约束" loading="lazy" /></a><br/>审批约束</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/tenant.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/tenant.png']" alt="租户管理" loading="lazy" /></a><br/>租户管理</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/tenant-members.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/tenant-members.png']" alt="租户成员" loading="lazy" /></a><br/>租户成员</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/edition.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/edition.png']" alt="版本套餐" loading="lazy" /></a><br/>版本套餐</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/subscription.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/subscription.png']" alt="我的订阅" loading="lazy" /></a><br/>我的订阅</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/notification.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/notification.png']" alt="通知公告" loading="lazy" /></a><br/>通知公告</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/inbox.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/inbox.png']" alt="我的消息" loading="lazy" /></a><br/>我的消息</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/message-template.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/message-template.png']" alt="消息模板" loading="lazy" /></a><br/>消息模板</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/message-record.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/message-record.png']" alt="邮件短信" loading="lazy" /></a><br/>邮件短信</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/chat.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/chat.png']" alt="在线聊天" loading="lazy" /></a><br/>在线聊天</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/chat-audit.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/chat-audit.png']" alt="聊天审计" loading="lazy" /></a><br/>聊天审计</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/file-library.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/file-library.png']" alt="文件管理" loading="lazy" /></a><br/>文件管理</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/file-storage.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/file-storage.png']" alt="存储配置" loading="lazy" /></a><br/>存储配置</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/export-center.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/export-center.png']" alt="导出中心" loading="lazy" /></a><br/>导出中心</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/dict.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/dict.png']" alt="字典管理" loading="lazy" /></a><br/>字典管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/config.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/config.png']" alt="参数配置" loading="lazy" /></a><br/>参数配置</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/numbering.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/numbering.png']" alt="业务编号" loading="lazy" /></a><br/>业务编号</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/job.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/job.png']" alt="任务调度" loading="lazy" /></a><br/>任务调度</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/cache.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/cache.png']" alt="缓存管理" loading="lazy" /></a><br/>缓存管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/server.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/server.png']" alt="服务监控" loading="lazy" /></a><br/>服务监控</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/version.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/version.png']" alt="版本管理" loading="lazy" /></a><br/>版本管理</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/email-config.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/email-config.png']" alt="邮件配置" loading="lazy" /></a><br/>邮件配置</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/sms-config.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/sms-config.png']" alt="短信配置" loading="lazy" /></a><br/>短信配置</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/bot-config.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/bot-config.png']" alt="机器人配置" loading="lazy" /></a><br/>机器人配置</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/telegram-bot.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/telegram-bot.png']" alt="Telegram 机器人" loading="lazy" /></a><br/>Telegram 机器人</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/openapi-app.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/openapi-app.png']" alt="应用管理" loading="lazy" /></a><br/>应用管理</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/openapi-credentials.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/openapi-credentials.png']" alt="开放接口凭证" loading="lazy" /></a><br/>开放接口凭证</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-access.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-access.png']" alt="访问日志" loading="lazy" /></a><br/>访问日志</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-api.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-api.png']" alt="开放接口日志" loading="lazy" /></a><br/>开放接口日志</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-operation.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-operation.png']" alt="操作日志" loading="lazy" /></a><br/>操作日志</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-login.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-login.png']" alt="登录日志" loading="lazy" /></a><br/>登录日志</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-exception.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-exception.png']" alt="异常日志" loading="lazy" /></a><br/>异常日志</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-diff.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-diff.png']" alt="数据变更日志" loading="lazy" /></a><br/>数据变更日志</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-permission.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-permission.png']" alt="权限变更日志" loading="lazy" /></a><br/>权限变更日志</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-trace-timeline.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-trace-timeline.png']" alt="日志链路时间线" loading="lazy" /></a><br/>日志链路时间线</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-migration.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-migration.png']" alt="升级记录" loading="lazy" /></a><br/>升级记录</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/codegen.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/codegen.png']" alt="代码生成" loading="lazy" /></a><br/>代码生成</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/ai-provider.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/ai-provider.png']" alt="AI 提供商" loading="lazy" /></a><br/>AI 提供商</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/ai-prompt.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/ai-prompt.png']" alt="AI 提示词" loading="lazy" /></a><br/>AI 提示词</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/knowledge.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/knowledge.png']" alt="知识库" loading="lazy" /></a><br/>知识库</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/ai-assistant.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/ai-assistant.png']" alt="AI 助手" loading="lazy" /></a><br/>AI 助手</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/workflow-definition.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/workflow-definition.png']" alt="流程定义" loading="lazy" /></a><br/>流程定义</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/workflow-json.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/workflow-json.png']" alt="流程 JSON 编辑" loading="lazy" /></a><br/>流程 JSON 编辑</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/workflow-instance.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/workflow-instance.png']" alt="流程实例" loading="lazy" /></a><br/>流程实例</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/workflow-todo.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/workflow-todo.png']" alt="我的待办" loading="lazy" /></a><br/>我的待办</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/preferences.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/preferences.png']" alt="偏好设置" loading="lazy" /></a><br/>偏好设置</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/schema-page.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/schema-page.png']" alt="高级列表" loading="lazy" /></a><br/>高级列表</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/editors.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/editors.png']" alt="Markdown 编辑器" loading="lazy" /></a><br/>Markdown 编辑器</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/editor-json.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/editor-json.png']" alt="JSON 编辑器" loading="lazy" /></a><br/>JSON 编辑器</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/editor-rich-text.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/editor-rich-text.png']" alt="富文本编辑器" loading="lazy" /></a><br/>富文本编辑器</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/navigation.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/navigation.png']" alt="全局导航" loading="lazy" /></a><br/>全局导航</td>
  </tr>
</table>

<style scoped>
.feature-preview-table {
  display: table;
  table-layout: fixed;
  width: 100%;
}

.feature-preview-table td {
  width: 50%;
  vertical-align: top;
  text-align: center;
}

.feature-preview-table img {
  display: block;
  width: 100%;
  height: auto;
}
</style>
