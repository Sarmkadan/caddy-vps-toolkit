using System;
using System.IO;

namespace CaddyVpsToolkit.Utilities.Test
{
    public static class TestPathCombineBan
    {
        public static string BadMethod()
        {
            // This should trigger BA0001 error
            return Path.Combine("base", "part");
        }
    }
}
