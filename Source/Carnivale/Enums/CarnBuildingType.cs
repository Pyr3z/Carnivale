using System;

namespace Carnivale
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
        Vendor = 32,
        ManagerOnly = 64,
        Entrance = 128,
        Any = 255
    }
}
