using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    namespace RobotPrototype
    {
        public class IndustrialRobot : IRobotPrototype
        {
            public string ModelName { get; set; }
            public double BatteryCapacity { get; set; }
            public string SoftwareVersion { get; set; }

            public string IndustrialTask { get; set; }

            public IndustrialRobot(
                string modelName,
                double batteryCapacity,
                string softwareVersion,
                string industrialTask)
            {
                ModelName = modelName;
                BatteryCapacity = batteryCapacity;
                SoftwareVersion = softwareVersion;
                IndustrialTask = industrialTask;
            }

            public IRobotPrototype Clone()
            {
                return new IndustrialRobot(
                    ModelName,
                    BatteryCapacity,
                    SoftwareVersion,
                    IndustrialTask
                );
            }

            public void DisplayRobot()
            {
                Console.WriteLine("Industrial Robot");
                Console.WriteLine($"Model Name: {ModelName}");
                Console.WriteLine($"Battery Capacity: {BatteryCapacity} hours");
                Console.WriteLine($"Software Version: {SoftwareVersion}");
                Console.WriteLine($"Industrial Task: {IndustrialTask}");
                Console.WriteLine();
            }
        }
    }
}
