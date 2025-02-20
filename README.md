# StableFluid dimulation with VFXGraph

The objective of this practical work is to make some fluid simulation using the VFXGraph of Unity. You can press play mode et move around to see the fluid simulation. There are spheres in the scene that affect the fluid. 

## Table of content
1. [Prerequisites](#Prerequisites)
2. [Features](#Features)
3. [Explanation](#Explanation)
4. [Difficulty](#Difficulty)

## Prerequisites

- The project was made with Unity 6000.0.36f1  

## Features

- Done:
    - Use Keijiro Takahashi algorithm for fluid simulation and apply a texture to a 3D plane.  
    - Create a system of step to simulate an infinite walk simulation. (Plane attach to the player)  
    - Generate a grid of particle place randomly.  
    - Move particle and change color based on velocity texture. Generate with Keijiro algorithm.
    - Make several additional collisions which have an impact on the velocity.
    - Infinite mode with particles

## Explanation
- **Fluid.cs:** 
    - This script is a GPU-based fluid simulation. It leverages Compute Shaders to handle advection, diffusion, external force application, and velocity projection, ...

    - Key Features:  
        - Real-time stable fluid simulation with adjustable resolution.
        - External force application based on player and target positions.
        - Double buffering for smooth rendering.
        - Texture management to represent velocity fields and fluid dynamics.
        - GPU optimization using Compute Shaders and thread dispatching.
        - This file is based on the StableFluids project.  

- **Fluid.compute:** 
    -  This compute shader implements Stable Fluids algorithm for real-time fluid simulation. It performs key operations on the GPU, ensuring high efficiency and smooth fluid motion.

    - Key Features
        - Advection: Moves fluid based on velocity fields.  
        - External Force Application: Adds forces to the fluid based on an external source.  
        - Projection Step: Ensures a divergence-free velocity field using the Jacobi iterative method.  
        - Jacobi Solver: Used for pressure field calculation and diffusion.    
        - Threaded Execution: Uses an 8x8 thread grid for optimized parallel computation.  
        - This file is based on the StableFluids project.

Jarod Sengkeo and Maxence Retier