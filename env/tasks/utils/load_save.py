import os
import platform
import shutil
import time

from .init_task import InitTaskProxy

SAVE_SOURCE = "tasks/saves"


def get_save_path() -> str:
    system = platform.system()
    if system == "Windows":
        # Windows
        save_path = os.path.join(os.getenv('APPDATA'), 'StardewValley', 'Saves')
    elif system == "Darwin":
        # macOS
        save_path = os.path.expanduser('~/.config/StardewValley/Saves')
    elif system == "Linux":
        # Linux
        save_path = os.path.expanduser('~/.config/StardewValley/Saves')
    else:
        raise Exception(f"Unsupported system: {system}")
    print(f"The save path is: {save_path}")
    return save_path


def copy_save_folder(save_type: str, overwrite: bool = True, port: int = 0):
    save_path = get_save_path()
    os.makedirs(save_path, exist_ok=True)
    source_path = os.path.join(SAVE_SOURCE, save_type)
    save_names = os.listdir(source_path)
    save_name = save_names[0] + "_Port_" + str(port)
    dest_path = os.path.join(save_path, save_name)
    source_path = os.path.join(source_path, save_names[0])

    if os.path.exists(dest_path):
        if overwrite:
            print(f"The save folder: {save_name}, already exists and will be overwritten.")
            shutil.rmtree(dest_path)
        else:
            print(f"The save folder: {save_name}, already exists. The copy operation cancels.")
            return


    os.makedirs(dest_path, exist_ok=True)
    shutil.copytree(source_path, dest_path, dirs_exist_ok=True)

    os.rename(os.path.join(dest_path, save_names[0]), os.path.join(dest_path, save_name))
    if os.path.exists(dest_path):
        print(f"The save folder: {save_name}, is copied successfully.")
    else:
        print(f"The copy operation fails.")


def load_save(proxy: InitTaskProxy, save_type: str, init_commands: list):
    copy_save_folder(save_type, port=proxy.port)
    source_path = os.path.join(SAVE_SOURCE, save_type)
    save_names = os.listdir(source_path)
    save_name = save_names[0] + "_Port_" + str(proxy.port)
    proxy.load_game_record(save_name)
    time.sleep(1)

    if init_commands is not None:
        for command in init_commands:
            exec("proxy." + command)
            time.sleep(1)
    time.sleep(1)
