import os
import atexit
from typing import Dict, Any
from copy import deepcopy


from stardojo.utils.dict_utils import kget
from stardojo.utils.string_utils import replace_unsupported_chars
from stardojo import constants
from stardojo.log import Logger
from stardojo.config import Config
from stardojo.memory import LocalMemory
from stardojo.provider.llm.llm_factory import LLMFactory
from stardojo.environment.skill_registry_factory import SkillRegistryFactory
from stardojo.environment.ui_control_factory import UIControlFactory
from stardojo.gameio.io_env import IOEnvironment
from stardojo.gameio.game_manager import GameManager
from stardojo.planner.stardew_planner import StardewPlanner
from log_processor import process_log_messages
from env.stardew_env import *
import logging
from env.env_constants import *

from stardojo.provider import (
    StardewSelfReflectionPostprocessProvider,
    StardewSelfReflectionProvider,
    StardewSelfReflectionPreprocessProvider,
    StardewTaskInferencePostprocessProvider,
    StardewTaskInferencePreprocessProvider,
    StardewTaskInferenceProvider,
    SkillCurationProvider,
    AugmentProvider
)

config = Config()
logger = Logger()
io_env = IOEnvironment()

logging.basicConfig(
    filename='app.log',          # log
    filemode='a',                # additional mode
    format='%(asctime)s - %(levelname)s - %(message)s',  # format of log
    datefmt='%Y-%m-%d %H:%M:%S', # time format
    level=logging.INFO           # logs (DEBUG, INFO, WARNING, ERROR, CRITICAL)
)

