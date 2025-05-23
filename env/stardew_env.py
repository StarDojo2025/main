from __future__ import annotations
import gymnasium as gym
import json
import numpy as np
import base64
import sys
import os

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from utils import utils

sys.path.append(os.path.dirname((os.path.abspath(__file__))))
import observation
import actions
from env.utils.utils import *
import subprocess
import platform
from PIL import Image
import time
import socket
import datetime
import socket
import cv2
from collections import deque
import dotenv

dotenv.load_dotenv()

STARDEW_APP_PATH = os.getenv("STARDEW_APP_PATH")
mod_path = STARDEW_APP_PATH

base_dir = os.path.dirname(os.path.abspath(__file__))
object_id_path = os.path.join(base_dir, 'game_data/Objects.json')
_object_id_map = json.load(open(object_id_path))["content"]

os_type = platform.system()
print(f"os_type: {os_type}")
if os_type == "Windows":
    import win32con
    import win32gui

LAUNCH_PATH = os.path.expanduser(mod_path)
PORT_ARG = "--port-id"
SAMPLE_RATE = "--sample-rate" # percentage


def call_actions(action: str, print_debug : bool = True) -> None|str:
    '''
    you can use console to play the game
    '''

    # spilt it with " " space
    instruction = action.split(" ")

    inst_name = instruction[0] # get instruction name
    args = instruction[1:]

    # change type
    processed_args = []
    for arg in args:
        if check_is_number(arg):
            # Convert to int or float based on the type
            processed_args.append(
                int(arg) if check_is_int(arg) else float(arg)
            )
        else:
            processed_args.append(arg)

    # finally call the function
    # Check if the command exists
    if not hasattr(actions, inst_name):
        print(f"Warning: Unknown command: {inst_name}")

    if print_debug:
        print(f"call: {inst_name}({processed_args})")
    try:
        ret_str = getattr(actions, inst_name)(*processed_args) # pass the args into the
        if ret_str != None:
            return ret_str

    except IndexError:
        print("Error: Command requires more arguments")
    except ValueError as e:
        print(f"Error: Invalid argument value - {str(e)}")
    except TypeError as e:
        print(f"Error: Wrong number or type of arguments - {str(e)}")
    except AttributeError as e:
        print(f"Error: Invalid command or action - {str(e)}")
    except Exception as e:
        print(f"An unexpected error occurred: {str(e)}")

def is_port_available(port):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        return s.connect_ex(('127.0.0.1', port)) != 0


def find_and_kill_process_by_port(ports):
    
    system = platform.system()

    for port in ports:
        try:
            print(f"Checking port {port}...")
            if system == "Windows":
                # Windows 
                command = f'netstat -ano | findstr :{port}'
                output = subprocess.check_output(command, shell=True, encoding='utf-8')
                for line in output.splitlines():
                    if f":{port}" in line:
                        pid = line.strip().split()[-1]
                        print(f"Found process on port {port}, PID: {pid}")
                        os.system(f"taskkill /PID {pid} /F")
                        print(f"Process {pid} terminated.")
            else:
                # Linux macOS 
                command = f'lsof -i :{port}'
                output = subprocess.check_output(command, shell=True, encoding='utf-8')
                for line in output.splitlines():
                    if f":{port}" in line:
                        parts = line.split()
                        pid = parts[1]  # PID
                        print(f"Found process on port {port}, PID: {pid}")
                        os.system(f"kill -9 {pid}")
                        print(f"Process {pid} terminated.")
        except subprocess.CalledProcessError as e:
            print(f"No process is using port {port} or an error occurred: {e}")

def kill_pid(pid):
    system = platform.system()
    if system == "Windows":
        # Windows 
        os.system(f"taskkill /PID {pid} /F")
        print(f"Process {pid} terminated.")
    else:
        # Linux macOS
        os.system(f"kill -9 {pid}")
        print(f"Process {pid} terminated.")


