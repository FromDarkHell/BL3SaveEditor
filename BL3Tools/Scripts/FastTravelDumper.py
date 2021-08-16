import json
from unrealsdk import * # type: ignore[import]

fastTravelStations = unrealsdk.FindAll("Class /Script/GbxTravelStation.FastTravelStationData", True)[1:]
mapping = {}
for station in fastTravelStations:
    mapping.update( {str(station).split(" ")[1] : station.DisplayName} )

with open("./Mods/Tools/Data/fast_travel_to_name.json", "w+") as outFile:
    json.dump(mapping, outFile)    