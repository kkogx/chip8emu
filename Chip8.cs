using System;

namespace Chip8Emu
{
    public class Chip8
    {

        public ushort OP { get; private set; } 

        public ushort PC { get; private set; }

        public ushort I { get; private set; }

        public ushort SP { get; private set; }

        public byte DT { get; private set; }

        public byte ST { get; private set; }

        // 0x000-0x1FF - Chip 8 interpreter (contains font set in emu)
        // 0x050-0x0A0 - Used for the built in 4x5 pixel font set (0-F)
        // 0x200-0xFFF - Program ROM and work RAM
        public readonly byte[] memory = new byte[4096];

        public readonly byte[] V = new byte[16];

        public readonly ushort[] stack = new ushort[16];

        public readonly bool[] keys = new bool[16];

        public readonly Screen screen = new();

        private readonly Random _rnd = new();

        private readonly Action<Func<string>> _log;

        public Chip8(SimpleLogger log)
        {
            _log = log.GetExpensiveLogger();
            Reset();
        }

        public void Load(byte[] program)
        {
            Reset();
            Array.Copy(program, 0, memory, 0x200, Math.Min(program.Length, memory.Length - 0x200));
        }

        public void Reset()
        {
            OP = I = SP = DT = ST = 0;
            PC = 0x200;
            Array.Clear(memory, 0, memory.Length);
            Array.Clear(V, 0, V.Length);
            Array.Clear(stack, 0, stack.Length);

            var digitBytes = GetDigitBytes();
            Array.Copy(digitBytes, 0, memory, 0, digitBytes.Length);

            screen.Reset();
        }

        public void Cycle()
        {
            //fetch opcode
            OP = FetchOp();

            //decode opcode
            var instr = Decode(OP);

            //execude opcode
            instr.Invoke();

            _log(() => $"Current INS = {instr.Method.Name}, ");
            _log(() => $"     Next INS = {Decode(FetchOp()).Method.Name}");
        }

        private ushort FetchOp() => (ushort)(memory[PC] << 8 | memory[PC + 1]);

        private Action Decode(ushort OP)
        {
            switch (OP & 0xFFFF)
            {
                case 0x00E0: return I_00E0_CLS;
                case 0x00EE: return I_00EE_RET;
            }
            switch (OP & 0xF000)
            {
                case 0x0000: return I_0nnn_SYS_addr;
                case 0x1000: return I_1nnn_JP_addr;
                case 0x2000: return I_2nnn_CALL_addr;
                case 0x3000: return I_3xkk_SE_Vx_byte;
                case 0x4000: return I_4xkk_SNE_Vx_byte;
                case 0x5000: return I_5xy0_SE_Vx_Vy;
                case 0x6000: return I_6xkk_LD_Vx_byte;
                case 0x7000: return I_7xkk_ADD_Vx_byte;
                case 0x9000: return I_9xy0_SNE_Vx_Vy;
                case 0xA000: return I_Annn_LD_I_addr;
                case 0xB000: return I_Bnnn_JP_V0_addr;
                case 0xC000: return I_Cxkk_RND_Vx_byte;
                case 0xD000: return I_Dxyn_DRW_Vx_Vy_nibble;
            }
            switch (OP & 0xF00F)
            {
                case 0x8000: return I_8xy0_LD_Vx_Vy;
                case 0x8001: return I_8xy1_OR_Vx_Vy;
                case 0x8002: return I_8xy2_AND_Vx_Vy;
                case 0x8003: return I_8xy3_XOR_Vx_Vy;
                case 0x8004: return I_8xy4_ADD_Vx_Vy;
                case 0x8005: return I_8xy5_SUB_Vx_Vy;
                case 0x8006: return I_8xy6_SHR_Vx_Vy;
                case 0x8007: return I_8xy7_SUBN_Vx_Vy;
                case 0x800E: return I_8xyE_SHL_Vx_Vy;
            }
            switch (OP & 0xF0FF)
            {
                case 0xE09E: return I_Ex9E_SKP_Vx;
                case 0xE0A1: return I_E0A1_SKNP_Vx;
                case 0xF007: return I_Fx07_LD_Vx_DT;
                case 0xF00A: return I_Fx0A_LD_Vx_D;
                case 0xF015: return I_Fx15_LD_DT_V;
                case 0xF018: return I_Fx18_LD_ST_V;
                case 0xF01E: return I_Fx1E_ADD_I_V;
                case 0xF029: return I_Fx29_LD_F_V;
                case 0xF033: return I_Fx33_LD_B_V;
                case 0xF055: return I_Fx55_LD_I_V;
                case 0xF065: return I_Fx65_LD_Vx;
            }

            throw new NotImplementedException(OP.ToHex());
        }

