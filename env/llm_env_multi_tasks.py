import os.path
from stardew_env import *
from agent.stardojo.stardojo_react_agent import *
from tasks.base import *
import importlib.util
import types
import uuid
import sys
import logging
from env.tasks.base import *
from env.tasks.utils import load_task
import env.tasks.open as debug_task
from env.tasks.utils.init_task import InitTaskProxy
import argparse
from typing import Any

class SkillExecutor:
    def __init__(
        self,
        actionproxy: Any,
        module_name: str = "stardojo.environment.stardew.atomic_skills.basic_skills",
    ) -> None:
        self.actionproxy = actionproxy
        self.module_name = module_name
        self._load_skills()

    def _load_skills(self):
        
        unique_id = uuid.uuid4().hex
        unique_module_name = f"{self.module_name}_{unique_id}"

        
        if unique_module_name in sys.modules:
            skill_module = sys.modules[unique_module_name]
        else:
            
            spec = importlib.util.find_spec(self.module_name)
            if spec is None:
                raise ImportError(f"Module {self.module_name} not found.")

           
            skill_module = importlib.util.module_from_spec(spec)

           
            spec.loader.exec_module(skill_module)

           
            sys.modules[unique_module_name] = skill_module

        
        setattr(skill_module, 'actionproxy', self.actionproxy)

       
        for func_name in dir(skill_module):
            func = getattr(skill_module, func_name)
           
            if callable(func) and not func_name.startswith("__"):
                
                setattr(self, func_name, func)


