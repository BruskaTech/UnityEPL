# Features

These are the different features for use and how add them to your project

## Adding the UnityEPL Assembly Definition Reference (UnityEPLExtensions folder)

In order to implement things as "part of" the UnityEPL and extend it's functionality, you will need to create an assembly definition reference (asmref). This is used for things like mkaing your own experiment config variables, language strings, or a new sync box.

1. Make a folder called *UnityEPLExtensions* in the Script folder.
1. Navigate to that folder in the Unity Project Window.
1. Add the asmref (assembly definition reference) for UnityEPL in the folder
    1. Right click in that folder
    1. Create an asmref named *UnityEPL* (Right Click: Create > Scripting > Assembly Definition Reference)
    1. Click on the assembly definition reference
    1. Select the little dot next to "Assembly Definition" in the Inspector
    1. Select the item named UnityEPL
    1. Click the "Apply" button

## Adding Config Variables

1. Make sure you have already created the UnityEPLExtensions folder, [as explained here](#adding-the-unityepl-assembly-definition-reference-unityeplextensions-folder).
1. Create a file named *MyConfig.cs*
Add a static partial class named ```Config``` inside the UnityEPL namespace.

    ```csharp
    namespace UnityEPL {
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

1. Make sure you have already created the UnityEPLExtensions folder, [as explained here](#adding-the-unityepl-assembly-definition-reference-unityeplextensions-folder).
1. Create a file named *MyLangStrings.cs* in the UnityEPLExtensions folder
1. Add a static partial class named ```LangStrings``` inside the UnityEPL.Utilities namespace.

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