def get_game_hwnd(original_title):
    hwnd = win32gui.FindWindow(None, original_title)
    return hwnd


class StarDojo(gym.Env):
    def __init__(
            self, port: int = 5000,
            save_index: int = 0,
            new_game: bool = True,
            is_RL: bool = False,
            image_save_path : str = None,
            saved_game_file_name: str = None,
            observe_size: int = 3,
            output_video: bool = False,
            max_image_storage: int = 2
        ) -> None:
        super(StarDojo, self).__init__()
        self.new_game = new_game
        self.port = port
        if new_game:
            while not is_port_available(self.port): 
                find_and_kill_process_by_port(range(self.port, self.port + 1))
            while is_port_available(self.port):  
                if os_type == "Linux":
                    subprocess.Popen(["xvfb-run", "-a", "-s", f"-screen 0 1280x720x24", LAUNCH_PATH, PORT_ARG, str(self.port), SAMPLE_RATE, "100"],
                                     stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
                                     stderr=subprocess.DEVNULL,)
                elif os_type == "Windows":

                    startupinfo = subprocess.STARTUPINFO()
                    startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW
                    startupinfo.wShowWindow = win32con.SW_HIDE  

                    subprocess.Popen([LAUNCH_PATH, PORT_ARG, str(self.port), SAMPLE_RATE, "100", "--background"],
                                     stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
                                     stderr=subprocess.DEVNULL,
                                     startupinfo=startupinfo)

                elif os_type == "Darwin":
                    subprocess.Popen([LAUNCH_PATH, PORT_ARG, str(self.port), SAMPLE_RATE, "100"],
                                     stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
                                     stderr=subprocess.DEVNULL, )
                action_proxy = actions.ActionProxy(self.port)
                action_proxy.wait_for_server()

        # time.sleep(1)

        self.save_index = save_index
        self.action_space = gym.spaces.MultiDiscrete([2, 2, 8, 150, 36, 5, 1, 200, 200, 1000])
        self.observation_space = observation.get_observation_space()
        self.action_proxy =  actions.ActionProxy(self.port)
        self.is_RL = is_RL
        self.image_save_path = image_save_path
        self.obs = {}
        self.saved_game_file_name = saved_game_file_name
        self.observe_size = observe_size
        
        self.image_paths = deque(maxlen=max_image_storage)
        self.step_count = 0

        self.output_video_path = f'output_{datetime.datetime.now().strftime("%m-%d %H:%M:%S")}.mp4'  
        self.frame_rate = 30 
        self.video_writer = None
        self.output_video = output_video

    def reset(
            self,
            seed = 42,
            options = None
        ) -> dict:
        self.action_proxy = actions.ActionProxy(self.port)
        self.action_proxy.set_mmap_reader()
        # We need the following line to seed self.np_random
        super().reset(seed=seed)

        self.obs = {}
        self.action_proxy.wait_for_server()
        self.action_proxy.load_game_record(self.saved_game_file_name)
        self.action_proxy.wait_game_start()

        return self._get_obs()

    def exit(self):
        if self.video_writer is not None:
            self.video_writer.release()
            cv2.destroyAllWindows()
            self.video_writer = None

    def _get_obs(self, is_rl = False) -> dict:
        # update state
        before = time.time()
        obs_json = self.action_proxy.observe()
        after = time.time()
        obs_json = json.loads(obs_json)
        # decode RGBA map of screenshot
        screen_shot_raw = obs_json['ScreenShot']
        screen_shot_raw = base64.b64decode(screen_shot_raw)
        viewport_x, viewport_y = obs_json['MetaData']['ViewportSize'][0], obs_json['MetaData']['ViewportSize'][1]
        screen_shot_np = np.frombuffer(screen_shot_raw, dtype=np.uint8)
        obs_json['ScreenShot'] = screen_shot_np.reshape(viewport_y, viewport_x, 4)

        # format player position
        obs_json['Player']['Position'] = [obs_json['Player']['Position']['X'], obs_json['Player']['Position']['Y']]

        # fill the format of observation
        observation.fill_observation_space(obs_json, self.observation_space)

        # preprocess the observation json
        obs_json_processed = self.obs_preprocess(obs_json, 3, 3)

        if is_rl:
            return obs_json_processed, obs_json
        else:
            return obs_json_processed

    # Need to be deleted later
    def debug_get_obs(self) -> dict:
        # update state
        obs_raw = self.action_proxy.observe()
        obs_json = json.loads(obs_raw)
        observation.fill_observation_space(obs_json, self.observation_space)
        return obs_json

    def obs_preprocess(self, obs: dict, obs_size_x = 3, obs_size_y = 3) -> dict:
        '''
        ### Observation preprocesser
        - obs_x, y is the view range at x, y dirction (x - obs_size_x to x + obs_size_x), recommand 3 - 4
        
        '''
        if self.image_save_path != None:
            os.makedirs(self.image_save_path, exist_ok=True)

            img_name = f"screenshot_{self.port}_{self.step_count}.jpeg"
            img_path = f"{self.image_save_path}/{img_name}"
            rgb_image = obs['ScreenShot'][:, :, :3].astype(np.uint8) # RGB, NOT A
            img = Image.fromarray(rgb_image)
            # img = img.resize((640, 320)) #debug only
            # 如果deque已满，获取并删除即将被移除的图片文件
                
            if not img_path in self.image_paths and len(self.image_paths) == self.image_paths.maxlen:
                old_img_path = self.image_paths[0]  # 获取最旧的图片路径
                if os.path.exists(old_img_path):
                    os.remove(old_img_path)  # 删除文件
            if not img_path in self.image_paths:
                self.image_paths.append(img_path)
                img.save(img_path, 'JPEG')
        else:
            img_path = "" # No img_save_path
        if self.output_video:
            rgb_image = obs['ScreenShot'][:, :, :3].astype(np.uint8)
            if self.video_writer is None:
                h, w, _ = rgb_image.shape  
                frame_size = (w, h)
                fourcc = cv2.VideoWriter.fourcc('M', 'P', '4', 'V')  
                self.video_writer = cv2.VideoWriter(self.output_video_path, fourcc, self.frame_rate, frame_size)

                
            rgb_image = cv2.cvtColor(rgb_image, cv2.COLOR_RGB2BGR)
            self.video_writer.write(rgb_image)

        surroundings = obs["SurroundingsData"]
        info_list = []
        for info in surroundings:
            if info.get("crop_at_tile") is not None and info["crop_at_tile"] != "" and info["crop_at_tile"].get("seed_id") in _object_id_map:
                info["crop_at_tile"]["seed_name"] = _object_id_map[info["crop_at_tile"]["seed_id"]]["Name"]
                del info["crop_at_tile"]["seed_id"]

            if info.get("debris_at_tile") is not None and info["debris_at_tile"] != "" and info["debris_at_tile"].strip('(O)') in _object_id_map:
                info["debris_at_tile"] = _object_id_map[info["debris_at_tile"].strip('(O)')]["Name"]

            new_info = {}
            for key in list(info.keys()):
                if info[key] != '' and info[key] != []:  
                    new_info[key] = info[key]
            info = new_info
            info_list.append(info)

        crops = obs["Crops"]
        for i, crop in enumerate(crops):
            crop_id = crop.get("id")
            if crop_id in _object_id_map:
                crop["id"] = _object_id_map[crop_id]["Name"]

        original_exits = obs["Exits"]
        exits = []
        # for exit in original_exits:
        #     exit_x = exit["position"]["X"]
        #     exit_y = exit["position"]["Y"]
        #     if exit_x >= 0 and exit_y >= 0:
        #         exits.append(exit)


        return_dict = obs
        return_dict.update({
            'basic_knowledge': [
                "1. Hoe is used to till the soil, Watering Can is used to water the soil, Pickaxe is used to break rocks, Axe is used to chop trees, Scythe is used to harvest crops.",
                "2. When you want to go through a door, move in front of it by 1 tile, and interact towards it.",
                "3. Please go to bed at night (after 18:00) even if your task is not yet complete!",
                "4. Call interact(direction) with a box, a shipping bin or anything else. Call use(direction) to use an item or tool in your inventory.",
                #"5. You have to wait multiple days for harvest before you plant crops."
            ],
            "health": str(obs["Player"]["Health"]),
            "energy": str(obs["Player"]["Stamina"]),
            "money": str(obs["Player"]["Money"]),
            "location": obs["Player"]["Location"],
            "position": obs["Player"]["Position"],
            "facing_direction": utils.get_direction_text(obs["Player"]["FacingDirection"]),
            "inventory": obs["Player"]["Inventory"],
            "chosen_item": obs["Player"]["CurrentInventory"],
            "time": str(obs["GameState"]["Time"]),
            "day": str(obs["GameState"]["DayOfMonth"]),
            "season": obs["GameState"]["Season"],
            "farm_animals": obs["Farm"]["Animals"],
            "farm_pets": obs["Farm"]["Pets"],
            "farm_buildings": obs["Farm"]["Buildings"],
            "image_paths": self.image_paths,
            "surroundings": info_list,
            "crops": crops,
            "exits": exits,
            "buildings": obs["Buildings"],
            "furniture": obs["Furnitures"],
            "npcs": obs["NPCs"],
            "shop_counters": obs["ShopCounters"],
            "current_menu": obs["CurrentMenuData"],
        })
        
        def lowercase_keys(d):
            new = {}
            for k, v in d.items():
                new_key = k.lower() if isinstance(k, str) else k
                if isinstance(v, dict):
                    new[new_key] = lowercase_keys(v)
                else:
                    new[new_key] = v
            return new

        return lowercase_keys(return_dict)

    def step(self, action: list[int]):
        if isinstance(self.action_space, gym.spaces.MultiDiscrete):
            if not self.action_space.contains(action):
                raise ValueError("Invalid action")
        else:
            raise ValueError("Invalid action space type")

        records = actions.convert_discrete_into_commands(action, self.action_proxy, self.is_RL)
        self.obs = self._get_obs()
        # assert self.observation_space.contains(self.obs)
        info = {
            "records": records
        }
        self.step_count += 1

        # observation, reward (not yet), terminated (not yet), truncated (not yet), info
        return self.obs, 0, False, False, info

    def render(self, mode='human'):
        print(f"State: {self.obs}")

    def close(self):
        pass

