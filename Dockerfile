# syntax=docker/dockerfile:1

ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0

# Stage 1: collect just the files needed for `dotnet restore`. dotnet-subset
# walks project references from the entry project and copies every csproj /
# Directory.Build.props / Directory.Packages.props / nuget.config into a
# minimal tree under /output. Bulk source is excluded — so the restore
# layer below only invalidates when a project's references change, not on
# every .cs edit. dotnet-subset is declared as a local tool in the repo
# root manifest (dotnet-tools.json — the SDK no longer requires the
# .config/ subfolder); restore the manifest first to make `dotnet subset`
# available.
FROM ${SDK_IMAGE} AS prepare-restore-files
WORKDIR /src
ENV HUSKY=0
COPY dotnet-tools.json ./
RUN dotnet tool restore
COPY . .
RUN dotnet subset restore src/LlamaShears/LlamaShears.csproj \
    --root-directory /src \
    --output /output

# Stage 2: restore + publish. The minimal tree from stage 1 lands first so
# the restore layer can be cached; only after restore succeeds do we lay
# down the full source tree for the actual build. HUSKY=0 short-circuits
# the post-restore git-hooks install target, which has no .git to bind to
# inside the container.
FROM ${SDK_IMAGE} AS build
WORKDIR /src
ENV HUSKY=0
# Node + npm are needed by the Api.Web project's BundleJs MSBuild target
# (esbuild bundles JS sources into wwwroot/dist during dotnet publish).
RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm \
    && rm -rf /var/lib/apt/lists/*
COPY --from=prepare-restore-files /output .
RUN dotnet restore src/LlamaShears/LlamaShears.csproj
COPY . .
RUN dotnet publish src/LlamaShears/LlamaShears.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# Runtime stage. Debian-based aspnet — keeps the project's glibc-built
# native deps (ONNX Runtime, sqlite-vec) happy without gcompat shims.
FROM ${ASPNET_IMAGE} AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# All persistent state lives under Paths:DataRoot. /data is mounted by
# compose; nothing should land outside it.
#
# The host also layers /data/appsettings.json and
# /data/appsettings.{Environment}.json on top of the bundled defaults
# (both optional, both reload-on-change). Drop a file into the mounted
# volume to override config without rebuilding.
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
