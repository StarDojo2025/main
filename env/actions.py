import logging
import socket
import json
import os
import time
import select
import struct
import mmap
import dotenv
import platform
import cbor
import msgpack

dotenv.load_dotenv()

base_dir = os.path.dirname(os.path.abspath(__file__))
crafting_recipes_path = os.path.join(base_dir, 'game_data/CraftingRecipes.json')
_crafting_recipes = json.load(open(crafting_recipes_path))
mmap_size = 4 * 1024 * 1024  # 8MB



class CaseInsensitiveDict(dict):
    def __setitem__(self, key, value):
        super().__setitem__(key.lower(), value)

    def __getitem__(self, key):
        return super().__getitem__(key.lower())

    def __delitem__(self, key):
        return super().__delitem__(key.lower())

    def __contains__(self, key):
        return super().__contains__(key.lower())

    def get(self, key, default=None):
        return super().get(key.lower(), default)

    def pop(self, key, default=None):
        return super().pop(key.lower(), default)

    def setdefault(self, key, default=None):
        return super().setdefault(key.lower(), default)


def convert_to_case_insensitive_dict(d):
    ci_dict = CaseInsensitiveDict()
    for key, value in d.items():
        if isinstance(value, dict):
            value = convert_to_case_insensitive_dict(value)

        if isinstance(value, list):
            value_new = []
            for list_item in value:
                if isinstance(list_item, dict):
                    value_new.append(convert_to_case_insensitive_dict(list_item))
                else:
                    value_new.append(list_item)
            value = value_new
        ci_dict[key] = value
    return ci_dict


class SharedMemoryReader:
    def __init__(self, mmap_file, port):
        self.mmap_size = mmap_size

        os_type = platform.system()
        stardew_app_path = os.getenv("STARDEW_APP_PATH")
        stardew_app_parent_path = os.path.dirname(stardew_app_path)
        if os_type == "Linux":
            mmap_file = os.path.join(f"{stardew_app_parent_path}/shared_memory_{port}.bin")
        elif os_type == "Darwin":
            mmap_file = os.path.join(f"{stardew_app_parent_path}/shared_memory_{port}.bin")
        else:
            mmap_file = os.path.join(f"{stardew_app_parent_path}\shared_memory_{port}.bin")
        self.mmap_file = mmap_file
        self.f = open(self.mmap_file, "r+b")
        self.mm = mmap.mmap(self.f.fileno(), self.mmap_size, access=mmap.ACCESS_WRITE)

    def read_from_mmap(self):
        start_time = time.time()
        while True:
            self.mm.seek(0) 
            flag = struct.unpack("B", self.mm.read(1))[0]
            if time.time() - start_time > 30:
                print("Timeout: Server is not ready.")
                return None
            if flag == 1:
                self.mm.seek(4)
                length = struct.unpack("I", self.mm.read(4))[0] 
                if length > 0:
                    data = self.mm.read(length)
                    print(type(data))
                    # data = msgpack.unpackb(data, raw=False)
                    data = cbor.loads(data)
                    data = convert_to_case_insensitive_dict(data)
                    self.mm.seek(0)
                    self.mm.write(struct.pack("B", 0))
                    return data

    def close(self):
        self.mm.close()
        self.f.close()


