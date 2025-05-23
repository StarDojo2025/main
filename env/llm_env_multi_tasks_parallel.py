import logging
import importlib.util
import uuid
import sys

from stardew_env import *
from agent.stardojo.stardojo_react_agent import *
from tasks.base import *
from env.tasks.utils import load_task
import env.tasks.open as debug_task
from env.tasks.utils.init_task import InitTaskProxy
from typing import Any
from pathlib import Path
import logging
from typing import Any, Callable, Dict, List, Optional, Sequence, Tuple, Type, Union
import argparse
import multiprocessing as mp
from queue import Empty
from multiprocessing import Manager
from stable_baselines3.common.vec_env.base_vec_env import (
    CloudpickleWrapper,
    VecEnv,
    VecEnvIndices,
    VecEnvObs,
    VecEnvStepReturn,
)
from stable_baselines3.common.vec_env.patch_gym import _patch_env
import time
import signal

def _worker(
    remote: mp.connection.Connection,
    parent_remote: mp.connection.Connection,
    env_fn_wrapper: CloudpickleWrapper,
    task_queue: mp.Queue = None,
) -> None:

    parent_remote.close()
    env = _patch_env(env_fn_wrapper.var())
    env.set_task_queue(task_queue)
    reset_info: Optional[Dict[str, Any]] = {}
    while True:
        try:
            cmd, data = remote.recv()
            if cmd == 'reset':
                result = env.reset()
                remote.send((result))
            elif cmd == "get_queue_empty_attri":
                result = env.get_queue_empty_attri()
                remote.send((result))
            elif cmd == "set_agent":
                result = env.set_agent()
                remote.send((result))
            elif cmd == "pipeline_shutdown":
                result = env.pipeline_shutdown()
                remote.send((result))
            elif cmd == "_get_obs":
                observation = env._get_obs()
                remote.send((observation))
            elif cmd == "step":
                observation, reward, done, truncated, info = env.step(data)
                remote.send((observation, reward, done, truncated, info))
            else:
                raise NotImplementedError(f"`{cmd}` is not implemented in the worker")
        except Exception as e:
            print(f"An error occurred: {e}")
            break


