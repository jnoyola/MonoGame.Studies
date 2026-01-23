# MonoGame.Studies

A series of studies in MonoGame. Building up game techniques and tools one step at a time.

## Study 1: Animated 3D Character

| Stage | Branch | Diff to Previous |
| ----- | ------ | ---------------- |
| **Stage 1.1** | [study-1.1](https://github.com/jnoyola/MonoGame.Studies/tree/study-1.1) | - |
| **Stage 1.2** | [study-1.2](https://github.com/jnoyola/MonoGame.Studies/tree/study-1.2) | [study-1.1 -> study-1.2](https://github.com/jnoyola/MonoGame.Studies/compare/study-1.1...study-1.2) |
| **Stage 1.3** | [study-1.3](https://github.com/jnoyola/MonoGame.Studies/tree/study-1.3) | [study-1.2 -> study-1.3](https://github.com/jnoyola/MonoGame.Studies/compare/study-1.2...study-1.3) |
| **Stage 1.4** | [study-1.4](https://github.com/jnoyola/MonoGame.Studies/tree/study-1.4) | [study-1.3 -> study-1.4](https://github.com/jnoyola/MonoGame.Studies/compare/study-1.3...study-1.4) |

### Stage 1.1: Skinned Low-Poly Character

We start with rendering a skinned 3D character with simple skeletal animations.
In MonoGame this can be done using the built-in `SkinnedEffect` for textured models.
However, we are going to use a low-poly, flat-shaded character which looks best with vertex coloring.
The built-in `BasicEffect` can handle vertex coloring, but does not handle skeletal animations.
Thus we will need a new custom effect to render our animated vertex-colored character.

This study includes the following:
1. Processing and importing a skinned vertex-colored GLTF model
2. Processing and importing GLTF animations
3. Applying animations to compute bone transforms
4. Rendering the character with vertex colors and bone transforms

### Stage 1.2: Blended Animations

This stage adds a lot of complexity to the animation definitions, animation player, and state.
We are no longer limited to playing a single animation on the character's entire skeleton, but can now
blend animations across different parts of the skeleton. Layers are designed to be defined for a game's
specific needs, but are demonstrated here with 2 override layers (base and upper body) and one additive
layer (which can stack multiple fine adjustments).

This study includes the following:
1. Content structures for animation metadata, layer definitions, and bone masks
2. Sampling and blending multiple override and additive layers
3. Five new animations to demonstrate the various types of blending

### Stage 1.3: Performance

This stage dives into performance optimizations that reduce pose calculation time and crank up frames per
second. Depending on hardware, these optimizations could provide ~20% increase in performance.

This study includes the following:
1. A testbed for performance, animating 1000 individual characters with layered animations
2. Pruning animation channels that are not animated

## Future Studies

Control Schemes, UI, Special Effects