class StarDojoLLM(StarDojo):

    def __init__(
            self, port: int = 10783,
            save_index: int = 0,
            new_game: bool = False,
            is_RL: bool = False,
            image_save_path: str = None,
            agent: PipelineRunner = None,
            task: TaskBase = None,
            image_obs: bool = False,
            needs_pausing: bool = True,
            output_video: bool = False,
    ) -> None:
        super().__init__(port, save_index, new_game, is_RL, image_save_path, output_video=output_video)
        self.agent = agent
        self.task = task
        self.needs_pausing = needs_pausing
        self.skill_executer = SkillExecutor(actionproxy=self.action_proxy)
        self.last_action = None
        self.image_obs = image_obs
        self.step_num = 0
        self.task_proxy = InitTaskProxy(port)
        if task is not None:
            self.action_proxy.wait_for_server()
            task.init_task(self.task_proxy)
            self.action_proxy.set_mmap_reader()
            self.agent.reconfigure_root_logger(port=None, task=None)
            
    def get_last_part(self, s):
        return s.split('.')[-1]

    def __generate_tile_info(self, tile):

        new_tile = []

        if "object" in tile.keys():
            new_tile.extend(tile['object'])

        if 'exit to other scene (go to this tile and you will move to other scene)' in tile.keys():
            new_tile.append(
                f"exit: {tile['exit to other scene (go to this tile and you will move to other scene)']}"
            )
        if 'npc on this tile' in tile.keys():
            new_tile.append(
                f"npc: {tile['npc on this tile']}"
            )

        if len(new_tile) == 0:
            new_tile = ["empty"]

        return new_tile

    def __tile_postprocess(self, tiles):

        new_tiles = {}

        for tile in tiles:

            if 'tile_properties' in tile.keys():
                new_tiles[f"{tile['position']}({tile['tile_properties']})"] = self.__generate_tile_info(tile)
            else:
                new_tiles[f"{tile['position']}"] = self.__generate_tile_info(tile)

        string = ""

        for tile in new_tiles.keys():
            string += f"{tile}: "
            infos = []
            for info in new_tiles[tile]:
                   infos.append(str(info))

            string += ", ".join(infos) 

            string += "\n"

        return string

    def _tile_info_preprocess(self, obs : dict):
        surroundings = obs['surroundingsdata']
        center_postion = obs['player']['position']  # [x_c, y_c]
        new_surroundings = []
        for tile in surroundings:
            new_tile = {}
            abs_position = tile['position'] #[x, y]

            rel_position = [abs_position[0] - center_postion[0], abs_position[1] - center_postion[1]] #可以测试一下反一下行不行
            new_tile['position'] = rel_position
            objects = []
            if tile['building_info'] != '':
                objects.append(self.get_last_part(tile['building_info']))

            if tile['crop_at_tile'] != None:
                if isinstance(tile['crop_at_tile'], str):
                    objects.append(tile['crop_at_tile'])
                else:
                    objects.append(tile['crop_at_tile']['seed_name'])

            if tile['debris_at_tile'] != '':
                objects.append(self.get_last_part(tile['debris_at_tile']))

            if tile['object_at_tile'] != '':
                objects.append(self.get_last_part(tile['object_at_tile']))

            if tile['terrain_at_tile'] != '':
                objects.append(self.get_last_part(tile['terrain_at_tile']))

            if tile['furniture_at_tile'] != '':
                objects.append(self.get_last_part(tile['furniture_at_tile']))

            if len(objects) != 0:
                new_tile['object'] = objects # else, no object list!

            if tile['exit_info'] != '':
                new_tile['exit to other scene (go to this tile and you will move to other scene)'] = tile['exit_info']

            if tile['npc_info'] != '':
                new_tile['npc on this tile'] = tile['npc_info']

            if tile['tile_properties'] != '':
                new_tile['tile_properties'] = tile['tile_properties']

            new_surroundings.append((new_tile))

        obs['surroundings'] = self.__tile_postprocess(new_surroundings) # update
        obs.pop('surroundingsdata')

        return obs

    def _process_index(self, obs :dict):
        inventory = obs['inventory']
        new_inventory = []
        for i, item in enumerate(inventory):
            name = item['name'] if 'name' in item else item['Name']
            quantity = item['quantity'] if 'quantity' in item else item['Quantity']
            if quantity != None:
                new_item = f"slot_index {i}: {name} (quantity: {quantity})"
            else:
                new_item = f"slot_index {i}: No item"
            new_inventory.append(new_item)

        obs['inventory'] = new_inventory

        return obs

    def _process_time(self, obs: dict):

        time_val = obs['time']

        if isinstance(time_val, str):
            time_val = int(time_val)

        time_str = f"{time_val:04d}"  

        hour = int(time_str[:2])  
        minute = time_str[2:] 

        if hour < 12:
            period = "AM"
            display_hour = hour if hour != 0 else 12 
        else:
            period = "PM"
            display_hour = hour - 12 if hour != 12 else 12  

        if hour >= 24:
            display_hour = hour - 24
            period = "AM"

        new_time = f"{display_hour}:{minute} {period}"
        obs['time'] = new_time

        return obs


    def _process_obs(self, obs : dict):
        obs = self._tile_info_preprocess(obs)
        obs = self._process_index(obs)
        obs = self._process_time(obs)
        # XX Date XX time
        return obs

    def _get_obs(self):
        obs = super()._get_obs()
        obs['action'] = self.last_action
        return obs

    def _get_processed_obs(self):
        obs = super()._get_obs()
        obs['action'] = self.last_action
        return self._process_obs(obs)

    def step(self, autoAction=None):
        obs = self._get_processed_obs()
        if self.needs_pausing:
            logging.log(logging.INFO, f"Starting to plan, the game is paused.")
            self.action_proxy.pause_game()
        try:
            skill_steps = self.agent.run_planning(obs, image_obs=self.image_obs, step_num = self.step_num)
        except Exception as e:
            logging.log(logging.ERROR, f"Error in planning: {e}")
            if self.needs_pausing:
                logging.log(logging.INFO, f"Finished planning, the game is resumed.")
                self.action_proxy.resume_game()
            return self.obs, 0, False, False, {}
        if self.needs_pausing:
            logging.log(logging.INFO, f"Finished planning, the game is resumed.")
            self.action_proxy.resume_game()
        action = skill_steps
        exec_info = self.agent.gm.execute_actions(action, self.skill_executer)

        self.last_action = action[0]

        self.step_num += 1

        res_params = {
            "exec_info": exec_info,
        }

        self.agent.memory.update_info_history(res_params)

        
        info = {
            "records": exec_info
        }
        obs = self._get_obs()
        self.obs = obs
        # observation, reward (not yet), terminated (not yet), truncated (not yet), info
        self.step_count += 1
        
        return self.obs, 0, self.task.evaluate(self.obs, self.task_proxy)['completed'], False, info
        # return self.obs, 0, False, False, info


