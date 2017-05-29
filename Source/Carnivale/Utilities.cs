using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace Carnivale
{
    public static class Utilities
    {
        public static bool IsCarnival(this Faction faction)
        {
            //return faction.def == _FactionDefOf.Carn_Faction_Roaming;
            return faction.def.defName.StartsWith("Carn_");
        }



        public static IEnumerable<Pawn> FindVendors(this Lord lord)
        {
            if (!lord.faction.IsCarnival())
            {
                Log.Error("The faction under " + lord + " is not a carnival.");
                yield break;
            }
            foreach (Pawn p in lord.ownedPawns)
            {
                if (p.TraderKind != null)
                    yield return p;
            }
        }

        public static CarnivalRole GetCarnivalRole(this Pawn p)
        {
            if (!p.Faction.IsCarnival())
            {
                Log.Error("Tried to get a CarnivalRole for " + p.NameStringShort + ", who is not in a carnival faction.");
                return CarnivalRole.None;
            }

            CarnivalRole role = 0;

            switch (p.kindDef.defName)
            {
                case "Carny":
                case "CarnyRare":
                    role = CarnivalRole.Entertainer;
                    break;
                case "CarnyRoustabout":
                    role = CarnivalRole.Worker;
                    break;
                case "CarnyTrader":
                    role = CarnivalRole.Vendor;
                    break;
                case "CarnyGuard":
                    role = CarnivalRole.Guard;
                    break;
                case "CarnyManager":
                    role = CarnivalRole.Manager;
                    break;
                default:
                    role = CarnivalRole.Chattel;
                    break;
            }

            return role;
        }


        public static void SetRoofFor(CellRect rect, Map map, RoofDef roof)
        {
            // pass a null RoofDef to clear a roof
            for (int i = rect.minZ; i <= rect.maxZ; i++)
            {
                for (int j = rect.minX; j <= rect.maxX; j++)
                {
                    IntVec3 cell = new IntVec3(j, 0, i);
                    map.roofGrid.SetRoof(cell, roof);
                }
            }
        }



        public static IEnumerable<IntVec3> CornerlessEdgeCells(CellRect rect)
        {
            int x = rect.minX + 1;
            int z = rect.minZ;
            while (x < rect.maxX)
            {
                yield return new IntVec3(x, 0, z);
                x++;
            }
            for (z++; z < rect.maxZ; z++)
            {
                yield return new IntVec3(x, 0, z);
            }
            for (x--; x > rect.minX; x--)
            {
                yield return new IntVec3(x, 0, z);
            }
            for (z--; z > rect.minZ; z--)
            {
                yield return new IntVec3(x, 0, z);
            }
        }
    }
}
