来源：https://basicapp.docs.xihanfun.com/preview

# 功能预览

<script setup>
const previewImages = import.meta.glob('../assets/preview/*.png', { eager: true, query: '?url', import: 'default' })
</script>

下面的截图与仓库 README 的「功能亮点」对应，逐项能力见[功能清单](./features)；想直接上手可打开[在线演示](https://basicapp.xihanfun.com)。

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
    <td align="center" colspan="2"><a :href="previewImages['../assets/preview/mobile.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/mobile.png']" alt="小屏适配" loading="lazy" /></a><br/><b>小屏适配</b><br/>登录、仪表盘、抽屉菜单、命令面板与灵动岛，手机浏览器打开即可使用</td>
  </tr>
  <tr>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/dashboard.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/dashboard.png']" alt="工作台" loading="lazy" /></a><br/><b>工作台</b><br/>小组件拖拽排布，看板布局云端保存</td>
    <td align="center" width="50%"><a :href="previewImages['../assets/preview/log-trace.png']" target="_blank" rel="noopener noreferrer"><img :src="previewImages['../assets/preview/log-trace.png']" alt="一个 TraceId 看全链路" loading="lazy" /></a><br/><b>一个 TraceId 看全链路</b><br/>七类日志串成时间线，桑基图看流向</td>
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

.feature-preview-table td[colspan] {
  width: 100%;
}

.feature-preview-table img {
  display: block;
  width: 100%;
  height: auto;
}
</style>
