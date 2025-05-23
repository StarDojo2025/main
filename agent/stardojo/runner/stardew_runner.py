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

# from stardojo.provider import VideoRecordProvider
# from stardojo.provider import VideoClipProvider
# from stardojo.provider import StardewInformationGatheringPreprocessProvider
# from stardojo.provider import StardewInformationGatheringPostprocessProvider
# from stardojo.provider import StardewSelfReflectionPreprocessProvider
# from stardojo.provider import StardewSelfReflectionPostprocessProvider
# from stardojo.provider import StardewTaskInferencePreprocessProvider
# from stardojo.provider import StardewTaskInferencePostprocessProvider
# from stardojo.provider import StardewActionPlanningPreprocessProvider
# from stardojo.provider import StardewActionPlanningPostprocessProvider
# from stardojo.provider import StardewInformationGatheringProvider
# from stardojo.provider import StardewSelfReflectionProvider
# from stardojo.provider import StardewActionPlanningProvider
# from stardojo.provider import StardewTaskInferenceProvider
# from stardojo.provider import SkillCurationProvider
# from stardojo.provider import SkillExecuteProvider
# from stardojo.provider import AugmentProvider


from stardojo.planner.stardew_planner import StardewPlanner
from log_processor import process_log_messages

config = Config()
logger = Logger()
io_env = IOEnvironment()


