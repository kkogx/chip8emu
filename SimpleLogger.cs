using System;
using System.Collections.Generic;

namespace Chip8Emu
{
    public class SimpleLogger
    {
        public Action<string> GetLogger() => (s) => Log(s);

        public Action<Func<string>> GetExpensiveLogger() => (s) => LogExpensive(s);

        public readonly List<(string, long)> Messages = new();

        public bool disableExpensiveLogs = false;

        public void Log(string msg)
        {
            Messages.Insert(0, (msg, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
        }

        public void LogExpensive(Func<string> msg)
        {
            if (!disableExpensiveLogs)
            {
                Messages.Insert(0, (msg(), DateTimeOffset.Now.ToUnixTimeMilliseconds()));
            }
        }
    }
}
