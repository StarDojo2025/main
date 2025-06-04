There are configurations you need to customize inside the python file you run.

### Core Configs:

```python
llmProviderConfig     = "./conf/openai_config.json"
embedProviderConfig   = "./conf/openai_config.json"
envConfig             = "./conf/env_config_stardew.json"
```

These files are set up under the `StarDojo/agent/conf/` directory, for your preferred LLM and environment settings.

### Runtime Parameters (`env_params`):

```python
env_params = {
    'port': 6000,
    'save_index': 0,
    'new_game': False,
    'image_save_path': "../env/screen_shot_buffer",
    'agent': react_agent,
    'needs_pausing': True,
    'image_obs': True,
    'task': task,
    'output_video': True,
}
```

* **`new_game: True`** — The environment will start a fresh game and close it upon task completion.
* **`new_game: False`** — You must manually open the game beforehand.