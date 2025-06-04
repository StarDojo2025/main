Before getting started, ensure the following prerequisites are satisfied:

#### 📌 Install Stardew Valley

First things first, make sure you own the official Stardew Valley game. Download and install it from your preferred platform, such as Steam.

#### 🔧 Install SMAPI (Stardew Modding API)

SMAPI is required to enable modding support. Our StarDojoMod is dependent of SMAPI.

- Official SMAPI website: [https://smapi.io/](https://smapi.io/)

> SMAPI is a community-developed modding API for Stardew Valley that intercepts game behavior and allows external mods to hook into it.

#### 🔧 Install StarDojoMod

##### 📦 Directly Download

If you don’t want to build the mod yourself, no worries — you can simply download the precompiled version from Nexus Mods:

* 👉 [Download StarDojoMod from Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/34175)

After downloading, just extract the contents into your `StardewValley/Mods/` folder.

##### 🛠 (Optional) Build StarDojoMod (C#)

If you want to build the StarDojoMod from source code:

1. Open `StarDojo/StarDojoMod/StarDojoMod.sln` using **Visual Studio (VSCode with C# extension is acceptable)**.
2. Ensure all dependencies are resolved (SMAPI should be referenced).
3. Build the solution to generate the mod DLL.
4. The built mod will be automatically placed in the SMAPI mods folder if properly configured, or you may manually copy the output to your `StardewValley/Mods/` directory.
