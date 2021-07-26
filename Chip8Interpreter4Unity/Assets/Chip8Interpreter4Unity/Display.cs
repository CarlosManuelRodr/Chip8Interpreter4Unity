using UnityEngine;
using UnityEngine.UI;

namespace Chip8Interpreter4Unity
{
    /// <summary>
    /// Emulates a 64x32 monochrome display.
    /// </summary>
    public class Display : MonoBehaviour
    {
        [SerializeField] Color backgroundColor = Color.black;
        [SerializeField] Color pixelColor = Color.white;

        private Texture2D displayTexture;

        void Awake()
        {
            displayTexture = new Texture2D(64, 32);
            displayTexture.filterMode = FilterMode.Point;
            GetComponent<Image>().material.mainTexture = displayTexture;
            ClearDisplay();
        }

        public void ClearDisplay()
        {
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 32; j++)
                    displayTexture.SetPixel(i, j, backgroundColor);
            }
        }

        public void SetPixel(int x, int y, bool on)
        {
            Color pixel = on ? pixelColor : backgroundColor;
            displayTexture.SetPixel(x, 31 - y, pixel);
        }

        public void ApplyChanges()
        {
            displayTexture.Apply();
        }
    }
}