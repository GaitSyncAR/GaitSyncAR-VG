# GaitSyncAR-VG (Unity Orchestrator)

This repository contains the core Unity 3D application for the **GaitSyncAR** rehabilitation platform. It is specifically designed and optimised for the **Viture Luma Ultra** augmented reality glasses. 

As the "orchestrator" of the platform, this application manages the immersive AR environment, handles live telemetry from custom hardware, and synthesises raw step data into actionable clinical metrics.

## Key Features

* **Immersive AR Cueing:** Delivers rhythmic visual and auditory metronome cues through the Viture Luma Ultra's optical see-through (OST) display to guide patient walking pace and stimulate neuroplasticity.
* **Accessible UI/UX:** Features a gesture-free interface designed specifically for users with motor impairments or tremors.
* **BLE Sensor Bridge:** Establishes a zero-latency Bluetooth Low Energy (BLE) handshake to receive real-time timestamped data from the custom nRF52840 ankle sensors.
* **Clinical Analytics Engine:** Processes raw gait data (`GaitMetrics.cs`) to calculate:
  * Cadence (Steps per Minute)
  * Temporal Symmetry Ratio
  * Stride Time Variability (CV%)
  * Inter-limb Temporal Phase Offset
* **Post-Session Dashboard:** Automatically generates a comprehensive end-of-session report (`SessionPageController.cs`), complete with an aggregated 10-point "Stability Score" to help patients and clinicians track rehabilitation progress.

---

## Prerequisites

To open, compile, and deploy this project, you must ensure your development environment meets the following requirements:

* **Unity Editor:** Version **`6000.2.12f1`** is strictly required to ensure compatibility with the project's dependency graph and UI Toolkit assets.
* **Target Build Support:** Ensure the appropriate build modules (typically Android, depending on your specific Viture neckband/bridge setup) are installed via Unity Hub.
* **AR/VR Packages:** The project relies on specific XR plug-in management tools compatible with the Viture hardware ecosystem.

---

## Getting Started: Compilation and Deployment

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/GaitSyncAR/GaitSyncAR-VG.git

2. Open in Unity Hub: Locate the cloned directory and open it using Unity Editor version 6000.2.12f1. Allow the Editor time to resolve packages and import assets on the first boot.

3. Configure Build Settings:
  - Navigate to File > Build Profiles.
  - Ensure the target platform is set correctly for your AR glasses bridge (e.g., Android).

4. Deploy:
  Connect your mobile device via USB, ensure Developer Mode/USB Debugging is enabled, and click Build and Run.

## System Architecture
This software is one component of the broader GaitSyncAR ecosystem. For this application to track gait metrics successfully, the user must be wearing the paired hardware.
- For the overarching project documentation and demonstration videos, please visit the [GaitSyncAR Root Repository](https://github.com/GaitSyncAR).
- For the custom sensor embedded C code, visit the [GaitSyncAR_NRF_Sense Repository](https://github.com/GaitSyncAR/GaitSyncAR_NRF_Sense).
