using RimWorld;
using Verse;

namespace Carnivale
{
    public class LordToil_StrikeCarnival : LordToil_Carn
    {
        // FIELDS + PROPERTIES //

        


        // CONSTRUCTORS //

        public LordToil_StrikeCarnival() { }


        // OVERRIDE METHODS //

        public override void Init()
        {
            foreach (var building in Info.carnivalBuildings)
            {
                Designation des = new Designation(building, DesignationDefOf.Uninstall);
                Map.designationManager.AddDesignation(des);
            }
        }


        public override void UpdateAllDuties()
        {
            foreach (var worker in Info.pawnsWithRole[CarnivalRole.Worker])
            {
                DutyUtility.BuildCarnival(worker, Info.setupCentre, Info.baseRadius);
            }
        }

    }
}
