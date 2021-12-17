using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu
{
    public class PerfCounter
    {
        private readonly int delta = 3000;

        private readonly System.Timers.Timer _perfTimer;

        private readonly Action<string> _log;

        private long cycles = 0;

        private long draws = 0;

        public PerfCounter(SimpleLogger log)
        {
            _log = log.GetLogger();
            _perfTimer = new System.Timers.Timer(delta);
            _perfTimer.Elapsed += (sender, e) => LogPerf();
            _perfTimer.AutoReset = true;
            _perfTimer.Start();
        }

        public void CountCycle() => ++cycles;

        public void CountDraw() => ++draws;

        private void LogPerf()
        {
            _log($"CPU = {cycles*1000/delta}hz, FPS = {draws*1000/delta}hz");
            cycles = 0;
            draws = 0;
        }
    }
}
