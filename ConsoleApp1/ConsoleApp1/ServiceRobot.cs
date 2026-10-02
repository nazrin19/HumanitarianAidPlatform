using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    namespace RobotPrototype
    {
        public class ServiceRobot : IRobotPrototype
        {
            public string ModelName { get; set; }
            public double BatteryCapacity { get; set; }
            public string SoftwareVersion { get; set; }

            public string ServiceTask { get; set; }

            public ServiceRobot(
                string modelName,
                double batteryCapacity,
                string softwareVersion,
                string serviceTask)
            {
                ModelName = modelName;
                BatteryCapacity = batteryCapacity;
                SoftwareVersion = softwareVersion;
                ServiceTask = serviceTask;
            }

            public IRobotPrototype Clone()
            {
                return new ServiceRobot(
                    ModelName,
                    BatteryCapacity,
                    SoftwareVersion,
                    ServiceTask
                );
            }

            public void DisplayRobot()
            {
                Console.WriteLine("Service Robot");
                Console.WriteLine($"Model Name: {ModelName}");
                Console.WriteLine($"Battery Capacity: {BatteryCapacity} hours");
                Console.WriteLine($"Software Version: {SoftwareVersion}");
                Console.WriteLine($"Service Task: {ServiceTask}");
                Console.WriteLine();
            }
        }
    }
}
        
    
