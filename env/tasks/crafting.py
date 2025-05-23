from .base import TaskBase
from env.tasks.utils.obs_check import *


class Crafting(TaskBase):
    def evaluate(self, obs, proxy) -> dict:
        if self.evaluator == "craft":
            self.check_list = [self.object]
            self.current_quantity = count(self.check_list, obs["inventory"], "object_at_inventory")

        if self.current_quantity >= self.quantity:
            completed = True
        else:
            completed = False

        return {
            "completed": completed,
            "quantity": self.current_quantity,
        }