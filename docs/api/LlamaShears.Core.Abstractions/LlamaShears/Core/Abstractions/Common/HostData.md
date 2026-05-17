# LlamaShears.Core.Abstractions.Common.HostData

Assembly: `LlamaShears.Core.Abstractions`

Host-level system info exposed on the data context under the
`host` key. Captured once at process start; all consumers
(templates, services, prompts) see the same snapshot.

## Parameters

- `Hostname` — Machine name reported by the OS at process start.
- `Username` — User the host process is running as.
- `OperatingSystem` — Human-readable OS description (e.g. `"Linux 6.12.10-arch1-1 #1 SMP …"`).
- `RuntimeIdentifier` — .NET runtime identifier for the host (e.g. `"linux-x64"`, `"win-arm64"`).
- `ProcessorArchitecture` — CPU architecture reported by the OS (e.g. `"X64"`, `"Arm64"`).

## Fields

### `DataKey`

Data-context key under which a [HostData](HostData.md) snapshot is stored.

## Properties

### `Hostname`

Machine name reported by the OS at process start.

### `OperatingSystem`

Human-readable OS description (e.g. `"Linux 6.12.10-arch1-1 #1 SMP …"`).

### `ProcessorArchitecture`

CPU architecture reported by the OS (e.g. `"X64"`, `"Arm64"`).

### `RuntimeIdentifier`

.NET runtime identifier for the host (e.g. `"linux-x64"`, `"win-arm64"`).

### `Username`

User the host process is running as.

## Methods

### `HostData`(string Hostname, string Username, string OperatingSystem, string RuntimeIdentifier, string ProcessorArchitecture)

Host-level system info exposed on the data context under the
`host` key. Captured once at process start; all consumers
(templates, services, prompts) see the same snapshot.

#### Parameters

- `Hostname` — Machine name reported by the OS at process start.
- `Username` — User the host process is running as.
- `OperatingSystem` — Human-readable OS description (e.g. `"Linux 6.12.10-arch1-1 #1 SMP …"`).
- `RuntimeIdentifier` — .NET runtime identifier for the host (e.g. `"linux-x64"`, `"win-arm64"`).
- `ProcessorArchitecture` — CPU architecture reported by the OS (e.g. `"X64"`, `"Arm64"`).

