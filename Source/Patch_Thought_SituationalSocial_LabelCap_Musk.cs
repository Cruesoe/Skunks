using HarmonyLib;
using RimWorld;
using Verse;

namespace Skunks;

[HarmonyPatch(typeof(Thought_SituationalSocial), "get_LabelCap")]
internal static class Patch_Thought_SituationalSocial_LabelCap_Musk
{
	private static void Postfix(Thought_SituationalSocial __instance, ref string __result)
	{
		if (__instance.def != null && __instance.CurStageIndex == 1 && !(__instance.def.defName != "Ugly"))
		{
			Pawn otherPawn = __instance.otherPawn;
			HediffDef skunkStinkHediff = SkunksDefOf.SkunkStinkHediff;
			if (otherPawn?.health?.hediffSet != null && skunkStinkHediff != null && otherPawn.health.hediffSet.HasHediff(skunkStinkHediff))
			{
				__result = "Horrid stench";
			}
		}
	}
}
