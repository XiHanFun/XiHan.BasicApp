import { h, watch } from "vue";
import Theme from "vitepress/theme";
import "./rainbow.css";
import "./vars.css";
import "./overrides.css";

let homePageStyle: HTMLStyleElement | undefined;

export default {
  ...Theme,
  Layout: () => {
    return h(Theme.Layout, null, {});
  },
  enhanceApp(ctx) {
    // 先执行默认主题的 enhanceApp，注册 Badge 等全局组件（否则 <Badge> 渲染为空）
    Theme.enhanceApp?.(ctx);

    if (typeof window === "undefined") return;

    watch(
      () => ctx.router.route.data.relativePath,
      () => updateHomePageStyle(location.pathname === "/"),
      { immediate: true }
    );
  },
};

// 检测浏览器，添加到类中进行条件样式设置
if (typeof window !== "undefined") {
  const browser = navigator.userAgent.toLowerCase();
  if (browser.includes("chrome"))
    document.documentElement.classList.add("browser-chrome");
  else if (browser.includes("firefox"))
    document.documentElement.classList.add("browser-firefox");
  else if (browser.includes("safari"))
    document.documentElement.classList.add("browser-safari");
}

// 加快主页上的彩虹动画
function updateHomePageStyle(value: boolean) {
  if (value) {
    if (homePageStyle) return;

    homePageStyle = document.createElement("style");
    homePageStyle.innerHTML = `
    :root {
      animation: rainbow 12s linear infinite;
    }`;
    document.body.appendChild(homePageStyle);
  } else {
    if (!homePageStyle) return;

    homePageStyle.remove();
    homePageStyle = undefined;
  }
}
