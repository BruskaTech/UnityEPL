# FAQ

This is the Frequently Asked Questions document for the PsyForge

## General

These are general questions often asked.

1. ### What does the name stand for?

    - PsyForge stands for Unity Experiment Programming Library.

## Why Async/Await over IEnumerators

So after looking at even the basic example, you will notice that the 'async', 'await', and 'Task' keywords sprinkled everywhere.
This is opposite from what you usually see with Unity ('IEnumerator' and 'yield return' sprinkled everywhere).
Both IEnumerators (in Unity) and async/await act similarly in Unity. Each has its pros and cons.
Below I will explain what each is and why the foundation of PsyForge uses async/await.

### What are IEnumerators

These are the basis for how Unity used to work. Essentially, every function that had to run across multiple frames used an ```IEnumerator```. When you wanted to wait for one frame you would ```yield return null``` in your function. These functions could stact too. For example, you could ```yield return new WaitForSeconds(1)``` to wait for 1 second. Altogether, it would look like this.

```csharp
    IEnumerator Foo() {
        yield return null; // Wait for 1 frame
        yield return new WaitForSeconds(1); // Wait for 1 second
    }
```

### What is Async/Await

So now this new thing comes along, async/await. You mark an function ```async``` any time it is going to wait for something to happen. You mark a function call ```await``` when you are waiting for another async function. The equivalent function of above would be this:

```csharp
    async void Foo() {
        await Awaitable.NextFrameAsync(); // Wait for 1 frame
        await Task.Delay(1000); // Wait for 1 second

        // Note, when using the PsyForgem it should be the following:
        // await MaineManager.Instance.Delay(1000); // Wait for 1 second

        // Or if in an PsyForge Experiment method:
        // await manager.Delay(1000); // Wait for 1 second
    }
```

You can see that they are pretty similar. So what's the difference?

### Pros of IEnumerator

1. It is what was used in the past, which means there is lots of documentation.
1. It compiles down to a single state machine, which means it is very optimized.

### Pros of Async/Await

