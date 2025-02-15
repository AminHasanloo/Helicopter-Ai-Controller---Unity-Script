### 🚁 Helicopter Controller - Unity Script
## 🎮 Overview

This Unity C# script provides a comprehensive controller for helicopters with features like automatic takeoff, movement, and landing. It is optimized for realistic helicopter flight mechanics with detailed rotor behavior, state management, and waypoint-based navigation.
## ✨ Features

    State Management: Manages multiple states: Grounded, Taking Off, Hovering, Moving, and Landing.
    Waypoint Navigation: Supports random or sequential waypoint movement.
    Realistic Rotor Simulation: Rotors adjust speed based on the helicopter’s state.
    Height Limitation: Ensures helicopters stay below a maximum altitude.
    Sound Effects: Integrates engine and blade sounds for enhanced realism.
    Event Triggers: Unity Events for takeoff, hovering, landing, and moving.

## 🚀 Getting Started

    Add the Script: Attach the HelicopterController to your helicopter GameObject.
    Set Waypoints: Define waypoints for navigation.
    Adjust Settings: Customize movement, sound, and rotor properties to your preference.

## 🎛️ Configuration Options
Movement Settings

    Hover Force: 150f - Controls how much upward force is applied during hovering.
    Forward Speed: 50f - Defines the helicopter’s max forward movement speed.
    Turn Speed: 2f - Determines how fast the helicopter rotates.
    Max Height: 80f - Maximum altitude the helicopter can reach.

Waypoint Settings

    Waypoints: Array of Transform objects for navigation.
    Random Movement: Enable random waypoint selection.
    Waypoint Radius: Tolerance radius for waypoint arrival.

Rotor Settings

    Main Rotor Speed: Controls the speed of the main rotor rotation.
    Tail Rotor Speed: Adjusts the tail rotor speed for stability.# Helicopter-Ai-Controller---Unity-Script
This Unity C# script provides a comprehensive controller for helicopters with features like automatic takeoff, movement, and landing. It is optimized for realistic helicopter flight mechanics with detailed rotor behavior, state management, and waypoint-based navigation.