class ActionProxy:
    def __init__(self, port: int):
        self.port = port
        self.timeout = 5
        self.mmap_reader = None

    def set_mmap_reader(self):
        self.mmap_reader = SharedMemoryReader(mmap_size, self.port)

    def _post_message(self, message: str, print_message: bool = True) -> str:

        client_socket = None
        start_time = time.time()
        try:
            host = '127.0.0.1'
            port = self.port
            client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

            
            client_socket.setblocking(False)

            if f"move" in message:
                client_socket.settimeout(30)
            elif "observe" in message or "get_surroundings" in message:
                client_socket.settimeout(30)
            elif "wait_for_server" in message:
                client_socket.settimeout(30)
            else:
                client_socket.settimeout(self.timeout)

           
            result = client_socket.connect_ex((host, port))  
            reconnect_time = 0
            while result != 0:
                assert reconnect_time <=10, "reconnect too many time, the game need restart!"
                print(message.encode('utf-8'), "reconnecting!", result)
                result = client_socket.connect_ex((host, port)) 
                reconnect_time += 1
          
            client_socket.sendall(message.encode('utf-8'))
            is_observe = "observe" in message and "observe_v2" not in message

            if is_observe:
                # print("using mmap to receive message")
                return self.mmap_reader.read_from_mmap()

            response = []
            while True:
                data = client_socket.recv(1024)
                if not data:
                    break
                data_block = data.decode('utf-8')
                response.append(data_block)
                full_response_tmp = ''.join(response)
                if full_response_tmp.endswith("<EOF>"):
                    break

            full_response = ''.join(response)[:-5]
            # if print_message:
                # print(f"response from server(sample)：{full_response[:200]}")
            return full_response

        except AssertionError as e:
            
            print(f"AssertionError: {e}")
            assert False, f"got a Error: {e}"

        except Exception as e:
            print(message.encode('utf-8'))
            print(f"Error: {e}")
            if print_message:
                full_response = ''.join(response)
                print(f"response from server(sample)：{full_response[:20]}")


        finally:
            if client_socket:
                client_socket.close()
            end_time = time.time()
            # print("running time for function ", message, " is ", end_time - start_time)

    def wait_for_server(self):
        start_time = time.time()
        host = '127.0.0.1'
        port = self.port
        while True:
            try:
                with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client_socket:
                    client_socket.connect((host, port))
                print("Server is ready and listening.")
                break

            except ConnectionRefusedError:
                if time.time() - start_time > 30:
                    print("Timeout: Server is not ready.")
                    break
                print("Waiting for server to start listening...")
                time.sleep(1)  

    def move(self, x: int, y: int) -> bool:
        message = f"move_relative%{x}%{y}"
        success = self._post_message(message)
        if success is None:
            print(f"move%{x}%{y} return value is None")
            return False
        success = (success.lower() == "true")
        if not success:
            retries = 5
            for r in range(retries):
                print(f"Retrying moving to adjacent tile retries:[{r}]")
                if r == 0:
                    message = f"move_relative%{x}%{y+1}"
                elif r == 1:
                    message = f"move_relative%{x-1}%{y}"
                elif r == 2:
                    message = f"move_relative%{x}%{y-1}"
                elif r == 3:
                    message = f"move_relative%{x+1}%{y}"
                else:
                    message = f"move_relative%{x}%{y+2}"
                success = self._post_message(message)
                if success is None:
                    print(f"{message} return value is None")
                    return False
                success = (success.lower() == "true")
                if success:
                    break
        return success

    def move_step(self, direction: int) -> None:
        if (direction < 1) or (direction > 4):
            raise ValueError("Direction must be between 1 and 4")
        message = f"move_step%{direction}"
        self._post_message(message)

    def craft(self, item_id: int) -> None:
        '''
        ### Usage
        Craft an item based on its item ID.

        ### Paramaters
        item_id: All possible items to be crafted
        '''
        crafting_dict = _crafting_recipes["content"]
        crafting_id = list(crafting_dict.keys())[item_id]
        message = f"craft%{crafting_id}"
        self._post_message(message)

    def turn(self, direction: int) -> None:
        if direction<0 or direction>3:
            print("invalid direction for turning")
            return
        message = f"turn%{direction}"
        self._post_message(message)

    def open_map(self) -> None:
        message = f"open_map"
        self._post_message(message)

    def exit_menu(self) -> None:
        message = f"exit_menu"
        self._post_message(message)
        
    def use(self, direction: int) -> None:
        '''
        ### Usage
        Use an item from the inventory, specifying the direction of use.

        ### Paramaters
        slot_index: Inventory slot indices
        direction: 0: up, 1: right, 2: down, 3: left
        '''
        self.turn(direction)
        message = f"use"
        self._post_message(message)

    def choose_item(self, slot_index: int) -> None:
        message = f"choose_item%{slot_index}"
        self._post_message(message)

    # def interact(self) -> None:
    #     message = f"interact"
    #     self._post_message(message)
        
    def interact(self, direction: int) -> None:
        '''
        ### Usage
        Use an item from the inventory, specifying the direction of use.

        ### Paramaters
        slot_index: Inventory slot indices
        direction: 0: up, 1: right, 2: down, 3: left
        '''
        self.turn(direction)
        message = f"interact"
        self._post_message(message)
        
    def choose_option(self, option_index: int, quantity: int = None, direction: int = None) -> None:
        if quantity is None:
            quantity = 0
        if direction is None:
            direction = 0
        message = f"choose_option%{option_index}%{quantity}%{direction}"
        self._post_message(message)

    def sell_current_item(self):
        message = "sell_current_item"
        self._post_message(message)

    def attach_item(self, slot_index: int) -> None:
        message = f"attach%{slot_index}"
        self._post_message(message)

    def observe(self) -> str:
        message = "observe_v2%3"
        ret_str = self._post_message(message)
        return ret_str

    def unattach_item(self) -> None:
        message = "unattach"
        self._post_message(message)

    def enter_load_menu(self) -> None:
        message = f"enter_load_menu"
        self._post_message(message)

    def load_game(self, index: int) -> None:
        message = f"load_game%{index}"
        self._post_message(message)

    def load_game_record(self, record_name: str):
        message = f"load_game_record%{record_name}"
        self._post_message(message)
        print(f"loaded game record: {record_name}")

    def exit_to_title(self) -> None:
        message = f"exit_title"
        self._post_message(message)

    def exit_menu(self):
        message = "exit_menu"
        self._post_message(message)

    def wait_game_start(self):
        message = "wait_game_start"
        self._post_message(message)
        print(f"game started")

    def pause_game(self):
        message = "pause"
        self._post_message(message)

    def resume_game(self):
        message = "resume"
        self._post_message(message)

    def get_surroundings(self, size: int) -> str:
        message = f"get_surroundings%{size}"
        ret_str = self._post_message(message)
        return ret_str

    def get_tile_info(self, x, y):
        message = f"get_tile_info%{x}%{y}"
        ret_str = self._post_message(message)
        return ret_str


