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
