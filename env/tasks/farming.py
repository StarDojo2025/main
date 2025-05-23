from .base import TaskBase
from env.tasks.utils.obs_check import *


class Farming(TaskBase):
    def evaluate(self, obs, proxy) -> dict:
        if self.last_obs is None:
            self.last_obs = obs
            return {
                "completed": False,
                "quantity": 0,
            }

        if self.evaluator == "clear":
            if self.object == "Debris":
                self.check_list = ["Weeds", "Stone", "Twig"]
            else:
                self.check_list = [self.object]

            if self.object == "Stone":
                self.quantity_change = count_check(self.check_list, obs["inventory"], self.last_obs["inventory"],
                                                   False, "object_at_inventory")
            else:
                self.quantity_change = surrounding_check(self.check_list, obs["surroundingsdata"],
                                                         self.last_obs["surroundingsdata"], True,
                                                         "object_at_tile")

        elif self.evaluator == "till":
            self.check_list = ["StardewValley.TerrainFeatures.HoeDirt"]
            self.quantity_change = surrounding_check(self.check_list, obs["surroundingsdata"],
                                                     self.last_obs["surroundingsdata"], False,
                                                     "terrain_at_tile")

        elif self.evaluator == "sow":
            if self.object == "Dirt":
                self.check_list = [self.tool]
                self.quantity_change = count_check(self.check_list, obs["crops"], self.last_obs["crops"],
                                                   False, "seeds_at_tile")
            else:
                self.check_list = ['StardewValley.TerrainFeatures.FruitTree']
                self.quantity_change = surrounding_check(self.check_list, obs["surroundingsdata"],
                                                         self.last_obs["surroundingsdata"], False,
                                                         "terrain_at_tile")

        elif self.evaluator == "water":
            if self.object == "Crop":
                self.check_list = []
            else:
                self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["crops"], self.last_obs["crops"], False,
                                               "watered_crop")

        elif self.evaluator == "fertilize":
            self.check_list = [self.tool]
            self.quantity_change = count_check(self.check_list, obs["inventory"], self.last_obs["inventory"],
                                               True, "object_at_inventory")

        elif self.evaluator == "harvest":
            self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["inventory"], self.last_obs["inventory"],
                                               False, "object_at_inventory")

        elif self.evaluator == "skill":
            self.quantity_change = obs["player"]["skills"]["farming"] - self.last_obs["player"]["skills"]["farming"]

        elif self.evaluator == "open" or self.evaluator == "close":
            if self.object == "Deluxe Coop, Deluxe Barn":
                self.check_list = ["Deluxe Coop", "Deluxe Barn"]
            else:
                self.check_list = [self.object]

            if self.evaluator == "open":
                self.quantity_change = count_check(self.check_list, obs["farm"]["buildings"],
                                                   self.last_obs["farm"]["buildings"], False,
                                                   "opening_building")
            else:
                self.quantity_change = count_check(self.check_list, obs["farm"]["buildings"],
                                                   self.last_obs["farm"]["buildings"], False,
                                                   "closing_building")

        elif self.evaluator == "pet":
            if self.object == "Animal":
                self.check_list = []
            else:
                self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["farm"]["animals"] + obs["farm"]["pets"],
                                               self.last_obs["farm"]["animals"] + self.last_obs["farm"]["pets"],
                                               False, "touched_animal")

        elif self.evaluator == "fill":
            if self.object == "Feeding Bench":
                self.check_list = ["Deluxe Coop", "Deluxe Barn"]
                self.quantity_change = count_check(self.check_list, obs["farm"]["buildings"],
                                                   self.last_obs["farm"]["buildings"], False,
                                                   "full_bench")
            else:
                self.check_list = [self.object]
                self.quantity_change = count_check(self.check_list, obs["farm"]["buildings"],
                                                   self.last_obs["farm"]["buildings"], False, "full_bowl")

        elif self.evaluator == "incubate":
            if self.object == "Chicken":
                self.check_list = ["White Chicken", "Brown Chicken"]
            else:
                self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["farm"]["animals"],
                                               self.last_obs["farm"]["animals"], False,
                                               "animal")

        elif self.evaluator == "friendship":
            if self.object != "Animal":
                self.check_list = [self.object]
            self.current_quantity = count(self.check_list, obs["farm"]["animals"] + obs["farm"]["pets"],
                                          "animal_friendship")

        elif self.evaluator == "mood":
            self.current_quantity = count(self.check_list, obs["farm"]["animals"], "mood")

        elif self.evaluator == "profession":
            self.check_list = [self.object]
            self.quantity_change = count_check(self.check_list, obs["player"]["professions"],
                                               self.last_obs["player"]["professions"], False,
                                               "profession")

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
