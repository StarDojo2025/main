
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