class SubprocVecEnv():
    """
    Creates a multiprocess vectorized wrapper for multiple environments, distributing each environment to its own
    process, allowing significant speed up when the environment is computationally complex.

    For performance reasons, if your environment is not IO bound, the number of environments should not exceed the
    number of logical cores on your CPU.

    .. warning::

        Only 'forkserver' and 'spawn' start methods are thread-safe,
        which is important when TensorFlow sessions or other non thread-safe
        libraries are used in the parent (see issue #217). However, compared to
        'fork' they incur a small start-up cost and have restrictions on
        global variables. With those methods, users must wrap the code in an
        ``if __name__ == "__main__":`` block.
        For more information, see the multiprocessing documentation.

    :param env_fns: Environments to run in subprocesses
    :param start_method: method used to start the subprocesses.
           Must be one of the methods returned by multiprocessing.get_all_start_methods().
           Defaults to 'forkserver' on available platforms, and 'spawn' otherwise.
    """

    def __init__(self, env_fns: List[Callable[[], gym.Env]], start_method: Optional[str] = None, task_queue: mp.Queue = None,):
        self.waiting = False
        self.closed = False
        n_envs = len(env_fns)

        if start_method is None:
            # Fork is not a thread safe method (see issue #217)
            # but is more user friendly (does not require to wrap the code in
            # a `if __name__ == "__main__":`)
            forkserver_available = "forkserver" in mp.get_all_start_methods()
            start_method = "forkserver" if forkserver_available else "spawn"
        ctx = mp.get_context(start_method)

        self.remotes, self.work_remotes = zip(*[ctx.Pipe() for _ in range(n_envs)])
        self.processes = []
        for work_remote, remote, env_fn in zip(self.work_remotes, self.remotes, env_fns):
            args = (work_remote, remote, CloudpickleWrapper(env_fn), task_queue)
            # daemon=True: if the main process crashes, we should not cause things to hang
            process = ctx.Process(target=_worker, args=args, daemon=True)  # type: ignore[attr-defined]
            process.start()
            self.processes.append(process)
            work_remote.close()

        self.num_envs = len(env_fns)
        # seeds to be used in the next call to env.reset()
        self._seeds: List[Optional[int]] = [None for _ in range(self.num_envs)]
        # options to be used in the next call to env.reset()
        self._options: List[Dict[str, Any]] = [{} for _ in range(self.num_envs)]

    def reset(self, ):
        for env_idx, remote in enumerate(self.remotes):
            remote.send(("reset", None))
        results = []
        for env_idx, remote in enumerate(self.remotes):
            result = remote.recv()
            results.append(result)
        return results

    def _get_obs(self):
        for env_idx, remote in enumerate(self.remotes):
            try:
                remote.send(("_get_obs", None))
            except Exception as e:
                print(f"Error sending data to remote {env_idx}: {e}")
        results = []
        for env_idx, remote in enumerate(self.remotes):
            try:
                result = remote.recv()
                results.append(result)
            except EOFError:
                print(f"Remote {env_idx} connection closed unexpectedly.")
                results.append(None)  
            except Exception as e:
                print(f"Error receiving data from remote {env_idx}: {e}")
                results.append(None) 
        obs = results  
        return obs

    def set_agent(self, ):
        for env_idx, remote in enumerate(self.remotes):
            remote.send(("set_agent", None))
        results = []
        for env_idx, remote in enumerate(self.remotes):
            result = remote.recv()
            results.append(result)
        return results

    def pipeline_shutdown(self):
        for env_idx, remote in enumerate(self.remotes):
            try:
                remote.send(("pipeline_shutdown", None))
            except Exception as e:
                print(f"Error sending data to remote {env_idx}: {e}")
        results = []
        for env_idx, remote in enumerate(self.remotes):
            try:
                result = remote.recv()
                results.append(result)
            except EOFError:
                print(f"Remote {env_idx} connection closed unexpectedly.")
                results.append(None) 
            except Exception as e:
                print(f"Error receiving data from remote {env_idx}: {e}")
                results.append(None) 
        return results

    def step(self,):
        for env_idx, remote in enumerate(self.remotes):
            remote.send(("step", None))
        results = []
        for env_idx, remote in enumerate(self.remotes):
            result = remote.recv()
            results.append(result)
        obs, rews, dones, truncated, infos  = zip(*results)  # type: ignore[assignment]
        
        return obs, rews, dones, truncated, infos

    def get_queue_empty_attri(self):
        for env_idx, remote in enumerate(self.remotes):
            try:
                remote.send(("get_queue_empty_attri", None))
            except Exception as e:
                print(f"Error sending data to remote {env_idx}: {e}")
        results = []
        for env_idx, remote in enumerate(self.remotes):
            try:
                result = remote.recv()
                results.append(result)
            except EOFError:
                print(f"Remote {env_idx} connection closed unexpectedly.")
                results.append(None)  
            except Exception as e:
                print(f"Error receiving data from remote {env_idx}: {e}")
                results.append(None) 
        return results


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
            self, port: int = 6000,
            save_index: int = 0,
            new_game: bool = True,
            is_RL: bool = False,
            image_save_path: str = None,
            agent: PipelineRunner = None,
            task: TaskBase = None,
            image_obs: bool = False,
            needs_pausing: bool = True,
            env_id=0,
            llm_provider_config_path= None,
            embed_provider_config_path= None,
            use_self_reflection= False,
            use_task_inference= False,
            envconfig = None,
            output_video: bool = False,
    ) -> None:
        
        time.sleep(env_id * 0.3)
        self.log_dir_name = str(port) + str(time.time())
        super().__init__(port, save_index, new_game, is_RL, image_save_path, output_video=output_video)
        time.sleep(5)
        self.agent = None 
        self.task = None
        self.needs_pausing = needs_pausing
        # self.skill_executer = SkillExecutor(actionproxy=self.action_proxy)
        self.skill_executer = None
        self.last_action = None
        self.image_obs = image_obs
        self.task_proxy = InitTaskProxy(port)

        self.config = None
        self.logger = None

        self.skill_steps = None
        self.terminated = False
        self.truncated = False
        self.step_num = 0
        self.env_id = env_id
        self.task_queue = None
        self.llm_provider_config_path = llm_provider_config_path
        self.embed_provider_config_path = embed_provider_config_path
        self.use_self_reflection = use_self_reflection
        self.use_task_inference = use_task_inference
        self.envconfig = envconfig
        self.max_turn_count = None
        self.if_task_queue_empty = False
        self.current_task_finsh = True
        self.task_config = None

    def get_queue_empty_attri(self):
        return self.if_task_queue_empty

    def set_task_queue(self, task_queue):
        self.task_queue = task_queue

    def reset(self, ) -> bool:
        time.sleep(self.env_id * 0.3)
        self.action_proxy = actions.ActionProxy(self.port)
        self.skill_executer = SkillExecutor(actionproxy=self.action_proxy)

        try:
            if self.current_task_finsh:
                self.task_config = self.task_queue.get_nowait()
            task = load_task.load_task(**self.task_config)
            if task.difficulty == "easy":
                self.max_turn_count = 30
            elif task.difficulty == "medium":
                self.max_turn_count = 50
            else:
                self.max_turn_count = 200

            self.task = task

            if self.new_game and os_type == "Linux":
                while not is_port_available(self.port):  
                    find_and_kill_process_by_port(range(self.port, self.port + 1))
                while is_port_available(self.port):  
                    if os_type == "Linux":
                        subprocess.Popen(
                            ["xvfb-run", "-a", "-s", f"-screen 0 1280x720x24", LAUNCH_PATH, PORT_ARG, str(self.port),
                             SAMPLE_RATE, "100"],
                            stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
                            stderr=subprocess.DEVNULL, )
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
                    self.action_proxy = actions.ActionProxy(self.port)
                    self.action_proxy.wait_for_server()
                    time.sleep(1)

            self.action_proxy.wait_for_server()
            self.task.init_task(self.task_proxy)
            self.step_num = 0
            self.terminated = False
            self.truncated = False
            return True

        except Empty:

            self.if_task_queue_empty = True
            return False


    def set_agent(self,):
        react_agent_config = {"llm_provider_config_path": self.llm_provider_config_path,
                              "embed_provider_config_path": self.embed_provider_config_path,
                              "task_description": self.task.llm_description,
                              "use_self_reflection": self.use_self_reflection,
                              "use_task_inference": self.use_task_inference}

        agent = PipelineRunner(**react_agent_config, envConfig=self.envconfig, max_turn_count=self.max_turn_count, log_dir_name=self.log_dir_name)
        self.config = agent.get_config()
        self.logger = agent.reconfigure_root_logger(port=self.port, task=self.task.llm_description)
        self.agent = agent
        return  True

    def pipeline_shutdown(self):
        self.agent.pipeline_shutdown()
        return None

    def get_last_part(self, s):
        if isinstance(s, str):
            return s.split('.')[-1]
        else:
            try:
                s = str(s)
                return s.split('.')[-1]
            except:
                return s

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

    def _tile_info_preprocess(self, obs: dict):
        surroundings = obs['surroundingsdata']
        center_postion = obs['player']['position']  # [x_c, y_c]
        new_surroundings = []
        for tile in surroundings:
            new_tile = {}
            abs_position = tile['position']  # [x, y]

            rel_position = [abs_position[0] - center_postion[0], abs_position[1] - center_postion[1]]  
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
                new_tile['object'] = objects  # else, no object list!

            if tile['exit_info'] != '':
                new_tile['exit to other scene (go to this tile and you will move to other scene)'] = tile['exit_info']

            if tile['npc_info'] != '':
                new_tile['npc on this tile'] = tile['npc_info']

            if tile['tile_properties'] != '':
                new_tile['tile_properties'] = tile['tile_properties']

            new_surroundings.append((new_tile))

        obs['surroundings'] = self.__tile_postprocess(new_surroundings)  # update
        obs.pop('surroundingsdata')

        return obs

    def _process_index(self, obs: dict):
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

    def _process_obs(self, obs: dict):
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
        try:
            if self.terminated or self.truncated:  
                self.current_task_finsh = True
                if_reset_sucess = self.reset()
                if if_reset_sucess:
                    self.set_agent()
                    return None, 0, False, False, {}

                return None, 0, True, False, {}
            else:
                self.action_proxy.set_mmap_reader()
                self.logger.write(f"Port {self.port}: Running Task: {self.task.llm_description} "
                            f"on {str(self.agent.llm_provider.provider_cfg)}, Step {self.step_num}")

                obs = self._get_processed_obs()

                if self.needs_pausing:
                    self.logger.write(f"Port {self.port}: Starting to plan, the game is paused.")
                    self.action_proxy.pause_game()
                try:
                    skill_steps = self.agent.run_planning(obs, step_num=self.step_num, image_obs=self.image_obs)
                except Exception as e:
                    logging.log(logging.ERROR, f"Error in planning: {e}")
                    if self.needs_pausing:
                        logging.log(logging.INFO, f"Finished planning, the game is resumed.")
                        self.action_proxy.resume_game()
                    return self.obs, 0, False, False, {}
                if self.needs_pausing:
                    self.logger.write(f"Port {self.port}: Finished planning, the game is resumed.")
                    self.action_proxy.resume_game()
                action = skill_steps
                exec_info = self.agent.gm.execute_actions(action, self.skill_executer) 

                self.step_num += 1
                self.step_count += 1
                if len(action) != 0:
                    self.last_action = action[0]

                res_params = {
                    "exec_info": exec_info,
                }

                self.agent.memory.update_info_history(res_params)

                # assert self.observation_space.contains(self.obs)
                info = {
                    "records": exec_info
                }
                obs = self._get_obs()
                self.obs = obs
                self.terminated = self.task.evaluate(self.obs, self.task_proxy)['completed']

                if self.step_num % self.config.checkpoint_interval == 0:
                    self.memory_save()

                self.logger.write(
                            f"Port {self.port}: Current Quantity: {self.task.current_quantity}, Completion: {self.terminated}")

                if self.step_num > self.config.max_turn_count:
                    self.logger.write('Port {}: Max steps reached, exiting.'.format(self.port))
                    self.truncated = True
                    self.pipeline_shutdown()

                if self.terminated:
                    self.logger .write('Port {}: Satisfied task completion condition, exiting.'.format(self.port))
                    self.pipeline_shutdown()

                return self.obs, 0, self.terminated, self.truncated, info
        except AssertionError as e:
            self.current_task_finsh = False
            self.logger.write(
                f"Port {self.port}: got a error {e}, and we will restart it!!!!!!!!!!!!")
            if_reset_sucess = self.reset()
            if if_reset_sucess:
                self.set_agent()
                return None, 0, False, False, {}

            return None, 0, True, False, {}

    def memory_save(self):
        checkpoint_path = os.path.join(self.agent.checkpoint_path, 'checkpoint_{:06d}.json'.format(self.step_num))
        # self.agent.memory.save(checkpoint_path)


