import socket
import time


class InitTaskProxy:
    def __init__(self, port: int):
        self.port = port
        self.timeout = 10

    def _post_message(self, message: str, print_message: bool = False) -> str:
        try:
            host = '127.0.0.1'
            port = self.port
            client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            client_socket.connect((host, port))
            # print("connected to server")
            # if print_message:
            #     print(message.encode('utf-8'))

       
            client_socket.sendall(message.encode('utf-8'))

     
            response = []
            while True:
                data = client_socket.recv(1024)
                if not data:
                    break
                data_block = data.decode('utf-8')
                response.append(data_block)
                
                full_response_tmp = ''.join(response)
                if full_response_tmp.endswith("<EOF>"):
                    # print("message sent")
                    break

            full_response = ''.join(response)[:-5]
            # if print_message:
            # print(f"response from server(sample)：{full_response[:200]}")
            return full_response

        except Exception as e:
            print(f"Error: {e}")

        finally:
            client_socket.close()
            # if 'response' in locals():
            # print(f"connection closed，received： {len(response)} KB")

    def _wait_for_server(self):
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
                if time.time() - start_time > self.timeout:
                    print("Timeout: Server is not ready.")
                    break
                print("Waiting for server to start listening...")
                time.sleep(1)  

    def set_base_stamina(self, amount: int = 588) -> None:
        message = f"set_base_stamina%{amount}"
        self._post_message(message)

    def set_stamina(self, amount: int = None) -> None:
        message = f"set_stamina%{amount}"
        self._post_message(message)

    def set_base_health(self, amount: int = 150) -> None:
        message = f"set_base_health%{amount}"
        self._post_message(message)

    def set_health(self, amount: int = None) -> None:
        message = f"set_health%{amount}"
        self._post_message(message)

    def set_backpack_size(self, size: int = 36) -> None:
        message = f"set_backpack_size%{size}"
        self._post_message(message)

    def clear_backpack(self) -> None:
        message = f"clear_backpack"
        self._post_message(message)

    def set_money(self, amount: int = 10000000) -> None:
        message = f"set_money%{amount}"
        self._post_message(message)

    def add_item_by_id(self, id: str, count: int = 1, quality: int = 0) -> None:
        message = f"add_item_by_id%{id}%{count}%{quality}"
        self._post_message(message)

    def add_item_by_name(self, name: str, count: int = 1, quality: int = 0) -> None:
        message = f"add_item_by_name%{name}%{count}%{quality}"
        self._post_message(message)

    def world_clear(self, entity: str = "debris", location_name: str = "current") -> None:
        message = f"world_clear%{entity}%{location_name}"
        self._post_message(message)

    def place_item(self, item: str, type: str = None, x: int = None, y: int = None) -> None:
        message = f"place_item%{item}%{type}%{x}%{y}"
        self._post_message(message)

    def set_terrain(self, terrain: str, id: str = None, x: int = None, y: int = None) -> None:
        message = f"set_terrain%{terrain}%{id}%{x}%{y}"
        self._post_message(message)

    def place_crop(self, crop: str, x: int = None, y: int = None) -> None:
        message = f"place_crop%{crop}%{x}%{y}"
        self._post_message(message)

    def grow_crop(self, day: int = 100, x: int = None, y: int = None) -> None:
        message = f"grow_crop%{day}%{x}%{y}"
        self._post_message(message)

    def grow_tree(self, day: int = 100, x: int = None, y: int = None) -> None:
        message = f"grow_tree%{day}%{x}%{y}"
        self._post_message(message)

    def build(self, type: str, force: bool = True, x: int = None, y: int = None) -> None:
        message = f"build%{type}%{force}%{x}%{y}"
        self._post_message(message)

    def move_building(self, x_source: int = None, y_source: int = None, x_dest: int = None, y_dest: int = None) -> None:
        message = f"move_building%{x_source}%{y_source}%{x_dest}%{y_dest}"
        self._post_message(message)

    def remove_building(self, x: int = None, y: int = None) -> None:
        message = f"remove_building%{x}%{y}"
        self._post_message(message)

    def spawn_pet(self, type: str = "dog", breed: str = "0", name: str = None, x: int = None, y: int = None) -> None:
        message = f"spawn_pet%{type}%{breed}%{name}%{x}%{y}"
        self._post_message(message)

    def build_stable(self, x: int = None, y: int = None) -> None:
        message = f"build_stable%{x}%{y}"
        self._post_message(message)

    def spawn_animal(self, type: str, name: str = None) -> None:
        message = f"spawn_animal%{type}%{name}"
        self._post_message(message)

    def grow_animal(self, name: str = None) -> None:
        message = f"grow_animal%{name}"
        self._post_message(message)

    def animal_friendship(self, name: str = None, friendship: int = None) -> None:
        message = f"animal_friendship%{name}%{friendship}"
        self._post_message(message)

    def warp(self, location_name: str, x: int = None, y: int = None) -> None:
        message = f"warp%{location_name}%{x}%{y}"
        self._post_message(message)

    def warp_mine(self, level: int = 1) -> None:
        message = f"warp_mine%{level}"
        self._post_message(message)

    def warp_volcano(self, level: int = 1) -> None:
        message = f"warp_volcano%{level}"
        self._post_message(message)

    def warp_home(self) -> None:
        message = f"warp_home"
        self._post_message(message)

    def warp_shop(self, npc: str) -> None:
        message = f"warp_shop%{npc}"
        self._post_message(message)

    def warp_character(self, npc: str, location: str = None, x: int = None, y: int = None) -> None:
        message = f"warp_character%{npc}%{location}%{x}%{y}"
        self._post_message(message)

    def remove_item(self, x: int = None, y: int = None) -> None:
        message = f"remove_item%{x}%{y}"
        self._post_message(message)

    def set_date(self, year: int = None, season: str = None, day: int = None) -> None:
        message = f"set_date%{year}%{season}%{day}"
        self._post_message(message)

    def set_time(self, time: int = None) -> None:
        message = f"set_time%{time}"
        self._post_message(message)

    def character_tile(self) -> None:
        message = f"character_tile"
        self._post_message(message)

    def lookup(self, name: str) -> None:
        message = f"lookup%{name}"
        self._post_message(message)

    def set_deepest_mine_level(self, level: int = 120) -> None:
        message = f"set_deepest_mine_level%{level}"
        self._post_message(message)

    def set_monster_stat(self, monster: str, kills: int = 0) -> None:
        message = f"set_monster_stat%{monster}%{kills}"
        self._post_message(message)

    def complete_quest(self, id: str) -> None:
        message = f"complete_quest%{id}"
        self._post_message(message)

    def add_recipe(self, type: str, recipe: str = None) -> None:
        message = f"add_recipe%{type}%{recipe}"
        self._post_message(message)

    def upgrade_house(self, level: int = 3) -> None:
        message = f"upgrade_house%{level}"
        self._post_message(message)

    def spawn_junimo_note(self, id: str = None) -> None:
        message = f"spawn_junimo_note%{id}"
        self._post_message(message)

    def complete_room_bundle(self, id: str = None) -> None:
        message = f"complete_room_bundle%{id}"
        self._post_message(message)

    def joja_membership(self) -> None:
        message = f"joja_membership"
        self._post_message(message)

    def community_development(self, id: str = None) -> None:
        message = f"community_development%{id}"
        self._post_message(message)

    def load_game_record(self, record_name: str):
        message = f"load_game_record%{record_name}"
        self._post_message(message)
        print(f"loaded game record: {record_name}")

    def set_max_luck(self) -> None:
        message = f"set_max_luck"
        self._post_message(message)

    def print_luck(self) -> None:
        message = f"print_luck"
        self._post_message(message)

    def receive_mail(self, mail: str) -> None:
        message = f"receive_mail%{mail}"
        self._post_message(message)

    def trigger_event(self, id: str) -> None:
        message = f"trigger_event%{id}"
        self._post_message(message)

    def seen_event(self, id: str, see_or_forget: bool = True) -> None:
        message = f"seen_event%{id}%{see_or_forget}"
        self._post_message(message)

    def mark_bundle(self, id: str) -> None:
        message = f"mark_bundle%{id}"
        self._post_message(message)

    def start_quest(self, id: str) -> None:
        message = f"start_quest%{id}"
        self._post_message(message)

    def npc_friendship(self, npc: str, value: int) -> None:
        message = f"npc_friendship%{npc}%{value}"
        self._post_message(message)

    def all_npc_friendship(self, value: int) -> None:
        message = f"all_npc_friendship%{value}"
        self._post_message(message)

    def dating(self, npc: str) -> None:
        message = f"dating%{npc}"
        self._post_message(message)

    def rain(self) -> None:
        message = f"rain"
        self._post_message(message)

    def get_monster_kills(self, monster: str) -> int:
        message = f"get_monster_kills%{monster}"
        ret_str = self._post_message(message)
        return int(ret_str)

    def start_help_quest(self, type: str) -> None:
        message = f"start_help_quest%{type}"
        self._post_message(message)


if __name__ == '__main__':
    port = 10783
    proxy = InitTaskProxy(port)
    # proxy.add_item_by_name("Copper Ore", 5)
