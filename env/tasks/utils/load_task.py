import yaml
import os
import env.tasks.base as base
import env.tasks.farming as farming
import env.tasks.exploration as exploration
import env.tasks.social as social
import env.tasks.crafting as crafting
import env.tasks.combat as combat
import env.tasks.open as open_tasks
from pathlib import Path
here = Path(__file__).resolve()
tasks_path = here.parent.parent
TASK_SUITE_PATH = os.path.join(tasks_path, "task_suite")

# TASK_SUITE_PATH = "../task_suite"


def load_task(type: str, id: int) -> base.TaskBase:
    filename = type + ".yaml"
    task_path = os.path.join(TASK_SUITE_PATH, filename)
    with open(task_path, 'r', encoding='utf-8') as file:
        task_dict: dict = yaml.safe_load(file)

    task_key, task_value = list(task_dict.items())[id]
    llm_description = task_key
    object = task_value["object"]
    quantity = task_value["quantity"]
    tool = task_value["tool"]
    save = task_value["save"]
    evaluator = task_value["evaluator"]
    difficulty = task_value["difficulty"]
    init_commands = task_value["init_commands"]

    task = None
    if type == "farming" or type == "farming_lite":
        task = farming.Farming(llm_description, object, quantity, tool, save, init_commands, evaluator, difficulty)
    elif type == "exploration" or type == "exploration_lite":
        task = exploration.Exploration(llm_description, object, quantity, tool, save, init_commands, evaluator, difficulty)
    elif type == "social" or type == "social_lite":
        task = social.Social(llm_description, object, quantity, tool, save, init_commands, evaluator, difficulty)
    elif type == "crafting" or type == "crafting_lite":
        task = crafting.Crafting(llm_description, object, quantity, tool, save, init_commands, evaluator, difficulty)
    elif type == "combat" or type == "combating_lite":
        task = combat.Combat(llm_description, object, quantity, tool, save, init_commands, evaluator, difficulty)
    elif type == "open":
        task = open_tasks.Open(llm_description, object, quantity, tool, save, init_commands, evaluator, difficulty)

    return task
