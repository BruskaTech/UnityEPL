# Why Lambdas Are Bad in DoTS

They initally seem okay.

```csharp
    class SafeDict : EventLoop {
        Dictionary<string, object> dict;

        void Reset() {
            DoTS(() => {
                dict = new();
            })
        }
    }
```

When ```Reset()``` is called from another EventLoop, then it will change the value. This is NOT a race condition because DoTS only executes one thing at a time.

What if we need to pass something to the method though?

```csharp
    class SafeDict : EventLoop {
        Dictionary<string, object> dict;

        void SetVal(Dictionary<string, object> value) {
            DoTS(() => {
                dict = value;
            })
        }
    }
```

Well now we have a problem. When we pass the value into ```SetVal()```, it will actually be by reference. This CAN cause a race condition (and worse).

Let's consider this code.

```csharp

    class Test {
        Dictionary<string, object> originalDict;

        void MyTest() {
            SafeDict sd = new();

            originalDict = new();
            sd.SetVal(originalDict);
            originalDict["key"] = "value";
        }
    }

```

So ```Test``` is executing on the original thread. When ```sd``` is created, it starts a new thread for its EventLoop. Then originalDict is set to by empty. We vall ```SetVal()``` to pass that empty dictionary to the other thread and store it in the ```SafeDict```. Finally, we add an element to originalDict. So what does the dict look like inside ```SafeDict```?

Unfortunately, since Dictionaries set by reference, it is not empty. It instead is the same as originalDict. As a matter of fact, it points to the same memory as originalDict. Both threads are now changing the same value in a way that causes race conditions.

This is why you should NOT use lambdas in any function that takes an argument from another thread. You also have to consider cases where a value from a different thread is passed into many layers of functions until it gets to the lambda inside the ```DoTS()``` call. So if that sounds complicated, just don't use lambdas in the ```DoTS``` functions.
