from gymnasium import spaces
import numpy as np
import json


def get_observation_space():
    observation_space = spaces.Dict({
        "Player": spaces.Dict({
            "Name": spaces.Text(max_length=50),
            "Health": spaces.Box(low=0, high=100, shape=(), dtype=np.int32),
            "Stamina": spaces.Box(low=0, high=100, shape=(), dtype=np.float32),
            "Money": spaces.Box(low=0, high=np.inf, shape=(), dtype=np.int32),
            "Location": spaces.Text(max_length=100),
            "Position": spaces.Box(low=0, high=200, shape=(2,), dtype=np.float32),
            "Inventory": spaces.Sequence(spaces.Text(max_length=50)),
            "Skills": spaces.Dict({
                "Farming": spaces.Discrete(11),
                "Mining": spaces.Discrete(11),
                "Combat": spaces.Discrete(11),
                "Fishing": spaces.Discrete(11),
                "Foraging": spaces.Discrete(11),
            })
        }),
        "NPCs": spaces.Sequence(spaces.Dict({
            "Name": spaces.Text(max_length=50),
            "Location": spaces.Text(max_length=100),
            "Friendship": spaces.Box(low=0, high=1000, shape=(), dtype=np.int32)
        })),
        "Locations": spaces.Dict({ 
            "LocationName": spaces.Dict({
                "Tiles": spaces.Sequence(spaces.Dict({
                    "X": spaces.Box(low=0, high=200, shape=(), dtype=np.float32),
                    "Y": spaces.Box(low=0, high=200, shape=(), dtype=np.float32),
                    "IsPassable": spaces.Discrete(2),  # Boolean 
                    "TerrainFeature": spaces.Text(max_length=50),
                    "Object": spaces.Text(max_length=50)
                })),
                "Buildings": spaces.Sequence(spaces.Dict({
                    "Name": spaces.Text(max_length=50),
                    "Position": spaces.Box(low=0, high=np.inf, shape=(2,), dtype=np.float32),
                    "Owner": spaces.Text(max_length=50)
                })),
                "Characters": spaces.Sequence(spaces.Dict({
                    "Name": spaces.Text(max_length=50),
                    "Position": spaces.Box(low=0, high=np.inf, shape=(2,), dtype=np.float32)
                })),
                "IsOutdoors": spaces.Discrete(2),  # Boolean
                "SeasonForLocation": spaces.Text(max_length=20)
            })
        }),
        "GameState": spaces.Dict({
            "DayOfMonth": spaces.Discrete(31),
            "Season": spaces.Text(max_length=20),
            "Year": spaces.Box(low=0, high=np.inf, shape=(2,), dtype=np.float32),
            "TimeOfDay": spaces.Box(low=0, high=2400, shape=(2,), dtype=np.float32),
            "Weather": spaces.Text(max_length=20),
            "IsWeddingDay": spaces.Discrete(2)  # Boolean
        }),
        "Farm": spaces.Dict({
            "Crops": spaces.Sequence(spaces.Dict({
                "Type": spaces.Box(low=0, high=1000, shape=(2,), dtype=np.float32),
                "GrowthStage": spaces.Discrete(10),
                "Quality": spaces.Discrete(5)
            })),
            "Animals": spaces.Sequence(spaces.Dict({
                "Type": spaces.Text(max_length=50),
                "Name": spaces.Text(max_length=50),
                "Age": spaces.Box(low=0, high=1000, shape=(), dtype=np.int32),
                "Happiness": spaces.Box(low=0, high=100, shape=(), dtype=np.int32)
            })),
            "Buildings": spaces.Sequence(spaces.Dict({
                "Type": spaces.Text(max_length=50),
                "BuildingsData": spaces.Text(max_length=1000)
            }))
        }),
        "Progression": spaces.Dict({
            "CommunityCenter": spaces.Discrete(2),
            "MineLevel": spaces.Box(low=0, high=120, shape=(2,), dtype=np.float32),
            "SkullCavernLevel": spaces.Box(low=0, high=1000, shape=(), dtype=np.int32),
            "Achievements": spaces.Box(low=0, high=1000, shape=(2,), dtype=np.float32),
        }),
        "CurrentMenuData": spaces.Dict({
            "MenuType": spaces.Text(max_length=50),
            "Details": spaces.Text(max_length=1000)
        })
    })
    return observation_space


def fill_observation_space(data, space):
    if isinstance(space, dict):
        for key, sub_space in space.items():
            if key not in data:
                if isinstance(sub_space, spaces.Dict):
                    data[key] = {}
                elif isinstance(sub_space, spaces.Sequence):
                    data[key] = []
                elif isinstance(sub_space, spaces.Discrete):
                    data[key] = 0
                elif isinstance(sub_space, spaces.Box):
                    data[key] = float(sub_space.low if sub_space.low.size == 1 else 0)
                elif isinstance(sub_space, spaces.Text):
                    data[key] = ""
            if isinstance(data[key], dict) and isinstance(sub_space, spaces.Dict):
                fill_observation_space(data[key], sub_space.spaces)
            elif isinstance(data[key], list) and isinstance(sub_space, spaces.Sequence):
                for i in range(len(data[key])):
                    if isinstance(data[key][i], dict) and isinstance(sub_space.feature_space, spaces.Dict):
                        fill_observation_space(data[key][i], sub_space.feature_space.spaces)
