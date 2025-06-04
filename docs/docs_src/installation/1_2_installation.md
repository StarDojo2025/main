### Step 1: Add Environment Variables

Create or edit the file at `StarDojo/env/.env` and include the following keys:

```bash
# Required
STARDEW_APP_PATH=/path/to/StardewModdingAPI

# Optional (if using external LLM services)
OA_OPENAI_KEY=sk-xxxx
```

* Make sure `STARDEW_APP_PATH` correctly points to the **file path** of `StardewModdingAPI.exe` on Windows or `StardewModdingAPI` on Linux/Mac.

### Step 2: Initialize Environment

```bash
cd StarDojo
source setup.sh
```

For Windows, execute the command below instead.
```bash
.\setup.ps1
```

This command installs dependencies and prepares the shell environment for easy agent launching.