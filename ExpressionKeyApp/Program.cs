using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionKeyApp
{
    class Program
    {
        static void Main(string[] args)
        {
            new ExpressionKeyLookupTests();
            //?Look into KeyedCollection

            Console.WriteLine("Press any key to continue!");
            Console.ReadLine();
        }
    }
}
