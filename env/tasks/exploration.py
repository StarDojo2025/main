from .base import TaskBase
from env.tasks.utils.obs_check import *


class Exploration(TaskBase):
    def evaluate(self, obs, proxy) -> dict:
        if self.last_obs is None:
            self.last_obs = obs
            return {
                "completed": False,
                "quantity": 0,
            }

        if self.evaluator == "harvest":
            self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["inventory"], self.last_obs["inventory"],
                                               False, "object_at_inventory")

        elif self.evaluator == "silo":
            self.quantity_change = count_check(self.check_list, obs["farm"]["buildings"],
                                               self.last_obs["farm"]["buildings"], False, "silo")

        elif self.evaluator == "skill":
            if self.object == "Foraging Skill":
                check_object = "foraging"
            elif self.object == "Mining Skill":
                check_object = "mining"
            else:
                check_object = "fishing"

            self.quantity_change = (obs["player"]["skills"][check_object] -
                                    self.last_obs["player"]["skills"][check_object])

        elif self.evaluator == "profession":
            self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["player"]["professions"],
                                               self.last_obs["player"]["professions"], False,
                                               "profession")
        elif self.evaluator == "bundle":
            self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["Progression"]["Bundles"],
                                               self.last_obs["Progression"]["Bundles"], False, "bundle")
        elif self.evaluator == "museum":
            if self.object == "Item":
                self.quantity_change = len(obs["Progression"]["Museum"]) - len(self.last_obs["Progression"]["Museum"])
            else:
                self.check_list = [self.object]
                self.quantity_change = count_check(self.check_list, obs["Progression"]["Museum"],
                                                   self.last_obs["Progression"]["Museum"], False,
                                                   "museum")

        elif self.evaluator == "repair":
            self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["Progression"]["Repairs"],
                                               self.last_obs["Progression"]["Repairs"], False,
                                               "repair")

        elif self.evaluator == "location":
            if obs['player']['location'] == self.object:
                self.current_quantity = 1

        elif self.evaluator == "accept":
            self.quantity_change = count_check(self.check_list, obs["Progression"]["Quests"],
                                               self.last_obs["Progression"]["Quests"], False,
                                               "help_quest")

        elif self.evaluator == "quit":
            self.quantity_change = (len(self.last_obs["Progression"]["Quests"]) -
                                    len(obs["Progression"]["Quests"]))

        elif self.evaluator == "reward":
            self.quantity_change = count_check(self.check_list, obs["Progression"]["Quests"],
                                               self.last_obs["Progression"]["Quests"], True,
                                               "completed_quest")

        elif self.evaluator == "complete_help":
            self.quantity_change = count_check(self.check_list, obs["Progression"]["Quests"],
                                               self.last_obs["Progression"]["Quests"], False,
                                               "completed_quest")

        elif self.evaluator == "exchange":
            self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["inventory"], self.last_obs["inventory"],
                                               False, "object_at_inventory")

        elif self.evaluator == "complete_story":
            self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["Progression"]["Quests"],
                                               self.last_obs["Progression"]["Quests"], True,
                                               "story_quest")

        elif self.evaluator == 'watch':
            curMenuData = obs['CurrentMenuData']
            if curMenuData is not None and 'dialogues' in curMenuData and curMenuData['dialogues'] is not None and len(
                    curMenuData['dialogues']) > 0 and 'forecast' in curMenuData['dialogues'][0]:
                self.current_quantity = 1

        elif self.evaluator == 'read':
            curMenuData = obs['CurrentMenuData']
            if curMenuData is not None and 'type' in curMenuData and curMenuData['type'] == 'Letter':
                self.current_quantity = 1

        elif self.evaluator == 'sleep':
            if 'callbackdata' in obs and 'ondaystarted' in obs['callbackdata']:
                dayStartedTimes = obs['callbackdata']['ondaystarted']
                if dayStartedTimes > 1:
                    self.current_quantity = 1

        self.last_obs = obs
        self.current_quantity += self.quantity_change
        if self.current_quantity >= self.quantity:
            completed = True
        else:
            completed = False

        return {
            "completed": completed,
            "quantity": self.current_quantity,
        }
