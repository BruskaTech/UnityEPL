# Features

These are the different features for use and how add them to your project

## Adding the PsyForge Assembly Definition Reference (PsyForgeExtensions folder)

In order to implement things as "part of" the PsyForge and extend it's functionality, you will need to create an assembly definition reference (asmref). This is used for things like mkaing your own experiment config variables, language strings, or a new sync box.

1. Make a folder called *PsyForgeExtensions* in the Script folder.
1. Navigate to that folder in the Unity Project Window.
1. Add the asmref (assembly definition reference) for PsyForge in the folder
    1. Right click in that folder
    1. Create an asmref named *PsyForge* (Right Click: Create > Scripting > Assembly Definition Reference)
    1. Click on the assembly definition reference
    1. Select the little dot next to "Assembly Definition" in the Inspector
    1. Select the item named PsyForge
    1. Click the "Apply" button

## Adding Config Variables

1. Make sure you have already created the PsyForgeExtensions folder, [as explained here](#adding-the-psyforge-assembly-definition-reference-psyforgeextensions-folder).
1. Create a file named *MyConfig.cs*
Add a static partial class named ```Config``` inside the PsyForge namespace.

    ```csharp
    namespace PsyForge {
        public static partial class Config {
            // config items go here
        }
    }
    ```

1. Implement each config item in your ```Config``` class like this:

    ```csharp
    public static string videoPath { get { return GetSetting<string>("videoPath"); } }
    ```

## Adding Multi-Language Strings (LangStrings)

1. Make sure you have already created the PsyForgeExtensions folder, [as explained here](#adding-the-psyforge-assembly-definition-reference-psyforgeextensions-folder).
1. Create a file named *MyLangStrings.cs* in the PsyForgeExtensions folder
1. Add a static partial class named ```LangStrings``` inside the PsyForge.Utilities namespace.

    ```csharp
    namespace PsyForge.Utilities {
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

## Adding a New Syncbox

We will create a fake sync box named TestSyncBox to demonstrate how to do it.

1. Make sure you have already created the PsyForgeExtensions folder, [as explained here](#adding-the-psyforge-assembly-definition-reference-psyforgeextensions-folder).
1. Create a file named *TestSyncBox.cs* in the PsyForgeExtensions folder.
1. Create the class in the ```PsyForge.ExternalDevices``` namespace that inherits from ```SyncBox```

    ```csharp
    using System.Threading.Tasks;

    namespace PsyForge.ExternalDevices {
        public class TestSyncBox : SyncBox {

        }
    }
    ```

1. Next implement the ```Init()``` function. This should be where all initialization takes place.

    ```csharp
    public override Task Init() {
        UnityEngine.Debug.Log("Init SyncBox");
        return Task.CompletedTask;
    }
    ```

1. Implement the ```TearDown()``` function. This will be called automatically when the experiment is quit.

    ```csharp
    public override Task TearDown() {
        UnityEngine.Debug.Log("TearDown SyncBox");
        return Task.CompletedTask;
    }
    ```

1. Finally, implement the ```Pulse()``` function. This should always have a delay. It also can't run more than once per frame (otherwise it throws an exception).

    ```csharp
    public override async Task Pulse() {
        UnityEngine.Debug.Log("Pulse SyncBox");
        await Task.Delay(1000);
    }
    ```

<details>
<summary>Altogether it looks like this.</summary>

```csharp
using System.Threading.Tasks;

namespace PsyForge.ExternalDevices {
    public class TestSyncBox : SyncBox {
        public override Task Init() {
            UnityEngine.Debug.Log("Init SyncBox");
            return Task.CompletedTask;
        }

        public override async Task Pulse() {
            UnityEngine.Debug.Log("Pulse SyncBox");
            await Task.Delay(1000);
        }

        public override Task TearDown() {
            UnityEngine.Debug.Log("TearDown SyncBox");
            return Task.CompletedTask;
        }
    }
}
```

</details>
