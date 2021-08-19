import unrealsdk
from unrealsdk import *  # type: ignore[import]
import json
import os
from typing import Dict, List

# partMapping = {"PART": {"Dependencies": [], "Excluders": []}}
partMapping: Dict[str, Dict[str, List[str]]] = {}

for part in unrealsdk.FindAll("Class /Script/GbxGameSystemCore.ActorPartData", True)[2:]:
    if "Default__" in str(part):
        continue
    dependencies = [str(x).split(" ")[-1] for x in list(part.Dependencies)]
    excluders = [str(x).split(" ")[-1] for x in list(part.Excluders)]
    partMapping[str(part).split(" ")[-1]] = {"Dependencies": dependencies, "Excluders": excluders}
    # break


with open("./Mods/Tools/Data/valid_part_database.json", "w+") as outFile:
    json.dump(partMapping, outFile, indent=4, sort_keys=True)
