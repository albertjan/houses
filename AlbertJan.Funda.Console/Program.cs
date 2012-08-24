using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlbertJan.Funda.Console
{
    class Program
    {
        static void Main (string[] args)
        {
            var runner = new RunningTotal("/amsterdam", false);
            runner.NewRealtor += runner_NewRealtor;
            runner.NumberOfObjects += runner_NumberOfObjects;
            runner.Start();
            System.Console.WriteLine("Total number of realtors active in amsterdam: " + runner.Realtors.Count);
            var count = 1;
            foreach (var realtor in runner.Realtors.OrderByDescending (r => r.Value.NumberOfObjects).Select(r => r.Value).Take(10))
            {
                System.Console.WriteLine("Rank " + count + " " + realtor.Name + " has " + realtor.NumberOfObjects);
                count++;
            }

            System.Console.ReadLine();
        }

        static void runner_NumberOfObjects (object sender, NumberOfObjectsEventArgs e)
        {
            System.Console.WriteLine ("total objects: " + e.NumberOfObjects);
        }

        static void runner_NewRealtor (object sender, NewRealtorEventArgs e)
        {
            System.Console.WriteLine("new realtor: " + e.Realtor.Name);
            e.Realtor.ObjectCounted += Realtor_ObjectCounted;
        }

        static void Realtor_ObjectCounted (object sender, ObjectCountedEventArgs e)
        {
            System.Console.WriteLine (e.Realtor.Name + " has " + e.Realtor.NumberOfObjects);
        }
    }
}
