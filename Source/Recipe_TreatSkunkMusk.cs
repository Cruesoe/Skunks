using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Skunks;

public class Recipe_TreatSkunkMusk : Recipe_Surgery
{
	public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
	{
		if (thing is Pawn pawn && pawn.health?.hediffSet != null)
		{
			return pawn.health.hediffSet.HasHediff(SkunksDefOf.SkunkStinkHediff);
		}
		return false;
	}

	public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
	{
		if (pawn.health.hediffSet.HasHediff(SkunksDefOf.SkunkStinkHediff))
		{
			yield return pawn.RaceProps.body.corePart;
		}
	}

	public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
	{
		if (billDoer != null)
		{
			if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
			{
				return;
			}
			TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
		}
		if (pawn.health.hediffSet.GetFirstHediffOfDef(SkunksDefOf.SkunkStinkHediff) is Hediff_SkunkStink hediff_SkunkStink)
		{
			hediff_SkunkStink.StartSlowCleanse(Hediff_SkunkStink.SlowCleanseDurationTicks);
			Messages.Message((billDoer != null) ? (billDoer.LabelShort + " has started cleansing " + pawn.LabelShort + "'s skunk musk. It should wear off over about a day.") : (pawn.LabelShort + "'s skunk musk is being cleansed and should wear off over about a day."), pawn, MessageTypeDefOf.PositiveEvent);
		}
	}
}
