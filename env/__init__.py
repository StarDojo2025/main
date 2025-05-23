from gymnasium import register

register(
    id='StarDojo-v0',
    entry_point='env.stardew_env:StarDojo',
    disable_env_checker=True
)