# Reactional Music Plugin for Unity

Welcome to the Reactional Music Plugin for Unity! This plugin enables dynamic, real-time music interaction within your Unity projects, powered by Reactional Music's API.
It includes downloadable demo scenes and games to help you explore and test the API’s capabilities through scripts and an Editor Widget.

## Table of Contents
1. [Features](#features)
2. [Installation](#installation)
3. [Setup](#setup)
4. [Using the Demo Scene](#using-the-demo-scene)
5. [Blueprints and Functionality](#blueprints-and-functionality)
6. [Editor Widget Usage](#editor-widget-usage)
7. [Custom Menu Bar](#custom-menu-bar)

## Features
**Reactional Manager**: A game object that controls music interaction.
- **Downloadable Demo Scenes and Games**: Demonstrating core plugin functionality and showcasing API integration.
**Script-Based Examples**: C# scripts for API interaction.
**Basic Playback Script**: A nifty example that demonstrates various ways to load, swap, and transition between Tracks & Themes seamlessly. Whit lots of options.
**Reactional Editor Widget**: A UI tool for managing music bundles, themes, and tracks in Pie or Runtime.
**Reactional Free Bundle**: A free bundle to get you started with testing the plugin with the Widget and in Code. 


## Installation
1. Download the plugin and import the .unitypackage into your Unity project.
2. Ensure the assets are placed in Assets/ReactionalMusic/.
3. Verify that the Reactional Editor Widget is accessible from the menu.

## Setup
**Get a Music Bundle**
Open Tools > Reactional > Reactional Editor Widget.
Connect to the Reactional Platform to get your bundle. Or use the Free Reactional Bundle that comes with the Plugin.
Add a bundle into the Streaming Asset Folder

**Add the Reactional Manager**
Navigate to Toolbar > Reactional > Add Reactional Manager
Or, if the project supports Reactional, a prompt will appear when opening Unity, allowing you to add it.

**Press "Reload Bundle"**
Ensures the correct tracks and themes are loaded into the project.

**Playback**
Use the Reactional Widget to test your Bundle or set up custom behaviours inside Basic Playback Script or anywhere you like.

---

## Using the Demo Scenes and Games
The demo scenes and games provides a sandbox environment to test the Reactional Music plugin’s capabilities. This scene demonstrates how to use blueprints and the API to interact with themes, tracks, stingers, and other music elements.

**Downloading Demo Scenes & Games**
   - Navigate to Toolbar → Download Games, and open the downloaded asset in a new project.
   - 
**Play the mini-game and Interact with the Scripts**:
   - Interact with the provided scripts to test the API’s functionality.
   - Play the scene to experience reactional functionality:

---

## Scripts and Functionality
Each Script in the plugin is designed to showcase specific interactions with the Reactional Music API. Below is an overview of the primary blueprints included:

- **BasicPlayback** → Core script to test track loading and transitions.

- Animation
  - **AnimationBeatSync** → Syncs an animation curve to the music, repeating on each beat.

- Audio
  - **InstrumentOverride** → Simple showcase of the InstrumentOverride method, change the range values in runtime to see how it affects the outcome.
  - **PitchAdjustingSFX** → Harmonizes SFX with music by adjusting pitch.
  - **TriggerClipOnBeat** → Triggers an audio clip on a beat.

- Events
  - **OnBarBeatEvent** → Shows how to use the OnBarBeat delegate.
  - **ScaleTaggedMeshesOnBeat** → Pulses mesh scale over each beat of the music.
  - **SetThemeControlByDistance** → Adjusts music elements based on player distance.
  - **ThemeControlsData** → Parent for other scripts to inherit theme inspector properties.
  - **TrackControlsData** → Parent for other scripts to inherit track inspector properties.
  - **TriggerThemeControlOnCollision** → Reacts to triggers by changing music parameters.
  - **TriggerTrackOnCollision** → Loads and starts a new track when triggered.

- Rendering
  - **CameraPulseOnBeat** → Affects the camera zoom over each beat of the music.

- UI
  - **TrackProgressBar** → Displays the progress of track playback in UI.

- VFX
  - **VisualEffectsGraphOnNoteEvent** → Sends an event to the VFX on theme note generation.
  - **VisualEffectsGraphPulseOnBeat** → Sends an event to the VFX on each beat.

---

## Editor Widget Usage
The Reactional Editor Widget provides an easy way to interact with music themes and tracks:

**Open the Editor Widget:** Navigate to Tools > Reactional > Reactional Editor Widget.
**Widget Controls:**
 - Access Platform.
 - Test and preview themes, tracks, and stingers.
 - Control the volumes in real-time.
 - Print theme controls to console.
 - Access various features of the Reactional Music API directly from the editor.

---

## Custom Menu Bar
Under the Tools Tab in Unity, the Reactional Menu provides quick access to:

- Platform
- Documentation
- Discord
- Forum
- Website

- Plugin Project Settings
- Reactional Editor Widget
- Download Scenes & Games

- Readme
- Changelog

For more information, refer to the [Reactional Music API Documentation](https://docs.reactionalmusic.com/Unity/Unity%20API).

If you encounter any issues or need further assistance, please reach out to the support team or check the documentation for troubleshooting tips.

---

Thank you for using the Reactional Music Plugin! We hope it brings your projects to life with dynamic, interactive music experiences.
