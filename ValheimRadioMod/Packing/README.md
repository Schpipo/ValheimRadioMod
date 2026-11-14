# ValheimRadioMod  
This is my first mod and I used AI for code writing, bug fixing and Unity usage.  
Using the JotunnModStub as a base for the VS project and the Unity asset.  
The radio was hand modelled in Blender and some texture work done with gimp.  
The mod is a work in progress and I will be adding more features as time goes on.  

## Installation  
For automated install use Thunderstore  

For manual installation, place RadioMod folder in your BepInEx/plugins folder.  

In both cases, you also need to create an Audio folder in the RadioMod folder and add your audio files in subfolders.  
Folder names will be used as station names and audio files will be played in the order they are found.  

Audio Folder = ...\Valheim\BepInEx\plugins\ValheimRadioMod\Audio:  

Audio  
-- Station 1  
---- Track 1.mp3  
---- Track 2.mp3  
---- Track 3.mp3  
-- Station 2  
---- Track 1.ogg  
---- Track 2.ogg  
---- Track 3.ogg  
	
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
0.1.1 added MonoMod.Backports.dll to mod zip  
0.1.12 added MonoMod.ILHelpers.dll to mod zip  
0.2.0 updated group sync and permissions for better multiplayer support. updated version to replace 0.1.12  

## Known issues  
You can find the github at: https://github.com/Schpipo/ValheimRadioMod  
