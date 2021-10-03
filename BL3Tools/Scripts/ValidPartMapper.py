import unrealsdk  # type: ignore[import]
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

# getall InventoryBalanceData RuntimeGenericPartList
anointmentMapping: Dict[str, List[str]] = {}
for balance in unrealsdk.FindAll("Class /Script/GbxInventory.InventoryBalanceData", True)[2:]:
    if "Default__" in str(balance):
        continue
    partList = balance.RuntimeGenericPartList
    anointments = []

    if partList.bEnabled:
        anointments = [str(x.PartData).split(" ")[-1] for x in list(partList.PartList) if x.PartData != None]

    anointmentMapping[str(balance).split(" ")[-1]] = anointments

with open("./Mods/Tools/Data/valid_part_database.json", "w+") as outFile:
    json.dump(partMapping, outFile, indent=4, sort_keys=True)

with open("./Mods/Tools/Data/valid_generics.json", "w+") as outFile:
    json.dump(anointmentMapping, outFile, indent=4, sort_keys=True)
