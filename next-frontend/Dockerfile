# 构建上下文为 frontend/。
FROM node:24-alpine AS build
WORKDIR /app

RUN corepack enable

COPY . .
RUN pnpm install --frozen-lockfile

# API 源。留空表示同源，由 nginx 反代 /api、/hubs、/uploads 到后端。
# 写入 .env.production.local：该文件在 Vite 的加载顺序中优先于 .env.production。
ARG VITE_API_BASE_URL=
RUN printf 'VITE_API_BASE_URL=%s\n' "$VITE_API_BASE_URL" > .env.production.local

# 该脚本先跑 vue-tsc --noEmit，类型不过即构建失败
RUN pnpm run build

FROM nginx:1.29-alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget -qO- http://127.0.0.1/ >/dev/null || exit 1
