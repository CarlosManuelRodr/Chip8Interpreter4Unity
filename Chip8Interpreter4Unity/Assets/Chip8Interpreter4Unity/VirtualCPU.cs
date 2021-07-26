using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Chip8Interpreter4Unity
{
    /// <summary>
    /// CPU implementation of the Chip-8 interpreter.
    /// References: http://devernay.free.fr/hacks/chip8/C8TECH10.HTM
    ///             https://tobiasvl.github.io/blog/write-a-chip-8-emulator/
    /// </summary>
    public class VirtualCPU
    {
        public byte[] RAM { get; private set; }
        public byte[] V { get; private set; }
        public Stack<ushort> Stack { get; private set; }
        public ushort I { get; private set; }
        public ushort PC { get; private set; }
        public byte DelayTimer { get; private set; }
        public byte SoundTimer { get; private set; }
        public byte Keyboard { get; set; }

        public byte[] Display { get; private set; }

        private Random rng;
        private Stopwatch watch;
        bool waitingForKeyPress;

        #region Public interface
        public VirtualCPU()
        {
            Initialize();
        }

        public void Initialize()
        {
            RAM = new byte[4096];
            V = new byte[16];
            Stack = new Stack<ushort>();
            I = 0;
            PC = 0;
            Display = new byte[64 * 32];
            Keyboard = 0x00;
            rng = new Random();
            watch = new Stopwatch();
            waitingForKeyPress = false;
        }

        private void InitializeFont()
        {
            // Default font is loaded at adresses 0x000 to 0x1FF.
            byte[] characters = new byte[]
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

            Array.Copy(characters, RAM, characters.Length);
        }

        public void LoadProgram(ushort[] program)
        {
            RAM = new byte[4096];
            InitializeFont();

            for (int i = 0; i < program.Length; i++)
            {
                RAM[0x200 + 0x2 * i] = GetFirstByte(program[i]);
                RAM[0x201 + 0x2 * i] = GetSecondByte(program[i]);
            }
            PC = 0x200;
        }

        public void Step()
        {
            // Update timers.
            if (!watch.IsRunning)
                watch.Start();
            if (watch.ElapsedMilliseconds > 16)
            {
                if (DelayTimer > 0)
                    DelayTimer--;
                if (SoundTimer > 0)
                    SoundTimer--;
                watch.Restart();
            }

            // Fetch opcode.
            ushort opcode = FetchOpcodeFromRAM();

            byte nibble = GetNibble(opcode);
            byte x = GetX(opcode);
            byte y = GetY(opcode);
            byte n = GetN(opcode);
            byte nn = GetNN(opcode);
            ushort nnn = GetNNN(opcode);

            // Execute.
            if (waitingForKeyPress)
            {
                LDxk2(x);
                return;
            }

            PC += 2;

            switch (nibble)
            {
                case 0x00:
                    switch (nn)
                    {
                        case 0xE0:
                            CLS();
                            break;
                        case 0xEE:
                            RET();
                            break;
                        default:
                            ThrowUnsupportedOpcode(opcode);
                            break;
                    }
                    break;
                case 0x01:
                    JPnnn(nnn);
                    break;
                case 0x02:
                    CALLnnn(nnn);
                    break;
                case 0x03:
                    SExkk(x, nn);
                    break;
                case 0x04:
                    SNExkk(x, nn);
                    break;
                case 0x05:
                    SExy(x, y);
                    break;
                case 0x06:
                    LDxkk(x, nn);
                    break;
                case 0x07:
                    ADDxkk(x, nn);
                    break;
                case 0x08:
                    switch (n)
                    {
                        case 0x00:
                            LDxy(x, y);
                            break;
                        case 0x01:
                            ORxy(x, y);
                            break;
                        case 0x02:
                            ANDxy(x, y);
                            break;
                        case 0x03:
                            XORxy(x, y);
                            break;
                        case 0x04:
                            ADDxy(x, y);
                            break;
                        case 0x05:
                            SUBxy(x, y);
                            break;
                        case 0x06:
                            SHRx(x);
                            break;
                        case 0x07:
                            SUBN(x, y);
                            break;
                        case 0x0E:
                            SHLx(x);
                            break;
                        default:
                            ThrowUnsupportedOpcode(opcode);
                            break;
                    }
                    break;
                case 0x09:
                    SNExy(x, y);
                    break;
                case 0x0A:
                    LDnnn(nnn);
                    break;
                case 0x0B:
                    JPv0nnn(nnn);
                    break;
                case 0x0C:
                    RNDxkk(x, nn);
                    break;
                case 0x0D:
                    DRWxyn(x, y, n);
                    break;
                case 0x0E:
                    switch (nn)
                    {
                        case 0x9E:
                            SKPx(x);
                            break;
                        case 0xA1:
                            SKNPx(x);
                            break;
                        default:
                            ThrowUnsupportedOpcode(opcode);
                            break;
                    }
                    break;
                case 0x0F:
                    switch (nn)
                    {
                        case 0x07:
                            LDxdt(x);
                            break;
                        case 0x0A:
                            LDxk1();
                            break;
                        case 0x15:
                            LDdtx(x);
                            break;
                        case 0x18:
                            LDstx(x);
                            break;
                        case 0x1E:
                            ADDix(x);
                            break;
                        case 0x29:
                            LDfx(x);
                            break;
                        case 0x33:
                            LDbx(x);
                            break;
                        case 0x55:
                            LDix(x);
                            break;
                        case 0x65:
                            LDxi(x);
                            break;
                        default:
                            ThrowUnsupportedOpcode(opcode);
                            break;
                    }
                    break;
                default:
                    ThrowUnsupportedOpcode(opcode);
                    break;
            }
        }
        private void ThrowUnsupportedOpcode(ushort opcode)
        {
            throw new Exception($"Unsupported opcode {opcode.ToString("X4")}");
        }

        #endregion

        #region Opcode fetching
        private ushort FetchOpcodeFromRAM()
        {
            return (ushort)((RAM[PC] << 8) | RAM[PC + 0x1]);
        }

        private byte GetFirstByte(ushort twoBytes)
        {
            return (byte)((twoBytes & 0xFF00) >> 8);
        }
        private byte GetSecondByte(ushort twoBytes)
        {
            return GetNN(twoBytes);
        }
        private byte GetNibble(ushort opcode)
        {
            return (byte)((opcode & 0xF000) >> 12);
        }

        private byte GetN(ushort opcode)
        {
            return (byte)(opcode & 0x000F);
        }

        private byte GetNN(ushort opcode)
        {
            return (byte)(opcode & 0x00FF);
        }

        private ushort GetNNN(ushort opcode)
        {
            return (ushort)(opcode & 0x0FFF);
        }

        private byte GetX(ushort opcode)
        {
            return (byte)((opcode & 0x0F00) >> 8);
        }

        private byte GetY(ushort opcode)
        {
            return (byte)((opcode & 0x00F0) >> 4);
        }
        #endregion

        #region Opcodes execution
        private void CLS()
        {
            for (int i = 0; i < Display.Length; i++)
                Display[i] = 0;
        }

        private void RET()
        {
            PC = Stack.Pop();
        }

        private void JPnnn(ushort nnn)
        {
            PC = nnn;
        }

        private void JPv0nnn(ushort nnn)
        {
            PC = (ushort)(nnn + V[0x0]);
        }

        private void CALLnnn(ushort nnn)
        {
            Stack.Push(PC);
            PC = nnn;
        }

        private void SExkk(byte x, byte kk)
        {
            if (V[x] == kk)
                PC += 2;
        }

        private void SExy(byte x, byte y)
        {
            if (V[x] == V[y])
                PC += 2;
        }

        private void SNExkk(byte x, byte kk)
        {
            if (V[x] != kk)
                PC += 2;
        }

        private void SNExy(byte x, byte y)
        {
            if (V[x] != V[y])
                PC += 2;
        }

        private void LDxkk(byte x, byte kk)
        {
            V[x] = kk;
        }

        private void LDxy(byte x, byte y)
        {
            V[x] = V[y];
        }

        private void LDnnn(ushort nnn)
        {
            I = nnn;
        }

        private void LDxdt(byte x)
        {
            V[x] = DelayTimer;
        }

        private void LDdtx(byte x)
        {
            DelayTimer = V[x];
        }

        private void LDstx(byte x)
        {
            SoundTimer = V[x];
        }

        private void LDxk1()
        {
            waitingForKeyPress = true;
        }

        private void LDxk2(byte x)
        {
            if (Keyboard != 0x00)
            {
                V[x] = Keyboard;
                waitingForKeyPress = false;
            }
        }

        private void LDfx(byte x)
        {
            I = (ushort)(5 * V[x]);
        }

        private void LDbx(byte x)
        {
            RAM[I] = (byte)(V[x] / 100);
            RAM[I + 1] = (byte)((V[x] % 100) / 10);
            RAM[I + 2] = (byte)(V[x] % 10);
        }

        private void LDix(byte x)
        {
            for (int i = 0; i <= x; i++)
                RAM[I + i] = V[i];
        }

        private void LDxi(byte x)
        {
            for (int i = 0; i <= x; i++)
                V[i] = RAM[I + i];
        }

        private void ADDxkk(byte x, byte kk)
        {
            V[x] += kk;
        }

        private void ADDxy(byte x, byte y)
        {
            ushort sum = (ushort)(V[x] + V[y]);
            V[0xF] = (byte)(sum > 255 ? 1 : 0);
            V[x] = (byte)(sum & 0x00FF);
        }

        private void ADDix(byte x)
        {
            I = (ushort)(I + V[x]);
        }

        private void SUBxy(byte x, byte y)
        {
            V[0xF] = (byte)(V[x] > V[y] ? 1 : 0);
            V[x] = (byte)(V[x] - V[y] & 0x00FF);
        }

        private void SUBN(byte x, byte y)
        {
            V[0xF] = (byte)(V[y] > V[x] ? 1 : 0);
            V[x] = (byte)(V[y] - V[x] & 0x00FF);
        }

        private void ORxy(byte x, byte y)
        {
            V[x] = (byte)(V[x] | V[y]);
        }

        private void ANDxy(byte x, byte y)
        {
            V[x] = (byte)(V[x] & V[y]);
        }

        private void XORxy(byte x, byte y)
        {
            V[x] = (byte)(V[x] ^ V[y]);
        }

        private void SHRx(byte x)
        {
            V[x] = (byte)(V[x] >> 1);
        }

        private void SHLx(byte x)
        {
            V[x] = (byte)(V[x] << 1);
        }

        private void RNDxkk(byte x, byte kk)
        {
            byte randomByte = (byte)rng.Next(0, 255);
            V[x] = (byte)(randomByte & kk);
        }

        private void DRWxyn(byte x, byte y, byte n)
        {
            int xCoord = V[x];
            int yCoord = V[y];
            int nInt = (int)n;

            V[0xF] = 0;

            for (int i = 0; i < nInt; i++)
            {
                byte spriteByte = RAM[I + i];
                for (int bit = 0; bit < 8; bit++)
                {
                    byte spriteBit = (byte)((spriteByte >> (7 - bit)) & 1);
                    int index = xCoord + bit + 64 * (i + yCoord);
                    if (index >= 2048)
                        continue;

                    if (spriteBit == 1 && Display[index] == 1)
                        V[0xF] = 1;

                    Display[index] = (byte)(Display[index] ^ spriteBit);
                }
            }
        }

        private void SKPx(byte x)
        {
            if (Keyboard == V[x])
                PC += 2;
        }

        private void SKNPx(byte x)
        {
            if (Keyboard != V[x])
                PC += 2;
        }

        #endregion
    }
}