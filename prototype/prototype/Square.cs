using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace prototype
{
    public class Square:ShapePrototype
    {
        public string type;

        public Square(string type)
        {
            this.type = type;
        }

        public ShapePrototype Clone()
        {
            return new Square(this.type);
        }

        public void Draw()
        {
            Console.WriteLine("Drawing a" + this.type);
        }
    }
}
