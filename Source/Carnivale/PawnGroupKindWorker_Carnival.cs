using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Carnivale
{
    public class PawnGroupKindWorker_Carnival : PawnGroupKindWorker
    {
        public override float MinPointsToGenerateAnything(PawnGroupMaker groupMaker)
        {
            return 0f;
        }

        public override bool CanGenerateFrom(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
        {
            return base.CanGenerateFrom(parms, groupMaker) &&
                groupMaker.traders.Any() &&
                (parms.tile == -1 ||
                    groupMaker.carriers.Any((PawnGenOption x) => Find.WorldGrid[parms.tile].biome.IsPackAnimalAllowed(x.kind.race)));
        }

        protected override void GeneratePawns(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults = true)
        {
            throw new NotImplementedException();
        }
    }
}
