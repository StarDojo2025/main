After setup, run python files under the `env/` directory.

| Mode                    | Command                        | Description                      |
| ----------------------- | ------------------------------ | -------------------------------- |
| 🧪 Single Task          | `python llm_env.py`                      | Runs a single benchmark task     |
| 🔁 Multi-task (serial)  | `python llm_env_multi_tasks.py`          | Runs multiple tasks sequentially |
| ⚡ Multi-task (parallel) | `python llm_env_multi_tasks_parallel.py` | Runs multiple tasks in parallel(Linux only)  |

Customize the tasks using `--task_params`, or `--task_name` and `--task_id` when running a single task.