        private void I_Ex9E_SKP_Vx() { PC += (ushort)(keys[V[OP >> 8 & 0x0F]] ? 4 : 2); }

        private void I_E0A1_SKNP_Vx() { PC += (ushort)(keys[V[OP >> 8 & 0x0F]] ? 2 : 4); }

        private void I_8xyE_SHL_Vx_Vy() { throw new NotImplementedException(); }

        private void I_8xy7_SUBN_Vx_Vy() { throw new NotImplementedException(); }

        private void I_8xy6_SHR_Vx_Vy() {
            var x = V[OP >> 8 & 0x0F];
            V[0xF] = (byte)(x & 1);
            V[OP >> 8 & 0x0F] >>= 1;
            PC += 2;
        }

        private void I_8xy5_SUB_Vx_Vy() 
        {
            int diff = V[OP >> 8 & 0x0F] - V[OP >> 4 & 0x0F];
            V[0xF] = diff > 0 ? (byte)1 : (byte)0;
            V[OP >> 8 & 0x0F] = (byte)diff;
            PC += 2;
        }

        private void I_8xy4_ADD_Vx_Vy() 
        {
            int sum = V[OP >> 8 & 0x0F] + V[OP >> 4 & 0x0F];
            V[OP >> 8 & 0x0F] = (byte)sum;
            V[0xF] = sum > 255 ? (byte)1 : (byte)0;
            PC += 2;
        }

        private void I_8xy3_XOR_Vx_Vy() { V[OP >> 8 & 0x0F] = (byte)(V[OP >> 8 & 0x0F] ^ V[OP >> 4 & 0x0F]); PC += 2; }

        private void I_8xy2_AND_Vx_Vy() { V[OP >> 8 & 0x0F] = (byte)(V[OP >> 8 & 0x0F] & V[OP >> 4 & 0x0F]); PC += 2; }

        private void I_8xy1_OR_Vx_Vy() { V[OP >> 8 & 0x0F] = (byte)(V[OP >> 8 & 0x0F] | V[OP >> 4 & 0x0F]); PC += 2; }

        private void I_8xy0_LD_Vx_Vy() { V[OP >> 8 & 0x0F] = V[OP >> 4 & 0x0F]; PC += 2; }

        private void I_0nnn_SYS_addr() { /* NOOP PC = (ushort)(OP & 0x0FFF);*/ }

        private void I_00EE_RET() { SP -= 1; PC = stack[SP]; PC += 2; }

        private void I_00E0_CLS() { screen.Reset(); PC += 2; }

        private void I_1nnn_JP_addr() { PC = (ushort)(OP & 0x0FFF); }

        private void I_2nnn_CALL_addr() { stack[SP] = PC; SP += 1; PC = (ushort)(OP & 0x0FFF); }

        private void I_3xkk_SE_Vx_byte()
        {
            var x = V[OP >> 8 & 0x0F];
            var kk = OP & 0x00FF;
            if (x == kk) PC += 2;
            PC += 2;
        }

        private void I_4xkk_SNE_Vx_byte() {
            var x = V[OP >> 8 & 0x0F];
            var kk = OP & 0x00FF;
            if (x != kk) PC += 2;
            PC += 2;
        }

        private void I_5xy0_SE_Vx_Vy() 
        {
            var x = V[OP >> 8 & 0x0F];
            var y = V[OP >> 4 & 0x0F];
            if (x == y) PC += 2;
            PC += 2;
        }

        private void I_6xkk_LD_Vx_byte() { V[OP >> 8 & 0x0F] = (byte)OP; PC += 2; }

        private void I_7xkk_ADD_Vx_byte() { V[OP >> 8 & 0x0F] = (byte)((V[OP >> 8 & 0x0F]) + (OP & 0x00FF)); PC += 2; }

        private void I_9xy0_SNE_Vx_Vy() {
            var x = V[OP >> 8 & 0x0F];
            var y = V[OP >> 4 & 0x0F];
            if (x != y) PC += 2;
            PC += 2;
        }

