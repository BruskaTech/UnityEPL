# UnityEPL Overview
UnityEPL (Unity Experiment Programming Library) can help researchers and scientists easily collect data from their Unity3D projects.  To use UnityEPL, all you need to do is download the asset from the asset store, import it, and add components to objects in your Unity3D scene.

There are two types of components: reporters and handlers.  Reporters allow you to choose what data you care about in your Unity3D project.  Handlers let you decide what to do with the data.

To add either type of component, select a game object in the object heierarchy (the left panel in the default Unity3D layout).  Then, select "Add Component" in the inspector (the right panel in the default Unity3D layout.)

![alt text](https://github.com/pennmem/UnityEPL/blob/master/images/add_component.png "Adding a UnityEPL component")

In the "Add Component" dropdown, select "UnityEPL," then "Handler" or "Reporter."  You will then be able to configure the component.

## Reporters

![alt text](https://github.com/pennmem/UnityEPL/blob/master/images/reporters.png "UnityEPL reporters")

There are currently four reporters, each for collecting a different type of data about your project.

### WorldDataReporter

This reporter is for recording the position of Unity3D objects that exist inside the 2D or 3D simulation.  Put the component on the object whose world position you are concerned with.  Configure the data reporting parameters according to your preferences by editing the component in the inspector window and described in the full documentation below.

### InputReporter

This reporter is for directly recording key strokes and mouse clicks in your experiment.  UnityEPL uses MacOS native level hooks to report high-accuraccy data on the MacOS platform.  For other platforms, the accuraccy of the data is limited to the frame rate of the project.  For example, 60 frames per second corresponds to 16.6 ms accuraccy for key and mouse events.

## Handlers

# Full documentation


# Example projects