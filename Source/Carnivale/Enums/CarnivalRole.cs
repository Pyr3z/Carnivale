using System;

namespace Carnivale
{
    [Flags]
    public enum CarnivalRole : byte
    {
        // Assigning power-of-2 byte values allows stacking
        None = 0,
        Worker = 1,
        Entertainer = 2,
        Cook = 4,
        Vendor = 8,
        Guard = 16,
        Carrier = 32,
        Manager = 64
    }

}
