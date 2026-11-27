using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace jpasharp.Tests
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestClassAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestMethodAttribute : Attribute
    {
    }

    public sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message)
        {
        }
    }

    public static class Assert
    {
        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition)
                throw new AssertionException(message ?? "Se esperaba true, pero fue false.");
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition)
                throw new AssertionException(message ?? "Se esperaba false, pero fue true.");
        }

        public static void IsNotNull(object value, string message = null)
        {
            if (value == null)
                throw new AssertionException(message ?? "Se esperaba un valor distinto de null.");
        }

        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new AssertionException(message ?? string.Format("Esperado <{0}>, obtenido <{1}>.", expected, actual));
        }

        public static void Contains(string expectedSubstring, string actual, string message = null)
        {
            if (actual == null || actual.IndexOf(expectedSubstring, StringComparison.Ordinal) < 0)
                throw new AssertionException(message ?? string.Format("Se esperaba que '{0}' contuviera '{1}'.", actual, expectedSubstring));
        }

        public static void DoesNotContain(string unexpectedSubstring, string actual, string message = null)
        {
            if (actual != null && actual.IndexOf(unexpectedSubstring, StringComparison.Ordinal) >= 0)
                throw new AssertionException(message ?? string.Format("No se esperaba '{0}' dentro de '{1}'.", unexpectedSubstring, actual));
        }

        public static TException Throws<TException>(Action action, string message = null) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertionException(message ?? string.Format(
                    "Se esperaba excepción {0}, pero se lanzó {1}: {2}",
                    typeof(TException).Name, ex.GetType().Name, ex.Message));
            }

            throw new AssertionException(message ?? string.Format("Se esperaba excepción {0}, pero no se lanzó ninguna.", typeof(TException).Name));
        }
    }

    public static class TestRunner
    {
        public static int RunAll(Assembly assembly)
        {
            int passed = 0;
            int failed = 0;

            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                .OrderBy(t => t.FullName);

            foreach (var testClass in testClasses)
            {
                var methods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null && m.GetParameters().Length == 0)
                    .OrderBy(m => m.Name);

                foreach (var method in methods)
                {
                    string testName = string.Format("{0}.{1}", testClass.Name, method.Name);
                    try
                    {
                        object instance = Activator.CreateInstance(testClass);
                        method.Invoke(instance, null);
                        Console.WriteLine("[PASS] {0}", testName);
                        passed++;
                    }
                    catch (TargetInvocationException ex)
                    {
                        Exception inner = ex.InnerException ?? ex;
                        Console.Error.WriteLine("[FAIL] {0}: {1}", testName, inner.Message);
                        failed++;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("[FAIL] {0}: {1}", testName, ex.Message);
                        failed++;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Resumen: {0} pruebas ejecutadas | {1} aprobadas | {2} fallidas", passed + failed, passed, failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
