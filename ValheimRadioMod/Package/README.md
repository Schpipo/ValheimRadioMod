# ValheimRadioMod  
This is my first mod and I used AI for code writing, bug fixing and Unity usage.  
Using the JotunnModStub for the VS project and the Unity asset.  
The radio was hand modelled in Blender and some texture work done with gimp.  
The mod is a work in progress and I will be adding more features as time goes on.  

## Installation  
For automated install use Thunderstore.  

For manual installation, place RadioMod folder in your BepInEx/plugins folder.  

BepInEx is required for this mod to work. You can find it here:  
https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/  
Jotunn is also required for this mod to work. You can find it here:  
https://valheim.thunderstore.io/package/ValheimModding/Jotunn/  

You also need to create an Audio folder in the RadioMod folder.  
Add your audio files in subfolders.  
Folder names will be used as station names.  
Audio files will first be played in numerical and second in alphabetical order. 

Audio Folder = ...\Valheim\BepInEx\plugins\ValheimRadioMod\Audio:  

- Audio  
	- Station 1  
		- 01 Track.mp3  
		- 02 Track.mp3  
		- 03 Track.mp3  
	- Station 2  
		- 01 Track.ogg  
		- 02 Track.ogg  
		- 03 Track.ogg  

## Features  
- Reads all subfolders in the Audio folder of this mod and plays MP3, OGG or WAV files found in those folders.  
- To accommodate the loading times of many audio files, the mod waits 10s after the game has loaded before it starts to read the audio files.  
- A GUI will show the progress of the loading and the number of files found and loaded.  
- Playback is synced by using world time together with total playlist duration.  
- Assigning a group to a station will load the settings of any other member and synchronize all settings across the members of that group.  

## Controls
| Action | Keyboard | Controller |
|--------|----------|------------|
| On/Off | E | A |
| Change Station | E (hold) | A (hold) |
| Assign Group | Shift + E | Y |
| Volume Control | Mouse wheel | A / Y (hold) |

## Changelog  
#### 0.1.0
- First publication for multiplayer testing  
#### 0.1.1
- Added MonoMod.Backports.dll to mod zip  
#### 0.1.12
- Added MonoMod.ILHelpers.dll to mod zip  
#### 0.2.0
- Updated group sync and permissions for better multiplayer support  
- Added (Xbox) controller support  
- Added a GUI and list of groups with Home, Work and Travel as default suggestions  
- Writing your own group name will add it the list of groups  

You can find the github at: https://github.com/Schpipo/ValheimRadioMod  
## Known issues  
DPadUp still fires when selecting a group in the group dialog