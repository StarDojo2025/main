import re

def check_is_number(string : str) -> bool:
    '''
    check whether the string is number
    '''
    pattern = r'^[+-]?([0-9]+(\.[0-9]*)?|\.[0-9]+)$'
    return re.fullmatch(pattern, string) is not None


def check_is_int(string : str) -> bool:
    '''
    check whether the string is int
    '''
    pattern = r'^[+-]?[0-9]+$'
    return re.fullmatch(pattern, string) is not None


def check_is_float(string : str) -> bool:
    '''
    check whether the string is float
    '''
    pattern = r'^[+-]?([0-9]+\.[0-9]*|\.[0-9]+)$'
    return re.fullmatch(pattern, string) is not None


def get_direction_text(direction: int) -> str:
    '''
    get the direction text
    '''
    if direction == 0:
        return 'up'
    elif direction == 1:
        return 'right'
    elif direction == 2:
        return 'down'
    elif direction == 3:
        return 'left'
    else:
        return 'unknown'

def get_facing_position(x: int, y: int, direction: int) -> tuple:
    '''
    get the facing position
    '''
    if direction == 0:
        return x, y - 1
    elif direction == 1:
        return x + 1, y
    elif direction == 2:
        return x, y + 1
    elif direction == 3:
        return x - 1, y
    else:
        return x, y