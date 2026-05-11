# syntax=docker/dockerfile:1

ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0

# Shared SDK base for every build-time stage. Static layers
# (WORKDIR, HUSKY=0 to short-circuit the post-restore git-hooks
# install target, and the apt install of node+npm needed by the
# Api.Web BundleJs MSBuild target) live here once, so a code-only
# rebuild reuses everything below the COPYs in the derived stages.
FROM ${SDK_IMAGE} AS sdk-base
WORKDIR /src
ENV HUSKY=0
RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm \
    && rm -rf /var/lib/apt/lists/*

# Stage 1: collect just the files needed for `dotnet restore`. dotnet-subset
# walks project references from the entry project and copies every csproj /
# Directory.Build.props / Directory.Packages.props / nuget.config into a
# minimal tree under /output. Bulk source is excluded — so the restore
# layer below only invalidates when a project's references change, not on
# every .cs edit. dotnet-subset is declared as a local tool in the repo
# root manifest (dotnet-tools.json — the SDK no longer requires the
# .config/ subfolder); restore the manifest first to make `dotnet subset`
# available.
FROM sdk-base AS prepare-restore-files
COPY dotnet-tools.json ./
RUN dotnet tool restore
COPY . .
RUN dotnet subset restore src/LlamaShears/LlamaShears.csproj \
    --root-directory /src \
    --output /output

# Stage 2: restore + publish. The minimal tree from stage 1 lands first so
# the restore layer can be cached; only after restore succeeds do we lay
# down the full source tree for the actual build.
FROM sdk-base AS build
COPY --from=prepare-restore-files /output .
RUN dotnet restore src/LlamaShears/LlamaShears.csproj
COPY . .
RUN dotnet publish src/LlamaShears/LlamaShears.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# Runtime base. Debian-based aspnet — keeps the project's glibc-built
# native deps (ONNX Runtime, sqlite-vec) happy without gcompat shims.
# Layered with the tools an in-container agent shell will reach for
# (`tree`, `git`), and the apt cache is refreshed last so the agent
# doesn't have to spend a turn running `apt-get update` before it can
# install anything else.
FROM ${ASPNET_IMAGE} AS runtime-base
WORKDIR /app

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

RUN apt-get update \
    && apt-get install -y --no-install-recommends tree git \
    && rm -rf /var/lib/apt/lists/* \
    && apt-get update

# Final runtime layer — drops the publish output on top of the prepared
# base. Splitting the published bits into their own stage keeps the
# base layer cacheable across code-only changes.
FROM runtime-base AS runtime

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "LlamaShears.dll"]
