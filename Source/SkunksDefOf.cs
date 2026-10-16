using RimWorld;
using Verse;

namespace Skunks;

[DefOf]
public static class SkunksDefOf
{
	public static ThoughtDef SkunkStinkThought;

	public static ThoughtDef SkunkStinkNearby;

	public static HediffDef SkunkStinkHediff;

	public static HediffDef StinkGlands;

	public static BodyPartDef Anus;

	public static ThingDef SkunkGas;

	public static ThingDef Skunk;

	public static SoundDef Skunk_Spray;

	public static HediffDef SmokeleafAddiction;

	static SkunksDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(SkunksDefOf));
	}
}
