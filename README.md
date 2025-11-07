# EDIA EYE Pico 

Eye tracking support for the PICO headsets (PICO 4E, not tested for other headsets) to provide eye tracking data in the context of the [EDIA Toolbox](https://edia-toolbox.github.io). 

# Features
Provides eye tracking data from a Aero headset per eye (left, right, center) at the device's native sampling frequency if wanted (120 Hz). All samples are provided once per Unity `Update()` via the interface of the [EDIA Eye](https://github.com/edia-toolbox/edia_eye) package. The package here "only" solves the parsing into the `EDIA Eye` compatible format.   

# Installation
You need: 
1. Import the [PICO Unity integration (v3.1.0)](https://github.com/Pico-Developer/PICO-Unity-Integration-SDK.git) package: 
    ```bash
    https://github.com/Pico-Developer/PICO-Unity-Integration-SDK.git#3.1.0
    ```
2. The [EDIA Core](https://github.com/edia-toolbox/edia_core/) package.
3. The [EDIA Eye](https://github.com/edia-toolbox/edia_eye/) package.
4. This package

# Scene setup
1. Follow the instructions on how to set up your scene as described in the ([EDIA Pico](https://mind-body-emotion.notion.site/EDIA-Eye-Pico-12803dd4773f80f7a757ebbfe0ae5845)) page, or have a look at our online documentation [EDIA Toolbox[(https://edia-toolbox.github.io/).

2, Import the `Demo Scene` from the `Samples` in the `EDIA Eye PICO` package. 
   1. Open the demo scene. 
   2. In the scene -> on the `Eye-Pico-DataConverterPico` prefab, find the `PXR_Manager` component.
   3. Activate `Eye Tracking` and `Eye Tracking Calibration`
   4. Set `FaceTracking` to `None` (if you need face tracking, you need to allow `unsafe code` in the `Player Settings`)
   > <details>
   >
   > <summary>Img</summary>  
   >
   > ![img.png](Media/Screenshots/PxrManager.png)
   > </details>

 3. Set up the project for working with `EDIA Eye PICO`:
    1. Follow the [Setup Instructions by PICO](https://developer.picoxr.com/document/unity/complete-project-settings/) to adjust the `Project Settings`
        1. `Scripting Backend`  : `IL2CPP`
        2. `Target Architecture`  : `ARM64`
        3. Set a `Keystore` and a PW
    2. In `Project Settings/Player/Other/`
        1. Set `Graphics API` to `OpenGLES3` (before `Vulkan`)
        2. Set `Active Input Handling` to `Input System Package (New)`
    3. In `Build Profiles`
        1. Switch the build target to `Android`
        2. Add the Demo Scene to the `Scene List` (and remove others)

     
# Questions or problems?
Please reach out to us. You find contact info on the central repo of the [EDIA Toolbox](https://github.com/edia-toolbox/edia_core/) or via [EDIA Toolbox[(https://edia-toolbox.github.io/).

# Citation
If you are using this repository for your research or other public work, please cite the [EDIA Toolbox](https://github.com/edia-toolbox/edia_core/).


