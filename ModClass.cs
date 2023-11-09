using System.Collections;
using Vasi;

namespace Kindred_Spirit
{
    public class Kindred_Spirit : Mod
    {
        internal static Kindred_Spirit Instance;
        public static bool DreamToggle = true;
        public static bool AbsoluteToggle = false;
        private int numberofhits = 0;
        private readonly List<string> statueName = new List<string>() { "Fallen Vessel", "Kindred Spirit", "Absolute Kindred Spirit" };
        private readonly List<string> statueDesc = new List<string>() { "Betrayed god of shadows", "Fellow god of the Abyss", "Pureborn god of Void" };
        private readonly List<string> bossMain = new List<string>() { "Vessel", "Spirit", "Kindred Spirit" };
        private readonly List<string> bossSuper = new List<string>() { "Fallen", "Kindred", "Absolute" };
        private readonly List<string> dreamnailDialogue = new List<string>() { "Betrayed! Betrayed!", "Sister... Trusted you...", "Trust none... Fight!" };
        public Kindred_Spirit() : base("Kindred Spirit") { }
        public override string GetVersion() => "v1.0.0.2";

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("GG_Hollow_Knight","Battle Scene/HK Prime"),
                ("GG_Mega_Moss_Charger","Mega Moss Charger"),
                //Shade Sibling (31) abyss 15
                ("Abyss_15", "Shade Sibling (32)"),
                ("GG_Nosk", "Mimic Spider")
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance = this;
            ResourceLoader.Initialise(preloadedObjects);
            SpritePositions.Initialise();

            On.HeroController.Start += AnimInit;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += CheckScene;
            ModHooks.LanguageGetHook += ChangeText;
            On.BossStatue.SetDreamVersion += SetDream;
        }

        private void SetDream(On.BossStatue.orig_SetDreamVersion orig, BossStatue self, bool value, bool useAltStatue, bool doAnim)
        {
            orig.Invoke(self, value, useAltStatue, doAnim);
            if (self.gameObject.name == "GG_Statue_MegaMossCharger")
            {
                DreamToggle = value;
                if (value == true)
                {
                    numberofhits += 1;
                    if (numberofhits == 5)
                    {
                        AbsoluteToggle = true;
                        numberofhits = 0;
                    }
                } else
                {
                    AbsoluteToggle = false;
                }
            }
        }

        private void AnimInit(On.HeroController.orig_Start orig, HeroController self)
        {
            orig.Invoke(self);

            Anims.AnimationInit();
        }

        private string ChangeText(string key, string sheetTitle, string orig)
        {
            int option = DreamToggle ? 1 : 0;
            if (AbsoluteToggle)
            {
                option = 2;
            }
            if (key == "NAME_MEGA_MOSS_CHARGER")
            {
                return statueName[option];
            }
            if (key == "GG_S_MEGAMOSS")
            {
                return statueDesc[option];
            }
            if (key == "FALLEN_VESSEL_MAIN")
            {
                return bossMain[option];
            }
            if (key == "FALLEN_VESSEL_SUB")
            {
                return "";
            }
            if (key == "FALLEN_VESSEL_SUPER")
            {
                return bossSuper[option];
            }
            if (key == "FALLEN_VESSEL_DN_1")
            {
                int random = UnityEngine.Random.Range(0,3);
                return dreamnailDialogue[random];
            }
            return orig;
        }

        private void CheckScene(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            if (arg1.name == "GG_Mega_Moss_Charger")
            {
                GameManager.instance.StartCoroutine(CheckCharger());
            }
            if (arg1.name == "GG_Workshop")
            {
                GameManager.instance.StartCoroutine(AddDreamToggle());
                numberofhits = 0;
            }
        }

        private static IEnumerator AddDreamToggle()
        {
            yield return new WaitWhile(() => (GameObject.Find("GG_Statue_MegaMossCharger") == null));
            DreamToggler.Create(GameObject.Find("GG_Statue_MegaMossCharger"));
        }

        private IEnumerator CheckCharger()
        {
            yield return new WaitWhile(() => GameObject.Find("Mega Moss Charger") == null);
            GameObject bossspawner = new GameObject();
            bossspawner.AddComponent<Boss>();
            GameObject.Find("Mega Moss Charger").SetActive(false);
        }


    }
}