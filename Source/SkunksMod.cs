using HarmonyLib;
using UnityEngine;
using Verse;

namespace Skunks;

public class SkunksMod : Mod
{
	public static SkunksSettings Settings { get; private set; }

	public SkunksMod(ModContentPack content)
		: base(content)
	{
		Settings = GetSettings<SkunksSettings>();
		new Harmony("guppyfacesarecute.skunks").PatchAll();
		LongEventHandler.ExecuteWhenFinished(BattleSkunkUtility.ApplyRaceChanges);
	}

	public override string SettingsCategory()
	{
		return "Skunks";
	}

	public override void DoSettingsWindowContents(Rect inRect)
	{
		Settings.DoWindowContents(inRect);
	}
}
