import json
import os




base_dir = os.path.dirname(os.path.abspath(__file__))
crafting_recipes_path = os.path.join(base_dir, 'game_data/CraftingRecipes.json')
_crafting_recipes = json.load(open(crafting_recipes_path))





def move(x: int, y: int) -> None:
    action = [-114514]*10 
    action[0] = 1 # move
    action[1] = 0 # turn
    action[2] = 0 # func
    action[3] = 0 # cradr iten id
    action[4] = 0 # item slot
    action[5] = 0 # direction
    action[6] = 0 # choose option index
    action[7] = x # possition x
    action[8] = y # possition y
    action[9] = 0 # quantity
    return action

def move_step(direction: int) -> None:
    action = [-114514]*10 
    action[0] = 1 # move
    action[1] = 0 # turn
    action[2] = 0 # func
    action[3] = 0 # cradr iten id
    action[4] = 0 # item slot
    action[5] = direction # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action

def craft(item_id: int) -> None:
    action = [-114514]*10 
    action[0] = 0 # move
    action[1] = 0 # turn
    action[2] = 3 # func
    action[3] = item_id # cradr iten id
    action[4] = 0 # item slot
    action[5] = 0 # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action

def turn(direction: int) -> None:
    action = [-114514]*10 
    action[0] = 0 # move
    action[1] = 1 # turn
    action[2] = 0 # func
    action[3] = 0 # cradr iten id
    action[4] = 0 # item slot
    action[5] = direction # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action


def use() -> None:
    action = [-114514]*10 
    action[0] = 0 # move
    action[1] = 0 # turn
    action[2] = 1 # func
    action[3] = 0 # cradr iten id
    action[4] = 0 # item slot
    action[5] = 0 # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action

def choose_item(slot_index: int) -> None:
    action = [-114514]*10 
    action[0] = 0 # move
    action[1] = 0 # turn
    action[2] = 5 # func
    action[3] = 0 # cradr iten id
    action[4] = slot_index # item slot
    action[5] = 0 # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action

def interact() -> None:
    action = [-114514]*10 #     
    action[0] = 0 # move
    action[1] = 0 # turn
    action[2] = 2 # func
    action[3] = 0 # cradr iten id
    action[4] = 0 # item slot
    action[5] = 0 # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action

def choose_option(option_index: int, quantity: int = None, x: int = None, y: int = None) -> None:
    action = [-114514]*10 #     
    action[0] = 0 # move
    action[1] = 0 # turn
    action[2] = 4 # func
    action[3] = 0 # cradr iten id
    action[4] = 0 # item slot
    action[5] = 0 # direction
    action[6] = option_index # choose option index
    action[7] = x # possition x
    action[8] = y # possition y
    action[9] = quantity # quantity
    return action

def attach(slot_index: int) -> None:
    action = [-114514]*10 #     
    action[0] = 0 # move
    action[1] = 0 # turn
    action[2] = 6 # func
    action[3] = 0 # cradr iten id
    action[4] = slot_index # item slot
    action[5] = 0 # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action

def observe() -> str:
    action = [-114514]*10 #     
    print("Warning: we do not provide observe option as it will automatically provide after any call")
    return action

def unattach() -> None:
    action = [-114514]*10 #     
    action[0] = 0 # move
    action[1] = 0 # turn
    action[2] = 7 # func
    action[3] = 0 # cradr iten id
    action[4] = 0 # item slot
    action[5] = 0 # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action

def exit_menu():
    action = [-114514]*10 #     
    action[0] = 0 # move
    action[1] = 0 # turn
    action[2] = 4 # func
    action[3] = 0 # cradr iten id
    action[4] = 0 # item slot
    action[5] = 0 # direction
    action[6] = 0 # choose option index
    action[7] = 0 # possition x
    action[8] = 0 # possition y
    action[9] = 0 # quantity
    return action