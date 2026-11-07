# 功能预览

<script setup>
const previewImages = import.meta.glob('../assets/preview/*.png', { eager: true, query: '?url', import: 'default' })
</script>

本页说明界面截图与在本机查看各个页面的方法，适用于希望直观了解功能的读者。逐项能力见[功能清单](./features)。

<table class="feature-preview-table">
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/theme-light-dark.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/theme-light-dark.png']" alt="亮 / 暗双主题" loading="lazy" /></a><br/><b>亮 / 暗双主题</b><br/>每个页面、每个组件逐一对过色</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/theme-colors.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/theme-colors.png']" alt="一个色值生成整套配色" loading="lazy" /></a><br/><b>一个色值生成整套配色</b><br/>Material You 动态取色，21 个中国传统色预设</td>
  </tr>
  <tr>
    <td align="center" colspan="2"><a :href="previewImages['../assets/preview/preference-center.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/preference-center.png']" alt="偏好中心" loading="lazy" /></a><br/><b>偏好中心</b><br/>布局、配色、密度、快捷键与云端同步一处调好，另一台设备实时生效</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/schema-list.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/schema-list.png']" alt="Schema 驱动列表页" loading="lazy" /></a><br/><b>Schema 驱动列表页</b><br/>行悬停速览、高级搜索、列设置开箱即用</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/command-palette.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/command-palette.png']" alt="命令面板式全局搜索" loading="lazy" /></a><br/><b>命令面板式全局搜索</b><br/><code>Ctrl / ⌘ + K</code> 呼出，拼音首字母直达页面与操作</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/split-view.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/split-view.png']" alt="应用内分屏" loading="lazy" /></a><br/><b>应用内分屏</b><br/>两个页面左右并排，互换不重载</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/control-center.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/control-center.png']" alt="多租户控制中心" loading="lazy" /></a><br/><b>多租户控制中心</b><br/>平台管理与各租户之间一处切换</td>
  </tr>
  <tr>
    <td align="center" colspan="2"><a :href="previewImages['../assets/preview/mobile.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/mobile.png']" alt="小屏适配" loading="lazy" /></a><br/><b>小屏适配</b><br/>登录、工作台、抽屉菜单、命令面板与列表页，手机浏览器打开即可使用</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/dashboard.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/dashboard.png']" alt="工作台" loading="lazy" /></a><br/><b>工作台</b><br/>小组件拖拽排布，看板布局云端保存</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-trace.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-trace.png']" alt="一个 TraceId 看全链路" loading="lazy" /></a><br/><b>一个 TraceId 看全链路</b><br/>八类日志串成时间线，桑基图看流向</td>
  </tr>
</table>

页面背后的数据都来自后端的操作，菜单与按钮按当前主体的权限码门控。

## 本地查看

按[快速开始](./getting-started)启动后端、执行 `seed`（开发环境写入演示数据）、启动前端，用 `superadmin` 登录，之后可按下表查看各页面：

| 想看 | 页面路由 | 看点 |
| --- | --- | --- |
| 工作台 | 登录后的首页 `/workbench/dashboard` | 公告轮播、今日统计与各包登记的图表、列表都来自 `workbench.widget-data`；拖拽排布后在另一个浏览器里实时同步 |
| 控制中心 | 顶栏入口 | 可进入的租户与平台（`superadmin` 已是演示租户 `demo` 的成员），切换后整页重建 |
| 用户管理 | `/identity/users` | SchemaPage 高级搜索、列设置、行速览；权限直授（授予 / 拒绝）、数据范围、强制下线、模仿登录 |
| 角色与授权 | `/access/roles` | 权限分配抽屉、Deny、有效期、上级角色、成员指派有效期 |
| 字段安全 | `/access/field-security` | 给某个角色把手机号设成部分脱敏，回到用户列表看效果 |
| 访问策略 | `/access/policies` | 六个模板、试运行、条件命中日志 |
| 租户 | `/tenancy/tenants`、`/tenancy/editions` | 开通三步、套餐白名单、配额与我的订阅 |
| 文件 | `/files/library` | 上传、秒传、预览（签名地址）、副本与主存储 |
| 导出中心 | 任一列表页导出 → `/files/export-center` | 后台按发起人身份逐页导出，完成后推送进度 |
| 日志链路 | `/auditing/traces` | 按 trace-id 串起访问、操作、登录、异常、数据变更日志的时间线与流向图 |
| 任务调度 | `/maintenance/jobs` | 代码登记的作业、Cron 构建器、立即执行与运行历史 |
| 在线聊天 | `/chat/conversations` | 两个浏览器各登录一个账号互发消息：已读回执、正在输入、撤回、回应、转发；建群与部门群 |
| 流程定义与实例 | `/workflow/definitions`、`/workflow/instances` | 全屏设计器、发布与新版本；实例详情里看变量、节点执行历史与流程图 |
| 审批中心 | `/workflow/approvals` | 待我审批、我已审批、我发起的、抄送我的；打开演示数据时演示租户里有费用报销、请假、采购、入职四个流程 |
| 打印模板 | `/printing/print-templates` | 拖拽设计、样例数据、预览与直打 |
| 代码生成 | `/codegeneration/code-gen` | 数据源、表结构导入、模板、预览产物、生成历史与零代码运行时 |
| AI | `/ai/providers`、`/ai/assistants`、`/ai/knowledge-documents` | 配好提供商后，在聊天的「新建」菜单里找助手，回复流式显示；知识库有检索试玩页签 |
| 接口文档 | `http://localhost:9710/scalar` | 全量与按包分组的文档，说明按 `Accept-Language` 本地化 |

亮暗主题、窄视口、键盘导航与减少动态效果都随壳层可用，可以在偏好中心里切换。

## 相关

- [功能清单](./features) · [快速开始](./getting-started)

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

.feature-preview-table td[colspan] {
  width: 100%;
}

.feature-preview-table img {
  display: block;
  width: 100%;
  height: auto;
}
</style>