1. You can have a return values for async functions.

    <details>
    <summary>Expand Explanation</summary>

    It is very hard to have return values from IEnumerator functions. In the best case it is unintuitive and bulky. In the worst case it's unintuitive, bulky, and hacky (with lots of room for mistakes). Most places online just tell you it's not possible. Below is a demonstration of how just in case it interests you.

    This is no problem for async/await methods.

    ```csharp
    async void Start() {
        var numFramesLogged = await FrameLogger();
    }

    async Task<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        await Awaitable.NextFrameAsync();
        UnityEngine.Debug.Log("Frame 2");
        return 2; // number of frames logged   
    }
    ```

    <details>
    <summary>Expand IEnumerator Example</summary>

    Take this simple example of an IEnumerator that logs "Frame 1" on the first frame, waits a frame, and logs "Frame 2" on the second frame.

    ```csharp
    IEnumerator Start() {
        yield return FrameLogger();
    }

    IEnumerator FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        yield return null;
        UnityEngine.Debug.Log("Frame 2");
    }
    ```

    Imagine that you wanted ```FrameLogger()``` to return how many frames were logged. You would have to change ```FrameLogger()``` to this.

    ```csharp
    IEnumerator<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        yield return 1;
        UnityEngine.Debug.Log("Frame 2");
        yield return 2; // number of frames logged 
    }
    ```

    But then you still don't actually have the value in ```Start()```, so you have to change ```Start()``` to this.

    ```csharp
    IEnumerator Start() {
        var enumerator = FrameLogger();
        yield return enumerator;
        var numFramesLogged = enumerator.Current;
    }
    ```

    This works because the Current value at the end of the iteration will be the final value.

    The final big problem comes when you need another normal IEnumerator INSIDE of ```FrameLogger()```. Now it becomes really bad because IEnumerator<int> must always return an int value.

    So let's say you decide that you only want to log a frame every second. The first thing you try is using the ```WaitForSeconds``` class from Unity, but it fails. It will complain ```Cannot implicitly convert type 'UnityEngine.WaitForSeconds' to 'int' (CS0029)```.

    ```csharp
    IEnumerator<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        yield return new WaitForSeconds(1); // ERROR
        UnityEngine.Debug.Log("Frame 2");
        yield return 2; // number of frames logged 
    }
    ```

     So you search around and TRY to figure out how to convert a UnityEngine.WaitForSeconds to an IEnumerator. Eventually you realize you need another function that wraps the use of ```WaitForSeconds```. So you make the ```Wait``` function. BUT THAT STILL DOESN'T WORK. It fails for nearly the same reason ```Cannot implicitly convert type 'IEnumerator' to 'int' (CS0029)```.

    ```csharp
    IEnumerator Wait(float seconds) {
        yield return new WaitForSeconds(seconds);
    }

    IEnumerator<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        yield return new Wait(1); // ERROR
        UnityEngine.Debug.Log("Frame 2");
        yield return 2; // number of frames logged 
    }
    ```

    So what went wrong here. The problem is that functions that return ```IEnumerator<int>``` must ALWAYS yield return ```int``` values (unlike when returning ```IEnumerator``` where can return any type).

    Well, what is the solution? Hacks.

    We're going to loop through the ```IEnumerator``` returned from ```Wait()``` and forcefully convery it to an int. This will work AS LONG AS the function runs to completion.

    ```csharp
    IEnumerator<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        var enumerator = Wait(1);
        while (enumerator.MoveNext()) {
            yield return (int) enumerator.Current;
        }
        UnityEngine.Debug.Log("Frame 2");
        yield return 2; // number of frames logged 
    }
    ```

    Well what happens if someone uses a ```yield break``` early? Then the whole thing falls apart and you return some random value forcably casted to int.

    So let's look at it all put together.

    ```csharp
    IEnumerator Start() {
        var enumerator = FrameLogger();
        yield return enumerator;
        var numFramesLogged = enumerator.Current;
    }

    IEnumerator Wait(float seconds) {
        yield return new WaitForSeconds(seconds);
    }

    IEnumerator<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        var enumerator = Wait(1);
        while (enumerator.MoveNext()) {
            yield return (int) enumerator.Current;
        }
        UnityEngine.Debug.Log("Frame 2");
        yield return 2; // number of frames logged 
    }
    ```

    Just for a direct comparison, this is what it would look like with async/await. This is event pause aware in an experiment.

    ```csharp
    async void Start() {
        var numFramesLogged = await FrameLogger();
    }

    async Task<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        await MainManager.Instance.Delay(1000);
        // 'await manager.Delay(1000);' inside of an experiment method
        UnityEngine.Debug.Log("Frame 2");
        return 2; // number of frames logged   
    }
    ```

    Wonderful!

    Now, to be completely fair, there are other ways to do this.
    <details>
    <summary>Expand Other Ways</summary>

    First, use the one is to use ```Task.Delay()```. This may not work for you because ```Task.Delay()``` will hang any IL2CPP builds (like WebGL for websites). So here are three other versions of you can use.

    ```csharp
    async Task<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        await ;
        UnityEngine.Debug.Log("Frame 2");
        return 2; // number of frames logged   
    }
    ```

    Second, just write your own basic timing function.

    ```csharp
    async Task<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        var endTime = Clock.UtcNow.AddSeconds(1);
        while (Clock.UtcNow < endTime) { 
            await Awaitable.NextFrameAsync();
        }
        UnityEngine.Debug.Log("Frame 2");
        return 2; // number of frames logged   
    }
    ```

    Third, use [UniTask](https://github.com/Cysharp/UniTask). I have to give a shoutout to this amazing framework. Someday, this may be integrated directly into PsyForge.

    ```csharp
    async Task<int> FrameLogger() {
        UnityEngine.Debug.Log("Frame 1");
        await UniTask.Delay(1000);
        UnityEngine.Debug.Log("Frame 2");
        return 2; // number of frames logged   
    }
    ```

    </details>

    </details>

    </details>

1. You can catch and handle the exceptions of async functions.

    <details>
    <summary>Expand Explanation</summary>

    In the C# Language, you are not allowed to put a yield in a try block.

    ```csharp
    IEnumerator Start() {
        try {
            // THIS DOES NOT COMPILE: Cannot yield a value in the body of a try block with a catch clause (CS1626)
            yield return FrameLogger();
        } catch(Exception e) {
            UnityEngine.Debug.Log(e);
        }
    }

    IEnumerator FrameLogger() {
        // do something
    }
    ```

    However, you are absolutely allowed to put an await statement in a try block.

    ```csharp
    async void Start() {
        try {
            await FrameLogger();
        } catch { }
    }

    async Task FrameLogger() {
        // do something
    }
    ```

    Just to be absolutely clear through, it is 'technically' possible to catch exceptions from an IEnumerator. This framework even provides an extension method for doing just this.

    <details>
    <summary>Expand Code Example</summary>

    ```csharp
    IEnumerator Start() {
        yield return FrameLogger().TryCatch((Exception e) => { 
            // Handle exception
        });
    }

    IEnumerator FrameLogger() {
        // do something
    }

    public static class EnumeratorExtensions {
        public static IEnumerator TryCatch<T>(this IEnumerator enumerator, Action<T> onError) 
            where T : Exception
        {
            object current;
            while (true) {
                try {
                    if (enumerator.MoveNext() == false) {
                        break;
                    }
                    current = enumerator.Current;
                } catch (T e) {
                    onError(e);
                    yield break;
                }
                yield return current;
            }
        }
    }
    ```

    </details>

    </details>

1. It is easier to use other outside C# code (such as network code)since using IEnumerators in this fashion is specific to Unity.

### Why we chose async/await

If you look at the pros of IEnumerator, it isn't very strong. Sticking to something just because it was the way it was done is not very good. And the amount of optimization for IEnumerators usually will not play a role overall slowness.

Unity is also increasing compatibility with async/await more and more with every release. They realize how important this language feature is and the benefits that it brings.

Now, just because the main experiments are written with async/await as the lead, doesn't mean you can't use IEnumerators. IEnumerators are used all over PsyForge internally. In addition, many tools are provided for IEnumerators (ex: EnumeratorExtensions::TryCatch) and for the switching between IEnumerators and Tasks (TaskExtensions::ToEnumerator and EventMonoBehaviour::ToCoroutineTask)

## Common Unity Errors

These are general questions often asked.

1. ### You just imported PsyForge and there are a bunch of errors

    - Make sure you close the unity editor and re-open it. I'm not sure why this is needed, but it is.

1. ### MainManager accessed before Awake was called

    1. Click *Edit > Project Settings*.
    1. Go to *Script Execution Order*.
    1. Click the *+* to add a script and select PsyForge.MainManager.
    1. Set the value of this new item to *-10* (or anything less than 0).

1. ### You start the experiment, but all you see is the empty background (looks like the sky)

    - You need to add the following code to start your experiment class.

        ```csharp
        protected void Start() {
            Run();
        }
        ```

    - Or you need to make sure that there is a configs folder defined on your Desktop (when running from the Unity Editor). Just copy the [configs folder](https://github.com/BruskaTech/PsyForge/tree/main/configs) from the PsyForge repo to your Desktop and anywhere that the executable is located.

1. ### Microphone class is used but Microphone Usage Description is empty in Player Settings

    - You need to give your unity a microphone description.
        1. Click *Edit > Project Settings*.
        1. Go to Player and look for "Microphone Usage Description".
        1. Write anything in the text box.

1. ### You start the experiment and it hangs

    - Check that you don't have two experiments active in your scene.
