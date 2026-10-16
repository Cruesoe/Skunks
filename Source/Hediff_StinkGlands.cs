using System.Text;
using RimWorld;
using Verse;

namespace Skunks;

public class Hediff_StinkGlands : Hediff
{
	public override string LabelInBrackets => (pawn?.TryGetComp<CompSkunkGas>())?.SpraysRemaining.ToString();

	public override string TipStringExtra
	{
		get
		{
			StringBuilder stringBuilder = new StringBuilder();
			string tipStringExtra = base.TipStringExtra;
			if (!tipStringExtra.NullOrEmpty())
			{
				stringBuilder.Append(tipStringExtra);
			}
			CompSkunkGas compSkunkGas = pawn?.TryGetComp<CompSkunkGas>();
			if (compSkunkGas != null)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.AppendLine();
				}
				stringBuilder.Append("Sprays remaining: ");
				stringBuilder.Append(compSkunkGas.SpraysRemaining);
				stringBuilder.Append('/');
				stringBuilder.Append(compSkunkGas.MaxSprays);
				int cooldownTicksRemaining = compSkunkGas.CooldownTicksRemaining;
				if (cooldownTicksRemaining > 0)
				{
					stringBuilder.AppendLine();
					stringBuilder.Append("Spray cooldown: ");
					stringBuilder.Append(((float)cooldownTicksRemaining / 60f).ToString("F0"));
					stringBuilder.Append("s");
				}
				int ticksUntilNextSpray = compSkunkGas.TicksUntilNextSpray;
				if (ticksUntilNextSpray > 0 && compSkunkGas.SpraysRemaining < compSkunkGas.MaxSprays)
				{
					stringBuilder.AppendLine();
					stringBuilder.Append("Next spray ready in: ");
					stringBuilder.Append(ticksUntilNextSpray.ToStringTicksToPeriod());
				}
			}
			return stringBuilder.ToString();
		}
	}

	public override bool ShouldRemove => false;
}
