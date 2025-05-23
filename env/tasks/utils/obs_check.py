# Helper function to count
def count(check_list: list, data_list: list, type: str) -> int:
    number = 0
    for data in data_list:
        if type == "seeds_at_tile":
            if (data['id'] in check_list) and (data['current_phase'] == 0):
                number += 1
        elif type == "watered_crop":
            if (check_list == [] or data['id'] in check_list) and (data['isWatered']):
                number += 1
        elif type == "object_at_inventory":
            if data['Name'] in check_list:
                number += data['Quantity']
        elif type == "opening_building":
            if (data['type'] in check_list) and (data['isAnimalDoorOpen']):
                number += 1
        elif type == "closing_building":
            if (data['type'] in check_list) and (not data['isAnimalDoorOpen']):
                number += 1
        elif type == "touched_animal":
            if (check_list == [] or data['Type'] in check_list) and (data['isTouched']):
                number += 1
        elif type == "full_bowl":
            if (data['type'] in check_list) and (data['isBowlFull']):
                number += 1
        elif type == "animal":
            if data['Type'] in check_list:
                number += 1
        elif type == "animal_friendship":
            if not check_list and data['Friendship'] >= 100:
                number += 1
            elif data['Type'] in check_list:
                number = data['Friendship']
                break
        elif type == "mood":
            if data['Happiness'] >= 255:
                number += 1
        elif type == "profession":
            if check_list[0] in data:
                number += 1
        elif type == "full_bench":
            if (data['type'] in check_list) and (data['hayNumber'] == 12):
                number += 1
        elif type == "building":
            if data['type'] in check_list:
                number += 1
        elif type == "silo":
            if data['type'] == "Silo":
                number = data['hayNumber']
                break
        elif type == "bundle":
            if data['name'] in check_list:
                if data['completed']:
                    number += 1
                break
        elif type == "museum":
            if data['itemName'] in check_list:
                number += 1
                break
        elif type == "repair":
            if data['project'] in check_list:
                if data['completed']:
                    number += 1
                break
        elif type == "repair_all":
            if not data['completed']:
                number = 0
                break
            number += 1
            if number == 5:
                number = 1
        elif type == "farmhouse":
            if data['type'] == "Farmhouse":
                number = data["upgradeLevel"]
                break
        elif type == "talk":
            if data['Name'] in check_list:
                if data['isTalked']:
                    number += 1
                break
        elif type == "gift":
            if data['Name'] in check_list:
                if data['GiftsToday']:
                    number += 1
                break
        elif type == "date":
            if data in check_list:
                number += 1
                break
        elif type == "propose":
            if data in check_list:
                number += 1
                break
        elif type == "npc_friendship":
            if data['Name'] in check_list:
                number = data['Friendship']
                break
        elif type == "help_quest":
            if data['id'] is None:
                number += 1
        elif type == "completed_quest":
            if data['completed']:
                number += 1
        elif type == "story_quest":
            if data['id'] in check_list:
                if not data['completed']:
                    number = 1
                break

    # print(number)
    return number


def surrounding_check(check_list: list, surrounding: list, last_surrounding: list, is_remove: bool, type: str) -> int:
    number = 0
    if is_remove:
        for last_tile in last_surrounding:
            if type in last_tile and last_tile[type] in check_list:
                position = last_tile["position"]
                for tile in surrounding:
                    if tile.get("position") == position and tile.get(type) not in check_list:
                        number += 1
                        break
    else:
        for tile in surrounding:
            if type in tile and tile[type] in check_list:
                position = tile["position"]
                for last_tile in last_surrounding:
                    if last_tile.get("position") == position and last_tile.get(type) not in check_list:
                        number += 1
                        break

    # print(number)
    return number


def building_moving_check(check_list: list, buildings: list, last_buildings: list, is_change: bool) -> int:
    number = 0
    for last_building in last_buildings:
        if last_building["type"] in check_list:
            building_id = last_building["id"]
            position = last_building["position"]
            for building in buildings:
                if building.get("id") == building_id and (building.get("position") != position) == is_change:
                    number += 1
                    break

    # print(number)
    return number


def count_check(check_list: list, data_list: list, last_data_list: list, is_remove: bool, type: str) -> int:
    current_count = count(check_list, data_list, type)
    last_count = count(check_list, last_data_list, type)

    if is_remove:
        return max(last_count - current_count, 0)
    else:
        return max(current_count - last_count, 0)
