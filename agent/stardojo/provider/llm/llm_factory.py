from stardojo.provider.llm.openai import OpenAIProvider
from stardojo.provider.llm.claude import ClaudeProvider
from stardojo.provider.llm.gemini import GeminiProvider
from stardojo.utils import Singleton


class LLMFactory(metaclass=Singleton):

    def __init__(self):
        self._builders = {}


    def create(self, llm_provider_config_path, embed_provider_config_path, **kwargs):

        llm_provider = None
        embed_provider = None

        key = llm_provider_config_path

        if "opensrc" in key:
            llm_provider = OpenAIProvider(is_opensource=True)
            llm_provider.init_provider(llm_provider_config_path)
            # embed_provider = llm_provider
            embed_provider = OpenAIProvider()
            embed_provider.init_provider(embed_provider_config_path)

        if "openai" in key:
            llm_provider = OpenAIProvider()
            llm_provider.init_provider(llm_provider_config_path)
            embed_provider = llm_provider
        elif "claude" in key:
            llm_provider = ClaudeProvider()
            llm_provider.init_provider(llm_provider_config_path)
            #logger.warn(f"Claude do not support embedding, use OpenAI instead.")
            embed_provider = OpenAIProvider()
            embed_provider.init_provider(embed_provider_config_path)
        elif "gemini" in key:
            llm_provider = GeminiProvider()
            llm_provider.init_provider(llm_provider_config_path)
            # logger.warn(f"Claude do not support embedding, use OpenAI instead.")
            embed_provider = OpenAIProvider()
            embed_provider.init_provider(embed_provider_config_path)

        if not llm_provider or not embed_provider:
            raise ValueError(key)

        return llm_provider, embed_provider
