# 📘 StarDojo Docs

## 🔧 1. Installation

This section describes how to prepare the environment for running the StarDojo agent benchmarking system in Stardew Valley.

### 1.1 Prerequisites

Before installing StarDojo, ensure the following prerequisites are satisfied:

#### 📌 Install Stardew Valley

Download and install the game from your preferred platform (e.g., Steam or GOG).

#### 🔧 Install SMAPI (Stardew Modding API)

SMAPI is required to enable modding support and interaction between the game and external scripts.

- Official SMAPI website: [https://smapi.io/](https://smapi.io/)

> SMAPI is a community-developed modding API for Stardew Valley that intercepts game behavior and allows external mods to hook into it.

#### 🛠 Build StarDojoMod (C#)

The custom mod `StarDojoMod` must be compiled before use:

1. Open `StarDojo/StarDojoMod/StarDojoMod.sln` using **Visual Studio (VSCode with C# extension is acceptable)**.
2. Ensure all dependencies are resolved (SMAPI should be referenced).
3. Build the solution to generate the mod DLL.
4. The built mod will be automatically placed in the SMAPI mods folder if properly configured, or you may manually copy the output to your `StardewValley/Mods/` directory.

---

### 1.2 StarDojo Installation

Once prerequisites are met, follow the steps below to initialize the StarDojo environment.

#### Step 1: Set Environment Variables

Create or modify the file at `StarDojo/env/.env` with the following content:

```bash
# Required
STARDEW_APP_PATH=/absolute/path/to/StardewModdingAPI

# Optional (for LLM integrations)
OA_OPENAI_KEY=your-openai-api-key
OPEN_SRC_KEY=your-other-key
```

#### Step 2: Initialize the Environment

Run the setup script to install dependencies and prepare runtime commands:

```bash
cd StarDojo
source setup.sh
```

> This script installs Python dependencies and prepares command-line aliases and directories for agent execution.


## 🧪 2. Benchmarking

The benchmarking system is located in the `env/` directory.

### 2.1 Running StarDojo

Run StarDojo benchmark by running the python files as listed below. Don't forget to check the settings as listed in 2.2 and 2.3.

| Mode                    | File                        | Description                      |
| ----------------------- | ------------------------------ | -------------------------------- |
| 🧪 Single Task          | `llm_env.py`                      | Runs a single benchmark task     |
| 🔁 Multi-task (serial)  | `llm_env_multi_tasks.py`          | Runs multiple tasks sequentially |
| ⚡ Multi-task (parallel) | `llm_env_multi_tasks_parallel.py` | Runs multiple tasks in parallel  |

> These Python files are located in the `StarDojo/env/` directory.

### 2.2 Configuration Files

Check the following configuration files as coded in the python file and replace if needed:

```python
llmProviderConfig     = "./conf/openai_config.json"
embedProviderConfig   = "./conf/openai_config.json"
envConfig             = "./conf/env_config_stardew.json"
```

>These configurations are located in `StarDojo/agent/conf/`.
These settings are in the 

### 2.3 Runtime Parameters

You can adjust `env_params` in the python file to control runtime behavior:

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

* Set `new_game=True` to automatically start and close the game.
* Set `new_game=False` to manually open the game before benchmarking.

---

## 🎮 3. Action Space

The action space in StarDojo defines the actions agent can perform within the Stardew Valley environment. Each action corresponds to a registered skill function and supports a clear call template and parameter specification.

Below is a list of available actions:

---

### 🔄 `move(x, y)`

Move to the tile at position `(x, y)`.

- **Parameters**:
  - `x` (int): X-coordinate of the destination.
  - `y` (int): Y-coordinate of the destination.

---

### 🛠 `craft(item)`

Craft an item by name.

- **Parameters**:
  - `item` (str): Name of the item to craft (e.g., `"chest"`).

---

### 🪓 `use(direction)`

Use the currently selected tool or item in a specified direction.

- **Parameters**:
  - `direction` (str): One of `"up"`, `"right"`, `"down"`, `"left"`.

---

### 🎯 `interact(direction)`

Interact with an object or NPC in a specific direction (also used for harvesting).

- **Parameters**:
  - `direction` (str): One of `"up"`, `"right"`, `"down"`, `"left"`.

---

### 🎒 `choose_item(slot_index)`

Select an item from the inventory.

- **Parameters**:
  - `slot_index` (int): Slot index (0–35).

---

### 📜 `choose_option(option_index, quantity=None, direction=None)`

Choose an option in a dialog or menu (e.g., shopping or interaction).

- **Parameters**:
  - `option_index` (int): Index of the option (1-based). Use `0` to close the menu.
  - `quantity` (int, optional): Quantity to buy/sell (default: None).
  - `direction` (str, optional): `"in"` to buy/take, `"out"` to sell/put.

---

### 🔧 `attach_item(slot_index)`

Attach an item (e.g., bait) to the current tool.

- **Parameters**:
  - `slot_index` (int): Index of the inventory item to attach.

---

### ❌ `unattach_item()`

Detach the currently attached item from the tool.

- **Parameters**: *None*

---

### 📑 `menu(option, menu_name)`

Open or close a specific menu.

- **Parameters**:
  - `option` (str): `"open"` or `"close"`.
  - `menu_name` (str): Name of the menu (e.g., `"map"`).

---

### 🧭 `navigate(name)`

Navigate to a known location using the built-in pathfinding system.

- **Parameters**:
  - `name` (str): Name of the target location (e.g., `"farm"`).

---

> ⚠️ **Note**: The `navigate` action is **disabled by default**. To enable it, you must manually configure in the file below:
> `agent/stardojo/environment/stardew/atomic_skills/basic_skills.py`.


> Each action is registered through the `@register_skill(...)` decorator and invoked by the agent via structured calls. These commands serve as the atomic building blocks for LLM agents in the StarDojo simulation environment.


---

## 👁️ 4. Observation Space

The observation space in StarDojo provides a structured and comprehensive snapshot of the agent's environment at each timestep. Observations are returned as a Python dictionary with the following fields:

---

### 🧍 Agent State

- **`energy`** (`str`)  
  The agent’s current stamina.

- **`money`** (`str`)  
  The amount of gold the player holds.

- **`location`** (`str`)  
  Name of the current game location (e.g., `"Farm"`, `"Town"`).

- **`position`** (`[int, int]`)  
  Player's current tile coordinates.

- **`facing_direction`** (`str`)  
  Human-readable direction: `"up"`, `"down"`, `"left"`, `"right"`.

- **`inventory`** (`list[dict]`)  
  A list of all inventory items, with fields such as:
  - `Name`, `Stack`, `Category`, etc.

- **`chosen_item`** (`dict`)  
  The currently selected item from inventory. Contains item-specific info.

---

### 🕒 World State

- **`time`** (`str`)  
  Current in-game time (e.g., `"7:00 AM"`).

- **`day`** (`str`)  
  Current day of the month (1–28).

- **`season`** (`str`)  
  Current season: `"spring"`, `"summer"`, `"fall"`, or `"winter"`.

---

### 🐄 Farm Information

- **`farm_animals`** (`list[dict]`)  
  All animals on the farm, with type and position data.

- **`farm_pets`** (`list[dict]`)  
  Pets on the farm.

- **`farm_buildings`** (`list[dict]`)  
  Includes barns, coops, silos, etc., with location and state.

---

### 🧱 Environment Layout

- **`surroundings`** (`list[dict]`)  
  Description of nearby tiles. Each entry includes:
  - `position`: relative offset (e.g., `[0, -1]`)
  - `object`: list of tags (e.g., `"Type: Dirt"`, `"Diggable: True"`)
  - *(Optional)* `npc on this tile`

- **`crops`** (`list[dict]`)  
  Detailed data of visible crops: location, stage, harvestable status.

- **`exits`** (`list[dict]`)  
  Reachable map exits from the current location.

---

### 🧱 Structures & Interior

- **`buildings`** (`list[dict]`)  
  General building data visible on the screen (non-farm).

- **`furniture`** (`list[dict]`)  
  Furniture placed indoors or outdoors, with type and location.

---

### 👥 Interactive Elements

- **`npcs`** (`list[dict]`)  
  All nearby non-player characters with positions and metadata.

- **`shop_counters`** (`list[dict]`)  
  Shop points of interaction, available options, inventory, etc.

- **`current_menu`** (`dict`)  
  Active UI menu details. May include:
  - `type`, `message`, `shopmenudata`, `animalsmenudata`, etc.

---

### 🖼️ Visual Inputs

- **`image_paths`** (`list[str]`)  
  A list of auto-generated file paths to screenshots representing the current frame, don't need to set manually. Opening the `image_obs` config in the `env_params` will enable visual inputs.

---

> ℹ️ **Note**: The default observation set is constrained as below, used when a lightweight input is desired:
>
> - **Health**: Current player health (int)  
> - **Energy**: Current stamina level (float)  
> - **Money**: Player gold (int)  
> - **Current Time**: Formatted as `"hh:mm AM/PM"`  
> - **Day**: Current day (int)  
> - **Season**: `"spring"`, `"summer"`, `"fall"`, or `"winter"`  
> - **Item in Your Hand**:
>     - `index` (int): Slot index  
>     - `currentitem` (str): Item name  
> - **Toolbar**: 36-slot list in format  
>     `"slot_index N: [Item Name] (quantity: Q)"` or `"slot_index N: No item"`  
> - **Current Menu**: A dictionary with keys like `type`, `message`, `shopmenudata`  
> - **Surrounding Blocks**:
>     - `position`: 2D offset  
>     - `object`: List of string attributes  
>     - *(Optional)* NPC on this tile

---

> 💡 **Customization Tip**:  
> You can freely modify or extend the observation format by editing the `_get_obs()` method in  
> `agent/stardojo/environment/stardew/stardew_env.py` under the `StarDojo` class.  
> Remember to also update the prompt templates to match any changes in the observation structure.

---

## 📌 Summary

For contributions or questions, please refer to the repository README or open an issue.

Happy Simulating 🌟

---
