# Shaders

The game's 17 shaders were rebuilt from the shipped Vulkan (SPIR-V) programs. Their sources are `Assets/Shader/*.shader`
and `Assets/Resources/**/*.shader`.

## How they were checked

1. **Text comparison.** A separate Unity 6000.3.11 project compiled every shipped keyword set of each shader for
   Vulkan, with pre-transform on as shipped. The SPIR-V was decoded with the same tools as the shipped programs and
   the normalised GLSL diffed.
2. **GPU comparison.** The shipped fragment program and ours ran on the GPU with identical inputs: per-pixel
   varyings, uniforms and textures. Outputs were compared pixel by pixel, and a test that changed one constant
   failed on 18.6% of pixels.

The text diff also flags register allocation and select-versus-branch choices that differ between Unity 6000.3.11
and the game's 6000.3.24 compiler. The GPU comparison decides whether the math is the same.

| Shader | Programs | GPU result |
|---|---|---|
| Shallows/Halo, Sky, Fuse Glow, World Text; Hidden/Shallows/Foam Field, Ripple Camera YUV | 2 each | identical text |
| Shallows/Backdrop | 4 | identical text |
| Shallows/Text, Mirror, Water, Beam, Blood, Glow, Particle | 4 each | same output |
| Shallows/Prop | 6 | same output |
| Shallows/TileTriplanar | 8 | same output |
| Hidden/Shallows/Ripple Camera Desktop | 1 | same output (fragment); vertex matched instruction by instruction |

Total: 59 of 59 shipped programs.

## URP

77 materials use URP's own `Lit`. A sample of 88 shipped Lit programs matches the URP 17.3.0 bundled with Unity
6000.3.11, once the project uses DXT5nm normal maps as shipped (the shipped code calls `UnpackNormalAG`).

## Textures

Texture import settings have `alphaIsTransparency` off. With it on, Unity rewrote the colour of fully transparent
texels, which wiped the player's face, eyes and nose (stored under alpha 0, where the alpha is a fur-tint mask).
