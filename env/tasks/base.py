import time
from abc import ABC, abstractmethod
import env.tasks.utils.load_save as load_save


class TaskBase(ABC):
    def __init__(self, llm_description: str, object: str, quantity: int, tool: str, save: str, init_commands: list,
                 evaluator: str, difficulty: str):
        self.current_quantity = 0
        self.quantity_change = 0
        self.check_list = []
        self.last_count = 0
        self.last_obs = None
        self.llm_description = llm_description
        self.object = object
        self.quantity = quantity
        self.tool = tool
        self.save = save
        self.init_commands = init_commands
        self.evaluator = evaluator
        self.difficulty = difficulty
        # self.init_task()

    def init_task(self, proxy):
        load_save.load_save(proxy, self.save, self.init_commands)

    @abstractmethod
    def evaluate(self, obs, proxy) -> dict:
        pass
