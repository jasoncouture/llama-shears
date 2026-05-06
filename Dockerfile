# syntax=docker/dockerfile:1

# Build stage. Full SDK so restore + publish work; HUSKY=0 short-circuits
# the post-restore git-hooks install target, which has no .git to bind to
# inside the container.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV HUSKY=0

COPY . .

RUN dotnet publish src/LlamaShears/LlamaShears.csproj \
    -c Release \
    -o /app/publish

# Runtime stage. Debian-based aspnet — keeps the project's glibc-built
# native deps (ONNX Runtime, sqlite-vec) happy without gcompat shims.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# All persistent state lives under Paths:DataRoot. /data is mounted by
# compose; nothing should land outside it.
ENV Paths__DataRoot=/data
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_NOLOGO=1

EXPOSE 8080

# Microsoft's ASP.NET image defaults to a non-root `app` user. The user
# wants root inside the container — flip it back.
USER root

VOLUME ["/data"]

ENTRYPOINT ["dotnet", "LlamaShears.dll"]
