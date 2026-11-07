# 前端镜像，构建上下文是 frontend/（docker compose 的 frontend 服务已配好）
# 构建产物由 Nginx 托管，并把 /api、/blobs、/hubs、/health 反向代理到后端（同源部署，不开跨域）

FROM node:24-alpine AS build
WORKDIR /app
# pnpm 版本取 package.json 的 packageManager
RUN corepack enable
COPY . .
# 没有锁文件
RUN pnpm install --no-frozen-lockfile
# 构建期 API 源：留空即同源，由 Nginx 反代到后端
ARG VITE_API_BASE_URL=
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
RUN pnpm build

FROM nginx:1.29-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
