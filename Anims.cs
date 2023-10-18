using FrogCore;
using System.Collections;
using UnityEngine.Rendering;

namespace Kindred_Spirit
{
    internal static class Anims
    {

        private static bool init = false;
        public static Dictionary<string, List<Sprite>> animationset = new Dictionary<string, List<Sprite>>();

        private static void LoadAnimation(string path, string name, int length)
        {
            List<Sprite> sprites = new List<Sprite>();
            for (int i = 0; i < length; i++)
            {
                //path takes you to the folder.
                sprites.Add(ResourceLoader.LoadSprite(path + "." + (i + 1).ToString() + ".png"));
            }

            animationset.Add(name, sprites);
        }

        private static void LoadKnightAnimation(string path, string name, int fps, tk2dSpriteAnimationClip.WrapMode wrapmode, int length, int xbound, int ybound)
        {
            tk2dSpriteAnimator animator = HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>();
            List<tk2dSpriteAnimationClip> list = animator.Library.clips.ToList<tk2dSpriteAnimationClip>();

            Texture2D texture1 = ResourceLoader.LoadTexture2D(path);

            string[] names = new string[length];
            Rect[] rects = new Rect[length];
            Vector2[] anchors = new Vector2[length];

            for (int i = 0; i < length; i++)
            {
                names[i] = name + (i + 1).ToString();
                rects[i] = new Rect(i * xbound, i * ybound, xbound, ybound);
                anchors[i] = new Vector2(0, 0);
            }

            GameObject knight = HeroController.instance.gameObject;

            tk2dSpriteCollectionData spriteCollectiondata = FrogCore.Utils.CreateTk2dSpriteCollection(texture1, names, rects, anchors, new GameObject());

            //lil cheap trick
            spriteCollectiondata.spriteDefinitions[0].material.shader = knight.GetComponent<MeshRenderer>().sharedMaterial.shader;

            tk2dSpriteAnimationFrame[] list1 = new tk2dSpriteAnimationFrame[length];

            for (int i = 0; i < length; i++)
            {
                tk2dSpriteAnimationFrame frame = new tk2dSpriteAnimationFrame();
                frame.spriteCollection = spriteCollectiondata;
                frame.spriteId = i;
                list1[i] = frame;
            }

            tk2dSpriteAnimationClip clip = new tk2dSpriteAnimationClip();
            clip.name = name;
            clip.fps = fps;
            clip.frames = list1;
            clip.wrapMode = wrapmode;

            clip.SetCollection(spriteCollectiondata);

            foreach (tk2dSpriteDefinition sprite in spriteCollectiondata.spriteDefinitions)
            {
                if (SpritePositions.spritepositions.ContainsKey(sprite.name))
                {
                    sprite.positions = SpritePositions.spritepositions[sprite.name];
                }
            }

            list.Add(clip);
            animator.Library.clips = list.ToArray();
        }

        public static IEnumerator PlayAnimation(string name, SpriteRenderer renderer, float length)
        {
            if (animationset.ContainsKey(name))
            {
                List<Sprite> sprites = animationset[name];
                float numofframes = sprites.Count;
                float fps = (1 / numofframes) * length; //for now,at least.
                for (int i = 0; i < sprites.Count; i++)
                {
                    if (renderer != null)
                    {
                        renderer.sprite = sprites[i];
                    }
                    yield return new WaitForSeconds(fps);
                }
            }
        }

        public static void setLoop(string name, int framestart)
        {
            tk2dSpriteAnimator animator = HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>();
            animator.GetClipByName(name).loopStart = framestart;
            animator.GetClipByName(name).wrapMode = tk2dSpriteAnimationClip.WrapMode.LoopSection;
        }

        //Back to it, I guess. Less sprites this time. That's nice.
        public static void AnimationInit()
        {
            if (!init)
            {
                init = true;
                //knight anims - since im copying knight anims im just adding it. i dont care.
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Idle.set.png", "IdleGV", 12, tk2dSpriteAnimationClip.WrapMode.Loop, 9, 61, 130);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Slash.set.png", "SlashGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 15, 109, 128);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.AltSlash.set.png", "SlashAltGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 15, 136, 133);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.UpSlash.set.png", "UpSlashGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 15, 116, 131);
                //
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Airborne.set.png", "AirborneGV", 16, tk2dSpriteAnimationClip.WrapMode.Once, 12, 103, 146);
                setLoop("AirborneGV", 9);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Dash.set.png", "DashGV", 18, tk2dSpriteAnimationClip.WrapMode.Once, 11, 192, 117);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Fall.set.png", "FallGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 6, 91, 135);
                setLoop("FallGV", 3);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.FireballAntic.set.png", "Fireball AnticGV", 30, tk2dSpriteAnimationClip.WrapMode.Once, 3, 81, 120);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Fireball2Cast.set.png", "Fireball2 CastGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 6, 212, 175);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Turn.set.png", "TurnGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 2, 72, 127);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Run.set.png", "RunGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 13, 86, 130);
                setLoop("RunGV", 6);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Land.set.png", "LandGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 3, 89, 133);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.IntroJump.set.png", "IntroJumpGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 1, 192, 117);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.CollectNormal3.set.png", "StunRiseGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 5, 156, 149);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.ToProne.set.png", "StunGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 5, 156, 149);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Death.set.png", "DeathGV", 10, tk2dSpriteAnimationClip.WrapMode.Once, 18, 116, 131);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.DoubleJump.set.png", "Double JumpGV", 16, tk2dSpriteAnimationClip.WrapMode.Once, 8, 137, 150);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.DownSlash.set.png", "DownSlashGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 6, 142, 130);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Focus.set.png", "FocusGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 7, 74, 126);
                setLoop("FocusGV", 2);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.FocusEnd.set.png", "Focus EndGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 3, 71, 126);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.FocusGetOnce.set.png", "Focus Get OnceGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 11, 102, 123);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.GetOff.set.png", "Get OffGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 5, 92, 140);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.QuakeAntic.set.png", "Quake AnticGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 5, 95, 136);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.Scream2.set.png", "Scream 2GV", 20, tk2dSpriteAnimationClip.WrapMode.Loop, 7, 306, 145);
                setLoop("Scream 2GV", 1);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.ScreamEnd2.set.png", "Scream End 2GV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 1, 106, 129);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.ScreamStart.set.png", "Scream StartGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 2, 76, 118);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.ShadowDash.set.png", "Shadow DashGV", 20, tk2dSpriteAnimationClip.WrapMode.Once, 11, 294, 139);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.SittingAsleep.set.png", "Sitting AsleepGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 1, 82, 118);
                LoadKnightAnimation("Kindred_Spirit.Resources.Sprites.WakeUpToSit.set.png", "Wake To SitGV", 12, tk2dSpriteAnimationClip.WrapMode.Once, 9, 84, 134);


            }

        }
    }
}
