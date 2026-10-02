using prototype;
using System;

public class Circle : ShapePrototype
{
    public string color;

    public Circle(string clrr)
    {
        this.color = clrr;
    }

    public ShapePrototype Clone()
    {
        return new Circle(this.color);
    }

    public void Draw()
    {
        Console.WriteLine("drawing a " + this.color + " circle");
    }
}