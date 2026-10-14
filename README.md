# Nexiara

Cross-Device Orchestration and Low-Latency Remote Operating System Control

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C# 13](https://img.shields.io/badge/C%23-13.0-239120?logo=c-sharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Windows 10/11](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?logo=windows&logoColor=white)](https://www.microsoft.com/windows)
[![Android Gateway](https://img.shields.io/badge/Gateway-Android-34A853?logo=android&logoColor=white)](https://dotnet.microsoft.com/apps/maui)
[![License: GPL-3.0](https://img.shields.io/badge/License-GPL--3.0-blue.svg)](https://www.gnu.org/licenses/gpl-3.0.en.html)

---

## Experimental Proof-of-Concept Overview

Nexiara is an experimental systems engineering proof-of-concept (PoC) designed to investigate high-throughput, low-latency desktop streaming and cross-device operating system interaction on modern hardware. Engineered on the .NET 10 runtime and C# 13, the architecture prioritizes minimal pipeline overhead through several core design tenets:

* **Zero-Copy Frame Ingestion Pipeline:** Integrates direct GPU surface acquisition pipelines with lock-free atomic buffer exchanges, bypassing managed heap allocations during active video transmission.
* **Hardware-Accelerated Media Pipelines:** Leverages DirectX Graphics Infrastructure (DXGI) desktop duplication interfaces and Direct3D 11 device contexts to extract framebuffer data directly from VRAM, combined with SIMD-oriented color-space transformations.
* **Kernel-Level Input Virtualization:** Employs low-level Win32 subsystem interop to inject hardware keyboard scan codes, relative mouse deltas, and normalized touch interactions with sub-millisecond dispatch times.
* **Decoupled Mesh Topology:** Employs asynchronous non-blocking network abstractions with zero-configuration UDP service discovery alongside high-throughput TCP and WebSocket streaming conduits.

---

## System Architecture

```
┌───────────────────────────────────────────────────────────────┐
│                      Nexiara.UI / Client                      │
│            (Desktop WPF/WinUI Overlay & Test Runner)          │
└───────────────────────────────┬───────────────────────────────┘
                                │
┌───────────────────────────────┴───────────────────────────────┐
│                       Nexiara.Core                            │
│  • PeerSession Management       • ActionDispatcher Pipeline   │
│  • TCP High-Throughput Engine   • UDP Beacon Auto-Discovery   │
└───────────────────────────────┬───────────────────────────────┘
                                │
        ┌───────────────────────┴───────────────────────┐
        ▼                                               ▼
┌───────────────────────────────┐       ┌───────────────────────────────┐
│       Nexiara.Streaming       │       │   Nexiara.Platform.Windows    │
│  • DXGI Desktop Duplication   │       │  • Win32 Raw Input Simulator  │
│  • Windows Graphics Capture   │       │  • Shared Clipboard Sync      │
│  • Fast NV12 Frame Pipeline   │       │  • System Audio & Diagnostics │
└───────────────────────────────┘       └───────────────────────────────┘
                                │
        ┌───────────────────────┴───────────────────────┐
        ▼                                               ▼
┌───────────────────────────────┐       ┌───────────────────────────────┐
│       Nexiara.Protocol        │       │        Nexiara.Mobile         │
│  • Action Serialization       │       │  • Android Client Gateway     │
│  • Mesh Capability Contracts  │       │  • Touch Event Mapping        │
└───────────────────────────────┘       └───────────────────────────────┘
```

The system is structured as a decoupled multi-tier pipeline:
1. **Presentation & Host Tier (`Nexiara.UI`, `NexiaraClient`):** Manages the host overlay, user configuration, rendering canvases, and integration diagnostic runners.
2. **Network & Routing Tier (`Nexiara.Core`):** Coordinates transport discovery, peer session lifecycles, and bidirectional action dispatching.
3. **Hardware Acceleration & Media Tier (`Nexiara.Streaming`):** Interfaces with GPU display outputs, manages color conversion pipelines, and controls frame buffering.
4. **Platform Integration Tier (`Nexiara.Platform.Windows`):** Encapsulates platform-specific unmanaged API bindings, input virtualization, clipboard hooks, and host telemetry.
5. **Contract & Protocol Tier (`Nexiara.Protocol`):** Houses immutable action envelopes, capability matrices, and ahead-of-time (AOT) source-generated JSON serialization contexts.
6. **Mobile Edge Tier (`Nexiara.Mobile`):** Provides a .NET MAUI mobile gateway for touch interaction, telemetry monitoring, and remote session reception on Android.

---

## Core Subsystems

### Nexiara.Streaming

The streaming subsystem manages the real-time visual pipeline from display surface acquisition to network serialization:

* **DirectX Desktop Duplication (`DxgiScreenCapturer`):** Interacts directly with the `IDXGIOutputDuplication` interface through Vortice.DXGI and Direct3D 11 (`ID3D11DeviceContext`). It accesses desktop textures resident in GPU memory and stages frames for processing with minimal CPU overhead.
* **Windows Graphics Capture (`WgcScreenCapturer`):** Employs the modern WinRT `Windows.Graphics.Capture` APIs as a secondary capture path, with an optimized GDI capture fallback (`GdiScreenCapturer`) for legacy display driver compatibility.
* **Color Space Transformation (`Nv12Converter`):** Implements an unmanaged unsafe ARGB-to-NV12 conversion routine using direct memory pointer arithmetic. Computes luminance ($Y$) and chroma sub-sampling ($UV$) planar layouts, matching the input format expected by hardware video encoders.
* **Lock-Free Triple Buffering (`LockFreeTripleBuffer<T>`):** Eliminates locking primitives and thread contention between the high-frequency capture thread and consumer encoding loops using atomic pointer swaps (`Interlocked.Exchange`).
* **Encoder Abstractions:** Provides a unified pipeline (`StreamPipeline`, `StreamServer`) supporting both hardware transforms via Media Foundation (`MediaFoundationEncoder`, `HardwareVideoEncoder`) and software fallback pathways (`SoftwareEncoder`).

### Nexiara.Core

The networking and orchestration subsystem coordinates node lifecycle, capability negotiation, and data dispatching:

* **Autonomous Discovery (`UdpDiscoveryService`, `UdpBeaconListener`):** Utilizes UDP broadcast beacons over local area networks to achieve zero-configuration node detection, automatic IP resolution, and cryptographic node pairing handshakes.
* **High-Throughput Node Transport (`TcpNodeClient`, `TcpNodeListener`):** Implements an asynchronous TCP socket engine optimized for continuous frame packets and structured action envelopes, managing socket stream segmentation without thread blocking.
* **WebSocket Bridge (`WebBridgeServer`):** Hosts a lightweight WebSocket server instance enabling seamless frame streaming and telemetry push directly to web and mobile endpoints.
* **Action Routing Pipeline (`ActionDispatcher`):** A decoupled asynchronous dispatcher that registers type-safe action handlers, routes incoming `ActionEnvelope` messages, and validates payload integrity across connected sessions (`PeerSession`).

### Nexiara.Platform.Windows

The Windows platform subsystem bridges managed runtime logic with low-level Win32 kernel and user APIs:

* **Input Virtualization (`Win32InputExecutor`):** Implements low-level cursor and keyboard synthesis via `user32!SendInput`. Translates relative touch deltas and absolute normalized screen coordinates, supporting mouse button flags, wheel scroll deltas, and keyboard virtual key mappings.
* **Process & Host Control (`Win32SystemExecutor`):** Enumerates system process trees, computes process memory working sets, and provides execution control (process termination and task launching).
* **Bidirectional Clipboard Integration (`WindowsClipboardProvider`):** Monitors and synchronizes system clipboard state across paired endpoints, automating text format conversion and cross-device paste operations.
* **Host Telemetry & Diagnostics (`WindowsTelemetryProvider`):** Collects active foreground window handles (`GetForegroundWindow`), window text titles, and system physical memory metrics.
* **Network Infrastructure Automation (`FirewallManager`):** Configures inbound and outbound Windows Advanced Firewall rules programmatically to prevent socket binding rejections during discovery and streaming sessions.

### Nexiara.Mobile

The mobile gateway delivers a remote control interface built on .NET MAUI:

* **Target Support:** Multi-targeted for Android (`net10.0-android`) and Windows desktop (`net10.0-windows10.0.19041.0`).
* **Touchpad & Gesture Engine (`TouchpadPage`):** Transforms high-frequency capacitive multi-touch inputs into synthesized cursor trajectories, single/double tap clicks, drag operations, and velocity-proportional scrolling gestures.
* **Real-Time Stream Receiver:** Houses a managed WebSocket streaming client receiving and rendering live desktop video frames directly within mobile viewports.
* **Local Persistence & Settings:** Manages persistent paired node credentials, sensitivity thresholds, and address caches using SQLite (`SQLitePCLRaw.bundle_green`) and MAUI storage preferences.

---

## Solution Structure

```
Nexiara/
├── Nexiara.slnx
├── src/
│   ├── Nexiara.Abstractions/       # Core interfaces, abstractions, and contract primitives
│   ├── Nexiara.Core/               # Mesh networking, TCP/UDP transports, dispatchers, and state management
│   ├── Nexiara.Mobile/             # .NET MAUI mobile client gateway (Android and Windows targets)
│   ├── Nexiara.Platform.Windows/   # Win32 P/Invoke interop, input simulation, clipboard, and telemetry
│   ├── Nexiara.Protocol/           # Action envelopes, DTO definitions, and AOT-safe JSON serialization contexts
│   ├── Nexiara.Streaming/          # DXGI/WGC screen capture, NV12 conversion, and video pipeline
│   ├── Nexiara.UI/                 # Desktop WPF interface, overlay controls, and integration test runner
│   └── NexiaraClient/              # Standalone desktop client agent and WebRTC streaming experiments
└── README.md
```

---

## Tech Stack

* **Runtime & Frameworks:** .NET 10 (CoreCLR, .NET Windows, .NET Android), C# 13
* **Graphics & Capture APIs:** DXGI Desktop Duplication API (`IDXGIOutputDuplication`), Direct3D 11 (`ID3D11DeviceContext`), Windows Graphics Capture (`Windows.Graphics.Capture`)
* **Hardware Acceleration & Interop:** Vortice.Windows bindings (Vortice.DXGI, Vortice.Direct3D11, Vortice.MediaFoundation), native Win32 P/Invoke (`user32.dll`, `kernel32.dll`, `dwmapi.dll`)
* **Network & Communication:** Asynchronous TCP/IP sockets (`System.Net.Sockets`), UDP broadcast/multicast beacons, System.Net.WebSockets, SIPSorcery WebRTC integration
* **Serialization & Code Generation:** High-performance reflectionless `System.Text.Json` source generators (`JsonSerializerContext` via `ActionJsonContext`)
* **Concurrency & Memory Management:** Unsafe direct memory pointers, atomic lock-free triple buffer (`Interlocked.Exchange`), asynchronous channels, and thread pool dispatchers
* **UI & Client Frameworks:** Windows Presentation Foundation (WPF), .NET MAUI, CommunityToolkit.Mvvm, CommunityToolkit.Maui

---

## Development Vector & Milestones

The ongoing development roadmap focuses on low-level performance profiling, latency reduction, and protocol hardening:

1. **DXGI Memory Profiling & Texture Recycling:**
   Transition the `DxgiScreenCapturer` pipeline from ad-hoc texture staging to a pre-allocated texture pool. Profile VRAM-to-system RAM transfer overhead under high display refresh rates (120 Hz / 144 Hz) to eliminate managed garbage collection churn and GPU pipeline stalls.
2. **End-to-End Latency Minimization:**
   Implement hardware-accelerated NV12 encoder pipelines utilizing Intel QuickSync and NVIDIA NVENC Media Foundation transforms (MFT). Optimize TCP socket configurations (`TcpNoDelay = true`, custom socket buffer sizes) to reduce glass-to-glass latency below 20 milliseconds across local Wi-Fi networks.
3. **Socket Reconnection Resilience & State Recovery:**
   Enhance `TcpNodeClient` and `PeerSession` with stateful reconnect logic, exponential backoff, and frame sequence acknowledgment tokens to seamlessly absorb transient network drops and mobile radio handoffs.
4. **Touch Jitter Smoothing & Dynamic Coordinate Filtering:**
   Incorporate dynamic low-pass filtering algorithms (e.g., 1€ filter) into `TouchpadPage` to eliminate cursor jitter caused by micro-fluctuations in capacitive mobile digitizers during subtle finger movements.
5. **Headless Integration Test Suite:**
   Migrate the self-contained verification routines in `NexiaraTestRunner` into an automated xUnit/NUnit test harness suitable for headless execution in continuous integration (CI/CD) pipelines.

---

## Build & Installation

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/) (version 10.0 or higher)
* Windows 10 / 11 x64 (Build 19041 or higher recommended for full DXGI / WGC feature sets)
* (Optional) Android SDK and .NET MAUI workload for building mobile packages:
  ```bash
  dotnet workload install maui-android
  ```

### Build Instructions

```bash
# Clone the repository
git clone https://github.com/blakedimm/nexiara.git
cd nexiara

# Restore solution dependencies
dotnet restore

# Compile all solution projects in Release configuration
dotnet build -c Release
```

### Running the Desktop Host

```bash
dotnet run --project src/Nexiara.UI/Nexiara.UI.csproj -c Release
```

---

## License

This project is licensed under the terms of the GNU General Public License v3.0 (GPL-3.0). For more details, refer to the [GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html).