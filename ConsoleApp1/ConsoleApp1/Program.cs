using ConsoleApp1.RobotPrototype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            
            ServiceRobot serviceRobot = new ServiceRobot(
                "CareBot-100",
                10,
                "v1.0",
                "Assisting patients"
            );

            Console.WriteLine("ORIGINAL SERVICE ROBOT");
            serviceRobot.DisplayRobot();

            
            ServiceRobot serviceRobotClone =
                (ServiceRobot)serviceRobot.Clone();

            
            serviceRobotClone.BatteryCapacity = 12;
            serviceRobotClone.SoftwareVersion = "v1.1";

            Console.WriteLine("CLONED AND CUSTOMIZED SERVICE ROBOT");
            serviceRobotClone.DisplayRobot();


            
            IndustrialRobot industrialRobot = new IndustrialRobot(
                "FactoryBot-200",
                15,
                "v2.0",
                "Welding"
            );

            Console.WriteLine("ORIGINAL INDUSTRIAL ROBOT");
            industrialRobot.DisplayRobot();

            
            IndustrialRobot industrialRobotClone =
                (IndustrialRobot)industrialRobot.Clone();

            
            industrialRobotClone.BatteryCapacity = 18;
            industrialRobotClone.SoftwareVersion = "v2.1";

            Console.WriteLine("CLONED AND CUSTOMIZED INDUSTRIAL ROBOT");
            industrialRobotClone.DisplayRobot();


            
            EntertainmentRobot entertainmentRobot =
                new EntertainmentRobot(
                    "FunBot-300",
                    8,
                    "v3.0",
                    "Dancing and interacting with visitors"
                );

            Console.WriteLine("ORIGINAL ENTERTAINMENT ROBOT");
            entertainmentRobot.DisplayRobot();

            
            EntertainmentRobot entertainmentRobotClone =
                (EntertainmentRobot)entertainmentRobot.Clone();

            
            entertainmentRobotClone.BatteryCapacity = 10;
            entertainmentRobotClone.SoftwareVersion = "v3.1";

            Console.WriteLine("CLONED AND CUSTOMIZED ENTERTAINMENT ROBOT");
            entertainmentRobotClone.DisplayRobot();
        }
    }
}