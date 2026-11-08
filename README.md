# ValheimRadioMod
This is my first mod and I used AI for code writing, bug fixing and Unity usage.
Using the JotunnModStub as a base for the VS project and the Unity asset.
The radio was hand modelled in Blender and some texture work done with gimp.
The mod is a work in progress and I will be adding more features as time goes on.

## Installation (manual)


## Features
Reads all subfolders in the Audio folder of this mod and plays mp3, ogg or wav files found in those folders.
To accomodate the loading times of many audio files, the mod waits 10s after the game has loaded before it starts to read the audio files.
A GUI will show the progress of the loading and will show the number of files found and loaded.
Playback is synced by using the JotunnModStub's network sync features.

E : on/off
E (hold) : change station
Shift + E : assign group to station

Assigning a group to a station will load the settings of any other member and synchronize all settings across the members of that group.

## Changelog


## Known issues
NAudio is not in use yet, so the mod will not work on Linux or MacOS. I will be adding NAudio support in the future.
You can find the github at:
