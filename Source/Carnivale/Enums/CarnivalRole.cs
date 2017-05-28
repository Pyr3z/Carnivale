

namespace Carnivale
{
    public enum CarnivalRole : byte
    {
        // Assigning power-of-2 byte values allows stacking
        None = 0,
        Entertainer = 1,
        Vendor = 2,
        Worker = 4,
        Guard = 8,
        Carrier = 16,
        Manager = 32,
        Chattel = 64
    }
}
