using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Skunks;

public class Hediff_SkunkStink : HediffWithComps
{
	private const int CheckInterval = 180;

	private const float OutdoorSpeedFactor = 1.1f;

	private const float IndoorSlowFactor = 0.9f;

	private const float ResistanceSpeedFactor = 2.5f;

	private const float GasRefreshFraction = 0.05f;

	private const float NearbyRadius = 3f;

	private int ticksLeft = -1;

	private int consecutiveOutsideTicks;

	private int consecutiveInsideTicks;

	private bool outdoorSpeedActive;

	private bool indoorSlowActive;

	private bool slowCleanseActive;

	public static int BaseDurationTicks => 300000;

	public static int EnvironmentThresholdTicks => 2500;

	public static int SlowCleanseDurationTicks => 60000;

	public int TicksLeft => Mathf.Max(0, ticksLeft);

	public override string LabelInBrackets
	{
		get
		{
			EnsureTicksInitialized();
			return ticksLeft.ToStringTicksToPeriod();
		}
	}

	public override string TipStringExtra
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			string tipStringExtra = base.TipStringExtra;
			if (!tipStringExtra.NullOrEmpty())
			{
				stringBuilder.AppendLine(tipStringExtra);
			}
			EnsureTicksInitialized();
			stringBuilder.Append("Time remaining: ");
			stringBuilder.AppendLine(ticksLeft.ToStringTicksToPeriod());
			if (slowCleanseActive)
			{
				stringBuilder.AppendLine("Being cleansed (herbal/milk treatment).");
			}
			if (outdoorSpeedActive)
			{
				stringBuilder.AppendLine("Outdoors long enough: wearing off 10% faster.");
			}
			else if (indoorSlowActive)
			{
				stringBuilder.AppendLine("Indoors long enough: wearing off 10% slower.");
			}
			return stringBuilder.ToString().TrimEnd(Array.Empty<char>());
		}
	}

	public override void PostAdd(DamageInfo? dinfo)
	{
		base.PostAdd(dinfo);
		if (ticksLeft < 0)
		{
			ticksLeft = BaseDurationTicks;
		}
		SyncSeverityFromTicks();
		ThoughtDef skunkStinkNearby = SkunksDefOf.SkunkStinkNearby;
		if (skunkStinkNearby != null)
		{
			pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(skunkStinkNearby);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref ticksLeft, "ticksLeft", -1);
		Scribe_Values.Look(ref consecutiveOutsideTicks, "consecutiveOutsideTicks", 0);
		Scribe_Values.Look(ref consecutiveInsideTicks, "consecutiveInsideTicks", 0);
		Scribe_Values.Look(ref outdoorSpeedActive, "outdoorSpeedActive", defaultValue: false);
		Scribe_Values.Look(ref indoorSlowActive, "indoorSlowActive", defaultValue: false);
		Scribe_Values.Look(ref slowCleanseActive, "slowCleanseActive", defaultValue: false);
	}

	public override void PostTickInterval(int delta)
	{
		base.PostTickInterval(delta);
		if (!pawn.IsHashIntervalTick(180, delta) || !pawn.Spawned || pawn.Dead)
		{
			return;
		}
		EnsureTicksInitialized();
		ApplyNearbyStinkThoughts();
		Map map = pawn.Map;
		IntVec3 position = pawn.Position;
		if (map == null || !position.InBounds(map))
		{
			return;
		}
		if (IsStandingInSkunkGas(position, map) && pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance) < 1f)
		{
			int num = Mathf.RoundToInt((float)BaseDurationTicks * 0.05f);
			ticksLeft = Mathf.Min(BaseDurationTicks, ticksLeft + num);
			SyncSeverityFromTicks();
			return;
		}
		UpdateEnvironmentState(pawn);
		float decayRateFactor = GetDecayRateFactor(pawn);
		ticksLeft -= Mathf.Max(1, Mathf.RoundToInt(180f * decayRateFactor));
		if (ticksLeft <= 0)
		{
			ticksLeft = 0;
			Severity = 0f;
		}
		else
		{
			SyncSeverityFromTicks();
		}
	}

	public void RemoveImmediately()
	{
		ticksLeft = 0;
		Severity = 0f;
	}

	public void StartSlowCleanse(int durationTicks)
	{
		EnsureTicksInitialized();
		ticksLeft = Mathf.Min(ticksLeft, Mathf.Max(1, durationTicks));
		slowCleanseActive = true;
		SyncSeverityFromTicks();
	}

	public override bool TendableNow(bool ignoreTimer = false)
	{
		return false;
	}

	private void ApplyNearbyStinkThoughts()
	{
		ThoughtDef skunkStinkNearby = SkunksDefOf.SkunkStinkNearby;
		HediffDef skunkStinkHediff = SkunksDefOf.SkunkStinkHediff;
		Map map = base.pawn.Map;
		if (skunkStinkNearby == null || map == null)
		{
			return;
		}
		IntVec3 position = base.pawn.Position;
		int num = GenRadial.NumCellsInRadius(3f);
		for (int i = 0; i < num; i++)
		{
			IntVec3 c = position + GenRadial.RadialPattern[i];
			if (!c.InBounds(map))
			{
				continue;
			}
			List<Thing> thingList = c.GetThingList(map);
			for (int j = 0; j < thingList.Count; j++)
			{
				if (thingList[j] is Pawn pawn && pawn != base.pawn && !pawn.Dead && pawn.RaceProps.Humanlike && (skunkStinkHediff == null || pawn.health?.hediffSet == null || !pawn.health.hediffSet.HasHediff(skunkStinkHediff)))
				{
					pawn.needs?.mood?.thoughts?.memories?.TryGainMemoryFast(skunkStinkNearby);
				}
			}
		}
	}

	private void EnsureTicksInitialized()
	{
		if (ticksLeft < 0)
		{
			ticksLeft = Mathf.RoundToInt(Mathf.Clamp01(Severity) * (float)BaseDurationTicks);
			if (ticksLeft <= 0)
			{
				ticksLeft = BaseDurationTicks;
			}
		}
	}

	private void SyncSeverityFromTicks()
	{
		Severity = Mathf.Clamp((float)ticksLeft / (float)BaseDurationTicks, 0.001f, 1f);
	}

	private void UpdateEnvironmentState(Pawn p)
	{
		Room room = p.GetRoom();
		if (room == null || room.PsychologicallyOutdoors)
		{
			consecutiveOutsideTicks += 180;
			consecutiveInsideTicks = 0;
			indoorSlowActive = false;
			if (consecutiveOutsideTicks >= EnvironmentThresholdTicks)
			{
				outdoorSpeedActive = true;
			}
		}
		else
		{
			consecutiveInsideTicks += 180;
			consecutiveOutsideTicks = 0;
			outdoorSpeedActive = false;
			if (consecutiveInsideTicks >= EnvironmentThresholdTicks)
			{
				indoorSlowActive = true;
			}
		}
	}

	private float GetDecayRateFactor(Pawn p)
	{
		float num = 1f;
		if (outdoorSpeedActive)
		{
			num *= 1.1f;
		}
		if (indoorSlowActive)
		{
			num *= 0.9f;
		}
		if (p.GetStatValue(StatDefOf.ToxicEnvironmentResistance) >= 1f)
		{
			num *= 2.5f;
		}
		return num;
	}

	private static bool IsStandingInSkunkGas(IntVec3 pos, Map map)
	{
		ThingDef skunkGas = SkunksDefOf.SkunkGas;
		if (skunkGas == null)
		{
			return false;
		}
		Gas gas = pos.GetGas(map);
		if (gas != null)
		{
			return gas.def == skunkGas;
		}
		return false;
	}
}
