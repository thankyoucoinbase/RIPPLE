using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	public static class Sfx
	{
		private static readonly Dictionary<string, AudioClip[]> loaded = new Dictionary<string, AudioClip[]>();

		public static AudioClip SplashSmall(int take)
		{
			// Rebuilt from native code (token 0x0600032E, RVA 0x2793F40)
			return Get("SplashSmall", take, () => ProceduralAudio.Splash(take));
		}

		public static AudioClip SplashBig(int take)
		{
			// Rebuilt from native code (token 0x0600032F, RVA 0x2794108)
			return Get("SplashBig", take, () => ProceduralAudio.Splash(take));
		}

		public static AudioClip Splash(float intensity, int take)
		{
			// Rebuilt from native code (token 0x06000330, RVA 0x2794200)
			return intensity < 0.45f ? SplashSmall(take) : SplashBig(take);
		}

		public static AudioClip MonsterRoar()
		{
			// Rebuilt from native code (token 0x06000331, RVA 0x279428C)
			return Get("MonsterRoar", -1, ProceduralAudio.Roar);
		}

		public static AudioClip JumpScare()
		{
			// Rebuilt from native code (token 0x06000332, RVA 0x2794344)
			return Get("JumpScare", -1, MonsterRoar);
		}

		public static AudioClip MonsterStep()
		{
			// Rebuilt from native code (token 0x06000333, RVA 0x27943FC)
			return Get("MonsterStep", -1, ProceduralAudio.Footstep);
		}

		public static AudioClip MonsterGrowl()
		{
			// Rebuilt from native code (token 0x06000334, RVA 0x27944B4)
			return Get("MonsterGrowl", -1, MonsterRoar);
		}

		public static AudioClip MonsterBreath()
		{
			// Rebuilt from native code (token 0x06000335, RVA 0x279456C)
			return Get("MonsterBreath", -1, () => null);
		}

		public static AudioClip Ambience()
		{
			// Rebuilt from native code (token 0x06000336, RVA 0x2794688)
			return Get("Ambience", -1, () => null);
		}

		public static AudioClip WadingLoop()
		{
			// Rebuilt from native code (token 0x06000337, RVA 0x27947A4)
			return Get("WadingLoop", -1, () => null);
		}

		public static AudioClip DeathSlam()
		{
			// Rebuilt from native code (token 0x06000338, RVA 0x27948C0)
			return Get("DeathSlam", -1, ProceduralAudio.Smash);
		}

		public static AudioClip BodySlam()
		{
			// Rebuilt from native code (token 0x06000339, RVA 0x2794978)
			return Get("BodySlam", -1, ProceduralAudio.Smash);
		}

		public static AudioClip Chomp()
		{
			// Rebuilt from native code (token 0x0600033A, RVA 0x2794A30)
			return Get("Chomp", -1, ProceduralAudio.Smash);
		}

		public static AudioClip BloodSplat()
		{
			// Rebuilt from native code (token 0x0600033B, RVA 0x2794AE8)
			return Get("BloodSplat", -1, () => SplashSmall(0));
		}

		public static AudioClip HandSmash()
		{
			// Rebuilt from native code (token 0x0600033C, RVA 0x2794C04)
			return Get("HandSmash", -1, BodySlam);
		}

		public static AudioClip Gulp()
		{
			// Rebuilt from native code (token 0x0600033D, RVA 0x2794CBC)
			return Get("Gulp", -1, ProceduralAudio.Smash);
		}

		public static AudioClip HandTap()
		{
			// Rebuilt from native code (token 0x0600033E, RVA 0x2794D74)
			return Get("HandTap", -1, ProceduralAudio.Tap);
		}

		public static AudioClip HandTapWet()
		{
			// Rebuilt from native code (token 0x0600033F, RVA 0x2794E2C)
			return Get("HandTapWet", -1, HandTap);
		}

		public static AudioClip ButtonPress()
		{
			// Rebuilt from native code (token 0x06000340, RVA 0x2794EE4)
			return Get("ButtonPress", -1, ProceduralAudio.Tick);
		}

		public static AudioClip Checkpoint()
		{
			// Rebuilt from native code (token 0x06000341, RVA 0x2794F9C)
			return Get("Checkpoint", -1, ProceduralAudio.Chime);
		}

		public static AudioClip Escape()
		{
			// Rebuilt from native code (token 0x06000342, RVA 0x2795054)
			return Get("Escape", -1, ProceduralAudio.Chime);
		}

		public static AudioClip BananaCollect()
		{
			// Rebuilt from native code (token 0x06000343, RVA 0x279510C)
			return Get("BananaCollect", -1, ProceduralAudio.Tick);
		}

		public static AudioClip BranchCreak()
		{
			// Rebuilt from native code (token 0x06000344, RVA 0x27951C4)
			return Get("BranchCreak", -1, ProceduralAudio.Creak);
		}

		public static AudioClip BranchSnap()
		{
			// Rebuilt from native code (token 0x06000345, RVA 0x279527C)
			return Get("BranchSnap", -1, ProceduralAudio.Snap);
		}

		public static AudioClip PlatformShake()
		{
			// Rebuilt from native code (token 0x06000346, RVA 0x2795334)
			return Get("PlatformShake", -1, ProceduralAudio.Tick);
		}

		public static AudioClip PlatformSink()
		{
			// Rebuilt from native code (token 0x06000347, RVA 0x27953EC)
			return Get("PlatformSink", -1, ProceduralAudio.Footstep);
		}

		public static AudioClip MemoryTileLight()
		{
			// Rebuilt from native code (token 0x06000348, RVA 0x27954A4)
			return Get("MemoryTileLight", -1, ProceduralAudio.TileLight);
		}

		public static AudioClip FuseSlot()
		{
			// Rebuilt from native code (token 0x06000349, RVA 0x279555C)
			return Get("FuseSlot", -1, ProceduralAudio.Clunk);
		}

		public static AudioClip FusesBlow()
		{
			// Rebuilt from native code (token 0x0600034A, RVA 0x2795614)
			return Get("FusesBlow", -1, ProceduralAudio.Zap);
		}

		public static AudioClip FuseBuzz()
		{
			// Rebuilt from native code (token 0x0600034B, RVA 0x27956CC)
			return Get("FuseBuzz", -1, () => null);
		}

		public static AudioClip DoorAlarm()
		{
			// Rebuilt from native code (token 0x0600034C, RVA 0x27957E8)
			return Get("DoorAlarm", -1, ProceduralAudio.Alarm);
		}

		public static AudioClip DoorSlam()
		{
			// Rebuilt from native code (token 0x0600034D, RVA 0x27958A0)
			return Get("DoorSlam", -1, ProceduralAudio.Smash);
		}

		public static AudioClip ShopBuy()
		{
			// Rebuilt from native code (token 0x0600034E, RVA 0x2795958)
			return Get("ShopBuy", -1, ProceduralAudio.Chime);
		}

		public static AudioClip ShopDenied()
		{
			// Rebuilt from native code (token 0x0600034F, RVA 0x2795A10)
			return Get("ShopDenied", -1, ProceduralAudio.Clunk);
		}

		public static AudioClip BeltStash()
		{
			// Rebuilt from native code (token 0x06000350, RVA 0x2795AC8)
			return Get("BeltStash", -1, ProceduralAudio.Tick);
		}

		public static AudioClip ReviewPress()
		{
			// Rebuilt from native code (token 0x06000351, RVA 0x2795B80)
			return Get("ReviewPress", -1, ProceduralAudio.Chime);
		}

		public static AudioClip DiscordPress()
		{
			// Rebuilt from native code (token 0x06000352, RVA 0x2795C38)
			return Get("DiscordPress", -1, ProceduralAudio.Pearl);
		}

		public static AudioClip SpongeSwell()
		{
			// Rebuilt from native code (token 0x06000353, RVA 0x2795CF0)
			return Get("SpongeSwell", -1, () => SplashBig(0));
		}

		public static AudioClip GlowStickCrack()
		{
			// Rebuilt from native code (token 0x06000354, RVA 0x2795E0C)
			return Get("GlowStickCrack", -1, ProceduralAudio.Tick);
		}

		public static AudioClip TreeRumble()
		{
			// Rebuilt from native code (token 0x06000355, RVA 0x2795EC4)
			return Get("TreeRumble", -1, () => null);
		}

		public static AudioClip TreeCrack()
		{
			// Rebuilt from native code (token 0x06000356, RVA 0x2795FE0)
			return Get("TreeCrack", -1, ProceduralAudio.Snap);
		}

		public static AudioClip TreeGrow()
		{
			// Rebuilt from native code (token 0x06000357, RVA 0x2796098)
			return Get("TreeGrow", -1, ProceduralAudio.Creak);
		}

		public static AudioClip TreeLeaves()
		{
			// Rebuilt from native code (token 0x06000358, RVA 0x2796150)
			return Get("TreeLeaves", -1, () => null);
		}

		public static AudioClip TreeBloom()
		{
			// Rebuilt from native code (token 0x06000359, RVA 0x279626C)
			return Get("TreeBloom", -1, GiftAppear);
		}

		public static AudioClip TreeRustle()
		{
			// Rebuilt from native code (token 0x0600035A, RVA 0x2796324)
			return Get("TreeRustle", -1, () => null);
		}

		public static AudioClip VendMotor()
		{
			// Rebuilt from native code (token 0x0600035B, RVA 0x2796440)
			return Get("VendMotor", -1, ProceduralAudio.Whirr);
		}

		public static AudioClip VendDrop()
		{
			// Rebuilt from native code (token 0x0600035C, RVA 0x27964F8)
			return Get("VendDrop", -1, ProceduralAudio.Smash);
		}

		public static AudioClip VendFlap()
		{
			// Rebuilt from native code (token 0x0600035D, RVA 0x27965B0)
			return Get("VendFlap", -1, ProceduralAudio.Clunk);
		}

		public static AudioClip VendEmpty()
		{
			// Rebuilt from native code (token 0x0600035E, RVA 0x2796668)
			return Get("VendEmpty", -1, ShopDenied);
		}

		public static AudioClip PearlBurst()
		{
			// Rebuilt from native code (token 0x0600035F, RVA 0x2796720)
			return Get("PearlBurst", -1, ProceduralAudio.Chime);
		}

		public static AudioClip PearlCollect()
		{
			// Rebuilt from native code (token 0x06000360, RVA 0x27967D8)
			return Get("PearlCollect", -1, ProceduralAudio.Pearl);
		}

		public static AudioClip PearlsDone()
		{
			// Rebuilt from native code (token 0x06000361, RVA 0x2796890)
			return Get("PearlsDone", -1, ShopBuy);
		}

		public static AudioClip GiftAppear()
		{
			// Rebuilt from native code (token 0x06000362, RVA 0x2796948)
			return Get("GiftAppear", -1, PearlBurst);
		}

		public static AudioClip GiftShimmer()
		{
			// Rebuilt from native code (token 0x06000363, RVA 0x2796A00)
			return Get("GiftShimmer", -1, () => null);
		}

		public static AudioClip GiftRustle()
		{
			// Rebuilt from native code (token 0x06000364, RVA 0x2796B1C)
			return Get("GiftRustle", -1, ProceduralAudio.Tick);
		}

		public static AudioClip GiftGrab()
		{
			// Rebuilt from native code (token 0x06000365, RVA 0x2796BD4)
			return Get("GiftGrab", -1, ProceduralAudio.Tick);
		}

		public static AudioClip GiftStretch()
		{
			// Rebuilt from native code (token 0x06000366, RVA 0x2796C8C)
			return Get("GiftStretch", -1, ProceduralAudio.Creak);
		}

		public static AudioClip GiftStringPull()
		{
			// Rebuilt from native code (token 0x06000367, RVA 0x2796D44)
			return Get("GiftStringPull", -1, ProceduralAudio.Snap);
		}

		public static AudioClip GiftCharge()
		{
			// Rebuilt from native code (token 0x06000368, RVA 0x2796DFC)
			return Get("GiftCharge", -1, () => null);
		}

		public static AudioClip GiftPop()
		{
			// Rebuilt from native code (token 0x06000369, RVA 0x2796F18)
			return Get("GiftPop", -1, ProceduralAudio.Smash);
		}

		public static AudioClip GiftConfetti()
		{
			// Rebuilt from native code (token 0x0600036A, RVA 0x2796FD0)
			return Get("GiftConfetti", -1, () => null);
		}

		public static AudioClip GiftFanfare()
		{
			// Rebuilt from native code (token 0x0600036B, RVA 0x27970EC)
			return Get("GiftFanfare", -1, Escape);
		}

		public static AudioClip GiftBananaWhoosh()
		{
			// Rebuilt from native code (token 0x0600036C, RVA 0x27971A4)
			return Get("GiftBananaWhoosh", -1, PearlBurst);
		}

		public static AudioClip GiftBananaCollect()
		{
			// Rebuilt from native code (token 0x0600036D, RVA 0x279725C)
			return Get("GiftBananaCollect", -1, PearlCollect);
		}

		public static AudioClip GiftDone()
		{
			// Rebuilt from native code (token 0x0600036E, RVA 0x2797314)
			return Get("GiftDone", -1, PearlsDone);
		}

		public static AudioClip GiftPoof()
		{
			// Rebuilt from native code (token 0x0600036F, RVA 0x27973CC)
			return Get("GiftPoof", -1, () => null);
		}

		public static float PitchFor(AudioClip clip, float pitch)
		{
			// Rebuilt from native code (token 0x06000370, RVA 0x27974E8)
			return ProceduralAudio.Made(clip) ? pitch : Mathf.Clamp(pitch, 0.85f, 1.15f);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x06000371, RVA 0x2797578)
			loaded.Clear();
		}

		private static AudioClip Get(string name, int take, Func<AudioClip> placeholder)
		{
			// Rebuilt from native code (token 0x06000372, RVA 0x2794038)
			AudioClip[] takes = Takes(name);
			if (takes.Length == 0)
			{
				return placeholder();
			}
			if (take >= 0)
			{
				return takes[take % takes.Length];
			}
			return takes[Random.Range(0, takes.Length)];
		}

		private static AudioClip[] Takes(string name)
		{
			// Rebuilt from native code (token 0x06000373, RVA 0x27975F0)
			if (!loaded.TryGetValue(name, out AudioClip[] takes))
			{
				List<AudioClip> list = new List<AudioClip>();
				AudioClip clip = Resources.Load<AudioClip>("Sounds/" + name);
				if (clip)
				{
					list.Add(clip);
				}
				for (int i = 1; i <= 12; i++)
				{
					AudioClip next = Resources.Load<AudioClip>($"Sounds/{name}_{i}");
					if (!next)
					{
						break;
					}
					list.Add(next);
				}
				takes = list.ToArray();
				loaded[name] = takes;
			}
			return takes;
		}
	}
}
