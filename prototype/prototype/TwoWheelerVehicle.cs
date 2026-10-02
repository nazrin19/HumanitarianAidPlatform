using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace prototype
{
    public class TwoWheelerVehicle:VehiclePrototype
    {
        public string fuel;

        public TwoWheelerVehicle(string engine,string model,string,string color,string fuel)
            : base(engine, model, price, color)
        {
            this.fuel = fuel;
        }

        public override VehiclePrototype Clone()
        {
            return new TwoWheelerVehicle(engine, model, price, color, fuel);
        }
    }
}
