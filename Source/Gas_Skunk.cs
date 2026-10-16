using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Skunks;

public class Gas_Skunk : Gas
{
	private const int CheckInterval = 60;

	protected override void TickInterval(int delta)
	{
		base.TickInterval(delta);
		if (!base.Spawned || base.Destroyed || !this.IsHashIntervalTick(60, delta))
		{
			return;
		}
		HediffDef skunkStinkHediff = SkunksDefOf.SkunkStinkHediff;
		ThingDef skunk = SkunksDefOf.Skunk;
		List<Thing> thingList = base.Position.GetThingList(base.Map);
		for (int i = 0; i < thingList.Count; i++)
		{
			if (!(thingList[i] is Pawn { Dead: false } pawn))
			{
				continue;
			}
			if (pawn.RaceProps.Animal)
			{
				if (skunk == null || pawn.def != skunk)
				{
					CompSkunkGas.StunAnimal(pawn);
				}
			}
			else if (pawn.RaceProps.Humanlike && pawn.health?.hediffSet != null && skunkStinkHediff != null && !(pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance) >= 1f) && !pawn.health.hediffSet.HasHediff(skunkStinkHediff))
			{
				pawn.health.AddHediff(skunkStinkHediff);
			}
		}
	}
}
