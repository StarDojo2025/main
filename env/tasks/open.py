from .base import TaskBase


class Open(TaskBase):
    def evaluate(self, obs, proxy) -> dict:
        return {
            'completed': False
        }
