using RimWorld;

namespace Skunks;

public class Thought_Memory_SkunkStink : Thought_Memory
{
	public override int CurStageIndex
	{
		get
		{
			if (pawn != null && SmokeleafUtility.HasSmokeleafDependence(pawn))
			{
				return 1;
			}
			return 0;
		}
	}
}
