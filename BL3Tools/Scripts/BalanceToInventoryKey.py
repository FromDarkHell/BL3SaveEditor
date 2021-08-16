import os
import json
from unrealsdk import * # type: ignore[import]
import unrealsdk

balances = unrealsdk.FindAll("Class /Script/GbxInventory.InventoryBalanceData", True)[1:]
mapping = {}
for balance in balances:
    balanceName = balance.GetObjectName()
    if("Default__" in balanceName): 
        continue
    partSetData = balance.PartSetData
    if(partSetData == None):
        balanceName = balanceName.split(" ")[-1]
        if "Head" in balanceName:
            mapping.update({balanceName : "BPInvPart_Customization_Head_C"})
            continue
        elif "WeaponSkin" in balanceName or "WeaponTrinket" in balanceName or "RoomDeco" in balanceName:
            mapping.update({balanceName : "InventoryCustomizationPartData"})
        elif "Skin" in balanceName:
            mapping.update({balanceName : "BPInvPart_Customization_Skin_C"})
            continue
        unrealsdk.Log(f"Skipping balance: {str(balance.Name)}...Missing part set")
        continue
    partDataClass = partSetData.PartDataClass
    balanceName = balanceName.split(" ")[-1]

    mapping.update({balanceName : partDataClass.GetName()})

with open("./Mods/Tools/Data/balance_to_inv_key.json", "w+") as outFile:
    json.dump(mapping, outFile)