class PipelineRunner():

    def __init__(self,
                 llm_provider_config_path: str,
                 embed_provider_config_path: str,
                 task_description: str,
                 use_self_reflection: bool = False,
                 use_task_inference: bool = False,
                 envConfig = None,
                 max_turn_count = None,
                 log_dir_name = None):

        self.llm_provider_config_path = llm_provider_config_path
        self.embed_provider_config_path = embed_provider_config_path

        self.task_description = task_description
        self.use_self_reflection = use_self_reflection
        self.use_task_inference = use_task_inference

        if envConfig is not None:
            config.load_env_config(envConfig)
        if max_turn_count is not None:
            config.max_turn_count = max_turn_count
        if log_dir_name is not None:
            config.set_work_dir_base(log_dir_name)
            config._set_dirs()

        # config.work_dir =
        config.set_logger_dirs()

        # Init internal params
        self.set_internal_params()

    def get_config(self):
        return config

    def reconfigure_root_logger(self, port=None, task=None):
        logger._configure_root_logger(port=port, task=task)
        return logger

    def set_internal_params(self, *args, **kwargs):

        self.provider_configs = config.provider_configs

        # Init LLM and embedding provider(s)
        lf = LLMFactory()
        self.llm_provider, self.embed_provider = lf.create(self.llm_provider_config_path,
                                                           self.embed_provider_config_path)

        srf = SkillRegistryFactory()
        srf.register_builder(config.env_short_name, config.skill_registry_name)
        self.skill_registry = srf.create(config.env_short_name, skill_configs=config.skill_configs,
                                         embedding_provider=self.embed_provider)

        ucf = UIControlFactory()
        ucf.register_builder(config.env_short_name, config.ui_control_name)
        self.env_ui_control = ucf.create(config.env_short_name)

        # Init game manager
        self.gm = GameManager(env_name=config.env_name,
                              embedding_provider=self.embed_provider,
                              llm_provider=self.llm_provider,
                              skill_registry=self.skill_registry,
                              ui_control=self.env_ui_control,
                              )

        self.memory = LocalMemory()

        # Init planner
        self.planner = StardewPlanner(llm_provider=self.llm_provider,
                                   planner_params=config.planner_params,
                                   frame_extractor=None,
                                   icon_replacer=None,
                                   object_detector=None,
                                   use_self_reflection=True,
                                   use_task_inference=True)

        # Init skill library
        skills = self.gm.retrieve_skills(query_task=self.task_description,
                                         skill_num=config.skill_configs[constants.SKILL_CONFIG_MAX_COUNT],
                                         screen_type=constants.GENERAL_GAME_INTERFACE)

        self.skill_library = self.gm.get_skill_information(skills, config.skill_library_with_code)

        self.memory.update_info_history({"skill_library": self.skill_library})

        self.provider_configs = config.provider_configs

        # Init checkpoint path
        self.checkpoint_path = os.path.join(config.work_dir, 'checkpoints')
        os.makedirs(self.checkpoint_path, exist_ok=True)

        self.augment = AugmentProvider()
        self.augment_methods = [
            self.augment
        ]

        self.self_reflection_preprocess = StardewSelfReflectionPreprocessProvider(gm=self.gm,
                                                                                  augment_methods=self.augment_methods)
        self.self_reflection = StardewSelfReflectionProvider(planner=self.planner, gm=self.gm)
        self.self_reflection_postprocess = StardewSelfReflectionPostprocessProvider(gm=self.gm)

        self.task_inference_preprocess = StardewTaskInferencePreprocessProvider(gm=self.gm)
        self.task_inference = StardewTaskInferenceProvider(planner=self.planner, gm=self.gm)
        self.task_inference_postprocess = StardewTaskInferencePostprocessProvider(gm=self.gm)

        init_params = {
            "task_description": self.task_description,
            "skill_library": self.skill_library,
            "exec_info": {
                "errors": False,
                "errors_info": ""
            },
            "action": "",
            "action_planning_reasoning": "",
            "self_reflection_reasoning": "",
            "summarization": "",
            # "toolbar_information": None,
            "subtask_description": "",
            "subtask_reasoning": "",
        }

        self.memory.update_info_history(init_params)

    def pipeline_shutdown(self):

        self.gm.cleanup_io()
        # self.video_recorder.finish_capture()

        log = process_log_messages(config.work_dir)

        with open(config.work_dir + f'/logs/{self.task_description}_log.md', 'a') as f:
            log = replace_unsupported_chars(log)
            f.write(log)

        logger.write('>>> Markdown generated.')
        logger.write('>>> Bye.')

    def skill_curation(self):
        all_generated_actions = self.memory.get_recent_history("all_generated_actions", k=1)[0]

        for extracted_skills in all_generated_actions:
            extracted_skills = extracted_skills['values']
            for extracted_skill in extracted_skills:
                self.gm.add_new_skill(skill_code=extracted_skill['code'])

        skills = self.gm.retrieve_skills(query_task=self.task_description,
                                         skill_num=config.skill_configs[constants.SKILL_CONFIG_MAX_COUNT],
                                         screen_type=constants.GENERAL_GAME_INTERFACE)

        self.skill_library = self.gm.get_skill_information(skills, config.skill_library_with_code)

        self.memory.update_info_history({"skill_library": self.skill_library})

    def run_self_reflection(self):

        logger.write("Stardew Self Reflection Preprocess")

        # 1. Prepare the parameters to call llm api
        self.self_reflection_preprocess()

        # 2. Call llm api for self reflection
        response = self.self_reflection()

        # 3. Postprocess the response
        self.self_reflection_postprocess(response)

    def run_task_inference(self):

        logger.write("Stardew Task Inferrence")

        # 1. Prepare the parameters to call llm api
        self.task_inference_preprocess()

        # 2. Call llm api for task inference
        response = self.task_inference()

        # 3. Postprocess the response
        self.task_inference_postprocess(response)
        
        
        
    def run_action_planning(self, obs, image_obs = True):
        '''
        add text_observation's memory segmentation
        '''
    

        # 1. Prepare the parameters to call llm api
        logger.write("Stardew Action Planning Preprocess")

        prompts = [
            # "Now, I will give you five screenshots for decision making."
            # "This screenshot is five steps before the current step of the game",
            # "This screenshot is three steps before the current step of the game",
            # "This screenshot is two steps before the current step of the game",
            #"This screenshot is the previous step of the game. The blue band represents the left side and the yellow band represents the right side.",
            #"This screenshot is the current step of the game. The blue band represents the left side and the yellow band represents the right side."
            "This screenshot is the previous step of the game.",
            "This screenshot is the current step of the game."
        ]

        if image_obs:
            self.memory.update_info_history({"image": obs[constants.IMAGES_INPUT_TAG_NAME][0][constants.IMAGE_PATH_TAG_NAME]}) # get image
            image_memory = self.memory.get_recent_history("image", k=config.action_planning_image_num)

            image_introduction = []
            for i in range(len(image_memory), 0, -1):
                image_introduction.append(
                    {
                        "introduction": prompts[-i],
                        "path": image_memory[-i] if image_obs else "", # "" if you don't use image
                        "assistant": ""
                    })
                processed_params = {
                    "image_introduction": image_introduction,
                    "toolbar_information": obs['inventory'],
                }
        
        processed_params.update(obs)

        self.memory.update_info_history(processed_params)



        # 2. Call llm api for action planning
        params = deepcopy(self.memory.working_area)
        data = self.planner.action_planning(input=params)
        response = data['res_dict']
        del params

        # 3. Postprocess the response
        logger.write("Stardew Action Planning Postprocess")

        processed_response = deepcopy(response)

        skill_steps = []
        if 'actions' in response:
            skill_steps = response['actions']

        if skill_steps:
            skill_steps = [i for i in skill_steps if i != '']
        else:
            skill_steps = ['']

        skill_steps = skill_steps[:config.number_of_execute_skills]

        if config.number_of_execute_skills > 1:
            actions = "[" + ",".join(skill_steps) + "]"
        else:
            actions = str(skill_steps[0])

        action_planning_reasoning = response.get('reasoning', '')
        print("Actions", actions)
        print("Action Planning Reasoning\n", action_planning_reasoning)

        processed_response.update({
            "action": actions,
            "action_planning_reasoning": action_planning_reasoning,
            "skill_steps": skill_steps,
        })
        self.memory.update_info_history(processed_response)

        # 4. Execute the actions
        params = deepcopy(self.memory.working_area)

        skill_steps = params.get("skill_steps", [])

        exec_info = self.gm.execute_actions(skill_steps)

        res_params = {
            "exec_info": exec_info,
        }

        self.memory.update_info_history(res_params)

        del params

    def run_planning(self, obs, step_num, image_obs=True):
        '''
        add text_observation's memory segmentation
        '''

        # 1. Prepare the parameters to call llm api
        logger.write("Stardew Action Planning Preprocess")

        prompts = [
            # "Now, I will give you five screenshots for decision making."
            # "This screenshot is five steps before the current step of the game",
            # "This screenshot is three steps before the current step of the game",
            # "This screenshot is two steps before the current step of the game",
            # "This screenshot is the previous step of the game. The blue band represents the left side and the yellow band represents the right side.",
            # "This screenshot is the current step of the game. The blue band represents the left side and the yellow band represents the right side."
            "This screenshot is the previous step of the game.",
            "This screenshot is the current step of the game."
        ]

        if image_obs:
            self.memory.update_info_history(
                {"image": ["../" + image_path for image_path in obs["image_paths"]]})  # get image
            image_memory = self.memory.get_recent_history("image", k=config.action_planning_image_num)

            image_introduction = []
            for i in range(len(image_memory), 0, -1):
                image_introduction.append(
                    {
                        "introduction": prompts[-i],
                        "path": image_memory[-i] if image_obs else "",  
                        "assistant": ""
                    })

            processed_params = {
                "image_introduction": {IMAGES_INPUT_TAG_NAME: [{
                IMAGE_PATH_TAG_NAME: "../" + image_path for image_path in obs["image_paths"]
            }]},
                "toolbar_information": obs['inventory'],
            }

        else:
            processed_params = {
                "toolbar_information": obs['inventory']
            }
        processed_params.update(obs)

        self.memory.update_info_history(processed_params)

        if step_num != 0:
            if self.use_self_reflection:
                self.run_self_reflection()

            if self.use_task_inference:
                self.run_task_inference()

        # 2. Call llm api for action planning
        params = deepcopy(self.memory.working_area)
        data = self.planner.action_planning(input=params)
        response = data['res_dict']
        del params

        # 3. Postprocess the response
        logger.write("Stardew Action Planning Postprocess")

        processed_response = deepcopy(response)

        skill_steps = []
        if 'actions' in response:
            skill_steps = response['actions']

        if skill_steps:
            skill_steps = [i for i in skill_steps if i != '']
        else:
            skill_steps = ['']

        skill_steps = skill_steps[:config.number_of_execute_skills]

        if config.number_of_execute_skills > 1:
            actions = "[" + ",".join(skill_steps) + "]"
        else:
            actions = str(skill_steps[0])

        action_planning_reasoning = response.get('reasoning', '')
        print("Actions", actions)
        print("Action Planning Reasoning\n", action_planning_reasoning)
        logging.log(logging.INFO, f"Action Planning Reasoning\n {action_planning_reasoning}")

        pre_energy = self.memory.get_recent_history("energy", k=1)[0]
        pre_money = self.memory.get_recent_history("money", k=1)[0]
        pre_health = self.memory.get_recent_history("health", k=1)[0]

        processed_response.update({
            "action": actions,
            "pre_action": skill_steps[0],
            "pre_energy": pre_energy,
            "pre_money": pre_money,
            "pre_health": pre_health,
            "action_planning_reasoning": action_planning_reasoning,
            "pre_decision_making_reasoning": action_planning_reasoning,
            "skill_steps": skill_steps,
        })
        self.memory.update_info_history(processed_response)

        # 4. Execute the actions
        params = deepcopy(self.memory.working_area)

        skill_steps = params.get("skill_steps", [])

        del params

        return skill_steps

def exit_cleanup(runner):
    logger.write("Exiting pipeline.")
    runner.pipeline_shutdown()