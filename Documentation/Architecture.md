# Architecture

This is the architecture document for the UnityEPL.

It also provides the best coding practices when using UnityEPL.

There are certain coding practices that should always be applied when coding in the UnityEPL.
Some are recommendations and some are requirements (code will break if you do not follow them).
We try to make as many errors occur at compile time, but there are always limitations of the language.

## Overview

There are two main components to UnityEPL: the Unity side and the Threading side.

The Unity side is what you will use for almost everything you want to build. It builds convenient packages on top of Unity for developing experiments. Some examples of this are a text displayer, error handler, config system, randomization that is consistent within participant, rating systems, questioneers, and more. You can also add any Unity asset from the store that you want to use and build new features. You probably don't need much of this architecture document if you are just using the Unity side.

Then there is the Threading side. Threading allows multiple things to happen at the same time on the computer. However, threading is inherently [VERY tricky](https://medium.com/swlh/common-multithreading-mistakes-e36ca8e98e7a). To address this (especially for people who probably don't want to spend there days learning the intricacies of threading and just want to build their experiment), this repo uses modern techniques to try and mitigate potential issues. This allows for thread-safe logging, error handling, and network interfaces across threads (including the main unity thread). It also provides other advantages such as much more accurate timing of external sources (ex: syncbox/eye-tracker) than Unity can normally provide and better CPU usage for large procesing jobs.

TL;DR Use the Threading side for safe and easy threading, to interact with external sources, and/or to use other CPUs to avoid stalling your Unity game. Use the Unity side for everything else.

## Important Code Structures

These are the important structures within the code.

### MainManager

The MainManager has two main jobs:

1. Hold the objects for all EventLoops
1. Allow other event loops to interact with Unity objects/functions

This is a bit monolithic, but it allows the objects to be held in one consistent object and allows for a clean interface with Unity.
This also means the MainManager is running on the main Unity thread.

### Events/Tasks

Events are async functions that run on an EventLoop or EventMonoBehavior thread.

The *EventLoop* is the default base class for classes that want to run events.
They set up a thread that, when any Do is called, runs the specified function on that thread.

Each thread only runs one event at a time in the order that they are initiated.
In other words, it time-multiplexes.
This is important because it guarantees events will not have any race conditions on accessed variables in the class.

#### Main Event Types

There are 3 main types of events:

1. *Do*
2. *DoWaitFor*
3. *DoGet*

A *Do* event creates an async function running on the receiving class' EventLoop and immediately continues in the current context.
A *DoWaitFor* event creates an async function running on the receiving class' EventLoop and then you can perform an async await for it to be done in the current context.
A *DoGet* event is just like DoWaitFor, but it also returns a value.

#### Special Event Types

There are 2 special types of events:

1. *DoIn*
2. *DoRepeating* (not implemented yet)

A *DoIn* event is just like Do, except that it delays for a supplied number of milliseconds before running the function.
A *DoRepeating* event allows you to start an event that can repeat itself on an interval for a set (or infinite) number of iterations.

You will notice that these are not unique event types, but rather convenience functions based on the 3 main event types.

#### EventLoop vs EventMonoBehavior

When the purpose of your class is to control Unity objects (e.g., VideoManager, InputManager, TextDisplayer), then you would normally inherit from *MonoBehaviour*. Unfortunately, you can't just inherit from *EventLoop* instead because all events are run on another thread, which would mean they can't interface with the Unity system. In order to resolve this conflict, you instead inherit from *EventMonoBehaviour*.

*EventMonoBehavior* is a special class that acts like both an *EventLoop* and a *MonoBehavior*.
There are two big differences:

1. Unlike *EventLoop*, *EventMonoBehaviour* does not create a new thread. It instead puts all events onto the main Unity thread using Coroutines. This is why all events in an *EventMonoBehavior* must return an *IEnumerator* instead of a *Task*.
2. Unlike *MonoBehavior*, the *Start* function should not be created. Instead, it forces you to override the *StartOverride* function. The *StartOverride* function does the exact same thing as the *Start* function in a normal *MonoBehaviour*. This is so that the *Start* function defined in can set up the *EventMonoBehaviour*. If you REALLY need to override the *Start* function for some reason, just remember to call the *base.Start()* in your overridden *Start* function.

#### Coding Practices

Here are some coding practices that should be followed when writing event code:

1. Always use EventLoops unless the class has to be a MonoBehaviour, then use an EventMonoBehaviour.
2. All (non-static) public methods should call a *Do* on a Helper method (that actually does the work) in order to guarantee thread safety.
3. All member variables should be private or protected. If you need to access these variables from outside, then create a getter that calls *DoGet*. This is again for thread safety.
4. You should probably never use static member variables. If you do, you will definitely have to use some sort of thread safety mechanism (such as a lock). I'd avoid this at all costs.

#### Thread Safety

These tasks only allow up to 4 blittable types to be passed into them. Blittable types are stack-based types that are contiguous in memory. More importantly, they can't contain references.
This is important because if you access the value of a reference across threads, it will cause race conditions.
That is, unless it is a thread-safe concurrent datatype. If you know this is the case, and you are REALLY sure you know what you're doing, and there is no other way to architect it, then you can use a lambda to grab that value as a reference and pass the lambda into your Event.

#### Notes

Some small things that are good to at least read once:

- If you need to pass more than 4 arguments, make a struct and pass that in.
- Prefer using *Do* over *DoWaitFor* and *DoGet*.

## Important Coding Practices

These are the important practices that are critical for all coders to understand and follow.

- Do NOT use *Task.Delay()*. Instead, use *Timing.Delay()*. They act exactly the same, but Timing.Delay knows how to handle the single-threaded nature of WebGL.

## Acronyms and Terms

Below are the common acronyms and terms used in this project.

### Acronyms

- EEG = Electroencephalogram

### Terms

- Elemem = CML EEG reading and stimulation control system
