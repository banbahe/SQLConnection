using System.Reflection;

namespace jpasharp.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return TestRunner.RunAll(Assembly.GetExecutingAssembly());
        }
    }
}
