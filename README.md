# Overview

This application is a basic demo of a functional game lobby system. Players can choose to either host a lobby or join an existing lobby. Lobbies automatically refresh every 5 seconds and can be triggered manually via the refresh button.

There are lots of live configuration that also takes place. Players can set their non-unique playername, and the lobby host can configure several lobby options which are synchronized to all clients.

The program utilizes Unity Lobby, a part of the Unity Gaming Services, which acts as a client/server model. To begin, you must first register your app with the Unity Gaming Services, enable Lobby, and download and install the Lobby package. In the game, you will need to authenticate (this application authenticates anonymously) and then write all the necessary methods to interface with the Lobby API.

This program serves as a proof-of-concept and partial prototype for a multiplayer Real-Time Strategy game currently in development in our team.

[Software Demo Video](https://youtu.be/72oV5vfh2ko)

# Network Communication

The model uses client/server. Under the hood, it uses a REST API over HTTPS (TCP on port 443).

# Development Environment

Unity 2022.3.10f1
Visual Studio 2022

C#

# Useful Websites

* [Unity Lobby Tutorial by Code Monkey](https://www.youtube.com/watch?v=-KDlEBfCBiU&pp=ygULdW5pdHkgbG9iYnk%3D)
* [Unity UI Documentation](https://docs.unity3d.com/Manual/UIToolkits.html)
* [Unity Lobby Documentation](https://docs.unity.com/ugs/manual/lobby/manual/unity-lobby-service)
* [Unity Lobby Services Specs](https://services.docs.unity.com/lobby/v1/#tag/Lobby)

# Future Work

* Host transfer
* Start game

# Credits

Images used:

[Image by kjpargeter](https://www.freepik.com/free-photo/dark-grunge-style-scratched-metal-surface_10167160.htm#query=black%20metal%20texture&position=1&from_view=keyword&track=ais) on Freepik

[Image by rawpixel.com](https://www.freepik.com/free-photo/silver-metallic-textured-background_4246716.htm#query=metal%20texture&position=2&from_view=keyword&track=ais) on Freepik
