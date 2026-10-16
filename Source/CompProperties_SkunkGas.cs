using Verse;

namespace Skunks;

public class CompProperties_SkunkGas : CompProperties
{
	public float radius = 5f;

	public float minAimDot = 0.935f;

	public int maxSprays = 5;

	public int cooldownTicks = 1200;

	public float regenDays = 3f;

	public CompProperties_SkunkGas()
	{
		compClass = typeof(CompSkunkGas);
	}
}
