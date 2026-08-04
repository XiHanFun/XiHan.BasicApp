# 构建上下文为 backend/。
# 直接发布 WebHost 单个工程（不经解决方案）：SolutionName 为空时 props/framework.props
# 判定 UseXiHanFrameworkSource=false，走 NuGet 包引用。
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY props/ ./props/
COPY src/ ./src/

ARG BUILD_CONFIGURATION=Release
RUN dotnet restore src/main/XiHan.BasicApp.WebHost/XiHan.BasicApp.WebHost.csproj
RUN dotnet publish src/main/XiHan.BasicApp.WebHost/XiHan.BasicApp.WebHost.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# HEALTHCHECK 需要 curl；基础镜像未自带
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

# 上传目录与日志目录在运行期写入，挂卷前先建好并交给非 root 运行账号
RUN mkdir -p wwwroot/uploads logs && chown -R $APP_UID:$APP_UID /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    Hosting__Urls=http://0.0.0.0:9708

EXPOSE 9708
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -fsS http://127.0.0.1:9708/health || exit 1

ENTRYPOINT ["dotnet", "XiHan.BasicApp.WebHost.dll"]
