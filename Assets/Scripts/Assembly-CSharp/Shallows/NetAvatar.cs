using System;
using System.Collections.Generic;
using Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shallows
{
	public class NetAvatar : NetworkBehaviour
	{
		public static readonly List<NetAvatar> Remotes = new List<NetAvatar>();

		[Header("Parts (set by the scene builder)")]
		public GameObject visual;

		public Transform head;

		public Transform body;

		public Transform leftHand;

		public Transform rightHand;

		public Renderer[] furRenderers;

		public TextMesh nameTag;

		public GameObject leftPebble;

		public GameObject rightPebble;

		public GameObject leftFuse;

		public GameObject rightFuse;

		[Tooltip("Helmet and fake light beam, shown where everyone wears a headlamp (Lights Out).")]
		public GameObject helmet;

		public MeshFilter beam;

		[Tooltip("The voice Speaker's object. It follows the head so voices come from the mouth.")]
		public Transform voice;

		private bool wasDying;

		private bool muted;

		private float nextVoiceCheck;

		private VoiceBoost boost;

		private Speaker speaker;

		private const int HoldingLeft = 1;

		private const int HoldingRight = 2;

		private const int Wet = 4;

		private const int Dying = 8;

		private const int FuseLeft = 16;

		private const int FuseRight = 32;

		private const int Body = 64;

		private const int BeltSlots = 3;

		private const float Smoothing = 18f;

		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private MaterialPropertyBlock block;

		private string shownNick;

		private bool tagOnChest;

		private Vector3 shownFur = new Vector3(-1f, 0f, 0f);

		private bool snapped;

		private ApeBody ape;

		private readonly GameObject[] heldShown = new GameObject[2];

		private readonly int[] heldKind = new int[2];

		private readonly GameObject[] beltShown = new GameObject[BeltSlots];

		private readonly int[] beltKind = new int[BeltSlots];

		private int shownWearing;

		private int shownLooks = -1;

		private bool shownTryOn;

		private bool shownOnline;

		private const int HeldFuse = 100;

		private static Mesh beamMesh;

		private static Material beamMaterial;

		private static Material glareMaterial;

		private static Mesh glareMesh;

		private const float MaxThrowSpeed = 30f;

		public static NetAvatar Local { get; private set; }

		[Networked]
		public Vector3 HeadPosition { get; set; }

		[Networked]
		public Quaternion HeadRotation { get; set; }

		[Networked]
		public Vector3 LeftPosition { get; set; }

		[Networked]
		public Quaternion LeftRotation { get; set; }

		[Networked]
		public Vector3 RightPosition { get; set; }

		[Networked]
		public Quaternion RightRotation { get; set; }

		[Networked]
		public float EyeHeight { get; set; }

		[Networked]
		public int Flags { get; set; }

		[Networked]
		public NetworkString<_16> Nick { get; set; }

		[Networked]
		public Vector3 Fur { get; set; }

		[Networked]
		public int Fingers { get; set; }

		[Networked]
		public int Carried { get; set; }

		[Networked]
		public int Wearing { get; set; }

		[Networked]
		public Vector3 BodyPosition { get; set; }

		[Networked]
		public Quaternion BodyRotation { get; set; }

		public Vector3 Feet
		{
			get
			{
				// Rebuilt from native code (token 0x060005E3, RVA 0x27E4E0C)
				return head.position + Vector3.down * Mathf.Max(0.3f, EyeHeight);
			}
		}

		public Vector3 NetFeet
		{
			get
			{
				// Rebuilt from native code (token 0x060005E4, RVA 0x27E2048)
				return HeadPosition + Vector3.down * Mathf.Clamp(EyeHeight, 0.3f, 1.2f);
			}
		}

		public bool NetDying
		{
			get
			{
				// Rebuilt from native code (token 0x060005E5, RVA 0x27E2100)
				return (Flags & (Dying | Body)) != 0;
			}
		}

		public string PlayerName
		{
			get
			{
				// Rebuilt from native code (token 0x060005E6, RVA 0x27E3094)
				return Nick.ToString();
			}
		}

		public float VoiceLoudness
		{
			get
			{
				// Rebuilt from native code (token 0x060005E7, RVA 0x27E4EC4)
				if (!ape || !ape.voice)
				{
					return 0f;
				}
				return ape.voice.Level;
			}
		}

		public Color FurColor
		{
			get
			{
				// Rebuilt from native code (token 0x060005E8, RVA 0x27E4F7C)
				if (!NetGuard.Ok(Fur))
				{
					return Color.gray;
				}
				return new Color(Mathf.Clamp01(Fur.x), Mathf.Clamp01(Fur.y), Mathf.Clamp01(Fur.z), 1f);
			}
		}

		public int PlayerId
		{
			get
			{
				// Rebuilt from native code (token 0x060005E9, RVA 0x27E507C)
				if (Object && Object.IsValid)
				{
					return Object.StateAuthority.PlayerId;
				}
				return -1;
			}
		}

		public string UserId
		{
			get
			{
				// Rebuilt from native code (token 0x060005EA, RVA 0x27E520C)
				if (Runner && Object && Object.IsValid)
				{
					return Runner.GetPlayerUserId(Object.StateAuthority);
				}
				return null;
			}
		}

		public bool Muted
		{
			get
			{
				// Rebuilt from native code (token 0x060005EB, RVA 0x27E5434)
				return muted;
			}
			set
			{
				// Rebuilt from native code (token 0x060005EC, RVA 0x27E543C)
				muted = value;
				if (boost)
				{
					boost.Muted = value;
				}
			}
		}

		private void RestartVoice()
		{
			// Rebuilt from native code (token 0x060005E0, RVA 0x27E49E0)
			if (!speaker && voice)
			{
				speaker = voice.GetComponent<Speaker>();
			}
			if (speaker && speaker.IsLinked)
			{
				speaker.RestartPlayback();
			}
			nextVoiceCheck = Time.time + 2f;
		}

		private void KeepVoicePlaying()
		{
			// Rebuilt from native code (token 0x060005E1, RVA 0x27E4AFC)
			if (!voice || Time.time < nextVoiceCheck)
			{
				return;
			}
			nextVoiceCheck = Time.time + 1f;
			if (!voice.gameObject.activeSelf)
			{
				voice.gameObject.SetActive(true);
			}
			if (voice.TryGetComponent(out AudioSource source))
			{
				if (!source.enabled)
				{
					source.enabled = true;
				}
				if (source.mute)
				{
					source.mute = false;
				}
			}
			if (!speaker)
			{
				speaker = voice.GetComponent<Speaker>();
			}
			if (!speaker)
			{
				return;
			}
			if (!speaker.enabled)
			{
				speaker.enabled = true;
			}
			if (speaker.IsLinked && !speaker.IsPlaying)
			{
				RestartVoice();
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Rebuilt from native code (token 0x060005E2, RVA 0x27E4D28)
			Local = null;
			Remotes.Clear();
		}

		public override void Spawned()
		{
			// Rebuilt from native code (token 0x060005ED, RVA 0x27E54D8)
			block = new MaterialPropertyBlock();
			if (beam && !beam.sharedMesh)
			{
				beam.sharedMesh = BeamMesh();
			}
			DressBeam();
			WearItemModels();
			if (helmet)
			{
				helmet.SetActive(false);
			}
			if (nameTag)
			{
				PixelFonts.Use(nameTag, PixelFonts.Sign, 0.045f);
			}
			if (HasStateAuthority)
			{
				Local = this;
				if (visual)
				{
					visual.SetActive(false);
				}
				ApplyProfile();
				PlayerProfile.Changed += ApplyProfile;
				return;
			}
			Remotes.Add(this);
			WearModel();
			boost = VoiceBoost.Fit(voice);
			tagOnChest = nameTag && ape && ape.WearNameTag(nameTag.transform);
			if (boost)
			{
				boost.Muted = muted;
			}
		}

		private void WearItemModels()
		{
			// Rebuilt from native code (token 0x060005EE, RVA 0x27E61E4)
			if (leftPebble)
			{
				ItemModels.Wear(leftPebble.transform, "Pebble_A");
			}
			if (rightPebble)
			{
				ItemModels.Wear(rightPebble.transform, "Pebble_B");
			}
			if (leftFuse)
			{
				ItemModels.Wear(leftFuse.transform, "Fuse");
			}
			if (rightFuse)
			{
				ItemModels.Wear(rightFuse.transform, "Fuse");
			}
			if (helmet)
			{
				ItemModels.Wear(helmet.transform, "HeadlampHelmet");
			}
		}

		private void WearModel()
		{
			// Rebuilt from native code (token 0x060005EF, RVA 0x27E65E4)
			if (!visual)
			{
				return;
			}
			Material template = furRenderers != null && furRenderers.Length != 0 && furRenderers[0] ? furRenderers[0].sharedMaterial : null;
			ape = ApeBody.Create(visual.transform, head, leftHand, rightHand, () => EyeHeight, template);
			if (!ape)
			{
				return;
			}
			if (voice && voice.GetComponent<AudioSource>())
			{
				ape.voice = voice.gameObject.AddComponent<VoiceLevel>();
			}
			ape.belt = BeltLook.Wear(ape, false);
			foreach (Transform child in head)
			{
				if (!helmet || child != helmet.transform)
				{
					foreach (Renderer part in child.GetComponents<Renderer>())
					{
						part.enabled = false;
					}
				}
			}
			foreach (Renderer part in body.GetComponentsInChildren<Renderer>(true))
			{
				part.enabled = false;
			}
			foreach (Transform hand in new Transform[] { leftHand, rightHand })
			{
				foreach (Renderer part in hand.GetComponents<Renderer>())
				{
					part.enabled = false;
				}
			}
			if (helmet)
			{
				helmet.transform.localPosition += new Vector3(0f, 0.01f, -0.07f);
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			// Rebuilt from native code (token 0x060005F0, RVA 0x27E6E34)
			if (Local == this)
			{
				Local = null;
				PlayerProfile.Changed -= ApplyProfile;
			}
			Remotes.Remove(this);
			for (int i = 0; i < 2; i++)
			{
				if (heldShown[i])
				{
					Destroy(heldShown[i]);
				}
			}
			for (int i = 0; i < BeltSlots; i++)
			{
				if (beltShown[i])
				{
					Destroy(beltShown[i]);
				}
			}
		}

		private void ApplyProfile()
		{
			// Rebuilt from native code (token 0x060005F1, RVA 0x27E6488)
			if (!Object || !HasStateAuthority)
			{
				return;
			}
			Nick = PlayerProfile.Name;
			Color color = PlayerProfile.Color;
			Fur = new Vector3(color.r, color.g, color.b);
		}

		public override void FixedUpdateNetwork()
		{
			// Rebuilt from native code (token 0x060005F2, RVA 0x27E7130)
			if (!HasStateAuthority)
			{
				return;
			}
			PlayerRig rig = PlayerRig.I;
			if (!rig)
			{
				return;
			}
			HeadPosition = rig.head.position;
			HeadRotation = rig.head.rotation;
			LeftPosition = rig.leftHand.position;
			LeftRotation = rig.leftHand.rotation;
			RightPosition = rig.rightHand.position;
			RightRotation = rig.rightHand.rotation;
			EyeHeight = rig.eyeHeight;
			Fingers = ApeBody.PackFingers(ApeBody.ReadFingers(true), ApeBody.ReadFingers(false));
			int carried = 0;
			int flags = 0;
			GameManager game = GameManager.I;
			if (game && game.grabbers != null)
			{
				foreach (HandGrabber grabber in game.grabbers)
				{
					if (!grabber || !grabber.Held)
					{
						continue;
					}
					if (grabber.Held.GetComponent<Fuse>())
					{
						flags |= grabber.left ? FuseLeft : FuseRight;
					}
					else
					{
						flags |= grabber.left ? HoldingLeft : HoldingRight;
						carried |= ((grabber.Held.netKind + 1) & 0xF) << (grabber.left ? 0 : 4);
					}
				}
			}
			Belt belt = Belt.I;
			if (belt)
			{
				for (int i = 0; i < BeltSlots; i++)
				{
					carried |= (belt.SlotCode(i) & 0xF) << (8 + 4 * i);
				}
			}
			Carried = carried;
			Wearing = CosmeticLook.Pack();
			if (rig.Wet)
			{
				flags |= Wet;
			}
			if (game && game.Dying)
			{
				flags |= Dying;
			}
			GorillaRagdoll ragdoll = DeathSequence.I ? DeathSequence.I.BodyOf(NetBridge.LocalPlayerId) : null;
			if (ragdoll && !ragdoll.Held)
			{
				BodyPosition = ragdoll.Position;
				BodyRotation = ragdoll.Rotation;
				flags |= Body;
			}
			Flags = flags;
		}

		public override void Render()
		{
			// Rebuilt from native code (token 0x060005F3, RVA 0x27E7728)
			if (HasStateAuthority)
			{
				return;
			}
			if (!NetGuard.Ok(HeadPosition) || !NetGuard.Ok(LeftPosition) || !NetGuard.Ok(RightPosition) || !NetGuard.Ok(BodyPosition)
				|| !NetGuard.Ok(HeadRotation) || !NetGuard.Ok(LeftRotation) || !NetGuard.Ok(RightRotation) || !NetGuard.Ok(EyeHeight))
			{
				return;
			}
			if ((Flags & Body) != 0 && DeathSequence.I)
			{
				GorillaRagdoll ragdoll = DeathSequence.I.BodyOf(Object.StateAuthority.PlayerId);
				if (ragdoll && !ragdoll.Held)
				{
					ragdoll.Follow(BodyPosition, BodyRotation);
				}
			}
			int flags = Flags;
			bool dying = (flags & Dying) != 0;
			if (visual && visual.activeSelf == dying)
			{
				visual.SetActive(!dying);
			}
			if (wasDying && !dying)
			{
				RestartVoice();
			}
			wasDying = dying;
			if (dying)
			{
				return;
			}
			KeepVoicePlaying();
			float t = snapped ? 1f - MathF.Exp(-Smoothing * Time.deltaTime) : 1f;
			snapped = true;
			head.SetPositionAndRotation(Vector3.Lerp(head.position, HeadPosition, t), Quaternion.Slerp(head.rotation, HeadRotation, t));
			leftHand.SetPositionAndRotation(Vector3.Lerp(leftHand.position, LeftPosition, t), Quaternion.Slerp(leftHand.rotation, LeftRotation, t));
			rightHand.SetPositionAndRotation(Vector3.Lerp(rightHand.position, RightPosition, t), Quaternion.Slerp(rightHand.rotation, RightRotation, t));
			float eyeHeight = Mathf.Max(0.3f, EyeHeight);
			Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
			if (forward.sqrMagnitude < 0.001f)
			{
				forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
			}
			body.SetPositionAndRotation(head.position + Vector3.down * (eyeHeight * 0.55f), Quaternion.LookRotation(forward.normalized));
			body.localScale = new Vector3(0.42f, eyeHeight * 0.5f, 0.36f);
			ShowHeld(0, (Flags & HoldingLeft) != 0 ? Carried & 0xF : 0, (Flags & FuseLeft) != 0, leftHand, leftPebble, leftFuse);
			ShowHeld(1, (Flags & HoldingRight) != 0 ? (Carried >> 4) & 0xF : 0, (Flags & FuseRight) != 0, rightHand, rightPebble, rightFuse);
			ShowBelt(forward.normalized);
			LookCheck.Tick();
			bool tryOn = TryOnCabana.InZone(head.position);
			bool online = PlayFabClient.LoggedIn;
			string user;
			if (ape && (Wearing != shownWearing || LookCheck.Version != shownLooks || tryOn != shownTryOn || shownOnline != online))
			{
				shownWearing = Wearing;
				shownLooks = LookCheck.Version;
				shownOnline = online;
				shownTryOn = tryOn;
				CosmeticLook look = CosmeticLook.On(ape);
				user = UserId;
				if (look)
				{
					look.WearPacked(Wearing, id => tryOn || LookCheck.Allowed(user, id));
				}
			}
			if (helmet)
			{
				bool headlamp = Atmosphere.HeadlampAt(head.position);
				if (headlamp != helmet.activeSelf)
				{
					helmet.SetActive(headlamp);
				}
			}
			if (voice)
			{
				voice.position = head.position;
			}
			if (ape)
			{
				ApeBody.UnpackFingers(Fingers, out Vector3 left, out Vector3 right);
				ape.leftFingers = left;
				ape.rightFingers = right;
			}
			string nick = Nick.ToString();
			if (nameTag)
			{
				if (nick != shownNick)
				{
					shownNick = nick;
					nameTag.text = PlayerProfile.Filter(nick);
					if (tagOnChest && ape)
					{
						PixelFonts.Use(nameTag, PixelFonts.Sign, Mathf.Min(ape.NameTagHeight, ape.NameTagWidth / Mathf.Max(nameTag.text.Length, 1)));
					}
				}
				if (!tagOnChest)
				{
					nameTag.transform.position = head.position + Vector3.up * 0.32f;
					Camera main = Camera.main;
					if (main)
					{
						nameTag.transform.rotation = Quaternion.LookRotation(nameTag.transform.position - main.transform.position);
					}
				}
			}
			if (Fur == shownFur || furRenderers == null || !NetGuard.Ok(Fur))
			{
				return;
			}
			shownFur = Fur;
			Color fur = new Color(Mathf.Clamp01(Fur.x), Mathf.Clamp01(Fur.y), Mathf.Clamp01(Fur.z), 1f);
			block.SetColor(BaseColorId, fur);
			foreach (Renderer part in furRenderers)
			{
				if (part)
				{
					part.SetPropertyBlock(block);
				}
			}
			if (ape)
			{
				ape.SetFur(fur);
			}
		}

		private void ShowHeld(int hand, int code, bool fuse, Transform holder, GameObject pebble, GameObject fuseObject)
		{
			// Rebuilt from native code (token 0x060005F4, RVA 0x27E89E0)
			int kind = fuse ? HeldFuse : (code == 0 && ((Flags >> (hand != 0 ? 1 : 0)) & 1) != 0 ? 1 : code);
			if (heldKind[hand] != kind)
			{
				heldKind[hand] = kind;
				if (heldShown[hand])
				{
					Destroy(heldShown[hand]);
				}
				heldShown[hand] = kind == HeldFuse ? FuseVisual() : (kind >= 1 ? Items.Visual((ItemKind)(kind - 1)) : null);
				if (heldShown[hand])
				{
					heldShown[hand].name = "Held " + heldShown[hand].name;
					heldShown[hand].transform.SetParent(visual ? visual.transform : transform, true);
				}
			}
			if (pebble && pebble.activeSelf)
			{
				pebble.SetActive(false);
			}
			if (fuseObject)
			{
				fuseObject.SetActive(fuse && !heldShown[hand]);
			}
			HeldGrip grip = kind == HeldFuse ? HeldGrip.Fuse : (kind >= 1 ? HeldGrip.For((ItemKind)(kind - 1)) : null);
			Transform shown = heldShown[hand] ? heldShown[hand].transform : null;
			if (ape)
			{
				ape.Hold(hand == 0, grip, shown);
			}
			else if (shown && holder)
			{
				HeldGrip.Fallback(holder, new Vector3(0f, -0.02f, 0.03f), out Vector3 position, out Quaternion rotation);
				shown.SetPositionAndRotation(position, rotation);
			}
		}

		private static GameObject FuseVisual()
		{
			// Rebuilt from native code (token 0x060005F5, RVA 0x27E96F0)
			GameObject fuse = new GameObject("fuse");
			fuse.layer = 2;
			if (ItemModels.Wear(fuse.transform, "Fuse"))
			{
				return fuse;
			}
			Destroy(fuse);
			return null;
		}

		private void ShowBelt(Vector3 forward)
		{
			// Rebuilt from native code (token 0x060005F6, RVA 0x27E8FA8)
			BeltLook look = ape ? ape.belt : null;
			Vector3[] slots = Belt.I ? Belt.I.slots : null;
			Quaternion facing = Quaternion.LookRotation(forward);
			for (int i = 0; i < BeltSlots; i++)
			{
				int kind = (Carried >> (8 + 4 * i)) & 0xF;
				if (beltKind[i] != kind)
				{
					beltKind[i] = kind;
					if (beltShown[i])
					{
						Destroy(beltShown[i]);
					}
					beltShown[i] = kind == 0 ? null : (kind == 15 ? FuseVisual() : Items.Visual((ItemKind)(kind - 1)));
					if (beltShown[i])
					{
						beltShown[i].name = "Belt " + beltShown[i].name;
						if (visual)
						{
							beltShown[i].transform.SetParent(visual.transform, true);
						}
					}
				}
				if (look)
				{
					Transform shown = beltShown[i] ? beltShown[i].transform : null;
					if (kind == 15)
					{
						look.Show(i, shown, 0.025f, 0.21f);
					}
					else if (kind == 0)
					{
						look.Show(i, shown, 0f, 0f);
					}
					else
					{
						look.Show(i, shown, Items.Radius((ItemKind)(kind - 1)), Items.Length((ItemKind)(kind - 1)));
					}
				}
				else if (beltShown[i])
				{
					Vector3 offset;
					if (slots != null && i < slots.Length)
					{
						offset = slots[i];
					}
					else if (i == 1)
					{
						offset = new Vector3(0f, -0.43f, 0.16f);
					}
					else
					{
						offset = new Vector3((i - 1) * 0.2f, -0.43f, 0.08f);
					}
					beltShown[i].transform.SetPositionAndRotation(head.position + facing * offset, facing);
				}
			}
		}

		private static Mesh BeamMesh()
		{
			// Rebuilt from native code (token 0x060005F7, RVA 0x27E5940)
			if (!beamMesh)
			{
				Vector3[] vertices = new Vector3[24];
				Color[] colors = new Color[24];
				int[] triangles = new int[72];
				for (int i = 0; i < 12; i++)
				{
					float angle = i * Mathf.PI * 2f / 12f;
					float sin = MathF.Sin(angle);
					float cos = MathF.Cos(angle);
					vertices[i] = new Vector3(cos * 0.04f, sin * 0.04f, 0f);
					vertices[i + 12] = new Vector3(cos * 1.4f, sin * 1.4f, 0f) + Vector3.forward * 4f;
					colors[i] = new Color(1f, 0.95f, 0.8f, 0.22f);
					colors[i + 12] = new Color(1f, 0.95f, 0.8f, 0f);
					int next = (i + 1) % 12;
					int t = i * 6;
					triangles[t] = i;
					triangles[t + 1] = i + 12;
					triangles[t + 2] = next + 12;
					triangles[t + 3] = i;
					triangles[t + 4] = next + 12;
					triangles[t + 5] = next;
				}
				Mesh mesh = new Mesh();
				mesh.name = "Headlamp Beam";
				mesh.vertices = vertices;
				mesh.colors = colors;
				mesh.triangles = triangles;
				beamMesh = mesh;
				beamMesh.RecalculateBounds();
			}
			return beamMesh;
		}

		private void DressBeam()
		{
			// Rebuilt from native code (token 0x060005F8, RVA 0x27E5D4C)
			if (!beam)
			{
				return;
			}
			Shader shader = Shader.Find("Shallows/Beam");
			if (!shader)
			{
				return;
			}
			if (!beamMaterial)
			{
				beamMaterial = new Material(shader) { name = "Headlamp Beam" };
				glareMaterial = new Material(shader) { name = "Headlamp Glare" };
				glareMaterial.SetFloat("_Glare", 1f);
				glareMaterial.SetColor("_Color", new Color(1f, 0.95f, 0.8f, 0.9f));
			}
			MeshRenderer beamRenderer = beam.GetComponent<MeshRenderer>();
			if (beamRenderer)
			{
				beamRenderer.sharedMaterial = beamMaterial;
				beamRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}
			if (!beam.transform.Find("Glare"))
			{
				GameObject glare = new GameObject("Glare");
				glare.layer = beam.gameObject.layer;
				glare.transform.SetParent(beam.transform, false);
				glare.AddComponent<MeshFilter>().sharedMesh = GlareMesh();
				MeshRenderer glareRenderer = glare.AddComponent<MeshRenderer>();
				glareRenderer.sharedMaterial = glareMaterial;
				glareRenderer.shadowCastingMode = ShadowCastingMode.Off;
				glareRenderer.receiveShadows = false;
			}
		}

		private static Mesh GlareMesh()
		{
			// Rebuilt from native code (token 0x060005F9, RVA 0x27E9834)
			if (!glareMesh)
			{
				Mesh mesh = new Mesh();
				mesh.name = "Headlamp Glare";
				mesh.vertices = new Vector3[]
				{
					new Vector3(-0.5f, -0.5f, 0f),
					new Vector3(0.5f, -0.5f, 0f),
					new Vector3(0.5f, 0.5f, 0f),
					new Vector3(-0.5f, 0.5f, 0f)
				};
				mesh.uv = new Vector2[]
				{
					new Vector2(0f, 0f),
					new Vector2(1f, 0f),
					new Vector2(1f, 1f),
					new Vector2(0f, 1f)
				};
				mesh.colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
				mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
				glareMesh = mesh;
				glareMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
			}
			return glareMesh;
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Splash(NetworkRunner runner, Vector3 position, float intensity, int kind, Vector3 heading, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x060005FA, RVA 0x27E9BE0)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Splash))
			{
				return;
			}
			if (!NetGuard.Ok(position) || !NetGuard.Ok(intensity) || !NetGuard.Ok(heading) || kind < 0 || kind > (int)SplashKind.Balloon || kind == (int)SplashKind.Monster)
			{
				return;
			}
			if (NetGuard.NearAnythingOf(info.Source, position, 4f) && SplashSystem.I)
			{
				SplashSystem.I.Loud(position, Mathf.Clamp01(intensity), (SplashKind)kind, true, NetGuard.Speed(heading, MaxThrowSpeed));
			}
		}

		[Rpc(Channel = RpcChannel.Unreliable, InvokeLocal = false)]
		public static void Rpc_Tap(NetworkRunner runner, Vector3 position, float hit, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x060005FB, RVA 0x27EA214)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Tap))
			{
				return;
			}
			if (NetGuard.Ok(position) && NetGuard.Ok(hit) && NetGuard.Near(info.Source, position, 2.5f))
			{
				HandTaps.PlayRemote(position, Mathf.Clamp01(hit));
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Throw(NetworkRunner runner, int kind, Vector3 position, Vector3 velocity, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x060005FC, RVA 0x27EA6A4)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Throw))
			{
				return;
			}
			if (!NetGuard.Ok(position) || !NetGuard.Ok(velocity) || kind < 0 || (kind > 3 && !LoungeCan.Is(kind)))
			{
				return;
			}
			if (!NetGuard.Near(info.Source, position, 3f) || !NetworkManager.I)
			{
				return;
			}
			GameObject ghost = NetworkManager.I.SpawnGhost(kind, position, NetGuard.Speed(velocity, MaxThrowSpeed));
			if (ghost)
			{
				NetGuard.Threw(info.Source, ghost.transform);
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Item(NetworkRunner runner, int kind, int id, Vector3 position, Vector3 velocity, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x060005FD, RVA 0x27EACC0)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Item))
			{
				return;
			}
			if (!NetGuard.Ok(position) || !NetGuard.Ok(velocity) || id == 0 || kind < 0 || kind > 3)
			{
				return;
			}
			if (!NetGuard.NearAnythingOf(info.Source, position, 4f))
			{
				return;
			}
			ShopItem item = ShopItem.Arrive(kind, id, info.Source.PlayerId, position, NetGuard.Speed(velocity, MaxThrowSpeed));
			if (item)
			{
				NetGuard.Threw(info.Source, item.transform);
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_ItemTaken(NetworkRunner runner, int id, bool gone, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x060005FE, RVA 0x27EB2E0)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Item))
			{
				return;
			}
			PlayerRef source = info.Source;
			ShopItem.Taken(id, gone, source.PlayerId, at => NetGuard.Near(source, at, 3f));
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Sponge(NetworkRunner runner, Vector3 position, float yaw, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x060005FF, RVA 0x27EB778)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Throw))
			{
				return;
			}
			if (!NetGuard.Ok(position) || !NetGuard.Ok(yaw) || !NetGuard.NearAnythingOf(info.Source, position, 4f))
			{
				return;
			}
			WaterVolume water = WaterVolume.At(position);
			if (water)
			{
				SpongeRaft.Open(new Vector3(position.x, water.SurfaceY, position.z), yaw, true);
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Caught(NetworkRunner runner, int monster, Vector3 victim, Vector3 fur, int player, int kill, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000600, RVA 0x27EBCA4)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Caught))
			{
				return;
			}
			if (!NetGuard.Ok(victim) || !NetGuard.Ok(fur) || info.Source.PlayerId != player || !NetGuard.Near(info.Source, victim, 4f))
			{
				return;
			}
			MonsterAI ai = MonsterAI.ByIndex(monster);
			if (!ai)
			{
				return;
			}
			NetGuard.Caught(info.Source);
			Kill caught = Kill.FromCode(kill);
			if (NetBridge.RunsMonsters)
			{
				ai.RemoteCaught(victim, caught.Code);
			}
			else
			{
				ai.WatchKill(victim, caught.Code);
			}
			if (DeathSequence.I)
			{
				DeathSequence.I.SpawnRemote(ai, victim, new Color(Mathf.Clamp01(fur.x), Mathf.Clamp01(fur.y), Mathf.Clamp01(fur.z), 1f), player, caught);
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Escape(NetworkRunner runner, string name, float time, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000601, RVA 0x27EC43C)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Escape))
			{
				return;
			}
			if (!NetGuard.Ok(time) || time > 86400f || time < 30f)
			{
				return;
			}
			NetAvatar avatar = NetGuard.AvatarOf(info.Source);
			if (!avatar)
			{
				return;
			}
			Transform goal = GameManager.I ? GameManager.I.goal : null;
			if (goal && !NetGuard.Near(info.Source, goal.position, 10f))
			{
				return;
			}
			string escaped = PlayerProfile.Filter(avatar.PlayerName);
			if (GameManager.I)
			{
				GameManager.I.OtherEscaped(escaped, time);
			}
			else
			{
				Leaderboard.Submit(escaped, time);
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_FuseRequest(NetworkRunner runner, int action, int fuse, Vector3 position, int player, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000602, RVA 0x27ECA6C)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.FuseRequest))
			{
				return;
			}
			if (!NetGuard.Ok(position) || fuse < 0 || fuse > 2)
			{
				return;
			}
			switch (action)
			{
			case 0:
				if (!FuseDoor.I.HeldBy(fuse, info.Source.PlayerId) && !NetGuard.Near(info.Source, FuseDoor.I.FusePosition(fuse), 3f))
				{
					return;
				}
				break;
			case 1:
				break;
			case 2:
				if (!NetGuard.Near(info.Source, position, 3f))
				{
					return;
				}
				break;
			default:
				return;
			}
			FuseDoor.I.Handle(action, fuse, position, info.Source.PlayerId);
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_FuseThrow(NetworkRunner runner, int fuse, Vector3 position, Vector3 velocity, int player, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000603, RVA 0x27ED12C)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.FuseThrow))
			{
				return;
			}
			if (!NetGuard.Ok(position) || !NetGuard.Ok(velocity) || info.Source.PlayerId != player || !NetGuard.Near(info.Source, position, 3f))
			{
				return;
			}
			if (!FuseDoor.I)
			{
				return;
			}
			FuseDoor.I.RemoteThrow(fuse, position, NetGuard.Speed(velocity, MaxThrowSpeed), player);
			if (fuse >= 0 && fuse < FuseDoor.I.fuses.Length && FuseDoor.I.fuses[fuse])
			{
				NetGuard.Threw(info.Source, FuseDoor.I.fuses[fuse].transform, 8f);
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Fuses(NetworkRunner runner, int packed, int holders, Vector3 loose0, Vector3 loose1, Vector3 loose2, float openLeft, int round, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000604, RVA 0x27ED828)
			if (!NetGuard.FromMaster(runner, info))
			{
				return;
			}
			if (!NetGuard.Ok(loose0) || !NetGuard.Ok(loose1) || !NetGuard.Ok(loose2) || !NetGuard.Ok(openLeft))
			{
				return;
			}
			FuseDoor.I.Receive(new FuseSnapshot
			{
				packed = packed,
				holder0 = (holders & 0xFF) - 1,
				holder1 = ((holders >> 8) & 0xFF) - 1,
				holder2 = ((holders >> 16) & 0xFF) - 1,
				loose0 = loose0,
				loose1 = loose1,
				loose2 = loose2,
				openLeft = Mathf.Clamp(openLeft, 0f, FuseDoor.I.openSeconds + 1f),
				round = round
			});
		}

		[Rpc(Channel = RpcChannel.Unreliable, InvokeLocal = false)]
		public static void Rpc_Clock(NetworkRunner runner, float authorityTime, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000605, RVA 0x27EDF30)
			if (NetGuard.FromMaster(runner, info) && NetGuard.Ok(authorityTime))
			{
				NetBridge.SyncClock(authorityTime);
			}
		}

		[Rpc(Channel = RpcChannel.Unreliable, InvokeLocal = false)]
		public static void Rpc_Monster(NetworkRunner runner, int monster, Vector3 position, float yaw, int state, Vector3 target, float speed, float sentAt, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000606, RVA 0x27EE32C)
			if (!NetGuard.FromMaster(runner, info))
			{
				return;
			}
			if (!NetGuard.Ok(position) || !NetGuard.Ok(yaw) || !NetGuard.Ok(target) || !NetGuard.Ok(speed) || !NetGuard.Ok(sentAt) || state < 0 || state > 4)
			{
				return;
			}
			NetBridge.LastMonsterUpdate = Time.time;
			MonsterAI ai = MonsterAI.ByIndex(monster);
			if (ai)
			{
				ai.SetRemote(position, yaw, (MonsterAI.State)state, target, Mathf.Clamp(speed, 0f, 40f), sentAt);
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Pebble(NetworkRunner runner, int action, int pebble, Vector3 position, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000607, RVA 0x27EEA1C)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Pebble) || !NetGuard.Ok(position))
			{
				return;
			}
			if (action == LoosePebbles.Taken && !NetGuard.Near(info.Source, position, 3f))
			{
				return;
			}
			if (LoosePebbles.I)
			{
				LoosePebbles.I.Receive(action, pebble, position);
			}
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_TreeRip(NetworkRunner runner, int id, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000608, RVA 0x27EEF4C)
			if (!NetGuard.Allow(runner, info, NetGuard.Kind.Tree))
			{
				return;
			}
			PlayerRef source = info.Source;
			WildTrees.I.RemoteRip(id, source.PlayerId, at => NetGuard.Near(source, at, 3f));
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Tree(NetworkRunner runner, int id, int level, Vector3 position, float yaw, int state, int winner, float age, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x06000609, RVA 0x27EF460)
			if (!NetGuard.FromMaster(runner, info) || !WildTrees.I)
			{
				return;
			}
			if (!NetGuard.Ok(position) || !NetGuard.Ok(yaw) || !NetGuard.Ok(age) || level < 0 || level > 64)
			{
				return;
			}
			WildTrees.I.Receive(new TreeSnapshot
			{
				id = id,
				level = level,
				position = position,
				yaw = yaw,
				state = state,
				winner = winner,
				age = Mathf.Clamp(age, 0f, 86400f)
			});
		}

		[Rpc(InvokeLocal = false)]
		public static void Rpc_Pebbles(NetworkRunner runner, int seed, int takenLow, int takenHigh, RpcInfo info = default)
		{
			// Rebuilt from native code (token 0x0600060A, RVA 0x27EFAB4)
			if (NetGuard.FromMaster(runner, info) && LoosePebbles.I)
			{
				LoosePebbles.I.ReceiveState(new PebbleSnapshot
				{
					seed = seed,
					taken = (uint)takenLow | ((long)takenHigh << 32)
				});
			}
		}
	}
}
