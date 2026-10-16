using UnityEngine;
using Verse;

namespace Skunks;

public class SkunksSettings : ModSettings
{
	public bool battleSkunks;

	public override void ExposeData()
	{
		Scribe_Values.Look(ref battleSkunks, "battleSkunks", defaultValue: false);
	}

	public void DoWindowContents(Rect inRect)
	{
		Listing_Standard listing_Standard = new Listing_Standard();
		listing_Standard.Begin(inRect);
		bool flag = battleSkunks;
		listing_Standard.CheckboxLabeled("Battle skunks!", ref battleSkunks, "Skunks become trainable and can learn attack target (Odyssey). Tamed combat skunks spray while they have charges and no spray cooldown, and fight in melee when they cannot spray. Off by default.");
		if (flag != battleSkunks)
		{
			BattleSkunkUtility.ApplyRaceChanges();
		}
		listing_Standard.End();
	}
}
