# Thronefall Multiplayer

This is the development repository for the WIP Thronefall Multiplayer mod.
It uses BepInEx to inject code into the game.

## Development Setup

This is written in CSharp so you will have to acquire an IDE that can read .sln files (for example JetBrains Rider)
You will also need an installation of Thronefall, run the setup.ps1 script to acquire all required dlls from your installation.
After that open the solution file and run the build.

## How to Run

Download BepInEx 6 and copy it into your Thronefall directory.
When you run the build it copies the ThronefallMultiplayer dlls to the correct plugin folder.
Run your executable either directly or through Steam.

## TODO

* Make UI for hosting/connecting to servers
* Make UI to leave a server
* Change networking so playerid != peerid