class PipelineRunner():

    def __init__(self,
                 llm_provider_config_path: str,
                 embed_provider_config_path: str,
                 task_description: str,
                 use_self_reflection: bool = False,
                 use_task_inference: bool = False):

        self.llm_provider_config_path = llm_provider_config_path
        self.embed_provider_config_path = embed_provider_config_path

        self.task_description = task_description
        self.use_self_reflection = use_self_reflection
        self.use_task_inference = use_task_inference

        # Init internal params
        self.set_internal_params()


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
        #self.video_recorder = VideoRecordProvider()
        # self.video_recorder = VideoRecordProvider(os.path.join(config.work_dir, 'video.mp4'))

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

        # Init video provider
        # self.video_clip = VideoClipProvider(gm = self.gm)

        self.provider_configs = config.provider_configs

        # Init augment providers
        # self.augment = AugmentProvider()
        # self.augment_methods = [
        #     self.augment
        # ]

        # Init module providers
        # self.information_gathering_preprocess = StardewInformationGatheringPreprocessProvider(gm=self.gm)
        # self.information_gathering = StardewInformationGatheringProvider(planner=self.planner, gm=self.gm)
        # self.information_gathering_postprocess = StardewInformationGatheringPostprocessProvider(
        #     gm=self.gm,
        #     **self.provider_configs.information_gathering_postprocess_provider
        # )

        # self.self_reflection_preprocess = StardewSelfReflectionPreprocessProvider(gm=self.gm, augment_methods=self.augment_methods)
        # self.self_reflection = StardewSelfReflectionProvider(planner=self.planner, gm=self.gm)
        # self.self_reflection_postprocess = StardewSelfReflectionPostprocessProvider(gm=self.gm)

        # self.task_inference_preprocess = StardewTaskInferencePreprocessProvider(gm=self.gm)
        # self.task_inference = StardewTaskInferenceProvider(planner=self.planner, gm=self.gm)
        # self.task_inference_postprocess = StardewTaskInferencePostprocessProvider(gm=self.gm)

        # self.action_planning_preprocess = StardewActionPlanningPreprocessProvider(
        #     gm=self.gm,
        #     **self.provider_configs.action_planning_preprocess_provider
        # )
        # self.action_planning = StardewActionPlanningProvider(planner=self.planner, gm=self.gm)
        # self.action_planning_postprocess = StardewActionPlanningPostprocessProvider(gm=self.gm)

        # self.skill_curation = SkillCurationProvider(gm=self.gm)

        # Init skill execute provider
        # self.skill_execute = SkillExecuteProvider(gm=self.gm)

        # Init checkpoint path
        self.checkpoint_path = os.path.join(config.work_dir, 'checkpoints')
        os.makedirs(self.checkpoint_path, exist_ok=True)


    def pipeline_shutdown(self):

        self.gm.cleanup_io()
        # self.video_recorder.finish_capture()

        log = process_log_messages(config.work_dir)

        with open(config.work_dir + '/logs/log.md', 'w') as f:
            log = replace_unsupported_chars(log)
            f.write(log)

        logger.write('>>> Markdown generated.')
        logger.write('>>> Bye.')


    def run(self):

        # 1. Initiate the parameters
        success = False
        init_params = {
            "task_description": self.task_description,
            "skill_library": self.skill_library,
            "exec_info": {
                "errors": False,
                "errors_info": ""
            },
            "pre_action": "",
            "pre_decision_making_reasoning": "",
            "pre_self_reflection_reasoning": "",
            "summarization": "",
            # "toolbar_information": None,
            "subtask_description": "",
            "subtask_reasoning": "",
        }

        self.memory.update_info_history(init_params)

        # 2. Switch to game
        # self.gm.switch_to_game()

        # 3. Start video recording
        # self.video_recorder.start_capture()

        # 4. Initiate screen shot path and video clip path
        # self.video_clip(init = True)

        # self.gm.pause_game()

        # 5. Augment image
        # self.augment()

        # 7. Start the pipeline
        step = 0

        while not success:
            try:
                # 7.1. Information gathering
                self.run_information_gathering()

                # 7.2. Self reflection
                self.run_self_reflection()

                # 7.3. Task inference
                self.run_task_inference()

                # 7.4. Skill curation
                # self.run_skill_curation()

                # 7.5. Action planning
                self.run_action_planning()

                step += 1

                if step % config.checkpoint_interval == 0:
                    checkpoint_path = os.path.join(self.checkpoint_path, 'checkpoint_{:06d}.json'.format(step))
                    self.memory.save(checkpoint_path)

                if step > config.max_turn_count:
                    logger.write('Max steps reached, exiting.')
                    break

            except KeyboardInterrupt:
                logger.write('KeyboardInterrupt Ctrl+C detected, exiting.')
                self.pipeline_shutdown()
                break

        self.pipeline_shutdown()

    def run_information_gathering(self):

        # 1. Prepare the parameters to call llm api
        logger.write("Stardew Information Gathering Preprocess")

        prompts = [
            "This is a screenshot of the current moment in the game with multiple augmentation to help you understand it better. The screenshot is organized into a grid layout with 15 segments, arranged in 3 rows and 5 columns. Each segment in the grid is uniquely identified by coordinates, which are displayed at the center of each segment in white text. The layout also features color-coded bands for orientation: a blue band on the left side and a yellow band on the right side of the screenshot."
        ]

        start_frame_id = self.memory.get_recent_history("start_frame_id", k=1)[0]
        end_frame_id = self.memory.get_recent_history("end_frame_id", k=1)[0]
        screenshot_path = self.memory.get_recent_history(constants.IMAGES_MEM_BUCKET, k=1)[0]
        augmented_screenshot_path = self.memory.get_recent_history(constants.AUGMENTED_IMAGES_MEM_BUCKET, k=1)[0]
        task_description = self.memory.get_recent_history("task_description", k=1)[0]

        # Gather information preparation
        # logger.write(f'Gather Information Start Frame ID: {start_frame_id}, End Frame ID: {end_frame_id}')
        # video_clip_path = self.video_recorder.get_video(start_frame_id, end_frame_id)

        # Configure the test
        # if you want to test with a pre-defined screenshot, you can replace the cur_screenshot_path with the path to the screenshot
        pre_defined_sreenshot = None
        pre_defined_sreenshot_augmented = None
        if pre_defined_sreenshot is not None:
            cur_screenshot_path = pre_defined_sreenshot
            cur_screenshot_path_augmented = pre_defined_sreenshot_augmented
        else:
            cur_screenshot_path = screenshot_path
            cur_screenshot_path_augmented = augmented_screenshot_path

        image_introduction = [
            {
                "introduction": prompts[-1],
                "path": cur_screenshot_path_augmented,
                "assistant": ""
            }
        ]

        # Configure the gather_information module
        gather_information_configurations = {
            "frame_extractor": False,  # extract text from the video clip
            "icon_replacer": False,
            "llm_description": True,  # get the description of the current screenshot
            "object_detector": False,
            "get_item_number": False  # use llm to get item number in the toolbox
        }

        processed_params = {
            "image_introduction": image_introduction,
            "task_description": task_description,
            "image": cur_screenshot_path,
            "augmented_image": cur_screenshot_path_augmented,
            # "video_clip_path": video_clip_path,
            "gather_information_configurations": gather_information_configurations
        }

        self.memory.working_area.update(processed_params)


        # 2. Call llm api for information gathering
        params = deepcopy(self.memory.working_area)
        data = self.planner.information_gathering(input=params)
        response = data['res_dict']
        del params

    
        # 3. Postprocess the response
        logger.write("Stardew Information Gathering Postprocess")

        processed_response = deepcopy(response)

        response['toolbar_information'] = self.prepare_toolbar_information(
            response['toolbar_dict_list'],
            response['selected_position'])
        response['image_description'] = response['description']

        # previous_toolbar_information = None
        # toolbar_information = None
        # selected_position = None

        energy = None
        dialog = None
        date_time = None

        if constants.IMAGE_DESCRIPTION in response:
            # if 'toolbar_information' in response:
            #     previous_toolbar_information = toolbar_information
            #     toolbar_information = response['toolbar_information']
            # if 'selected_position' in response:
            #     selected_position = response['selected_position']
            if 'energy' in response:
                energy = response['energy']
            if 'dialog' in response:
                dialog = response['dialog']
            if 'date_time' in response:
                date_time = response['date_time']
        else:
            logger.warn(f"No {constants.IMAGE_DESCRIPTION} in response.")

        processed_response.update({
            "response_keys": list(response.keys()),
            "response": response,
            # "toolbar_information": toolbar_information,
            # "previous_toolbar_information": previous_toolbar_information,
            # "selected_position": selected_position,
            "energy": energy,
            "dialog": dialog,
            "date_time": date_time,
        })

        self.memory.update_info_history(processed_response)



    def run_self_reflection(self):

        # 1. Prepare the parameters to call llm api
        logger.write(f'Stardew Self Reflection Preprocess')

        prompts = [
            "Here are the sequential frames of the character executing the last action."
        ]

        start_frame_id = self.memory.get_recent_history("start_frame_id", k=1)[0]
        end_frame_id = self.memory.get_recent_history("end_frame_id", k=1)[0]
        task_description = self.memory.get_recent_history("task_description", k=1)[0]
        pre_action = self.memory.get_recent_history("pre_action", k=1)[0]
        pre_decision_making_reasoning = self.memory.get_recent_history("pre_decision_making_reasoning", k=1)[0]
        exec_info = self.memory.get_recent_history("exec_info", k=1)[0]
        skill_library = self.memory.get_recent_history("skill_library", k=1)[0]
        datetime = self.memory.get_recent_history("datetime", k=1)[0]
        toolbar_information = self.memory.get_recent_history("toolbar_information", k=1)[0]
        previous_toolbar_information = self.memory.get_recent_history("previous_toolbar_information", k=1)[0]
        history_summary = self.memory.get_recent_history("history_summary", k=1)[0]
        subtask_description = self.memory.get_recent_history("subtask_description", k=1)[0]
        subtask_reasoning = self.memory.get_recent_history("subtask_reasoning", k=1)[0]

        processed_params = {
            "start_frame_id": start_frame_id,
            "end_frame_id": end_frame_id,
            "task_description": task_description,
            "skill_library": skill_library,
            "exec_info": exec_info,
            "pre_decision_making_reasoning": pre_decision_making_reasoning,
            "datetime": datetime,
            "toolbar_information": toolbar_information,
            "previous_toolbar_information": previous_toolbar_information,
            "history_summary": history_summary,
            "subtask_description": subtask_description,
            "subtask_reasoning": subtask_reasoning
        }

        if start_frame_id > -1:
            action_frames = []
            video_frames = self.video_recorder.get_frames(start_frame_id, end_frame_id)

            action_frames.append(self.augment_image(video_frames[0][1]))
            action_frames.append(self.augment_image(video_frames[-1][1]))

            image_introduction = [
                {
                    "introduction": prompts[-1],
                    "path": action_frames,
                    "assistant": "",
                    "resolution": "low"
                }]

            if pre_action:
                pre_action_name = []
                pre_action_code = []

                if isinstance(pre_action, str):
                    if "[" not in pre_action:
                        pre_action = "[" + pre_action + "]"
                elif isinstance(pre_action, list):
                    pre_action = "[" + ",".join(pre_action) + "]"

                for item in self.gm.convert_expression_to_skill(pre_action):
                    name, params = item
                    action_code, action_info = self.gm.get_skill_library_in_code(name)

                    pre_action_name.append(name)
                    pre_action_code.append(action_code if action_code is not None else action_info)
                previous_action = ",".join(pre_action_name)
                action_code = "\n".join(list(set(pre_action_code)))
            else:
                previous_action = ""
                action_code = ""

            if exec_info["errors"]:
                executing_action_error = exec_info["errors_info"]
            else:
                executing_action_error = ""

            processed_params.update({
                "image_introduction": image_introduction,
                "previous_action": previous_action,
                "action_code": action_code,
                "executing_action_error": executing_action_error,
                "previous_reasoning": pre_decision_making_reasoning,
            })

        self.memory.working_area.update(processed_params)

        # 2. Call llm api for self reflection
        params = deepcopy(self.memory.working_area)
        data = self.planner.self_reflection(input=params)
        response = data['res_dict']
        del params

        # 3. Postprocess the response
        logger.write(f'Stardew Self Reflection Postprocess')

        processed_response = deepcopy(response)

        if 'reasoning' in response:
            self_reflection_reasoning = response['reasoning']
        else:
            self_reflection_reasoning = ""

        processed_response.update({
            "self_reflection_reasoning": self_reflection_reasoning,
            "pre_self_reflection_reasoning": self_reflection_reasoning
        })

        self.memory.update_info_history(processed_response)


    def run_task_inference(self):

        # 1. Prepare the parameters to call llm api
        logger.write(f'Stardew Task Inference Preprocess')

        prompts = [
            "This screenshot is the current step of the game. The blue band represents the left side and the yellow band represents the right side."
        ]

        task_description = self.memory.get_recent_history("task_description", k=1)[0]
        previous_summarization = self.memory.get_recent_history("summarization", 1)[0]
        substask_description = self.memory.get_recent_history("subtask_description", 1)[0]
        substask_reasoning = self.memory.get_recent_history("subtask_reasoning", 1)[0]
        toolbar_information = self.memory.get_recent_history("toolbar_information", 1)[0]
        images = self.memory.get_recent_history(constants.AUGMENTED_IMAGES_MEM_BUCKET, 1)
        decision_making_reasoning = self.memory.get_recent_history('decision_making_reasoning', 1)
        self_reflection_reasoning = self.memory.get_recent_history('self_reflection_reasoning', 1)

        image_introduction = []
        image_introduction.append(
            {
                "introduction": prompts[-1],
                "path": images,
                "assistant": ""
            })

        processed_params = {
            "image_introduction": image_introduction,
            "previous_summarization": previous_summarization,
            "task_description": task_description,
            "subtask_description": substask_description,
            "subtask_reasoning": substask_reasoning,
            "previous_reasoning": decision_making_reasoning,
            "self_reflection_reasoning": self_reflection_reasoning,
            "toolbar_information": toolbar_information
        }

        self.memory.working_area.update(processed_params)

        # 2. Call llm api for task inference
        params = deepcopy(self.memory.working_area)
        data = self.planner.task_inference(input=params)
        response = data['res_dict']
        del params

        # 3. Postprocess the response
        logger.write(f'Stardew Task Inference Postprocess')

        processed_response = deepcopy(response)

        history_summary = response['history_summary']

        subtask_description = response['subtask']
        subtask_reasoning = response['subtask_reasoning']

        processed_response.update({
            'summarization': history_summary,
            'subtask_description': subtask_description,
            'subtask_reasoning': subtask_reasoning
        })

        self.memory.update_info_history(processed_response)



    def run_action_planning(self):

        # 1. Prepare the parameters to call llm api
        logger.write("Stardew Action Planning Preprocess")

        prompts = [
            "Now, I will give you five screenshots for decision making."
            "This screenshot is five steps before the current step of the game",
            "This screenshot is three steps before the current step of the game",
            "This screenshot is two steps before the current step of the game",
            "This screenshot is the previous step of the game. The blue band represents the left side and the yellow band represents the right side.",
            "This screenshot is the current step of the game. The blue band represents the left side and the yellow band represents the right side."
        ]

        pre_action = self.memory.get_recent_history("pre_action", k=1)[0]
        pre_self_reflection_reasoning = self.memory.get_recent_history("pre_self_reflection_reasoning", k=1)[0]
        toolbar_information = self.memory.get_recent_history("toolbar_information", k=1)[0]
        selected_position = self.memory.get_recent_history("selected_position", k=1)[0]
        summarization = self.memory.get_recent_history("summarization", k=1)[0]
        skill_library = self.memory.get_recent_history("skill_library", k=1)[0]
        task_description = self.memory.get_recent_history("task_description", k=1)[0]
        subtask_description = self.memory.get_recent_history("subtask_description", k=1)[0]
        history_summary = self.memory.get_recent_history("summarization", k=1)[0]

        # Decision making preparation
        toolbar_information = toolbar_information if toolbar_information is not None else self.toolbar_information
        selected_position = selected_position if selected_position is not None else 1

        previous_action = ""
        previous_reasoning = ""
        if pre_action:
            previous_action = self.memory.get_recent_history("action", k=1)[0]
            previous_reasoning = self.memory.get_recent_history("decision_making_reasoning", k=1)[0]

        previous_self_reflection_reasoning = ""
        if pre_self_reflection_reasoning:
            previous_self_reflection_reasoning = self.memory.get_recent_history("self_reflection_reasoning", k=1)[0]

        # @TODO Temporary solution with fake augmented entries if no bounding box exists. Ideally it should read images, then check for possible augmentation.
        image_memory = self.memory.get_recent_history("augmented_image", k=config.action_planning_image_num)

        image_introduction = []
        for i in range(len(image_memory), 0, -1):
            image_introduction.append(
                {
                    "introduction": prompts[-i],
                    "path": image_memory[-i],
                    "assistant": ""
                })

        processed_params = {
            "pre_self_reflection_reasoning": pre_self_reflection_reasoning,
            "toolbar_information": toolbar_information,
            "selected_position": selected_position,
            "summarization": summarization,
            "skill_library": skill_library,
            "task_description": task_description,
            "subtask_description": subtask_description,
            "history_summary": history_summary,
            "previous_action": previous_action,
            "previous_reasoning": previous_reasoning,
            "previous_self_reflection_reasoning": previous_self_reflection_reasoning,
            "image_introduction": image_introduction
        }

        self.memory.working_area.update(processed_params)

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
        pre_action = "[" + ",".join(skill_steps) + "]"

        if config.number_of_execute_skills > 1:
            actions = "[" + ",".join(skill_steps) + "]"
        else:
            actions = str(skill_steps[0])

        decision_making_reasoning = response['reasoning']
        pre_decision_making_reasoning = decision_making_reasoning

        processed_response.update({
            "pre_action": pre_action,
            "action": actions,
            "pre_decision_making_reasoning": pre_decision_making_reasoning,
            "decision_making_reasoning": decision_making_reasoning,
            "skill_steps": skill_steps,
        })
        self.memory.update_info_history(processed_response)

        # 4. Execute the actions
        params = deepcopy(self.memory.working_area)

        skill_steps = params.get("skill_steps", [])
        # pre_screen_classification = params.get("pre_screen_classification", "")
        # screen_classification = params.get("screen_classification", "")
        pre_action = params.get("pre_action", "")

        self.gm.unpause_game()

        # @TODO: Rename GENERAL_GAME_INTERFACE
        # if (pre_screen_classification.lower() == constants.GENERAL_GAME_INTERFACE and
        #         (screen_classification.lower() == constants.MAP_INTERFACE or
        #          screen_classification.lower() == constants.SATCHEL_INTERFACE) and pre_action):
        #     exec_info = self.gm.execute_actions([pre_action])

        start_frame_id = self.video_recorder.get_current_frame_id()
        exec_info = self.gm.execute_actions(skill_steps)
        screenshot_path = self.gm.capture_screen()
        end_frame_id = self.video_recorder.get_current_frame_id()

        # try:
        #     pause_flag = self.gm.pause_game(screen_classification.lower())
        #     logger.write(f'Pause flag: {pause_flag}')
        #     if not pause_flag:
        #         self.gm.pause_game(screen_type=None)
        # except Exception as e:
        #     logger.write(f"Error while pausing the game: {e}")

        # exec_info also has the list of successfully executed skills. skill_steps is the full list, which may differ if there were execution errors.
        pre_action = exec_info["last_skill"]
        # pre_screen_classification = screen_classification

        logger.write(f"Execute skill steps by frame id ({start_frame_id}, {end_frame_id}).")

        res_params = {
            "start_frame_id": start_frame_id,
            "end_frame_id": end_frame_id,
            "screenshot_path": screenshot_path,
            "pre_action": pre_action,
            # "pre_screen_classification": pre_screen_classification,
            "exec_info": exec_info,
        }

        self.memory.update_info_history(res_params)

        del params

        # 5. Execute the augment providers
        # self.augment()


    # def run_skill_curation(self):

    #     # 1. Call skill curation
    #     self.skill_curation()





    # def run_information_gathering(self):

    #     # 1. Prepare the parameters to call llm api
    #     self.information_gathering_preprocess()

    #     # 2. Call llm api for information gathering
    #     response = self.information_gathering()

    #     # 3. Postprocess the response
    #     self.information_gathering_postprocess(response)


    # def run_self_reflection(self):

    #     # 1. Prepare the parameters to call llm api
    #     self.self_reflection_preprocess()

    #     # 2. Call llm api for self reflection
    #     response = self.self_reflection()

    #     # 3. Postprocess the response
    #     self.self_reflection_postprocess(response)


    # def run_task_inference(self):

    #     # 1. Prepare the parameters to call llm api
    #     self.task_inference_preprocess()

    #     # 2. Call llm api for task inference
    #     response = self.task_inference()

    #     # 3. Postprocess the response
    #     self.task_inference_postprocess(response)


    # def run_action_planning(self):

    #     # 1. Prepare the parameters to call llm api
    #     self.action_planning_preprocess()

    #     # 2. Call llm api for action planning
    #     response = self.action_planning()

    #     # 3. Postprocess the response
    #     self.action_planning_postprocess(response)

    #     # 4. Execute the actions
    #     self.skill_execute()

    #     # 5. Execute the augment providers
    #     self.augment()


    # def run_skill_curation(self):

    #     # 1. Call skill curation
    #     self.skill_curation()


def exit_cleanup(runner):
    logger.write("Exiting pipeline.")
    runner.pipeline_shutdown()


def entry(args):

    task_description = "No Task"

    pipelineRunner = PipelineRunner(llm_provider_config_path=args.llmProviderConfig,
                                    embed_provider_config_path=args.embedProviderConfig,
                                    task_description=task_description,
                                    use_self_reflection = True,
                                    use_task_inference = True)

    atexit.register(exit_cleanup, pipelineRunner)

    pipelineRunner.run()
