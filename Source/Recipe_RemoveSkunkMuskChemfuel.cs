using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Skunks;

public class Recipe_RemoveSkunkMuskChemfuel : Recipe_RemoveHediff
{
	private const float BurnChance = 0.5f;

	private const int BurnDamageMin = 5;

	private const int BurnDamageMax = 10;

	public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
	{
		bool num = pawn.health?.hediffSet != null && pawn.health.hediffSet.HasHediff(recipe.removesHediff);
		base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
		if (!num || pawn.Dead || pawn.health?.hediffSet == null || pawn.health.hediffSet.HasHediff(recipe.removesHediff) || !Rand.Chance(0.5f))
		{
			return;
		}
		List<BodyPartRecord> list = GetBurnCandidates(pawn).ToList();
		if (list.Count != 0)
		{
			int num2 = Rand.RangeInclusive(2, 4);
			for (int i = 0; i < num2; i++)
			{
				BodyPartRecord hitPart = list.RandomElement();
				float amount = Rand.RangeInclusive(5, 10);
				pawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, amount, 0f, -1f, billDoer, hitPart));
			}
			Messages.Message("Chemfuel cleansing burned " + pawn.LabelShort + ".", pawn, MessageTypeDefOf.NegativeHealthEvent);
		}
	}

	private static IEnumerable<BodyPartRecord> GetBurnCandidates(Pawn pawn)
	{
		BodyPartRecord head = pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Head).FirstOrFallback();
		foreach (BodyPartRecord notMissingPart in pawn.health.hediffSet.GetNotMissingParts())
		{
			if (!IsHeadRelated(notMissingPart, head))
			{
				yield return notMissingPart;
			}
		}
	}

	private static bool IsHeadRelated(BodyPartRecord part, BodyPartRecord head)
	{
		if (part == null)
		{
			return true;
		}
		if (head == null)
		{
			return part.def == BodyPartDefOf.Head;
		}
		for (BodyPartRecord bodyPartRecord = part; bodyPartRecord != null; bodyPartRecord = bodyPartRecord.parent)
		{
			if (bodyPartRecord == head)
			{
				return true;
			}
		}
		if (head.parent != null && part.parent == head.parent && part != head)
		{
			return true;
		}
		return false;
	}
}
