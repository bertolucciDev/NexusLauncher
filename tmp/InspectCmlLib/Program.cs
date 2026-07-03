using System;
using System.Linq;
using System.Reflection;

var asm = Assembly.LoadFrom(@"C:\Users\luiso\.nuget\packages\cmllib.core\4.0.6\lib\netstandard2.0\CmlLib.Core.dll");

foreach (var t in asm.GetTypes()
             .Where(t => t.FullName != null && (t.FullName.Contains("Auth") || t.FullName.Contains("Session") || t.FullName.Contains("Login")))
             .OrderBy(t => t.FullName))
{
    Console.WriteLine(t.FullName);
}
