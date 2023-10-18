using System.Collections;
using Vasi;

namespace Kindred_Spirit
{
    internal class DreamToggler : MonoBehaviour
    {
        public static void Create(GameObject megamossstatue)
        {
            BossStatue bossStatue = megamossstatue.GetComponent<BossStatue>();
            bossStatue.isAlwaysUnlockedDream = true;
            bossStatue.isAlwaysUnlocked = true;

            bossStatue.dreamStatueStatePD = "statueStateMegaMossCharger";
            bossStatue.dreamBossDetails = bossStatue.bossDetails;
            bossStatue.dreamBossScene = bossStatue.bossScene;

            GameObject toggler = megamossstatue.Child("dream_version_switch");
            toggler.SetActive(true);

            BossStatueDreamToggle dreamtoggle = toggler.GetComponentInChildren<BossStatueDreamToggle>(true);

            dreamtoggle.SetState(true);

            GameManager.instance.StartCoroutine(DelayedActivation(dreamtoggle, bossStatue));
        }

        private static IEnumerator DelayedActivation(BossStatueDreamToggle toggler, BossStatue statue)
        {
            yield return new WaitForFinishedEnteringScene();
            toggler.SetOwner(statue);
            Kindred_Spirit.DreamToggle = statue.UsingDreamVersion;
            statue.SetDreamVersion(Kindred_Spirit.DreamToggle, true, false);
            if (!Kindred_Spirit.DreamToggle)
            {
                if (toggler.gameObject.Child("lit_pieces") != null)
                {
                    toggler.gameObject.Child("lit_pieces").SetActive(false);
                }
                toggler.gameObject.Child("Statue Pt").GetComponent<ParticleSystem>().Stop();
            }
        }
    }
}
