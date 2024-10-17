# PsyForge

A library for easy creation of 2D and 3D psychology experiments.

## Overview

The experimental designs required to explore and understand neural mechanisms are becoming increasingly complex. There exists a multitude of experimental programming libraries for both 2D and 3D games; however, none provide a simple and effective way to handle high-frequency inputs in real-time (i.e., a closed-loop system). This is because the fastest these games can consistently react is once a frame (usually 30Hz or 60Hz). We introduce PsyForge, a framework for creating 2D and 3D experiments that handle high-frequency real-time data. It uses a safe threading paradigm to handle high precision inputs and outputs while still providing all of the power, community assets, and vast documentation of the Unity game engine. PsyForge supports most platforms such as Windows, Mac, Linux, iOS, Android, VR, and Web (with convenient psiTurk integration). Similar to other experimental programming libraries, it also includes common experimental components such as logging, configuration, text display, audio recording, language switching, and an EEG alignment system. PsyForge allows experimenters to quickly and easily create high quality, high throughput, cross-platform games that can handle high-frequency closed-loop systems.

For more information than what is in this document, please see the [Documentation Site](https://bruskatech.github.io/PsyForge).

## Making a Basic Experiment

Here is how to start making a basic experiment.

1. Add PsyForge as a submodule to your project
    1. Open the Unity Package Manager (Top Menu Bar: Window > Package Manager)
    1. Click the plus in the top left corner and select "Install package from git URL"
    1. Paste the url for this github repo and click "Install"

    ```sh
    https://github.com/BruskaTech/PsyForge.git
    ```

    1. Wait for the install to finish (it may take a couple minutes)
1. Inherit `ExperimentBase` on your main experiment class.
1. Implement the abstract methods `PreTrials`, `TrialStates`, `PracticeTrialStates`, and `PostTrials`.

To see a basic example, check the [Examples folder](https://github.com/BruskaTech/PsyForge/tree/main/Example)

## Types of Experiments and Components Available

There are many types of experiments, but here are a few common ones and the useful components for them. There is also a list of generally useful components.

### General Components

- Config
- Logging
- ErrorNotifier
- NetworkInterface
- InputManager
- List/Array shuffling (including ones that are consistent per participant)
- Random values that are consistent per participant

### Word List Experiments

- TextDisplay
- SoundRecorder
- VideoPlayer

### Spatial (3D) Experiments

- SpawnItems
- PickupItems

### Closed-Loop Experiments

- EventLoop
- ElememInterface

## FAQ

See the [FAQ](https://bruskatech.github.io/PsyForge/articles/FAQ.html).

## Authors

### PsyForge Authors

James Bruska

### UnityEPL 2.0 Authors

James Bruska, Connor Keane, Ryan Colyer

### UnityEPL 1.0 Authors

Henry Solberg, Jesse Pazdera
