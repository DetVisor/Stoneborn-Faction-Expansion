using Verse;

namespace StonebornExpansion;

public class CompProperties_AutoDrill : CompProperties
{
    public CompProperties_AutoDrill()
    {
        compClass = typeof(CompAutoDrill);
    }

    public int ticksPerPortion = 10000;
}