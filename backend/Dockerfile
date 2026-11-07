# 后端镜像，构建上下文是 backend/（docker compose 的 init / backend 服务已配好）
# 后端以源码引用 XiHan.Framework：构建时按 props/framework.props 锁定的提交克隆框架，与 CI 同一做法，需要能访问 GitHub
# 框架仓库在别处（如镜像站）时用 --build-arg XIHAN_FRAMEWORK_REPO=<地址> 改指

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG XIHAN_FRAMEWORK_REPO=https://github.com/XiHanFun/XiHan.Framework.git
WORKDIR /src

# 目录与本机开发一致：XiHan.BasicApp/backend 与 XiHan.Framework 并排，props/framework.props 的缺省相对路径即可找到框架源码
COPY props/framework.props XiHan.BasicApp/backend/props/framework.props
RUN commit=$(grep -oP '(?<=<XiHanFrameworkCommit>)[0-9a-f]+' XiHan.BasicApp/backend/props/framework.props) \
    && git clone --quiet --filter=blob:none --no-checkout "$XIHAN_FRAMEWORK_REPO" XiHan.Framework \
    && git -C XiHan.Framework checkout --quiet "$commit"

COPY . XiHan.BasicApp/backend/
RUN dotnet publish XiHan.BasicApp/backend/src/XiHan.BasicApp.Host/XiHan.BasicApp.Host.csproj -c Release -o /app/publish

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
