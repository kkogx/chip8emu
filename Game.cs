using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace Chip8Emu
{
    public class Game : Microsoft.Xna.Framework.Game
    {
        // Graphics
        const uint PIXEL_ON = 0xFFFFFFFF;
        const uint PIXEL_OFF = 0x00000000;

        const int displayWidth = 64;
        const int displayHeight = 32;
        const int pixelScale = 8;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _canvas;
        private uint[] _pixels;

        // IO
        private KeyboardState _lastKeyState = new();
        private readonly Keys[] _keys = new[]
            {
                Keys.D0, Keys.D1, Keys.D2, Keys.D3,
                Keys.D4, Keys.D5, Keys.D6, Keys.D7,
                Keys.D8, Keys.D9, Keys.A, Keys.B,
                Keys.C, Keys.D, Keys.E, Keys.F,
            };

        // Execution
        private bool running = false;
        private int cpuSpeed = 1000; // [herz]
        private int romIdx = 0;

        // Other
        private readonly Chip8Host _chip;
        private readonly PerfCounter _perf;
        private readonly SimpleLogger _log;

        public Game(Chip8Host chip, PerfCounter perf, SimpleLogger log)
        {
            _chip = chip;
            _perf = perf;
            _log = log;

            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 960;
            _graphics.PreferredBackBufferHeight = 640;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();

            _font = Content.Load<SpriteFont>("Arial");

            _canvas = new Texture2D(GraphicsDevice, displayWidth, displayHeight);
            _pixels = new uint[displayWidth * displayHeight];

            _log.GetLogger()("(L)oad next rom, (R)eload rom");
            _log.GetLogger()("(S)tepping toggle, (Space) to step");
            _log.GetLogger()("(0)-(9), (A)-(F) - Chip8 keys");
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            UpdateKeys();
            UpdateLogic(gameTime);
            UpdatePixels();
            UpdateLog();
        }

        private void UpdateLogic(GameTime gameTime)
        {
            if(running)
            {
                var cycles = gameTime.ElapsedGameTime.Milliseconds * cpuSpeed / 1000 + 1;
                for(int i = 0; i < cycles; i++)
                {
                    _chip.Cycle();
                }
            }
        }

        private void UpdateKeys()
        {
            var currentState = Keyboard.GetState();

            if (currentState.IsKeyPressed(_lastKeyState, Keys.Escape))
                Exit();

            if (currentState.IsKeyPressed(_lastKeyState, Keys.S))
            {
                running = !running;
                _log.disableExpensiveLogs = running;
                _log.GetLogger()($"Stepping={!running}");
            }

            if (currentState.IsKeyPressed(_lastKeyState, Keys.Space))
                _chip.Cycle();

            if (currentState.IsKeyPressed(_lastKeyState, Keys.R))
                _chip.Reload();

            if (currentState.IsKeyPressed(_lastKeyState, Keys.L))
                LoadNextRom();

            for (int i = 0; i < _keys.Length; i++)
                _chip.Keys[i] = currentState.IsKeyDown(_keys[i]);

            _lastKeyState = currentState;
        }

        private void LoadNextRom()
        {
            var files = Directory.GetFiles("Roms");
            var rom = files[romIdx++ % files.Length];
            _chip.Load(rom);
        }

        private void UpdatePixels()
        {
            var pixels = _chip.GetPixels();
            for (int x = 0; x < displayWidth; x++)
            {
                for (int y = 0; y < displayHeight; y++)
                {
                    PSet(x, y, pixels[x, y] ? PIXEL_ON : PIXEL_OFF);
                }
            }
        }

        private void UpdateLog()
        {
            // remove old msgs
            var _logQueue = _log.Messages;
            //_logQueue.RemoveAll(m => DateTimeOffset.Now.ToUnixTimeMilliseconds() - m.Item2 > 60*1000);
            _logQueue.RemoveRange(Math.Min(20, _logQueue.Count), Math.Max(_logQueue.Count - 20, 0));
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            var spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            //draw opcode, I, pc
            const int mainOffsetY = 280;
            const int mainOffsetX = 8;
            const int mainColumnDist = 80;
            var regs = new[] {
                ("OP", _chip.OP),
                ("PC", _chip.PC),
                ("SP", _chip.SP),
                ("DT", _chip.DT),
                (" I", _chip.I) 
            };
            for (int i = 0; i < regs.Length; i++) {
                spriteBatch.DrawString(_font, regs[i].Item1,
                    new Vector2(mainColumnDist * i + mainOffsetX, mainOffsetY), Color.DarkBlue);
                spriteBatch.DrawString(_font, regs[i].Item2.ToHex(),
                    new Vector2(mainColumnDist * i + mainOffsetX + 30, mainOffsetY), Color.White);
            }

            //draw registries
            const int regOffsetY = 310;
            const int regOffsetX = 16;
            var vregs = _chip.GetRegistries();
            for (int regIdx = 0; regIdx < 16; regIdx++)
            {
                var regHex = regIdx.ToString("X");
                var offset = _font.MeasureString(regHex);
                int row = regIdx / 8;
                int col = regIdx % 8;
                var hex = vregs[regIdx].ToHex();
                spriteBatch.DrawString(_font, regHex, new Vector2(50 * col - offset.X + regOffsetX, 15 * row + regOffsetY), Color.DarkBlue);
                spriteBatch.DrawString(_font, hex, new Vector2(50 * col + 5 + regOffsetX, 15 * row + regOffsetY), Color.White);
            }

            //draw memory dump
            const int memOffsetY = 360;
            const int memOffsetX = 8;
            var hexData = ToHexRows(_chip.GetProgramMemoryDump());
            for (int colIdx = 0; colIdx < hexData.GetLength(0); colIdx++)
            {
                for (int rowIdx = 0; rowIdx < hexData.GetLength(1); rowIdx += 1)
                {
                    var hex = hexData[colIdx, rowIdx];
                    if (hex != null)
                    {
                        var hexPositionInMemory = rowIdx * hexData.GetLength(0) + colIdx + 0x200;
                        spriteBatch.DrawString(_font, hex, new Vector2(25 * colIdx + memOffsetX, 15 * rowIdx + memOffsetY), 
                            (_chip.PC == hexPositionInMemory || _chip.PC == hexPositionInMemory - 1) ? Color.Purple : Color.White);
                    }
                }
            }

            //draw log
            var _logQueue = _log.Messages;
            for(int msgIdx = 0; msgIdx < _logQueue.Count; msgIdx++)
            {
                spriteBatch.DrawString(_font, _logQueue[msgIdx].Item1, new Vector2(420, 18 * msgIdx + mainOffsetY), Color.Purple);
            }

            GraphicsDevice.Textures[0] = null;
            _canvas.SetData(_pixels, 0, displayWidth * displayHeight);
            spriteBatch.Draw(_canvas, new Rectangle(8, 8, displayWidth * pixelScale, displayHeight * pixelScale), Color.White);

            spriteBatch.End();

            _perf.CountDraw();
        }

        public void PSet(int x, int y, uint color)
        {
            var pos = x + (y * displayWidth);

            if (pos >= 0 && pos < _pixels.Length)
            {
                _pixels[pos] = color;
            }
        }

        public uint PGet(int x, int y)
        {
            var pos = x + (y * displayWidth);

            if (pos >= 0 && pos < _pixels.Length)
            {
                return _pixels[pos];
            }

            return 0;
        }

        private static string[,] ToHexRows(byte[] memory)
        {
            var hexArray = memory.ToHex().ToCharArray();

            var result = new string[16, 256];
            for(int hexIdx = 0; hexIdx < hexArray.Length-1; hexIdx+=2)
            {
                var idx = hexIdx / 2;
                result[idx % 16, idx / 16] = new string(new char[] { hexArray[hexIdx], hexArray[hexIdx + 1] });
            }

            return result;
        }
    }
}
