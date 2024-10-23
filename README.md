# FrogunFix
Ultrawide mod for Frogun

## Installation Instructions:

- Grab the latest release of FrogunFix from [here](https://github.com/KingKrouch/FrogunFix/releases).
- Extract the .zip archive's contents into the game directory.<br />(e.g. "**steamapps\common\Frogun\Windows**" for Steam).
- To adjust any settings open the config file located in **\BepInEx\Config\FrogunFix.cfg**

## Linux and Steam Deck Notes:

If you are playing on Linux or the Steam Deck, you will need to change the compatibility prefix to Proton Experimental *(as the native Linux client has issues, and is incompatible at the moment)* and then adjust the game's launch options through the game properties on Steam.

You will need to append this to the beginning of the game's launch options before playing: ```WINEDLLOVERRIDES="winhttp=n,b" %command%```
