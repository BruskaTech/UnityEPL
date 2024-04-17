#############
FAQ
#############
This is the Frequently Asked Questions document for the UnityEPL

.. contents:: **Table of Contents**
    :depth: 2

*************
General
*************
These are general questions often asked 

=============
What does the name stand for?
=============
UnityEPL stands for Unity Experiment Programming Library


*************
Common Unity Errors
*************
These are general questions often asked 

=============
You just imported UnityEPL and there are a bunch of errors
=============
Make sure you close the unity editor and re-open it.
I'm not sure why this is needed, but it is.

=============
InterfaceManager accessed before Awake was called
=============
#. Click *Edit > Project Settings*
#. Go to *Script Execution Order*
#. Click the *+* to add a script and select UnityEPL.InterfaceManager
#. Set the value of this new item to *-10* (or anything less than 0)

=============
You start the experiment, but all you see is the empty background (looks like the sky)
=============
You need to add the following code to start your experiment class.

.. code:: csharp

    protected void Start() {
        Run();
    }

Or you need to make sure that there is a configs folder defined on your Desktop (when running from the Unity Editor).
Just copy the configs folder from the UnityEPL repo to your Desktop and anywhere that the executable is located.

=============
Microphone class is used but Microphone Usage Description is empty in Player Settings.
=============
You need to give your unity a microphone description
#. Click *Edit > Project Settings*
#. Go to Player and look for "Microphone Usage Description"
#. Write anything in text box

=============
You start the experiment and it hangs
=============
Check that you don't have two experiments active in your scene

