using Microsoft.Xna.Framework.Input;
using System;

namespace Chip8Emu
{
    public static class GameExtensions
    {
        public static bool IsKeyPressed(this KeyboardState state, KeyboardState prevState, Keys key)
        {
            return state.IsKeyDown(key) && !prevState.IsKeyDown(key);
        }
    }
}