def run_stardojo_batch(
    llm_config_path: str,
    embed_config_path: str,
    env_config_path: str,
    tasks_params: list,
    epoch_num: int = 1,
    port: int = 6000,
    save_index: int = 0,
    new_game: bool = False,
    image_save_path: str = "../env/screen_shot_buffer",
    output_video: bool = True,
    checkpoint_interval: int = 5
):

    logging.basicConfig(
        filename='app.log',
        filemode='a',
        format='%(asctime)s - %(levelname)s - %(message)s',
        datefmt='%Y-%m-%d %H:%M:%S',
        level=logging.INFO
    )

    config.checkpoint_interval = checkpoint_interval

    for epoch in range(epoch_num):
        for task_param in tasks_params:
            config.load_env_config(env_config_path)
            task = load_task.load_task(task_param['task_name'], task_param['task_id'])
            
            if task.difficulty == "easy":
                config.max_turn_count = 30
            elif task.difficulty == "medium":
                config.max_turn_count = 50
            else:
                config.max_turn_count = 200

            react_agent = PipelineRunner(
                llm_provider_config_path=llm_config_path,
                embed_provider_config_path=embed_config_path,
                task_description=task.llm_description,
                use_self_reflection=False,
                use_task_inference=False
            )
            atexit.register(exit_cleanup, react_agent)

            env = StarDojoLLM(
                port=port,
                save_index=save_index,
                new_game=new_game,
                image_save_path=image_save_path,
                agent=react_agent,
                needs_pausing=True,
                image_obs=True,
                task=task,
                output_video=output_video
            )
            time.sleep(1)

            terminated = truncated = False
            step = 0
            while not terminated and not truncated:
                try:
                    logging.info(f"Running Task: {task.llm_description} Step {step}")
                    obs, reward, terminated, truncated, info = env.step()
                    step += 1

                    if step % checkpoint_interval == 0:
                        checkpoint_path = os.path.join(react_agent.checkpoint_path, f'checkpoint_{step:06d}.json')
                        # react_agent.memory.save(checkpoint_path)

                    if step > config.max_turn_count:
                        print('Max steps reached, exiting.')
                        break

                    if terminated:
                        env.action_proxy.exit_to_title()
                        print('Task completed, exiting.')
                        break

                except KeyboardInterrupt:
                    print('Interrupted by user.')
                    react_agent.pipeline_shutdown()
                    env.exit()
                    break

            react_agent.pipeline_shutdown()
            env.exit()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Run StarDojo LLM Task Batch")
    parser.add_argument("--epoch_num", type=int, default=1, help="How many times to repeat task list")
    parser.add_argument("--llm_config", type=str, default="./conf/openai_config.json")
    parser.add_argument("--embed_config", type=str, default="./conf/openai_config.json")
    parser.add_argument("--env_config", type=str, default="./conf/env_config_stardew.json")
    parser.add_argument("--port", type=int, default=6000)
    parser.add_argument("--save_index", type=int, default=0)
    parser.add_argument("--new_game", action="store_true")
    parser.add_argument("--output_video", action="store_true")
    parser.add_argument("--image_save_path", type=str, default="../env/screen_shot_buffer")
    parser.add_argument(
    "--task_params", type=str,
    default='[{\"task_name\": \"farming\", \"task_id\": 0}, {\"task_name\": \"farming\", \"task_id\": 3}]',
    help="Task parameters as JSON string"
)

    args = parser.parse_args()

    tasks_params = json.loads(args.task_params)

    run_stardojo_batch(
        llm_config_path=args.llm_config,
        embed_config_path=args.embed_config,
        env_config_path=args.env_config,
        tasks_params=tasks_params,
        epoch_num=args.epoch_num,
        port=args.port,
        save_index=args.save_index,
        new_game=args.new_game,
        image_save_path=args.image_save_path,
        output_video=args.output_video
    )
