using FrogCore.Ext;
using System.Reflection;
using Vasi;

namespace Kindred_Spirit
{
    internal class ResourceLoader 
    {
        public static GameObject hkprime;
        public static GameObject megamoss;
        public static GameObject shadeenemy;
        public static GameObject nosk;
        public static void Initialise(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            hkprime = preloadedObjects["GG_Hollow_Knight"]["Battle Scene/HK Prime"];
            UnityEngine.Object.DontDestroyOnLoad(hkprime);
            megamoss = preloadedObjects["GG_Mega_Moss_Charger"]["Mega Moss Charger"];
            UnityEngine.Object.DontDestroyOnLoad(megamoss);
            shadeenemy = preloadedObjects["Abyss_15"]["Shade Sibling (32)"];
            UnityEngine.Object.DontDestroyOnLoad(shadeenemy);
            nosk = preloadedObjects["GG_Nosk"]["Mimic Spider"];
            UnityEngine.Object.DontDestroyOnLoad (nosk);
        }

        public static Texture2D LoadTexture2D(string path)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            MemoryStream memoryStream = new((int)stream.Length);
            stream.CopyTo(memoryStream);
            stream.Close();
            var bytes = memoryStream.ToArray();
            memoryStream.Close();

            var texture2D = new Texture2D(1, 1);
            _ = texture2D.LoadImage(bytes);
            texture2D.anisoLevel = 0;

            return texture2D;
        }

        public static Sprite LoadSprite(string path)
        {
            Texture2D texture = LoadTexture2D(path);
            return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), Vector2.one / 2, 100.0f);
        }

    }
}