        private void I_Annn_LD_I_addr() { I = (ushort)(OP & 0x0FFF); PC += 2; }

        private void I_Bnnn_JP_V0_addr() { throw new NotImplementedException(); }

        private void I_Cxkk_RND_Vx_byte() 
        {
            V[OP >> 8 & 0x0F] = (byte)(_rnd.Next(0, 256) & (OP & 0x00FF));
            PC += 2;
        }

        /*
         * Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision.
         * The interpreter reads n bytes from memory, starting at the address stored in I. 
         * These bytes are then displayed as sprites on screen at coordinates (Vx, Vy). 
         * Sprites are XORed onto the existing screen. If this causes any pixels to be erased, VF is set to 1, otherwise it is set to 0. 
         * If the sprite is positioned so part of it is outside the coordinates of the display, 
         * it wraps around to the opposite side of the screen. 
         * 
         * Kudos to https://www.arjunnair.in/p37/ for this function, I could could not bother with writing it myself.
         */
        private void I_Dxyn_DRW_Vx_Vy_nibble()
        {
            var x = V[OP >> 8 & 0x0F];
            var y = V[OP >> 4 & 0x0F];
            var n = OP & 0x0F;

            V[0xF] = 0;
            for (var i = 0; i < n; i++)
            {
                byte sprite = memory[I + i];
                var initrow = (y + i) % 32;
                if (initrow < y) break;

                for (var f = 0; f < 8; f++)
                {
                    var b = (sprite & 0x80) >> 7;
                    var col = (x + f) % 64;
                    var row = initrow + ((x + f) / 64);

                    if (b == 1)
                    {
                        if (screen.pixels[col, row] != false)
                        {
                            screen.pixels[col, row] = false;
                            V[0xF] = 1;
                        }
                        else
                            screen.pixels[col, row] = true;
                    }

                    sprite <<= 1;
                }
            }

            PC += 2;
        }

        private void I_Fx07_LD_Vx_DT() { V[OP >> 8 & 0x0F] = DT; PC += 2; }

        private void I_Fx0A_LD_Vx_D() {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i])
                {
                    V[OP >> 8 & 0x0F] = (byte)i;
                    PC += 2;
                }
            }
        }

        private void I_Fx15_LD_DT_V() { DT = V[OP >> 8 & 0x0F]; PC += 2; }

        private void I_Fx18_LD_ST_V() { _log(() => "NotImplemented - I_Fx18_LD_ST_V"); PC += 2; }

        private void I_Fx1E_ADD_I_V() { I += V[OP >> 8 & 0x0F]; PC += 2; }

        private void I_Fx29_LD_F_V() 
        {
            var x = V[OP >> 8 & 0x0F];
            I = (ushort)(5 * x);
            PC += 2; 
        }

        private void I_Fx33_LD_B_V() { var dec = V[OP >> 8 & 0x0F]; memory[I] = (byte)((dec % 1000) / 100); memory[I + 1] = (byte)((dec % 100) / 10); memory[I + 2] = (byte)(dec % 10); PC += 2; }

        private void I_Fx55_LD_I_V() 
        {
            var x = OP >> 8 & 0x0F;
            for(int i = 0; i < x; i++)
            {
                memory[I + i] = V[i];
            }
            PC += 2;
        }

        private void I_Fx65_LD_Vx() { for (int i = 0; i <= (OP >> 8 & 0x0F); i++) V[i] = memory[I + i]; PC += 2; }

        internal void Tick()
        {
            DT = (byte)Math.Max(DT - 1, 0);
        }

        public class Screen
        {
            public const int displayWidth = 64;

            public const int displayHeight = 64;

            internal bool[,] pixels = new bool[displayWidth, displayHeight];

            public void Reset() {
                Array.Clear(pixels, 0, pixels.Length);
            }
        }

        private static byte[] GetDigitBytes()
        {
            return new byte[]
            {
                0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2 
                0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
                0x90, 0x90, 0xF0, 0x10, 0x10, // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
                0xF0, 0x10, 0x20, 0x40, 0x40, // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90, // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
                0xF0, 0x80, 0x80, 0x80, 0xF0, // C
                0xE0, 0x90, 0x90, 0x90, 0xE0, // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
                0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            };
        }
    }
}
