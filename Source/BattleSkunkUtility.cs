using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Skunks;

public static class BattleSkunkUtility
{
	private static TrainabilityDef cachedOriginalTrainability;

	private static List<TrainableDef> cachedOriginalSpecialTrainables;

	private static bool cachedOriginalCaptured;

	public static void ApplyRaceChanges()
	{
		ThingDef skunk = SkunksDefOf.Skunk;
		if (skunk?.race == null)
		{
			return;
		}
		CaptureOriginalRace(skunk);
		if (SkunksMod.Settings != null && SkunksMod.Settings.battleSkunks)
		{
			skunk.race.trainability = TrainabilityDefOf.Intermediate;
			TrainableDef attackTargetTrainable = GetAttackTargetTrainable();
			if (attackTargetTrainable != null)
			{
				if (skunk.race.specialTrainables == null)
				{
					skunk.race.specialTrainables = new List<TrainableDef>();
				}
				if (!skunk.race.specialTrainables.Contains(attackTargetTrainable))
				{
					skunk.race.specialTrainables.Add(attackTargetTrainable);
				}
			}
		}
		else
		{
			skunk.race.trainability = cachedOriginalTrainability ?? TrainabilityDefOf.None;
			skunk.race.specialTrainables = CloneSpecialTrainables(cachedOriginalSpecialTrainables);
		}
	}

	public static bool IsBattleSkunk(Pawn skunk)
	{
		if (skunk == null || SkunksDefOf.Skunk == null || skunk.def != SkunksDefOf.Skunk)
		{
			return false;
		}
		if (SkunksMod.Settings == null || !SkunksMod.Settings.battleSkunks)
		{
			return false;
		}
		if (skunk.Faction == null || !skunk.Faction.IsPlayer)
		{
			return false;
		}
		if (skunk.training == null)
		{
			return false;
		}
		if (skunk.training.HasLearned(TrainableDefOf.Release))
		{
			return true;
		}
		TrainableDef attackTargetTrainable = GetAttackTargetTrainable();
		if (attackTargetTrainable != null)
		{
			return skunk.training.HasLearned(attackTargetTrainable);
		}
		return false;
	}

	public static bool IsInCombat(Pawn skunk)
	{
		if (skunk?.mindState == null)
		{
			return false;
		}
		if (skunk.mindState.enemyTarget != null && !skunk.mindState.enemyTarget.Destroyed)
		{
			return true;
		}
		if (skunk.MentalStateDef == MentalStateDefOf.Manhunter || skunk.MentalStateDef == MentalStateDefOf.ManhunterPermanent)
		{
			return true;
		}
		return false;
	}

	public static Pawn GetCombatTarget(Pawn skunk)
	{
		if (skunk?.mindState?.enemyTarget is Pawn { Destroyed: false } pawn)
		{
			return pawn;
		}
		if ((skunk?.playerSettings?.Master)?.mindState?.enemyTarget is Pawn { Destroyed: false } pawn2)
		{
			return pawn2;
		}
		return null;
	}

	private static void CaptureOriginalRace(ThingDef skunk)
	{
		if (!cachedOriginalCaptured)
		{
			cachedOriginalTrainability = skunk.race.trainability;
			cachedOriginalSpecialTrainables = CloneSpecialTrainables(skunk.race.specialTrainables);
			cachedOriginalCaptured = true;
		}
	}

	private static List<TrainableDef> CloneSpecialTrainables(List<TrainableDef> source)
	{
		if (source == null || source.Count == 0)
		{
			return null;
		}
		return new List<TrainableDef>(source);
	}

	private static TrainableDef GetAttackTargetTrainable()
	{
		if (!ModsConfig.OdysseyActive)
		{
			return null;
		}
		return DefDatabase<TrainableDef>.GetNamedSilentFail("AttackTarget");
	}
}
