import unrealsdk
import json

names = {}
prefixes = {}

# Weapons
for part in unrealsdk.FindAll("InventoryPartData", True):
    partObj = part.GetObjectName().split(" ")[-1]
    
    if part.PrefixPartList is not None:
        if len(list(part.PrefixPartList)) != 0:
            prefixPart = max([x for x in part.PrefixPartList], key = lambda p: p.Priority)
            prefixes[partObj] = prefixPart.PartName
    
    if part.TitlePartList is not None:
        if len(list(part.TitlePartList)) != 0:
            titlePart = max([x for x in part.TitlePartList], key = lambda p: p.Priority)
            names[partObj] = titlePart.PartName

# Real simple fix up because lol eridian fabricator
names["/Game/Gear/Weapons/HeavyWeapons/Eridian/_Shared/_Design/Parts/Part_Eridian_Fabricator.Part_Eridian_Fabricator"] = "Eridian Fabricator"

# Items
for obj in unrealsdk.FindAll("InventoryNamingStrategyData", True):
    if obj.SingleNames is None: continue
    for entry in obj.SingleNames:
        if None in (entry.Part, entry.NamePart):
            continue
        part = entry.Part.GetObjectName().split(" ")[-1]
        names[part] = entry.NamePart.PartName

# Customizations
for obj in unrealsdk.FindAll("GbxCustomizationData", True):
    if obj.BalanceData is None: continue
    balanceData = obj.BalanceData
    names[balanceData.GetObjectName().split(" ")[-1]] = obj.CustomizationName

with open("./Mods/Tools/Data/part_name_mapping.json", "w+") as outFile:
    json.dump(names, outFile)

with open("./Mods/Tools/Data/prefix_name_mapping.json", "w+") as outFile:
    json.dump(prefixes, outFile)