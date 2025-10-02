This directory contains textures extracted from old games, such as CS 1.6

Worflow to get the textures:

1. Download the .bsp map file from the preferred game
	> CS 1.6 & HL1 are GoldSrc games --> GoldSrc bsp's
	> CS:S, HL2, CS:GO are source engine games --> Source engine bsp's

2. Use "Wintextract" (https://developer.valvesoftware.com/wiki/Wintextract) to get a .wad file (containing all textures) from the .bsp

3a. For GoldSrc maps: Use TexMex (https://valvedev.info/tools/texmex/) to open the extracted .wad file and see and export the individual textures.
3b. For Source Engine maps: Use Wally (https://developer.valvesoftware.com/wiki/Wally) to open the extracted .wad file and see and export the individual textures.

4. Put the extracted textures in this directory - they can now be used in SurfaceDefs or WallMaterialDefs