# 后端镜像，构建上下文是 backend/（docker compose 的 init / backend 服务已配好）
# XiHan.Framework 按 props/framework.props 的版本从 NuGet 还原，不需要框架源码

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/XiHan.BasicApp.Host/XiHan.BasicApp.Host.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# 可写的数据都放 /app/data（挂卷）：文件存储、数据保护密钥、框架缺省的文件系统存储
RUN mkdir -p /app/data/files /app/data/keys /app/data/blobs && chown -R app:app /app/data
# 监听地址只由 Kestrel 端点决定：清掉基础镜像自带的 ASPNETCORE_HTTP_PORTS=8080，免得启动时报覆盖警告
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS= \
    Kestrel__Endpoints__Http__Url=http://+:9710 \
    BasicApp__Files__LocalRootPath=/app/data/files \
    BasicApp__DataProtection__KeysDirectory=/app/data/keys \
    XiHan__ObjectStorage__FileSystem__RootPath=/app/data/blobs

USER app
EXPOSE 9710
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "XiHan.BasicApp.Host.dll"]
