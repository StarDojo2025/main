# 🌾 StarDojo README

This project provides a benchmarking and testing environment for language model within **Stardew Valley** using the `StardewModdingAPI` interface. It supports single-task, multi-task (sequential or parallel), and customizable observation/action spaces and tasks for agent evaluation.

Official Website: https://stardojo2025.github.io/stardojo/

Docs: [docs.md](./docs.md)

---

## 🔧 1. Prerequisites

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

---

## ⚙️ 2. Environment Setup

### Step 1: Add Environment Variables

Create or edit the file at `StarDojo/env/.env` and include the following keys:

```bash
# Required
STARDEW_APP_PATH=/path/to/StardewModdingAPI

# Optional (if using external LLM services)
OA_OPENAI_KEY=sk-xxxx
````

* Make sure `STARDEW_APP_PATH` correctly points to the **file path** of `StardewModdingAPI.exe` on Windows or `StardewModdingAPI` on Linux/Mac.

### Step 2: Initialize Environment

```bash
cd StarDojo
source setup.sh
```

For Windows, execute the command below instead.
```bash
.\setup.ps1
```

This command installs dependencies and prepares the shell environment for easy agent launching.

---

## 🚀 3. Running Benchmark Tasks

After setup, run python files under the `env/` directory.

| Mode                    | Command                        | Description                      |
| ----------------------- | ------------------------------ | -------------------------------- |
| 🧪 Single Task          | `python llm_env.py`                      | Runs a single benchmark task     |
| 🔁 Multi-task (serial)  | `python llm_env_multi_tasks.py`          | Runs multiple tasks sequentially |
| ⚡ Multi-task (parallel) | `python llm_env_multi_tasks_parallel.py` | Runs multiple tasks in parallel  |

Customize the tasks using `--task_params`, or `--task_name` and `--task_id` when running a single task.

---

## 🛠 4. Configuration Guide

There are configurations you need to customize inside the python file you run.

### Core Configs:

```python
llmProviderConfig     = "./conf/openai_config.json"
embedProviderConfig   = "./conf/openai_config.json"
envConfig             = "./conf/env_config_stardew.json"
```

These files are set up under the `StarDojo/agent/conf/` directory, for your preferred LLM and environment settings.

### Runtime Parameters (`env_params`):

```python
env_params = {
    'port': 6000,
    'save_index': 0,
    'new_game': False,
    'image_save_path': "../env/screen_shot_buffer",
    'agent': react_agent,
    'needs_pausing': True,
    'image_obs': True,
    'task': task,
    'output_video': True,
}
```

* **`new_game: True`** — The environment will start a fresh game and close it upon task completion.
* **`new_game: False`** — You must manually open the game beforehand.

---

## 📦 5. Dependencies

* Python >= 3.10 (3.10.9 recommended)
* SMAPI + Stardew Valley
* Additional dependencies will be installed via `setup.sh`

---

## 📮 Contact

For issues or contributions, feel free to open an issue or pull request.

Happy Farming 🌽

