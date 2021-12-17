using System.Threading;

namespace Chip8Emu
{
    public class Controller
    {
        public readonly Semaphore step = new(0, 1);

        public bool stepping = true;
    }
}
