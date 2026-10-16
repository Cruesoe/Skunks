using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Skunks;

public class CompSkunkGas : ThingComp
{
	private const int AnimalStunTicks = 180;

	private const int ThreatMemoryTicks = 600;

	private const int FleeDistance = 32;

	private const int FaceAwayHoldTicks = 45;

	private const int CombatSprayInterval = 60;

	private int spraysRemaining = -1;

	private int lastSprayTick = -999999;

	private float regenTicks;

	private Thing lastThreat;

	private int faceAwayUntilTick = -1;

	private Rot4 faceAwayRot = Rot4.Invalid;

	private BodyPartRecord cachedAnus;

	private readonly HashSet<Pawn> sprayAffectedScratch = new HashSet<Pawn>();

	public CompProperties_SkunkGas Props => (CompProperties_SkunkGas)props;

	public int MaxSprays => Props.maxSprays;

	public int SpraysRemaining
	{
		get
		{
			EnsureInitialized();
			return spraysRemaining;
		}
	}

	public int TicksPerSpray => Mathf.Max(1, Mathf.RoundToInt(Props.regenDays * 60000f / (float)Mathf.Max(1, Props.maxSprays)));

	public int TicksUntilNextSpray
	{
		get
		{
			EnsureInitialized();
			if (spraysRemaining >= Props.maxSprays)
			{
				return 0;
			}
			return Mathf.Max(0, Mathf.CeilToInt((float)TicksPerSpray - regenTicks));
		}
	}

	public int CooldownTicksRemaining
	{
		get
		{
			int num = lastSprayTick + Props.cooldownTicks;
			return Mathf.Max(0, num - Find.TickManager.TicksGame);
		}
	}

	public bool CanSpray
	{
		get
		{
			EnsureInitialized();
			if (spraysRemaining <= 0 || CooldownTicksRemaining > 0)
			{
				return false;
			}
			if (!(parent is Pawn { Dead: false } pawn) || pawn.health?.hediffSet == null)
			{
				return false;
			}
			BodyPartRecord anus = GetAnus(pawn);
			if (anus == null || pawn.health.hediffSet.PartIsMissing(anus))
			{
				cachedAnus = null;
				return false;
			}
			return pawn.health.hediffSet.HasHediff(SkunksDefOf.StinkGlands);
		}
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		base.PostSpawnSetup(respawningAfterLoad);
		EnsureInitialized();
		EnsureStinkGlands();
	}

	public override void CompTickInterval(int delta)
	{
		base.CompTickInterval(delta);
		TickFaceAwayPose();
		TickRegen(delta);
		if (parent is Pawn pawn)
		{
			if (pawn.IsHashIntervalTick(30, delta))
			{
				UpdateFleeOrFight(pawn);
			}
			if (pawn.IsHashIntervalTick(60, delta))
			{
				TryCombatSpray(pawn);
			}
		}
	}

