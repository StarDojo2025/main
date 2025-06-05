
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

> **Note**: The default observation set is constrained as below, used when a lightweight input is desired:
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