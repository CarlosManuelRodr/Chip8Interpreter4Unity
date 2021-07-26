using System;
using UnityEngine;

namespace Chip8Interpreter4Unity
{
    /// <summary>
    /// Default input manager. Keys are mapped directly to keyboard.
    /// </summary>
    public class KeyboardInputManager : MonoBehaviour
    {
        public event Action<byte> onKeyDown;
        public event Action onKeyUp;

        private KeyCode key0 = KeyCode.Alpha0;
        private KeyCode key1 = KeyCode.Alpha1;
        private KeyCode key2 = KeyCode.Alpha2;
        private KeyCode key3 = KeyCode.Alpha3;
        private KeyCode key4 = KeyCode.Alpha4;
        private KeyCode key5 = KeyCode.Alpha5;
        private KeyCode key6 = KeyCode.Alpha6;
        private KeyCode key7 = KeyCode.Alpha7;
        private KeyCode key8 = KeyCode.Alpha8;
        private KeyCode key9 = KeyCode.Alpha9;
        private KeyCode keyA = KeyCode.A;
        private KeyCode keyB = KeyCode.B;
        private KeyCode keyC = KeyCode.C;
        private KeyCode keyD = KeyCode.D;
        private KeyCode keyE = KeyCode.E;
        private KeyCode keyF = KeyCode.F;

        void Update()
        {
            // KeyDown events.
            if (Input.GetKeyDown(key0))
                onKeyDown?.Invoke(0x00);
            else if (Input.GetKeyDown(key1))
                onKeyDown?.Invoke(0x01);
            else if (Input.GetKeyDown(key2))
                onKeyDown?.Invoke(0x02);
            else if (Input.GetKeyDown(key3))
                onKeyDown?.Invoke(0x03);
            else if (Input.GetKeyDown(key4))
                onKeyDown?.Invoke(0x04);
            else if (Input.GetKeyDown(key5))
                onKeyDown?.Invoke(0x05);
            else if (Input.GetKeyDown(key6))
                onKeyDown?.Invoke(0x06);
            else if (Input.GetKeyDown(key7))
                onKeyDown?.Invoke(0x07);
            else if (Input.GetKeyDown(key8))
                onKeyDown?.Invoke(0x08);
            else if (Input.GetKeyDown(key9))
                onKeyDown?.Invoke(0x09);
            else if (Input.GetKeyDown(keyA))
                onKeyDown?.Invoke(0x0A);
            else if (Input.GetKeyDown(keyB))
                onKeyDown?.Invoke(0x0B);
            else if (Input.GetKeyDown(keyC))
                onKeyDown?.Invoke(0x0C);
            else if (Input.GetKeyDown(keyD))
                onKeyDown?.Invoke(0x0D);
            else if (Input.GetKeyDown(keyE))
                onKeyDown?.Invoke(0x0E);
            else if (Input.GetKeyDown(keyF))
                onKeyDown?.Invoke(0x0F);

            // Release key events.
            if (Input.GetKeyUp(key0) || Input.GetKeyUp(key1) || Input.GetKeyUp(key2) || Input.GetKeyUp(key3) || Input.GetKeyUp(key4) ||
                Input.GetKeyUp(key5) || Input.GetKeyUp(key6) || Input.GetKeyUp(key7) || Input.GetKeyUp(key8) || Input.GetKeyUp(key9) ||
                Input.GetKeyUp(keyA) || Input.GetKeyUp(keyB) || Input.GetKeyUp(keyC) || Input.GetKeyUp(keyD) || Input.GetKeyUp(keyE) ||
                Input.GetKeyUp(keyF))
            {
                onKeyUp?.Invoke();
            }
        }
    }
}