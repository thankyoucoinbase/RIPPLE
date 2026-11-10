using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RippleTools
{
    /// Editor-only diagnostic (not part of the shipped game): renders the player model textured the way the game does
    /// it (Shallows.ApeBody.FurTexture with a fur colour, URP Lit), looking at the head from both sides.
    /// Writes Recovery/playtest/avatar/face_*.png. Batchmode: -executeMethod RippleTools.AvatarProbe.Run
    public static class AvatarProbe
    {
        public static void Run()
        {
            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Recovery", "playtest", "avatar"));
            Directory.CreateDirectory(outDir);
            var go = Object.Instantiate(Resources.Load<GameObject>("Ape/ShallowsPlayer"));
            var skin = go.GetComponentInChildren<SkinnedMeshRenderer>();
            // RippleTools.Editor can't reference Assembly-CSharp, so find the game type by name.
            var apeBody = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Shallows.ApeBody")).First(t => t != null);
            var furTexture = apeBody.GetMethod("FurTexture", BindingFlags.NonPublic | BindingFlags.Static);
            var head = skin.bones.First(b => b.name == "Head");
            var camGo = new GameObject("probe cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.35f, 0.4f);
            cam.fieldOfView = 40f;
            var light = new GameObject("probe light").AddComponent<Light>();
            light.type = LightType.Directional;
            RenderSettings.ambientLight = Color.gray;
            foreach (var (label, fur) in new[] { ("teal", new Color(0.13333334f, 0.43137255f, 0.41960785f)), ("blue", new Color(0.3f, 0.45f, 0.6f)) })
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var tex = (Texture2D)furTexture.Invoke(null, new object[] { fur });
                mat.SetTexture("_BaseMap", tex);
                mat.SetFloat("_Smoothness", 0.1f);
                skin.sharedMaterial = mat;
                foreach (var side in new[] { 1f, -1f })
                {
                    var dir = go.transform.forward * side;
                    camGo.transform.position = head.position + dir * 1.1f + Vector3.up * 0.05f;
                    camGo.transform.LookAt(head.position);
                    light.transform.rotation = Quaternion.LookRotation(-dir + Vector3.down * 0.5f);
                    var rt = new RenderTexture(512, 512, 24);
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    var shot = new Texture2D(512, 512, TextureFormat.RGB24, false);
                    shot.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                    shot.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(outDir, $"face_{label}_{(side > 0 ? "a" : "b")}.png"), shot.EncodeToPNG());
                }
            }
        }
    }
}