if __name__ == "__main__":
    forkserver_available = "forkserver" in mp.get_all_start_methods()
    start_method = "forkserver" if forkserver_available else "spawn"
    mp.set_start_method(start_method, force=True)

    logging.basicConfig(
        filename='app.log',  
        filemode='a',  
        format='%(asctime)s - %(levelname)s - %(message)s',  
        datefmt='%Y-%m-%d %H:%M:%S',  
        level=logging.INFO 
    )

    parser = argparse.ArgumentParser(description="Parallel StarDojoLLM Runner")
    parser.add_argument("--llm_config", type=str, default="./conf/openai_config.json")
    parser.add_argument("--embed_config", type=str, default="./conf/openai_config.json")
    parser.add_argument("--env_config", type=str, default="./conf/env_config_stardew.json")
    parser.add_argument("--parallel_numb", type=int, default=1)
    parser.add_argument("--start_port", type=int, default=10783)
    parser.add_argument("--task_params", type=str, default='[{"type": "farming", "id": 0}]', help="Task queue config (JSON list)")
    args = parser.parse_args()

    llmProviderConfig = args.llm_config
    embedProviderConfig = args.embed_config
    envConfig = args.env_config

    task_list = mp.Queue()
    for config in json.loads(args.task_params):
        task_list.put(config)

    env_params = []
    port = args.start_port
    for i in range(args.parallel_numb):
        each_env_params = {
            'port': port,
            'save_index': i,
            'new_game': True,
            'image_save_path': f"../agent/screen_shot_buffer{i}",
            'needs_pausing': True,
            'image_obs': True,
            "env_id": i,
            "llm_provider_config_path": llmProviderConfig,
            "embed_provider_config_path": embedProviderConfig,
            "use_self_reflection": False,
            "use_task_inference": False,
            "envconfig": envConfig,
        }
        env_params.append(each_env_params)
        port += 1

    ports_to_clear = range(args.start_port, args.start_port + len(env_params))
    find_and_kill_process_by_port(ports_to_clear)

    def make_env(params):
        return lambda: StarDojoLLM(**params)

    env = SubprocVecEnv([make_env(p) for p in env_params], start_method=start_method, task_queue=task_list)

    env.reset()
    env.set_agent()

    task_list_empty = False
    finished = False
    while (not task_list_empty) | (not finished):
        try:
            obs, reward, terminated_each, truncated_each, info = env.step()
            queue_empty = env.get_queue_empty_attri()
            task_list_empty = np.any(np.array(queue_empty))
            finished = np.all(np.array(terminated_each) | np.array(truncated_each))

        except KeyboardInterrupt:
            logging.error('KeyboardInterrupt Ctrl+C detected, exiting.')
            env.pipeline_shutdown()
            find_and_kill_process_by_port(ports_to_clear)
            break


