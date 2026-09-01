# ⚡ Nexiara — High-Performance Cross-Device Synchronization & Remote OS Control

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=c-sharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android-informational)](https://github.com/blakedimm/nexiara)
[![Low Latency](https://img.shields.io/badge/Streaming-DXGI%20%2F%20WGC-blueviolet)]()

> **Nexiara** is an enterprise-grade cross-device orchestration and low-latency remote control ecosystem built on **.NET 10**. It provides hardware-accelerated desktop capture, peer-to-peer UDP mesh discovery, Win32 raw input virtualization, and cross-platform mobile connectivity.

---

## 🏛 System Architecture

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

---

## 🚀 Key Modules & Capabilities

### 1. 📺 Low-Latency Screen Streaming (`Nexiara.Streaming`)
* **Hardware-Accelerated Frame Capture:** Multi-backend capture engine supporting **DXGI Desktop Duplication API** and modern **Windows Graphics Capture (WGC)** with GDI fallback.
* **NV12 Color Converter:** Zero-copy color space transformation pipeline designed for real-time video encoder ingestion.
* **Touch-to-Screen Mapping:** `AbsoluteTouchExecutor` transforming remote multi-touch coordinates into desktop pointer gestures.

### 2. 🌐 Mesh Networking & Peer Discovery (`Nexiara.Core`)
* **Zero-Config LAN Pairing:** Background UDP beacon broadcaster and listener (`UdpDiscoveryService` / `UdpBeaconListener`) for automatic client detection.
* **Bi-Directional Action Stream:** Fast async TCP node client/listener handling structured serialized actions without thread blocking.

### 3. 🪟 Windows OS Integration (`Nexiara.Platform.Windows`)
* **Win32 Input Virtualization:** Low-level keyboard and mouse simulation (`SendInput` via `Win32InputExecutor`).
* **Bidirectional Clipboard Sync:** Real-time text and media clipboard propagation between paired endpoints.
* **Firewall & Telemetry:** Automated Windows Firewall configuration rule management and system diagnostics.

### 4. 📱 Mobile Gateway (`Nexiara.Mobile`)
* Native Android client backend (.NET Android) supporting remote session initiation, touch mapping, and secure pairing storage.

---

## 📂 Solution Structure

```
Nexiara/
├── Nexiara.sln
├── src/
│   ├── Nexiara.Abstractions/       # Contracts, interfaces & base providers
│   ├── Nexiara.Core/               # TCP/UDP networking, dispatchers & sessions
│   ├── Nexiara.Protocol/           # Action payload models & JSON contexts
│   ├── Nexiara.Platform.Windows/   # Win32 APIs, input injection & clipboard
│   ├── Nexiara.Streaming/          # DXGI/WGC screen capture & pixel pipelines
│   ├── Nexiara.UI/                 # Desktop interface & overlay windows
│   ├── Nexiara.Mobile/             # Android mobile client architecture
│   └── NexiaraClient/              # Standalone client agent & streamer
└── README.md
```

---

## 🛠 Tech Stack

* **Core Platform:** C# 13 / .NET 10 (.NET Core, .NET Windows, .NET Android)
* **Capture APIs:** DXGI Desktop Duplication, Direct3D 11, Windows.Graphics.Capture
* **OS Interop:** P/Invoke Win32 APIs (`user32.dll`, `kernel32.dll`, `dwmapi.dll`)
* **Networking:** Async TCP/IP Sockets, UDP Multicast Beacons, System.Net.Sockets
* **Serialization:** High-performance System.Text.Json source generation

---

## 🚀 Building & Running

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/)
* Windows 10/11 (for Desktop Streaming & DXGI features)

```bash
# Clone the repository
git clone [https://github.com/blakedimm/nexiara.git](https://github.com/blakedimm/nexiara.git)
cd nexiara

# Restore and build the complete solution
dotnet restore
dotnet build Nexiara.sln -c Release
```

---

## 👨‍💻 Author
* **Developer:** [Blake](https://github.com/blakedimm)
* **Focus:** Low-Level Systems Programming, Distributed Mesh Networks & High-Performance Media Pipelines