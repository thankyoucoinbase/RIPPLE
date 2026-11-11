# Scripts

All game code is in `Assets/Scripts/Assembly-CSharp/` (namespace `Shallows`, plus the Photon Voice demo scripts and
`DataStreamDemo`, which the game compiled into Assembly-CSharp).

## How it was rebuilt

1. MfuscatorToolkit recovered the IL2CPP metadata (it was not encrypted) and AssetRipper exported the project.
2. The toolkit lifted method bodies from the ARM64 binary and injected the 1,072 that compiled as-is.
3. The other 1,806 methods (partial lifts, compiler-generated iterators/lambdas/async state machines, reverted
   bodies) were rebuilt by hand in 16 batches, each checked against the lift and the disassembly. A cleanup pass
   rewrote every remaining auto-filled method so the code reads like hand-written source with identical behaviour.
4. Each rebuilt method keeps a `// Rebuilt from native code (token ..., RVA ...)` comment pointing at its native code.

Rules the rebuild followed: same calls, order, constants, strings and conditions as the binary; same fields, field
order and attributes; field initializers taken from the native constructors; `MathF` where the binary calls
`sinf`/`cosf`/`expf`; `volatile` where it puts memory barriers; `float.MaxValue` where the lifter wrote infinity.

## Checks

- An audit over all files found 0 stub methods, 0 native constructor fields left unassigned, and 0
  compiler-generated leftovers. The only methods without their own provenance comment are the Fusion members the
  weaver generates and lambdas folded back into their methods.
- 0 compile errors in Unity.
- Fixes made to methods the automatic pass had filled wrongly include a self-recursive `Dispose`, a dropped
  return value in `MonsterAI.Update`, wrong `RenderTexture` overloads, infinity in place of `float.MaxValue`, and a
  garbled condition in `RippleCameraRecorder.Stop`.
- The Voice demo scripts were later replaced with Photon's original sources, which matched the rebuilt
  behaviour.

## Known judgement calls

A few values cannot be read from the binary because the callee ignores them (for example the argument
`GameManager.Update` passes to `Escape`). These are marked `TODO(recon)` with the reason, and behaviour is the
same whatever is passed.
