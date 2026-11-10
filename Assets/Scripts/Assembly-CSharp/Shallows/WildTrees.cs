using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Shallows
{
	[DefaultExecutionOrder(-4)]
	public class WildTrees : MonoBehaviour
	{
		public const int None = 0;

		public const int Growing = 1;

		public const int Taken = 2;

		public const int Gone = 3;

		public const float RollEvery = 30f;

		public const float Chance = 0.07f;

		public const float RestAfterTree = 180f;

		public const float Lifetime = 600f;

		public const float AfterTaken = 10f;

		private const float FirstRollAfter = 60f;

		private const float ShareEvery = 15f;

		private const float AnswerWait = 5f;

		private TreeSnapshot state;

		private float bornAt;

		private float takenAt;

		private float nextRoll;

		private float nextShare;

		private float rippedAt = -1f;

		private bool paidFor;

		private WildTree tree;

		private int nextId;

		public static WildTrees I { get; private set; }

		public WildTree Tree
		{
			get
			{
				// Rebuilt from native code (token 0x060003C5, RVA 0x279E750)
				return tree;
			}
		}

		private bool Alive
		{
			get
			{
				// Rebuilt from native code (token 0x060003C8, RVA 0x279E8B8)
				return state.state == Growing || state.state == Taken;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Boot()
		{
			// Rebuilt from native code (token 0x060003C0, RVA 0x279E178)
			if (I)
			{
				return;
			}
			GameManager game = GameManager.I ? GameManager.I : FindAnyObjectByType<GameManager>();
			if (!game || game.levels == null || game.levels.Length == 0)
			{
				return;
			}
			new GameObject("Wild Trees").AddComponent<WildTrees>();
		}

		private void Awake()
		{
			// Rebuilt from native code (token 0x060003C1, RVA 0x279E3B8)
			I = this;
			nextId = Random.Range(1, 0x100000);
			nextRoll = NetBridge.SharedTime + FirstRollAfter;
		}

		private void OnEnable()
		{
			// Rebuilt from native code (token 0x060003C2, RVA 0x279E47C)
			NetBridge.RoomJoined += OnRoomJoined;
			NetBridge.RoomGrew += CatchUp;
		}

		private void OnDisable()
		{
			// Rebuilt from native code (token 0x060003C3, RVA 0x279E578)
			NetBridge.RoomJoined -= OnRoomJoined;
			NetBridge.RoomGrew -= CatchUp;
		}

		private void OnDestroy()
		{
			// Rebuilt from native code (token 0x060003C4, RVA 0x279E674)
			if (I == this)
			{
				I = null;
			}
		}

		private void OnRoomJoined(bool fresh)
		{
			// Rebuilt from native code (token 0x060003C6, RVA 0x279E758)
			if (tree)
			{
				tree.Sink(true);
			}
			tree = null;
			state = default;
			paidFor = false;
			rippedAt = -1f;
			nextRoll = NetBridge.SharedTime + FirstRollAfter;
		}

		private void CatchUp()
		{
			// Rebuilt from native code (token 0x060003C7, RVA 0x279E838)
			if (NetBridge.MonsterAuthority && Alive)
			{
				Share();
			}
		}

		private void Update()
		{
			// Rebuilt from native code (token 0x060003C9, RVA 0x279E970)
			float now = NetBridge.SharedTime;
			if (rippedAt >= 0f && Time.time - rippedAt > AnswerWait && state.state == Growing && tree && tree.RippedHere)
			{
				rippedAt = -1f;
				tree.Taken(false);
			}
			if (!NetBridge.MonsterAuthority)
			{
				return;
			}
			switch (state.state)
			{
			case Taken:
				if (now - takenAt > AfterTaken)
				{
					Set(Gone, -1);
					return;
				}
				break;
			case Growing:
				if (now - bornAt > Lifetime)
				{
					Set(Gone, -1);
					return;
				}
				break;
			default:
				if (now >= nextRoll)
				{
					nextRoll = now + RollEvery;
					if (Random.value < Chance)
					{
						GrowSomewhere();
					}
				}
				return;
			}
			if (now >= nextShare)
			{
				Share();
			}
		}

		public bool GrowSomewhere(int level = -1)
		{
			// Rebuilt from native code (token 0x060003CA, RVA 0x279EC24)
			GameManager game = GameManager.I;
			if (!NetBridge.MonsterAuthority || !game || game.levels == null)
			{
				return false;
			}
			int count = game.levels.Length;
			if (count == 0)
			{
				return false;
			}
			for (int attempt = 0; attempt < 4; attempt++)
			{
				int pick = level < 0 ? Random.Range(0, count) : Mathf.Min(level, count - 1);
				if (WildTreeSpots.Find(game, pick, out Vector3 at, out Vector3 branch))
				{
					GrowAt(pick, at, branch);
					return true;
				}
			}
			return false;
		}

		public void GrowAt(int level, Vector3 at, Vector3 branch)
		{
			// Rebuilt from native code (token 0x060003CB, RVA 0x279F2E4)
			if (!NetBridge.MonsterAuthority)
			{
				return;
			}
			if (tree)
			{
				tree.Sink(true);
			}
			tree = null;
			state = new TreeSnapshot
			{
				id = nextId++,
				level = level,
				position = at,
				yaw = MathF.Atan2(branch.x, branch.z) * Mathf.Rad2Deg,
				state = Growing,
				winner = -1,
				age = 0f
			};
			bornAt = NetBridge.SharedTime;
			rippedAt = -1f;
			paidFor = false;
			Show(state);
			Share();
			Debug.Log(string.Format("Shallows: a wild banana tree is growing in level {0} at {1}.", level + 1, at));
		}

		private void Decide(int player)
		{
			// Rebuilt from native code (token 0x060003CC, RVA 0x279F98C)
			if (state.state == Growing)
			{
				Set(Taken, player);
			}
		}

		private void Set(int newState, int winner)
		{
			// Rebuilt from native code (token 0x060003CD, RVA 0x279EB48)
			state.state = newState;
			state.winner = winner;
			if (newState == Gone)
			{
				nextRoll = NetBridge.SharedTime + RestAfterTree;
			}
			else if (newState == Taken)
			{
				takenAt = NetBridge.SharedTime;
			}
			Show(state);
			Share();
		}

		private void Share()
		{
			// Rebuilt from native code (token 0x060003CE, RVA 0x279E8CC)
			nextShare = NetBridge.SharedTime + ShareEvery;
			NetBridge.ShareTree(state);
		}

		public void RemoteRip(int id, int player, Func<Vector3, bool> near)
		{
			// Rebuilt from native code (token 0x060003CF, RVA 0x279F9A8)
			if (!NetBridge.MonsterAuthority || state.state != Growing || state.id != id || !tree)
			{
				return;
			}
			if (near(tree.BunchPosition))
			{
				Decide(player);
			}
		}

		public void Receive(TreeSnapshot s)
		{
			// Rebuilt from native code (token 0x060003D0, RVA 0x279FAC8)
			if (NetBridge.MonsterAuthority || s.state < None || s.state > Gone)
			{
				return;
			}
			if (s.id != state.id)
			{
				paidFor = false;
				rippedAt = -1f;
			}
			else if (s.state < state.state)
			{
				return;
			}
			bornAt = NetBridge.SharedTime - Mathf.Max(0f, s.age);
			if (s.state == Taken && (s.id != state.id || state.state != Taken))
			{
				takenAt = NetBridge.SharedTime;
			}
			state = s;
			Show(s);
		}

		private void Show(TreeSnapshot s)
		{
			// Rebuilt from native code (token 0x060003D1, RVA 0x279F530)
			if (tree && (!tree.isActiveAndEnabled || tree.Level != s.level || (s.position - tree.transform.position).sqrMagnitude > 0.01f))
			{
				tree.Sink(true);
				tree = null;
			}
			if (s.state == None)
			{
				return;
			}
			if (s.state == Gone)
			{
				if (tree)
				{
					tree.Sink(false);
				}
				tree = null;
				return;
			}
			if (!tree)
			{
				float age = 0f;
				if (!NetBridge.MonsterAuthority)
				{
					age = s.age < 0f ? 0f : (s.age <= 1f ? s.age : Mathf.Max(s.age, WildTree.GrowTime));
				}
				tree = WildTree.Grow(s.level, s.position, Quaternion.Euler(0f, s.yaw, 0f) * Vector3.forward, age, s.state == Growing);
				if (tree)
				{
					tree.LocalRip = OnLocalRip;
				}
				return;
			}
			if (s.state == Taken)
			{
				bool mine = s.winner == NetBridge.LocalPlayerId;
				Vector3 bunch = tree.BunchPosition;
				tree.Taken(mine);
				if (mine)
				{
					Pay(s, bunch);
				}
				rippedAt = -1f;
			}
		}

		private void OnLocalRip()
		{
			// Rebuilt from native code (token 0x060003D2, RVA 0x279FD80)
			rippedAt = Time.time;
			if (!NetBridge.MonsterAuthority)
			{
				NetBridge.ReportTreeRip(state.id);
				return;
			}
			if (state.state == Growing)
			{
				Set(Taken, NetBridge.LocalPlayerId);
			}
		}

		private void Pay(TreeSnapshot s, Vector3 bunch)
		{
			// Rebuilt from native code (token 0x060003D3, RVA 0x279FC14)
			if (paidFor)
			{
				return;
			}
			paidFor = true;
			RewardPop pop = RewardPop.Show(bunch, Wallet.TreeReward);
			Wallet.Tree(s.level, (int paid, string note) =>
			{
				// Rebuilt from native code (token 0x060003D6, RVA 0x279FE60)
				if (paid != Wallet.TreeReward)
				{
					if (paid == 0)
					{
						if (GameManager.I)
						{
							GameManager.I.PlayLocal(Sfx.ShopDenied(), 0.35f, 1f);
						}
						if (pop)
						{
							pop.Refuse(note);
						}
					}
					else if (pop)
					{
						pop.Paid(paid);
					}
				}
				Debug.Log(string.Format("Shallows: wild tree paid {0} bananas", paid) + (string.IsNullOrEmpty(note) ? "" : " (" + note.Replace('\n', ' ') + ")"));
			});
		}
	}
}