def find_and_kill_process_by_port(ports):
    
    system = platform.system()

    for port in ports:
        try:
            print(f"Checking port {port}...")
            if system == "Windows":
                # Windows 
                command = f'netstat -ano | findstr :{port}'
                output = subprocess.check_output(command, shell=True, encoding='utf-8')
                for line in output.splitlines():
                    if f":{port}" in line:
                        pid = line.strip().split()[-1]
                        print(f"Found process on port {port}, PID: {pid}")
                        os.system(f"taskkill /PID {pid} /F")
                        print(f"Process {pid} terminated.")
                        break
            else:
                # Linux  macOS 
                command = f'lsof -i :{port}'
                output = subprocess.check_output(command, shell=True, encoding='utf-8')
                for line in output.splitlines():
                    if f":{port}" in line:
                        parts = line.split()
                        pid = parts[1]  # PID
                        print(f"Found process on port {port}, PID: {pid}")
                        os.system(f"kill -9 {pid}")
                        print(f"Process {pid} terminated.")
                        break
        except subprocess.CalledProcessError as e:
            print(f"No process is using port {port} or an error occurred: {e}")


if __name__ == "__main__":
    '''
    test code
    '''

    # ports_to_clear = range(10783, 10784)
    # find_and_kill_process_by_port(ports_to_clear)

    env_params = {
        'port': 6000,
        'save_index': 0,
        'new_game': False,
        'image_save_path': "./screen_shot_buffer",
    }
    env = StarDojo(**env_params)


    # for i in range(10):
    #     env.action_proxy.resume_game()
    #     env.action_proxy.move(80,16)
    #     env.action_proxy.pause_game()
    # env.action_proxy.resume_game()

    # env.action_proxy.choose_option(0,2)
    # env.action_proxy.resume_game()

    # for i in range(5000):
    #     env.action_proxy.choose_item(3)
    #     env.action_proxy.use()
    #     env.action_proxy.interact()
        # env.action_proxy.choose_option(0,0)

    # env.action_proxy.move(10,9)
    # print(res)
    # obs = env.reset()
    # before = time.time()
    # print(f"Time: {after - before}")
    # before = time.time()
    # time.sleep(2)
    # env.action_proxy.interact()
    #
    # for i in range(5):
    #     env.action_proxy.turn(1)
    #     env.action_proxy.use()
    #     env.action_proxy.move_step(2)
    #     env.action_proxy.choose_item(3)
    #     env.action_proxy.turn(3)
    #     env.action_proxy.choose_option(0,0)
    #     env.action_proxy.use()
    #     env.action_proxy.move_step(1)
    # env.action_proxy.interact()
    # env.action_proxy.resume_game()

    obs = env._get_obs()
    # env.action_proxy.move(-1,22)
    # env.action_proxy.move(0,0)
    # env.action_proxy.interact()
    # env.action_proxy.resume_game()
    # env.action_proxy.choose_option(0,0)
    # env.action_proxy.move(4,17)
    # env.action_proxy.interact()
    print("debug")
    # after = time.time()
    # print(f"Time: {after - before}")
    # before = time.time()
    # obs = env.action_proxy.use()
    # after = time.time()
    # print(f"Time: {after - before}")
    # with open('output.json', 'w') as f:
    #      json.dump(obs["Farm"]["Buildings"], f)
    # print(obs)dw
    # sum = 0

    # for i in range(20):
    #     before = time.time()
    #     # obs = env.action_proxy.observe()
    #     env._get_obs()
    #     # env.action_proxy.choose_item(0)
    #     # print(f"time_point_99: {time.thread_time()}")
    #
    #     # env._get_obs()
    # # env.action_proxy.use()
    #     after = time.time()
    #     print(f"Time: {after - before}")
    #     sum+=after-before
    # print(f"Average Time: {sum/20}")
        # print(obs["Player"]["Position"])
    # env.action_proxy.move(34,5)
    # env.action_proxy.use()
    # env.action_proxy.move_step(3)
    # env.action_proxy.choose_item(4)
    # env.action_proxy.choose_option(0,0,0,0)
    # env.action_proxy.use()
    # env.action_proxy.resume_game()
    # env.action_proxy.choose_option(0,0,0,0)
    # env.action_proxy.pause_game()
    # success = env.action_proxy._post_message("get_monster_kills%Slime")
    # print(success)
    # print(obs)
    # obs = env.reset()

    # print("Welcome to the game! Type 'help' for commands, 'stop' to exit.")
    # Input = ""
    # while Input != "stop":
    #     # continuously input
    #
    #     # get Input instructions
    #
    #     Input = input("\nYour Next Action:\n")
    #     input_array = Input.split()  # split by space
    #     # convert into int array
    #     actions_array = [int(i) for i in input_array]
    #     print(f"Action0: {actions_array}")
    #     if not env.action_space.contains(actions_array):
    #         print("Invalid action")
    #         continue
    #     print(f"Action: {actions_array}")
    #     obs, reward, terminated, truncated, info = env.step(actions_array)
    #     print(info)
