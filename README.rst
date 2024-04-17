#############
UnityEPL 3.0
#############

A library for easy creation of 2D and 3D psychology experiments.

.. contents:: **Table of Contents**
    :depth: 2

*************
Overview
*************
The experimental designs required to explore and understand neural mechanisms are becoming increasingly complex. There exists a multitude of experimental programming libraries for both 2D and 3D games; however, none provide a simple and effective way to handle high-frequency inputs in real-time (i.e., a closed-loop system). This is because the fastest these games can consistently react is once a frame (usually 30Hz or 60Hz). We introduce UnityEPL 3.0, a framework for creating 2D and 3D experiments that handle high-frequency real-time data. It uses a safe threading paradigm to handle high precision inputs and outputs while still providing all of the power, community assets, and vast documentation of the Unity game engine. UnityEPL 3.0 supports most platforms such as Windows, Mac, Linux, iOS, Android, VR, and Web (with convenient psiTurk integration). Similar to other experimental programming libraries, it also includes common experimental components such as logging, configuration, text display, audio recording, language switching, and an EEG alignment system. UnityEPL 3.0 allows experimenters to quickly and easily create high quality, high throughput, cross-platform games that can handle high-frequency closed-loop systems.

For more information than what is in this document, please see the `<Documentation>`_ folder

*************
Making an Experiment
*************
It's really easy to start making a basic experiment.

=============
Basic Instructions
=============

#. git submodule add git@github.com:pennmem/UnityEPL.git Assets/
#. Add asmref for UnityEPL in Scripts
#. Inherit ExperimentBase on your main experiment class
#. Implement the abstract methods PreTrials, TrialStates, and PostTrials

=============
Adding Config variables
=============

#. Add asmref for UnityEPL in Scripts
#. Create a partial class named Config
#. Implement each item in your config, so that it looks like this

.. code:: csharp

    public static bool elememOn { get { return Config.GetSetting<bool>("elememOn"); } }


=============
Types of Experiments and Components Available
=============
There are many types of experiments, but here are a few common ones and the useful components for them.
There is also a list of generally useful componenets

-------------
Word List Experiments
-------------
TextDsplayer
SoundRecorder
VideoPlayer

-------------
Spacial Experiments
-------------
SpawnItems
PickupItems

-------------
Closed-Loop Experiments
-------------
EventLoop
ElememInterface

-------------
General Components
-------------
Config
Logging
ErrorNotifier
NetworkInterface
InputManager
List/Array shuffling (including ones that are consistent per participant)
Random values that are consistent per participant


*************
FAQ
*************
See the FAQ Document


*************
Authors
*************
James Bruska, Connor Keane, Ryan Colyer
