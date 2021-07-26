using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using UnityEngine;

namespace Chip8Interpreter4Unity
{
    /// <summary>
    /// Utility class to parse opcodes.
    /// </summary>
    public static class BinaryReaderExtensions
    {
        public static ushort ReadUInt16BigEndian(this BinaryReader binaryReader)
        {
            return (ushort)((binaryReader.ReadByte() << 8) | binaryReader.ReadByte());
        }
    }

    /// <summary>
    /// Chip-8 Unity interpreter. Handles communication to display, speaker and input manager.
    /// </summary>
    public class Chip8Interpreter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Display display;
        [SerializeField] AudioSource speaker;
        [SerializeField] KeyboardInputManager inputManager;

        [Header("Parameters")]
        [SerializeField] TextAsset rom;
        [SerializeField] bool autostart;

        private VirtualCPU cpu;
        private Thread interpreterThread = null;
        private bool running = false;
        private bool playingSound = false;
        private Stopwatch timer;

        #region Monobehavior entry points
        private void Start()
        {
            timer = new Stopwatch();
            cpu = new VirtualCPU();
            inputManager.onKeyDown += PressKey;
            inputManager.onKeyUp += ReleaseKey;

            LoadROM();

            if (autostart)
                StartExecution();
        }

        private void OnDisable()
        {
            StopExecution();
        }

        private void Update()
        {
            DrawDisplay();
            PlaySound();

            if (Input.GetKeyDown(KeyCode.Space))
                ResetGame();
        }
        #endregion

        #region Public interface
        /// <summary>
        /// Reads a .bytes file and store the data into the CPU memory.
        /// </summary>
        public void LoadROM()
        {
            Stream stream = new MemoryStream(rom.bytes);

            List<ushort> program = new List<ushort>();
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length - 1)
                    program.Add(reader.ReadUInt16BigEndian());
            }

            cpu.LoadProgram(program.ToArray());
        }

        /// <summary>
        /// Copy the display buffer into the screen.
        /// </summary>
        private void DrawDisplay()
        {
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    bool isOn = cpu.Display[x + 64 * y] != 0;
                    display.SetPixel(x, y, isOn);
                }
            }
            display.ApplyChanges();
        }

        /// <summary>
        /// Activates sound if the CPU SoundTimer is not equal to zero.
        /// </summary>
        private void PlaySound()
        {
            if (!playingSound && cpu.SoundTimer != 0x00)
            {
                speaker.loop = true;
                speaker.Play();
                playingSound = true;
            }
            else if (playingSound && cpu.SoundTimer == 0)
            {
                speaker.Stop();
                playingSound = false;
            }
        }

        /// <summary>
        /// Handle KeyPress event.
        /// </summary>
        /// <param name="input">Nibble corresponding to the pressed key.</param>
        private void PressKey(byte input)
        {
            cpu.Keyboard = input;
        }

        /// <summary>
        /// Handle KeyRelease event.
        /// </summary>
        private void ReleaseKey()
        {
            cpu.Keyboard = 0x00;
        }

        /// <summary>
        /// Creates a thread for the Chip-8 CPU and starts it.
        /// </summary>
        public void StartExecution()
        {
            running = true;
            interpreterThread = new Thread(Run);
            interpreterThread.Start();
        }

        /// <summary>
        /// Abort the thread executing the CPU.
        /// </summary>
        public void StopExecution()
        {
            if (running)
            {
                running = false;
                interpreterThread.Abort();
                timer.Stop();
            }
        }

        /// <summary>
        /// Stops execution, clears registers and start again.
        /// </summary>
        public void ResetGame()
        {
            StopExecution();
            cpu.Initialize();
            LoadROM();
            StartExecution();
        }

        /// <summary>
        /// Handles the CPU ticks. 1000 instructions are processed every second.
        /// </summary>
        private void Run()
        {
            timer.Start();
            while (running)
            {
                if (timer.ElapsedMilliseconds > 1)
                {
                    cpu.Step();
                    timer.Restart();
                }
            }
        }

        #endregion
    }
}