# ValheimRadioMod
This is my first mod and I used AI for code writing, bug fixing and Unity usage.
Using the JotunnModStub as a base for the VS project and the Unity asset.
The radio was hand modelled in Blender and some texture work done with gimp.
The mod is a work in progress and I will be adding more features as time goes on.

## Installation (manual)
Install via Thunderstore
For manual installation, place in plugins folder and add your subfolders with music to the "Audio" folder.

## Features
Reads all subfolders in the Audio folder of this mod and plays MP3, OGG or WAV files found in those folders.
To accommodate the loading times of many audio files, the mod waits 10s after the game has loaded before it starts to read the audio files.
A GUI will show the progress of the loading and the number of files found and loaded.
Playback is synced by using the world time together with total playlist duration.

E : on/off
E (hold) : change station
Shift + E : assign group to station
Mouse wheel : Volume control

Assigning a group to a station will load the settings of any other member and synchronize all settings across the members of that group.

## Changelog
0.1.0 first publication for multiplayer testing

## Known issues
NAudio is not in use yet, so the mod will not work on Linux or MacOS. I will be adding NAudio support in the future.
You can find the github at: https://github.com/Schpipo/ValheimRadioMod