def convert_discrete_into_commands(action: list[int], action_proxy: ActionProxy, is_RL = False) -> str:
    """
    Parameters:

    action (list[int]): A list containing actions.
    action_proxy (ActionProxy): A proxy object used to send instructions.
    Returns:
        str: Description of the executed command.
    """
    if len(action) != 10:
        raise ValueError("Action list must have 10 elements.")

    # action list
    move_action = action[0]
    turn_action = action[1]
    func_action = action[2]
    craft_item_id = action[3]
    item_slot = action[4]
    direction = action[5]
    choose_option_index = action[6]
    pos_x = action[7]
    pos_y = action[8]
    quantity = action[9]

    command_descriptions = []

    # Move action
    if move_action == 1:
        if direction > 0:
            action_proxy.move_step(direction)
            command_descriptions.append(f"Moved one step in direction {direction}")
        elif not is_RL:
            action_proxy.move(pos_x, pos_y)
            command_descriptions.append(f"Moved to position ({pos_x}, {pos_y})")

    # Turn action
    if turn_action > 0 and direction > 0:
        action_proxy.turn(direction-1)
        command_descriptions.append(f"Turned direction {direction-1}")

    # Functional actions
    if func_action > 0:
        if func_action == 1:  # Use
            action_proxy.use()
            command_descriptions.append(f"Used item in slot {item_slot} facing direction {direction}")
        elif func_action == 2:  # Interact
            action_proxy.interact()
            command_descriptions.append(f"Interacted facing direction {direction}")
        elif func_action == 3:  # Craft
            action_proxy.craft(craft_item_id)
            command_descriptions.append(f"Crafted item with ID {craft_item_id}")
        elif func_action == 4:  # Choose option
            if choose_option_index == 0:
                action_proxy.exit_menu()
                command_descriptions.append("Exited menu")
            else:
                read_index = choose_option_index - 1
                action_proxy.choose_option(read_index, quantity, pos_x, pos_y)
                command_descriptions.append(
                    f"Chose option {read_index} with quantity {quantity} at position ({pos_x}, {pos_y})")
        elif func_action == 5:  # Choose item
            action_proxy.choose_item(item_slot)
            command_descriptions.append(f"Chose item in slot {item_slot}")
        elif func_action == 6:  # Attach
            action_proxy.attach(item_slot)
            command_descriptions.append(f"Attached item in slot {item_slot}")
        elif func_action == 7:  # Unattach
            action_proxy.unattach()
            command_descriptions.append("Unattached item")

    return " | ".join(command_descriptions)


if __name__ == '__main__':
    port = 10783
    proxy = ActionProxy(port)
