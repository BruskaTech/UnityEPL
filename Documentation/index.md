# UnityEPL 3.0

A library for easy creation of 2D and 3D psychology experiments.

## Overview

The experimental designs required to explore and understand neural mechanisms are becoming increasingly complex. There exists a multitude of experimental programming libraries for both 2D and 3D games; however, none provide a simple and effective way to handle high-frequency inputs in real-time (i.e., a closed-loop system). This is because the fastest these games can consistently react is once a frame (usually 30Hz or 60Hz). We introduce UnityEPL 3.0, a framework for creating 2D and 3D experiments that handle high-frequency real-time data. It uses a safe threading paradigm to handle high precision inputs and outputs while still providing all of the power, community assets, and vast documentation of the Unity game engine. UnityEPL 3.0 supports most platforms such as Windows, Mac, Linux, iOS, Android, VR, and Web (with convenient psiTurk integration). Similar to other experimental programming libraries, it also includes common experimental components such as logging, configuration, text display, audio recording, language switching, and an EEG alignment system. UnityEPL 3.0 allows experimenters to quickly and easily create high quality, high throughput, cross-platform games that can handle high-frequency closed-loop systems.

For more information than what is in this document, please see the [Documentation Site](https://bruskatech.github.io/UnityEPL).

## Making an Experiment

Here is how to start making a basic experiment.

### Basic Instructions

1. Add UnityEPL as a submodule to your project
    1. Open the Unity Package Manager (Top Menu Bar: Window > Package Manager)
    1. Click the plus in the top left corner and select "Install package from git URL"
    1. Paste the url for this github repo and click "Install"

    ```sh
    https://github.com/BruskaTech/UnityEPL.git
    ```

    1. Wait for the install to finish (it may take a couple minutes)
1. Inherit `ExperimentBase` on your main experiment class.
1. Implement the abstract methods `PreTrials`, `TrialStates`, `PracticeTrialStates`, and `PostTrials`.

To see a basic example, check the [Examples folder](https://github.com/BruskaTech/UnityEPL/tree/main/Example)

### Adding Config Variables and Multi-Language Strings

1. Make a folder called *UnityEPLExtensions* in the Script folder.
1. Navigate to that folder in the Unity Project Window.
1. Add the asmref (assembly definition reference) for UnityEPL in the folder
    1. Right click in that folder
    1. Create an asmref named *UnityEPL* (Right Click: Create > Scripting > Assembly Definition Reference)
    1. Click on the assembly definition reference
    1. Select the little dot next to "Assembly Definition" in the Inspector
    1. Select the item named UnityEPL
    1. Click the "Apply" button
1. Add config variables
    1. Create a file named *MyConfig.cs* and add a static partial class named ```Config``` inside the UnityEPL namespace.

        ```csharp
        namespace UnityEPL {
            public static partial class Config {
                // config items go here
            }
        }
        ```

    1. Implement each config item in your ```Config``` class like this:

        ```csharp
        public static string videoPath { get { return Config.GetSetting<string>("videoPath"); } }
        ```

1. Add multi-language strings
1. Create a file named *MyLangStrings.cs* and add a static partial class named ```LangStrings``` inside the UnityEPL.Utilities namespace.

    ```csharp
    namespace UnityEPL.Utilities {
        public static partial class LangStrings {
            // multi-language strings go here
        }
    }
    ```

1. Implement each multi-language string in your ```LangStrings``` class like this:

    ```csharp
    public static LangString SessionEnd() { return new( new() {
        { Language.English, "Yay! Session Complete.\n\n Press any key to quit." },
    }); }
    public static LangString TrialPrompt(uint trialNum) { return new( new() {
        { Language.English, $"Press any key to start Trial {trialNum}." },
        { Language.Spanish, $"Presione cualquier tecla para iniciar la Prueba {trialNum}."}
    }); }
    ```

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

See the [FAQ](https://bruskatech.github.io/UnityEPL/articles/FAQ.html).

## Original Authors

James Bruska, Ryan Colyer
