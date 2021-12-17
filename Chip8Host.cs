using System;
using System.IO;

namespace Chip8Emu
{
    public class Chip8Host
    {
        public ushort OP => _chip.OP; 

        public ushort PC => _chip.PC;

        public ushort I => _chip.I;

        public ushort SP => _chip.SP;

        public byte DT => _chip.DT;

        public byte ST => _chip.ST;

        public bool[] Keys => _chip.keys;

        private const int timerSpeed = 60; // [herz]

        private readonly Chip8 _chip;

        private readonly PerfCounter _perf;

        private readonly SimpleLogger _log;

        private readonly System.Timers.Timer _delayTimer;

        private byte[] _lastProgram = Array.Empty<byte>();

        public Chip8Host(Chip8 chip, PerfCounter perf, SimpleLogger log)
        {
            _chip = chip;
            _perf = perf;
            _log = log;

            _delayTimer = new System.Timers.Timer(1000/timerSpeed);
            _delayTimer.Elapsed += (sender, e) => _chip.Tick();
            _delayTimer.AutoReset = true;
            _delayTimer.Start();
        }

        public void Load(byte[] program)
        {
            _chip.Load(program);
            _lastProgram = program;
        }

        public void Load(string filename)
        {
            var program = File.ReadAllBytes(filename);
            Load(program);
            _log.GetLogger()($"loaded {filename}");
        }

        public void Cycle()
        {
            _chip.Cycle();
            _perf.CountCycle();
        }

        internal void Reload()
        {
            _chip.Reset();
            _chip.Load(_lastProgram);
        }

        internal byte[] GetProgramMemoryDump()
        {
            var copy = new byte[_lastProgram.Length];
            Array.Copy(_chip.memory, 0x200, copy, 0, _lastProgram.Length);
            return copy;
        }

        internal byte[] GetRegistries()
        {
            return _chip.V.Clone() as byte[];
        }

        internal bool[,] GetPixels()
        {
            return _chip.screen.pixels.Clone() as bool[,];
        }
    }
}
