using RimWorld;
using UnityEngine;
using Verse;

namespace StonebornExpansion;

public class CompAutoDrill : ThingComp
{
    private CompProperties_AutoDrill Props => props as CompProperties_AutoDrill;

    public CompPowerTrader powerComp;
    public CompRefuelable refuelableComp;
    private CompBreakdownable breakdownableComp;

    public float portionProgress;
    public bool usedLastTick;

    public float ProgressToNextPortionPercent => portionProgress / Props.ticksPerPortion;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        powerComp = parent.TryGetComp<CompPowerTrader>();
        refuelableComp = parent.TryGetComp<CompRefuelable>();
        breakdownableComp = parent.TryGetComp<CompBreakdownable>();
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref portionProgress, "portionProgress");
        Scribe_Values.Look(ref usedLastTick, "usedLastTick");
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        portionProgress = 0f;
        usedLastTick = false;
    }

    public override void CompTick()
    {
        base.CompTick();

        usedLastTick = false;

        if (!CanDrillNow())
        {
            return;
        }

        portionProgress += 1f;
        usedLastTick = true;
        refuelableComp?.Notify_UsedThisTick();

        if (!(portionProgress > Props.ticksPerPortion)) return;
        TryProducePortion();
        portionProgress = 0f;
    }

    public void TryProducePortion()
    {
        ThingDef resDef;
        int countPresent;
        IntVec3 cell;
        bool nextResource = GetNextResource(out resDef, out countPresent, out cell);
        if (resDef == null)
        {
            return;
        }

        int num = Mathf.Min(countPresent, resDef.deepCountPerPortion);
        if (nextResource)
        {
            parent.Map.deepResourceGrid.SetAt(cell, resDef, countPresent - num);
        }

        int stackCount = Mathf.Max(1, GenMath.RoundRandom(num));
        Thing thing = ThingMaker.MakeThing(resDef);
        thing.stackCount = stackCount;
        GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near, null, p => p != parent.Position && p != parent.InteractionCell);
        if (!nextResource || ValuableResourcesPresent())
        {
            return;
        }

        if (DeepDrillUtility.GetBaseResource(parent.Map, parent.Position) == null)
        {
            Messages.Message("DeepDrillExhaustedNoFallback".Translate(), parent, MessageTypeDefOf.TaskCompletion);
            return;
        }

        Messages.Message("DeepDrillExhausted".Translate(Find.ActiveLanguageWorker.Pluralize(DeepDrillUtility.GetBaseResource(parent.Map, parent.Position).label)), parent,
            MessageTypeDefOf.TaskCompletion);
        for (int i = 0; i < 21; i++)
        {
            IntVec3 c = cell + GenRadial.RadialPattern[i];
            if (c.InBounds(parent.Map))
            {
                ThingWithComps firstThingWithComp = c.GetFirstThingWithComp<CompDeepDrill>(parent.Map);
                if (firstThingWithComp != null && !firstThingWithComp.GetComp<CompDeepDrill>().ValuableResourcesPresent())
                {
                    firstThingWithComp.SetForbidden(value: true);
                }
            }
        }
    }

    public bool ValuableResourcesPresent()
    {
        return GetNextResource(out ThingDef _, out int _, out IntVec3 _);
    }

    public bool GetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
    {
        return DeepDrillUtility.GetNextResource(parent.Position, parent.Map, out resDef, out countPresent, out cell);
    }

    public bool Usable => (powerComp?.PowerOn ?? true) && (refuelableComp?.HasFuel ?? true) && !(breakdownableComp?.BrokenDown ?? false);

    public bool CanDrillNow()
    {
        if (!Usable) return false;

        return DeepDrillUtility.GetBaseResource(parent.Map, parent.Position) != null || ValuableResourcesPresent();
    }

    public override string CompInspectStringExtra()
    {
        if (!parent.Spawned) return null;
        GetNextResource(out var resDef, out var _, out var _);
        if (resDef == null)
        {
            return "DeepDrillNoResources".Translate();
        }

        return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " +
               ProgressToNextPortionPercent.ToStringPercent("F0");
    }
}