	private void TickFaceAwayPose()
	{
		if (faceAwayUntilTick >= 0)
		{
			if (Find.TickManager.TicksGame > faceAwayUntilTick)
			{
				faceAwayUntilTick = -1;
			}
			else if (faceAwayRot.IsValid && parent is Pawn { Dead: false } pawn)
			{
				pawn.Rotation = faceAwayRot;
			}
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.Look(ref spraysRemaining, "spraysRemaining", -1);
		Scribe_Values.Look(ref lastSprayTick, "lastSprayTick", -999999);
		Scribe_Values.Look(ref regenTicks, "regenTicks", 0f);
		Scribe_References.Look(ref lastThreat, "lastThreat");
	}

	public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
	{
		base.PostPostApplyDamage(dinfo, totalDamageDealt);
		if (parent is Pawn { Dead: false, Spawned: not false } pawn)
		{
			Thing instigator = dinfo.Instigator;
			if (instigator != null && instigator != pawn)
			{
				lastThreat = instigator;
			}
			if (!IsRangedDamage(dinfo, pawn))
			{
				TryFireSpray(pawn, dinfo.Instigator as Pawn);
			}
			UpdateFleeOrFight(pawn, forceRestartFlee: true);
		}
	}

	private bool TryFireSpray(Pawn skunk, Pawn target)
	{
		if (!CanSpray)
		{
			return false;
		}
		if (!ReleaseStinkCloud(skunk, target))
		{
			return false;
		}
		spraysRemaining--;
		lastSprayTick = Find.TickManager.TicksGame;
		PlaySpraySound(skunk);
		return true;
	}

	private void TryCombatSpray(Pawn skunk)
	{
		if (BattleSkunkUtility.IsBattleSkunk(skunk) && BattleSkunkUtility.IsInCombat(skunk) && CanSpray)
		{
			Pawn combatTarget = BattleSkunkUtility.GetCombatTarget(skunk);
			if (combatTarget != null && combatTarget.Spawned && combatTarget.Map == skunk.Map && skunk.Position.InHorDistOf(combatTarget.Position, Props.radius))
			{
				TryFireSpray(skunk, combatTarget);
			}
		}
	}

	private static void PlaySpraySound(Pawn skunk)
	{
		SoundDef skunk_Spray = SkunksDefOf.Skunk_Spray;
		if (skunk_Spray != null && skunk.Spawned)
		{
			skunk_Spray.PlayOneShot(skunk);
		}
	}

	private static bool IsRangedDamage(DamageInfo dinfo, Pawn skunk)
	{
		if (dinfo.Weapon != null && dinfo.Weapon.IsRangedWeapon)
		{
			return true;
		}
		if (dinfo.Instigator is Projectile)
		{
			return true;
		}
		Thing instigator = dinfo.Instigator;
		if (instigator != null && instigator != skunk && instigator.Spawned && instigator.Map == skunk.Map && !instigator.Position.AdjacentTo8WayOrInside(skunk.Position))
		{
			return true;
		}
		return false;
	}

	private void UpdateFleeOrFight(Pawn skunk, bool forceRestartFlee = false)
	{
		EnsureInitialized();
		if (!skunk.Spawned || skunk.Dead || skunk.Downed || skunk.jobs == null)
		{
			return;
		}
		if (skunk.InAggroMentalState || (BattleSkunkUtility.IsBattleSkunk(skunk) && BattleSkunkUtility.IsInCombat(skunk)))
		{
			StopFleeing(skunk);
		}
		else if (spraysRemaining > 0)
		{
			if (!IsThreatened(skunk))
			{
				return;
			}
			Thing fleeDanger = GetFleeDanger(skunk);
			if (fleeDanger == null)
			{
				return;
			}
			skunk.mindState.enemyTarget = null;
			if (skunk.CurJobDef != JobDefOf.Flee || skunk.CurJob == null || skunk.CurJob.targetB.Thing != fleeDanger || forceRestartFlee)
			{
				Job job = FleeUtility.FleeJob(skunk, fleeDanger, 32);
				if (job != null)
				{
					skunk.jobs.StartJob(job, JobCondition.InterruptForced);
				}
				else
				{
					skunk.mindState.StartFleeingBecauseOfPawnAction(fleeDanger);
				}
			}
		}
		else if (skunk.CurJobDef == JobDefOf.Flee || skunk.MentalStateDef == MentalStateDefOf.PanicFlee)
		{
			if (skunk.CurJobDef == JobDefOf.Flee)
			{
				skunk.jobs.EndCurrentJob(JobCondition.InterruptForced);
			}
			if (skunk.MentalStateDef == MentalStateDefOf.PanicFlee)
			{
				skunk.MentalState.RecoverFromState();
			}
		}
	}

	private static void StopFleeing(Pawn skunk)
	{
		if (skunk.CurJobDef == JobDefOf.Flee)
		{
			skunk.jobs.EndCurrentJob(JobCondition.InterruptForced);
		}
		if (skunk.MentalStateDef == MentalStateDefOf.PanicFlee)
		{
			skunk.MentalState.RecoverFromState();
		}
	}

	private Thing GetFleeDanger(Pawn skunk)
	{
		if (lastThreat != null && !lastThreat.Destroyed && lastThreat.Spawned && lastThreat.Map == skunk.Map)
		{
			return lastThreat;
		}
		Thing thing = skunk.mindState?.enemyTarget;
		if (thing != null && !thing.Destroyed)
		{
			return thing;
		}
		return null;
	}

	private bool IsThreatened(Pawn skunk)
	{
		Pawn_MindState mindState = skunk.mindState;
		if (mindState == null)
		{
			return false;
		}
		if (Find.TickManager.TicksGame < mindState.lastHarmTick + 600)
		{
			return true;
		}
		if (lastThreat != null && !lastThreat.Destroyed && lastThreat.Spawned && skunk.Position.InHorDistOf(lastThreat.Position, 18f))
		{
			return true;
		}
		if (mindState.enemyTarget != null && !mindState.enemyTarget.Destroyed)
		{
			return true;
		}
		return false;
	}

	private void TickRegen(int delta)
	{
		EnsureInitialized();
		if (spraysRemaining >= Props.maxSprays)
		{
			regenTicks = 0f;
		}
		else if (!(parent is Pawn { Dead: not false }))
		{
			regenTicks += delta;
			int ticksPerSpray = TicksPerSpray;
			while (regenTicks >= (float)ticksPerSpray && spraysRemaining < Props.maxSprays)
			{
				regenTicks -= ticksPerSpray;
				spraysRemaining++;
			}
			if (spraysRemaining >= Props.maxSprays)
			{
				regenTicks = 0f;
			}
		}
	}

	private void EnsureInitialized()
	{
		if (spraysRemaining < 0)
		{
			spraysRemaining = Props.maxSprays;
		}
	}

	private void EnsureStinkGlands()
	{
		if (!(parent is Pawn pawn) || pawn.health?.hediffSet == null)
		{
			return;
		}
		HediffDef stinkGlands = SkunksDefOf.StinkGlands;
		if (stinkGlands != null && !pawn.health.hediffSet.HasHediff(stinkGlands))
		{
			BodyPartRecord anus = GetAnus(pawn);
			if (anus != null && !pawn.health.hediffSet.PartIsMissing(anus))
			{
				pawn.health.AddHediff(stinkGlands, anus);
			}
		}
	}

	private BodyPartRecord GetAnus(Pawn skunk)
	{
		if (cachedAnus != null)
		{
			return cachedAnus;
		}
		BodyPartDef anusDef = SkunksDefOf.Anus;
		if (anusDef == null)
		{
			return null;
		}
		cachedAnus = skunk.RaceProps.body.AllParts.Find((BodyPartRecord p) => p.def == anusDef);
		return cachedAnus;
	}

	private bool ReleaseStinkCloud(Pawn skunk, Pawn attacker)
	{
		Map map = skunk.Map;
		ThingDef skunkGas = SkunksDefOf.SkunkGas;
		if (skunkGas == null || map == null)
		{
			return false;
		}
		IntVec3 position = skunk.Position;
		if (!TryGetAimDirection(position, attacker, skunk, map, out var aimDir))
		{
			return false;
		}
		bool flag = skunk.InAggroMentalState || BattleSkunkUtility.IsBattleSkunk(skunk);
		Thing thing = ((!flag) ? null : skunk.mindState?.enemyTarget);
		skunk.stances?.CancelBusyStanceHard();
		skunk.mindState.enemyTarget = null;
		faceAwayRot = Rot4.FromAngleFlat(aimDir.AngleFlat()).Opposite;
		skunk.Rotation = faceAwayRot;
		faceAwayUntilTick = Find.TickManager.TicksGame + 45;
		bool result = false;
		sprayAffectedScratch.Clear();
		foreach (IntVec3 item in GenRadial.RadialCellsAround(position, Props.radius, useCenter: false))
		{
			if (item.InBounds(map) && !item.Impassable(map) && IsInSprayCone(position, item, aimDir))
			{
				SpawnGas(skunkGas, item, map);
				AffectPawnsAt(item, map, skunk, sprayAffectedScratch);
				result = true;
			}
		}
		if (attacker != null && attacker.Spawned && attacker.Map == map && attacker.Position.InHorDistOf(position, Props.radius) && !attacker.Position.Impassable(map) && IsInSprayCone(position, attacker.Position, aimDir))
		{
			SpawnGas(skunkGas, attacker.Position, map);
			AffectPawn(attacker, skunk, sprayAffectedScratch);
			result = true;
		}
		if (flag && thing != null && !thing.Destroyed)
		{
			skunk.mindState.enemyTarget = thing;
		}
		return result;
	}

	private static void AffectPawnsAt(IntVec3 cell, Map map, Pawn skunk, HashSet<Pawn> already)
	{
		List<Thing> thingList = cell.GetThingList(map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (thingList[i] is Pawn pawn)
			{
				AffectPawn(pawn, skunk, already);
			}
		}
	}

	private static void AffectPawn(Pawn pawn, Pawn skunk, HashSet<Pawn> already)
	{
		if (pawn != null && !pawn.Dead && pawn != skunk && already.Add(pawn))
		{
			if (pawn.RaceProps.Humanlike)
			{
				TryCoatHuman(pawn);
			}
			else if (pawn.RaceProps.Animal)
			{
				StunAnimal(pawn);
			}
		}
	}

	private static void TryCoatHuman(Pawn pawn)
	{
		if (pawn.health?.hediffSet != null && !(pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance) >= 1f))
		{
			HediffDef skunkStinkHediff = SkunksDefOf.SkunkStinkHediff;
			if (skunkStinkHediff != null && !pawn.health.hediffSet.HasHediff(skunkStinkHediff))
			{
				pawn.health.AddHediff(skunkStinkHediff);
			}
		}
	}

