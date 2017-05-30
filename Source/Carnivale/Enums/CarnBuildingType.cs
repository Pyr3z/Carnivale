using System;

namespace Carnivale.Enums
{
    [Flags]
    public enum CarnBuildingType : byte
    {
        None = 0,
        Tent = 1,
        Stall = 2,
        Bedroom = 4,
        Attraction = 8,
        Kitchen = 16,
        Vendor = 32
    }
}
