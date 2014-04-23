##Unity uOS Plugin##

This is a very restricted port of the [uOS middleware](https://github.com/UnBiquitous/uos_core) as plugin for [Unity](http://unity3d.com) game engine. This port intends to enable basic communication between Unity games and a uOS-based ubiquitous smartspace. The following description assumes you are familiar with Unity environment and script programming, if you are not, please refer to their [documentation](http://unity3d.com/learn) for details before using this plugin.

** This plugin was coded in C# **

##Before you continue!##

This code is in a roughly crude state. Very little has been tested and the code is yet unstable, I can only assure that the common scenarios are working. Unity's threading model is very strict and will probably lead to network resource leaks if you test your game from inside the editor. For this reason, I suggest you build the game as an executable and run it in a separate process, to make sure all connections will be correctly closed. **Since the Unity Editor play mode does not create a different process, but actually only starts a different thread, all children threads created by your game (and this plugin needs to create some) may become orphans and run forever.** If you find a way to resolve this issue, please, let me know! (I've tried several ways without success). If this problem happens, a simple logout and login in your OS is enough to kill the open connections, but I warn you that debugging your game may be a pain in the a*.

## Ported Features ##

Here is a list of what is actually available from the original set of features from uOS. If you just want to make a basic test, you can skip to the next section.

#### What is already working ####

- uP messages and entities JSON serialization and deserialization;
- Basic Gateway that can list devices and call services on remote devices;
- MulticastRadar to detect devices in the local network (UDP);
- TCP connectivity;
- DeviceDriver services;
- Basic infrastructure to create new Drivers, but yet to be completed.

#### What is being implemented right know ####

- Local service handling and Application service handling;
- Connection cache;
- Improved driver registry and equivalence search;
- Better infrastructure to create new drivers and describe ther interfaces;
- Better integration with the Editor;
- Better logging and error reporting.

#### What is not planned to be implemented soon ####

- Other types of radar (ping, arp, etc...);
- Proxying;
- Ontologies;
- Anything else that is not listed in the previous section. :-)

## Getting Started ##

Here I assume you are familiar with Unity. If not, refer to their [documentation](http://unity3d.com/learn).

1. Clone or download the source for this plugin.

2. Copy the folder **'uOS'** to the root of your game project (you may also want the **'Test'** folder, for some samples of usage).

3. Go to menu item **uOS->Edit Settings** to edit connection parameters and other settings. I highly recommend that you change nothing and use the default settings.

4. Create or choose a scene object to hold your instance of the **uOS component** (uOS\uOS.cs), it may be any object that will exist during the scene's entire life. The main Camera, for instance is a good choice. Just add the component to the object. Remember: you **MUST** have exactly **ONE INSTANCE** of the uOS component in your game scene. Don't worry, the plugin will nag if you do not do it. 

5. Remember to import the library by adding **using namespace UOS;** to your script. Then, you must call *uOS.Init()* (a static method) before making any other API calls. I suggest you make this call on *Start* method of your main game controller. If you make this call on *Awake*, some problems might occur, prefere *Start*.

6. Now you are done! To access the API, just call one of the following methods on *uOS.gateway*:
  - *GetCurrentDevice*
  - *ListDevices*
  - *CallService* (synchronous version)
  - *CallService* (asynchronous version - **prefer this one!**)

7. The asynchronous version of the service call expects a callback delegate. This delegate receives information about the call, the response, the caller state (if provided) and any exceptions. It's your responsibility to check the data for problems.
 
8. **Do not forget to call** *uOS.TearDown()* **when you are done.**
