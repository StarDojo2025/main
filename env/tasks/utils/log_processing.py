import os
import re
import glob
import pandas as pd

step_pattern = r"Step (\d+)"
quantity_pattern = r"Current Quantity: (\d+)"

data = []

for md_file in glob.glob("*.md"):
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    task = md_file

    steps_matches = re.findall(step_pattern, content)
    steps_value = int(steps_matches[-1]) + 1 if steps_matches else 0

    completed = 1 if "Completion: True" in content else 0

    time_matches = re.findall(quantity_pattern, content)
    time_value = time_matches[-1] if time_matches else ""

    data.append([task, steps_value, completed, time_value])

df = pd.DataFrame(data, columns=["Task", "Steps", "Completed", "Current Quantity"])
df.to_excel("output.xlsx", index=False)

print("Generated output.xlsx")
