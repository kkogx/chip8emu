using Microsoft.Extensions.DependencyInjection;
using System;

namespace Chip8Emu
{
    public static class Program
    {

        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();
            services.AddSingleton<SimpleLogger>();
            services.AddSingleton<PerfCounter>();
            services.AddSingleton<Chip8Host>();
            services.AddSingleton<Chip8>();
            services.AddSingleton<Game>();

            var provider = services.BuildServiceProvider();
            provider.GetService<Chip8Host>().Load(READY);
            provider.GetService<Game>().Run(); //enter the MonoGame loop and never come back
        }

        static readonly byte[] READY =
        {
          0x00, //0x200 CLS
          0xe0,
          0xa2, //0x202 LOAD I, 0x21a - Sprite data address
          0x1a,
          0x64, //0x204 LOAD r4, 5 - 5 bytes per sprite
          0x05,
          0x61, //0x206 LOAD r1, 1 - the sprite counter
          0x01,
          0x62, //0x208 LOAD r2, $12 - X Position
          0x12,
          0x63, //0x20a LOAD r3, $0c - Y position
          0x0c,
          0xd2, //0x20c DRAW r2, r3, $5 - Draw sprite
          0x35,
          0x72, //0x20e ADD r2, 5 - Add 5 to X Position
          0x05,
          0xf4, //0x210 ADD I, r4 - Move I to next sprite 
          0x1e,
          0x71, //0x212 ADD r1, 1 - Increment sprite counter
          0x01,
          0x31, //0x214 SE 1, 6 - Skip next instruction if r1 = 6
          0x06,
          0x12, //0x216 JUMP to 0x20c
          0x0c,
          0x12, //0x218 JUMP here - infinite loop
          0x18, 

          //Sprite data - starts at 0x21a
          //R
          0xe0,   //11100000
          0x90,   //10010000
          0xe0,   //11100000
          0x90,   //10010000
          0x90,   //10010000
          //E
          0xf0,   //11110000
          0x80,   //10000000
          0xf0,   //11110000
          0x80,   //10000000
          0xf0,   //11110000
          //A
          0xf0,   //11110000
          0x90,   //10010000
          0xf0,   //11110000
          0x90,   //10010000
          0x90,   //10010000
          //D
          0xe0,   //11100000
          0x90,   //10010000
          0x90,   //10010000
          0x90,   //10010000
          0xe0,   //11100000
          //Y
          0x90,   //10010000
          0x90,   //100100000
          0x60,   //01100000
          0x20,   //00100000
          0x20,   //001000000
        };
    }
}
