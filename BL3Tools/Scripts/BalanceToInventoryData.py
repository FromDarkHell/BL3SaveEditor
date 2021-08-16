import unrealsdk
from unrealsdk import *  # type: ignore[import]
import json
import os

# This script creates a JSON file containing every balance and their respective inventory data.
# Do know that this data isn't actually a hard-limitation; Balances when saved in serials can have *any* InventoryData

balances = unrealsdk.FindAll("Class /Script/GbxInventory.InventoryBalanceData", True)[2:]
mapping = {}


for balance in balances:
    balanceName = balance.GetObjectName()
    if "Default__" in balanceName:
        continue
    inventoryData = balance.InventoryData
    if inventoryData == None:
        Log(f"Skipping balance: {balanceName}...Missing inventory data")
    inventoryData = inventoryData.Name
    balanceName = balanceName.split(" ")[-1]
    mapping[balanceName] = inventoryData

with open("./Mods/Tools/Data/balance_to_inv_data.json", "w+") as outFile:
    json.dump(mapping, outFile)
