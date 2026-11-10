using System.Collections.Generic;
using UnityEngine;

namespace Shallows
{
	public class DeathSequence : MonoBehaviour
	{
		[Tooltip("An inactive ragdoll gorilla that gets copied for each death.")]
		public GorillaRagdoll ragdollTemplate;

		[Tooltip("Bodies float this long after landing before they sink away (s).")]
		public float bodyLifetime = 25f;

		public int maxBodies = 6;

		private readonly Dictionary<int, GorillaRagdoll> bodies = new Dictionary<int, GorillaRagdoll>();

		private readonly List<GorillaRagdoll> order = new List<GorillaRagdoll>();

		public static DeathSequence I { get; private set; }

		private void Awake()
		{
			// Rebuilt from native code (token 0x06000183, RVA 0x2728BB0)
			I = this;
		}

		public GorillaRagdoll BodyOf(int playerId)
		{
			// Rebuilt from native code (token 0x06000184, RVA 0x2728C08)
			if (bodies.TryGetValue(playerId, out GorillaRagdoll body) && body)
			{
				return body;
			}
			return null;
		}

		public GorillaRagdoll SpawnLocal(MonsterAI monster, Vector3 bodyBottom, Kill kill)
		{
			// Rebuilt from native code (token 0x06000185, RVA 0x2728CC0)
			return Spawn(monster, bodyBottom, PlayerProfile.Color, NetBridge.LocalPlayerId, true, kill);
		}

		public GorillaRagdoll SpawnRemote(MonsterAI monster, Vector3 bodyBottom, Color fur, int playerId, Kill kill)
		{
			// Rebuilt from native code (token 0x06000186, RVA 0x27292E8)
			if (NetBridge.LocalPlayerId == playerId)
			{
				return null;
			}
			return Spawn(monster, bodyBottom, fur, playerId, false, kill);
		}

		private GorillaRagdoll Spawn(MonsterAI monster, Vector3 bodyBottom, Color fur, int playerId, bool local, Kill kill)
		{
			// Rebuilt from native code (token 0x06000187, RVA 0x2728DC0)
			if (!monster)
			{
				return null;
			}
			if (bodies.TryGetValue(playerId, out GorillaRagdoll previous) && previous)
			{
				order.Remove(previous);
				Destroy(previous.gameObject);
			}
			while (order.Count >= maxBodies)
			{
				if (order[0])
				{
					Destroy(order[0].gameObject);
				}
				order.RemoveAt(0);
			}
			Vector3 position = bodyBottom + Vector3.up * 0.25f;
			GorillaRagdoll body = ApeRagdoll.Create(position, fur);
			if (!body)
			{
				if (!ragdollTemplate)
				{
					return null;
				}
				body = Instantiate(ragdollTemplate, position, Quaternion.identity);
			}
			body.name = local ? "Your Body" : "Body of player " + playerId;
			body.gameObject.SetActive(true);
			body.Begin(monster, fur, local, bodyLifetime, kill);
			bodies[playerId] = body;
			order.Add(body);
			return body;
		}
	}
}
