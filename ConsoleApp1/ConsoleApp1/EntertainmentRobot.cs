using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    namespace RobotPrototype
    {
        public class EntertainmentRobot : IRobotPrototype
        {
            public string ModelName { get; set; }
            public double BatteryCapacity { get; set; }
            public string SoftwareVersion { get; set; }

            public string EntertainmentFeature { get; set; }

            public EntertainmentRobot(
                string modelName,
                double batteryCapacity,
                string softwareVersion,
                string entertainmentFeature)
            {
                ModelName = modelName;
                BatteryCapacity = batteryCapacity;
                SoftwareVersion = softwareVersion;
                EntertainmentFeature = entertainmentFeature;
            }

            public IRobotPrototype Clone()
            {
                return new EntertainmentRobot(
                    ModelName,
                    BatteryCapacity,
                    SoftwareVersion,
                    EntertainmentFeature
                );
            }

            public void DisplayRobot()
            {
                Console.WriteLine("Entertainment Robot");
                Console.WriteLine($"Model Name: {ModelName}");
                Console.WriteLine($"Battery Capacity: {BatteryCapacity} hours");
                Console.WriteLine($"Software Version: {SoftwareVersion}");
                Console.WriteLine($"Entertainment Feature: {EntertainmentFeature}");
                Console.WriteLine();
            }
        }
    }
    
}
