using Verse;

namespace Skunks;

internal static class SmokeleafUtility
{
	public static bool HasSmokeleafDependence(Pawn pawn)
	{
		HediffDef smokeleafAddiction = SkunksDefOf.SmokeleafAddiction;
		if (smokeleafAddiction != null && pawn?.health?.hediffSet != null)
		{
			return pawn.health.hediffSet.HasHediff(smokeleafAddiction);
		}
		return false;
	}
}
