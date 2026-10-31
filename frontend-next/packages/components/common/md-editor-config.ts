import { config } from 'md-editor-v3'

/*
 * md-editor-v3 的全局配置，随编辑器模块首次加载执行一次。
 *
 * 编辑器运行时从 CDN 往 <head> 插代码高亮与 KaTeX 两张样式表。不带 crossorigin 的跨域表，
 * 页面脚本读不到它的规则（CSSOM 安全限制），XiHan.UI 的 Portal 视觉桥无从判断它声明了什么，
 * 只能整份放弃样式索引：每个浮层同步都要枚举整张计算样式，祖先上任何 class 变化、
 * 任何模态浮层加滚动锁都让全页浮层重算一轮（偏好设置抽屉打开一次实测 650ms 主线程）。
 * CDN 回 Access-Control-Allow-Origin: *，以匿名 CORS 加载即可读。高亮主题缺省时按 atom
 * 那组属性取，一组覆盖全部主题。改用内网镜像时镜像须同样放开跨域，否则这两张表加载失败。
 */
config({
  editorExtensionsAttrs: {
    highlight: {
      css: {
        atom: {
          light: { crossOrigin: 'anonymous' },
          dark: { crossOrigin: 'anonymous' },
        },
      },
    },
    katex: {
      css: { crossOrigin: 'anonymous' },
    },
  },
})
