using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	[DefaultExecutionOrder(100)]
	public class ApeRagdoll : MonoBehaviour
	{
		private Transform model;

		private Transform torso;

		private Transform headPart;

		private Transform neck;

		private Transform head;

		private Vector3 modelPosition;

		private Quaternion modelRotation;

		private Quaternion headRest;

		private Quaternion headLocalRest;

		private Quaternion neckLocalRest;

		private Vector3 headScale;

		private readonly List<(Transform bone, Vector3 scale)> arms = new List<(Transform bone, Vector3 scale)>();

		private readonly List<(Transform bone, Transform part, Quaternion rest)> limbs = new List<(Transform bone, Transform part, Quaternion rest)>();

		private Material material;

		public static GorillaRagdoll Create(Vector3 chest, Color fur)
		{
			// Rebuilt from native code (token 0x060007CA, RVA 0x2818A68)
			GameObject prefab = Resources.Load<GameObject>("Ape/ShallowsPlayer");
			if (!prefab)
			{
				return null;
			}
			GameObject go = new GameObject("Gorilla Ragdoll");
			go.SetActive(false);
			ApeRagdoll ape = go.AddComponent<ApeRagdoll>();
			GorillaRagdoll ragdoll = ape.Build(prefab, fur);
			if (!ragdoll)
			{
				Destroy(go);
				return null;
			}
			go.transform.position = chest - ape.torso.localPosition;
			return ragdoll;
		}

		private GorillaRagdoll Build(GameObject prefab, Color fur)
		{
			// Rebuilt from native code (token 0x060007CB, RVA 0x2818C70)
			Transform root = new GameObject("Model").transform;
			root.SetParent(transform, false);
			GameObject body = Instantiate(prefab, root, false);
			body.name = "ShallowsPlayer";
			SkinnedMeshRenderer skin = body.GetComponentInChildren<SkinnedMeshRenderer>(true);
			Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
			foreach (Transform t in body.GetComponentsInChildren<Transform>(true))
			{
				bones[t.name] = t;
			}
			neck = Bone("Neck");
			head = Bone("Head");
			Transform eyeL = Bone("Eye.L");
			Transform eyeR = Bone("Eye.R");
			if (!skin || !neck || !head || !eyeL || !eyeR)
			{
				return null;
			}
			Vector3 facing = Vector3.ProjectOnPlane((eyeL.position + eyeR.position) * 0.5f - head.position, Vector3.up);
			root.localRotation = Quaternion.AngleAxis(Vector3.SignedAngle(facing, Vector3.forward, Vector3.up), Vector3.up);
			float eyeY = (eyeL.position.y + eyeR.position.y) * 0.5f;
			if (eyeY < 0.1f)
			{
				return null;
			}
			ShallowsTuning tuning = ShallowsTuning.I;
			float eyeHeight = PlayerRig.I ? PlayerRig.I.eyeHeight : 0.55f;
			float bodySize = tuning ? tuning.bodySize : 1.15f;
			float scale = eyeHeight / eyeY * bodySize;
			root.localScale = Vector3.one * scale;
			model = root;
			Vector3 eyes = (eyeL.position + eyeR.position) * 0.5f;
			material = ApeBody.NewFurMaterial(fur);
			skin.sharedMaterial = material;
			Bounds bounds = skin.localBounds;
			bounds.Expand(bounds.size.magnitude * 2f);
			skin.localBounds = bounds;
			headScale = head.localScale;
			float neckY = neck.position.y;
			float chestY = neckY * 0.42f;
			Rigidbody chest = Part("Chest", new Vector3(0f, chestY, 0f), Quaternion.identity, 12f);
			torso = chest.transform;
			CapsuleCollider chestCollider = chest.gameObject.AddComponent<CapsuleCollider>();
			chestCollider.radius = scale * 0.17f;
			chestCollider.height = neckY;
			chestCollider.center = new Vector3(0f, neckY * 0.5f - chestY, 0f);
			Rigidbody headBody = Part("Head", Vector3.Lerp(head.position, eyes, 0.55f), Quaternion.identity, 2.5f);
			headPart = headBody.transform;
			headBody.gameObject.AddComponent<SphereCollider>().radius = scale * 0.17f;
			Joint(headBody, chest, head.position, 35f, 35f, 40f, 45f, 9f);
			List<Rigidbody> parts = new List<Rigidbody> { chest, headBody };
			foreach (string side in new string[] { "L", "R" })
			{
				Transform upper = Bone("UpperArm." + side);
				Transform lower = Bone("LowerArm." + side);
				Transform hand = Bone("Hand." + side);
				Transform middle = Bone("MiddleProximal." + side);
				if (!upper || !lower || !hand || !middle)
				{
					return null;
				}
				arms.Add((upper, upper.localScale));
				Rigidbody upperBody = Limb(side == "L" ? "Left Upper Arm" : "Right Upper Arm", upper.position, lower.position, scale * 0.055f, 2.2f);
				Rigidbody lowerBody = Limb(side == "L" ? "Left Forearm" : "Right Forearm", lower.position, hand.position, scale * 0.055f, 1.4f);
				Vector3 tip = hand.position + (middle.position - hand.position) * 1.6f;
				Rigidbody handBody = Part(side == "L" ? "Left Hand" : "Right Hand", hand.position, Along(tip - hand.position), 0.7f);
				SphereCollider palm = handBody.gameObject.AddComponent<SphereCollider>();
				palm.radius = scale * 0.06f;
				palm.center = handBody.transform.InverseTransformPoint(Vector3.Lerp(hand.position, tip, 0.6f));
				Joint(upperBody, chest, upper.position, 90f, 90f, 45f, 6f, 7f);
				Joint(lowerBody, upperBody, lower.position, 110f, 15f, 25f, 8f, 7f);
				Joint(handBody, lowerBody, hand.position, 50f, 50f, 30f, 25f, 8f);
				parts.AddRange(new Rigidbody[] { upperBody, lowerBody, handBody });
				limbs.Add((upper, upperBody.transform, Quaternion.Inverse(upperBody.rotation) * upper.rotation));
				limbs.Add((lower, lowerBody.transform, Quaternion.Inverse(lowerBody.rotation) * lower.rotation));
				limbs.Add((hand, handBody.transform, Quaternion.Inverse(handBody.rotation) * hand.rotation));
			}
			modelPosition = torso.InverseTransformPoint(model.position);
			modelRotation = Quaternion.Inverse(torso.rotation) * model.rotation;
			headRest = Quaternion.Inverse(headPart.rotation) * head.rotation;
			headLocalRest = head.localRotation;
			neckLocalRest = neck.localRotation;
			foreach (Transform t in GetComponentsInChildren<Transform>(true))
			{
				t.gameObject.layer = 2;
			}
			GorillaRagdoll ragdoll = gameObject.AddComponent<GorillaRagdoll>();
			ragdoll.torso = chest;
			ragdoll.parts = parts.ToArray();
			ragdoll.furRenderers = new Renderer[0];
			return ragdoll;

			Transform Bone(string name)
			{
				// Rebuilt from native code (token 0x060007D5, RVA 0x281A328)
				return bones.TryGetValue(name, out Transform bone) ? bone : null;
			}
		}

		private static Quaternion Along(Vector3 dir)
		{
			// Rebuilt from native code (token 0x060007CC, RVA 0x281AA6C)
			Vector3 up = dir.normalized;
			Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, up);
			if (forward.sqrMagnitude < 0.0001f)
			{
				forward = Vector3.ProjectOnPlane(Vector3.down, up);
			}
			return Quaternion.LookRotation(forward.normalized, up);
		}

		private Rigidbody Part(string name, Vector3 position, Quaternion rotation, float mass)
		{
			// Rebuilt from native code (token 0x060007CD, RVA 0x281A3A0)
			GameObject go = new GameObject(name);
			go.transform.SetParent(transform, false);
			go.transform.SetPositionAndRotation(position, rotation);
			Rigidbody body = go.AddComponent<Rigidbody>();
			body.mass = mass;
			body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			body.solverIterations = 16;
			body.solverVelocityIterations = 8;
			return body;
		}

		private Rigidbody Limb(string name, Vector3 from, Vector3 to, float radius, float mass)
		{
			// Rebuilt from native code (token 0x060007CE, RVA 0x281A8C4)
			Rigidbody body = Part(name, from, Along(to - from), mass);
			CapsuleCollider capsule = body.gameObject.AddComponent<CapsuleCollider>();
			float length = Vector3.Distance(from, to);
			capsule.direction = 1;
			capsule.radius = radius;
			capsule.height = radius * 2f + length;
			capsule.center = new Vector3(0f, length * 0.5f, 0f);
			return body;
		}

		private static void Joint(Rigidbody part, Rigidbody to, Vector3 pivot, float swing1, float swing2, float twist, float tone, float damper)
		{
			// Rebuilt from native code (token 0x060007CF, RVA 0x281A51C)
			ConfigurableJoint joint = part.gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = to;
			joint.anchor = part.transform.InverseTransformPoint(pivot);
			joint.axis = Vector3.up;
			joint.secondaryAxis = Vector3.right;
			joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
			joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
			joint.lowAngularXLimit = new SoftJointLimit { limit = -twist };
			joint.highAngularXLimit = new SoftJointLimit { limit = twist };
			joint.angularYLimit = new SoftJointLimit { limit = swing1 };
			joint.angularZLimit = new SoftJointLimit { limit = swing2 };
			SoftJointLimitSpring spring = new SoftJointLimitSpring { spring = part.mass * 400f, damper = part.mass * 30f };
			joint.angularYZLimitSpring = spring;
			joint.angularXLimitSpring = spring;
			joint.rotationDriveMode = RotationDriveMode.Slerp;
			joint.slerpDrive = new JointDrive { positionSpring = tone, positionDamper = damper, maximumForce = float.MaxValue, useAcceleration = true };
			joint.projectionMode = JointProjectionMode.PositionAndRotation;
			joint.projectionDistance = 0.03f;
			joint.projectionAngle = 15f;
		}

		public void ShowHead(bool show)
		{
			// Rebuilt from native code (token 0x060007D0, RVA 0x281AD8C)
			if (head)
			{
				head.localScale = show ? headScale : headScale * 0.0001f;
			}
		}

		public void ShowArms(bool show)
		{
			// Rebuilt from native code (token 0x060007D1, RVA 0x281AE48)
			foreach ((Transform bone, Vector3 scale) in arms)
			{
				if (bone)
				{
					bone.localScale = show ? scale : scale * 0.0001f;
				}
			}
		}

		private void LateUpdate()
		{
			// Rebuilt from native code (token 0x060007D2, RVA 0x281B004)
			if (!torso || !model)
			{
				return;
			}
			model.SetPositionAndRotation(torso.TransformPoint(modelPosition), torso.rotation * modelRotation);
			if (headPart)
			{
				neck.localRotation = neckLocalRest;
				Quaternion look = headPart.rotation * headRest;
				neck.rotation = Quaternion.Slerp(Quaternion.identity, look * Quaternion.Inverse(neck.rotation), 0.5f) * neck.rotation;
				head.rotation = look;
			}
			foreach ((Transform bone, Transform part, Quaternion rest) in limbs)
			{
				if (part)
				{
					bone.rotation = part.rotation * rest;
				}
			}
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060007D3, RVA 0x281B654)
			if (material)
			{
				Destroy(material);
			}
		}
	}
}
