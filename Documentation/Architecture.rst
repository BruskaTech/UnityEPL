#############
Coding Practices
#############
This is the coding practices document for the UnityEPL

.. contents:: **Table of Contents**
    :depth: 2

*************
Overview
*************
There are certain coding practices that should always be applies when coding in the UnityEPL.
Some are recommendations and some are requirements (code will break if you do not follow it).
We try to make as many errors occur at compile time, but there are always limitations of the language.

*************
Important Code Structures
*************
These are the important structures within the code.

=============
MainManager
=============
The MainManager has two main jobs

#. Hold the objects for all EventLoops
#. Allow other event loops to interact with unity objects/functions

This is a bit monolithic, but is allows the objects to be held in one consistent object and allows for a clean interface with Unity.
This also means the MainManager is running on the main unity thread. 

=============
Events/Tasks
=============
Events are async functions that run on an EventLoop or EventMonoBehavior thread.

The *EventLoop* is the default base class for classes that want to run events.
They set up a thread that, when any Do is called, runs the specified function on that thread.

Each thread only runs one event at a time in the order that they are initiated. 
In other words, it time-multiplexes.
This is important because it guarentees events will not have any race conditions on accessed variables in the class.

-------------
Main Event Types
-------------
There are 3 main types of events:

#. *Do*
#. *DoWaitFor*
#. *DoGet*

A *Do* event creates an async function running on the receiving class' EventLoop and immediately continues in the current context. 
A *DoWaitFor* event creates an async function running on the receiving class' EventLoop and then you can perform an async await for it to be done in the current context.
A *DoGet* event is just like DoWaitFor, but it also returns a value

-------------
Special Event Types
-------------
There are 2 special types of events:

#. *DoIn*
#. *DoRepeating* (not implemented yet)

A *DoIn* event is just like Do, except that it delays for a supplied number of milliseconds before running the function
A *DoRepeating* event allows you to start an event that can repeat itself on an interval for a set (or infinite) number of iterations.

You will notice that these are not unique event types, but rather convenience functions based on the 3 main event types.

-------------
EventLoop vs EventMonoBehavior
-------------
When the purpose of your class is to control unity objects (ex: VideoManager, InputManager, TextDisplayer), then you would normally inherit from *MonoBehaviour*. Unfortunately, you can't just inherit from *EventLoop* instead because all events are run on another thread, which would mean they can't interface with the unity system. In order to resolve this conflict, you instead inherit from *EventMonoBehaviour*.

*EventMonoBehavior* is a special class that acts like both an *EventLoop* and a *MonoBehavior*.
There are two big diffrences:

#. Unlike *EventLoop*, *EventMonoBehaviour* does not create a new thread. It instead puts all events onto the main unity thread using Coroutines. This is why all events in an *EventMonoBehavior* must return an *IEnumerator* instead of a *Task*.
#. Unlike *MonoBehavior*, the *Start* function should not be created. Instead, it forces you to override the *StartOverride* function. The *StartOverride* function does the exact same thing as the *Start* function in a normal *MonoBehaviour*. This is so that the *Start* function defined in can setup the *EventMonoBehaviour*. If you REALLY need to override the *Start* function for some reason, just remember to call the *base.Start()* in your overriden *Start* function.

-------------
Coding Practices
-------------
Here are some coding practices that should be followed when writing event code

#. Always use EventLoops unless the class has to be a MonoBehaviour, then use an EventMonoBehaviour.
#. All (non-static) public methods should call a *Do* on a Helper method (that actually does the work) in order to guarentee thread safety.
#. All member variables should be private or protected. If you need to access these variables from outside, then create a getter that calls *DoGet*. This is again for thread safety.
#. You should probably never use static member variables. If you do, you will definitely have to use some sort of thread safety mechanism (such as a lock). I'd avoid this at all costs. 

-------------
Thread Safety
-------------

These tasks only allow up to 4 blittable types to be passed into them. Blittable types are stack based types that contiguous in memory. More importantly, they can't contain references.
This is important because if you access the value of a reference across thread, it will cause race conditions. 
That is, unless it is a thread safe concurrent datatype. If you know this is the case, and you are REALLY sure you know what you're doing, and there is no other way to architect it, then you can use a lambda to grab that value as a reference and pass the lambda into your Event.

-------------
Notes
-------------
Some small things that are good to at least read once. 

* If you need to pass more than 4 arguments, make a struct and pass that in.
* Prefer using *Do* over *DoWaitFor* and *DoGet*

*************
Important Coding Practices
*************
These are the important practices that are critical for all coders to understand and follow.
- Do NOT use *Task.Delay()*. Instead, use *Timing.Delay()*. They act exactly the same, but Timing.Delay knows how to handle the single threaded nature of webgl

*************
Acronyms and Terms
*************
Below are the common acronyms and terms used in this project

=============
Acronyms
=============
* EEG = Electroencephalogram

=============
Terms
=============
* Elemem = CML EEG reading and stimulation control system
