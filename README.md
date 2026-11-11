# 🌴 Jungle Cafe
> A restaurant management game set in a zoo taken over by rebellious animals.

**Authors:** Christina Castillo, Thais Shin, Ryusean Robinson, Phat Tran

**CPSC 362-11.**

## Table of Contents
1) Project Description
2) Tech Stack
3) Features
4) Directory structure
5) Demo Videos

## Project Description
Jungle Cafe is a restaurant management game where players run a food stand in a zoo that has been taken over by rebellious animals. Players must prepare and serve food to hungry animals while managing orders and avoiding being eaten by dissatisfied customers. The game combines cooking, time management, and survival mechanics with colorful pixel-art visuals. It is designed for casual gamers who enjoy cooking simulators and challenging gameplay.

## Tech Stack
The language used in this program is C#, the framework was clang tidy as a linter. There was a lot of dependencies due to working with unity, what follows is a list of unity dependencies and why they are there.
Unity's input system was used to get the players input including clicks and keyboard touches. Render pipelines universal was used to render graphics. Visual scripting to supplement code with node scripting. Ugui was used 
for user interfaces. Visual studio for the visual studio integration. Those are the main ones, but there are supplemental dependencies that full under testing, customization, and lighting.

## Features
- Hands-on gameplay for cooking, serving, and taking orders
- Custom scene and customers
- View Score Details to learn from your mistakes
- Timeline to see how much time you have left
- Customer Preferences

## Directory Structure
```
├── Assets
│   ├── Audio                     # Sound effects and music
│   ├── Data                      # Game Data
│   │   ├── Customers
│   │   ├── Foods
│   │   └── Recipes
│   ├── Images                    # 2D images and UI art
│   ├── Materials                 # Materials for each 3D prop
│   │   ├── Banana
│   │   ├── Fridge
│   │   ├── Grill
│   │   ├── RawSteak
│   │   ├── Register
│   │   ├── ServiceBell
│   │   └── Steak
│   ├── Models                    # 3D models
│   ├── Prefabs                   # Reusable game objects
│   │   └── Testing_Prefabs 
│   ├── Scenes                    # Game scenes
│   │   └── Testing
│   ├── ScriptableObjects         # Customer assets
│   │   ├── Customers
│   │   └── Testing_Customers
│   ├── Scripts                   # Game logic (scripts in C#)
│   │   ├── Cooking
│   │   ├── Customer
│   │   ├── Editor
│   │   ├── Orders
│   │   ├── Save
│   │   ├── Stations
│   │   └── UI
│   ├── Settings                  # Unity project settings
│   ├── Textures
│   └── Videos
├── Library
```
