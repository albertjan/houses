using System;

namespace AlbertJan.Funda
{
    public class NumberOfObjectsEventArgs : EventArgs
    {
        public int NumberOfObjects { get; set; }
    }

    public class ObjectCountedEventArgs : EventArgs
    {
        public Realtor Realtor { get; set; }
    }

    public class NewRealtorEventArgs : EventArgs
    {
        public Realtor Realtor { get; set; }
    }
}