	public static void StunAnimal(Pawn pawn)
	{
		if (pawn?.stances?.stunner != null && !pawn.Dead && !pawn.Downed && (SkunksDefOf.Skunk == null || pawn.def != SkunksDefOf.Skunk))
		{
			pawn.stances.stunner.StunFor(180, null, addBattleLog: false);
		}
	}

	private bool TryGetAimDirection(IntVec3 origin, Pawn attacker, Pawn skunk, Map map, out Vector3 aimDir)
	{
		if (TryAimAt(origin, attacker, map, out aimDir))
		{
			return true;
		}
		if (TryAimAt(origin, lastThreat as Pawn, map, out aimDir))
		{
			return true;
		}
		if (lastThreat != null && !lastThreat.Destroyed && lastThreat.Spawned && lastThreat.Map == map && lastThreat.Position != origin)
		{
			aimDir = (lastThreat.Position - origin).ToVector3().Yto0();
			if (aimDir.sqrMagnitude >= 0.001f)
			{
				aimDir.Normalize();
				return true;
			}
		}
		aimDir = skunk.Rotation.FacingCell.ToVector3().Yto0();
		if (aimDir.sqrMagnitude < 0.001f)
		{
			return false;
		}
		aimDir.Normalize();
		return true;
	}

	private static bool TryAimAt(IntVec3 origin, Pawn target, Map map, out Vector3 aimDir)
	{
		aimDir = Vector3.zero;
		if (target == null || !target.Spawned || target.Map != map || target.Position == origin)
		{
			return false;
		}
		aimDir = (target.Position - origin).ToVector3().Yto0();
		if (aimDir.sqrMagnitude < 0.001f)
		{
			return false;
		}
		aimDir.Normalize();
		return true;
	}

	private bool IsInSprayCone(IntVec3 origin, IntVec3 cell, Vector3 aimDir)
	{
		Vector3 rhs = (cell - origin).ToVector3().Yto0();
		float sqrMagnitude = rhs.sqrMagnitude;
		if (sqrMagnitude < 0.001f)
		{
			return false;
		}
		if (sqrMagnitude > Props.radius * Props.radius)
		{
			return false;
		}
		rhs.Normalize();
		return Vector3.Dot(aimDir, rhs) >= Props.minAimDot;
	}

	private static void SpawnGas(ThingDef gasDef, IntVec3 cell, Map map)
	{
		GenSpawn.Spawn(ThingMaker.MakeThing(gasDef), cell, map);
	}
}
