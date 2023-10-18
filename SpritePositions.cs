namespace Kindred_Spirit
{
    internal class SpritePositions : MonoBehaviour
    {

        public static Dictionary<string, Vector3[]> spritepositions = new Dictionary<string, Vector3[]>();

        private static void AddToPos(string name, double left, double right, double top, double bottom)
        {
            //assumes knight facing the right.
            List<Vector3> list = new List<Vector3>
            {
                new Vector3((float)left, (float)bottom, 0),
                new Vector3((float) right, (float) bottom, 0),
                new Vector3((float) left, (float) top, 0),
                new Vector3((float) right, (float) top, 0),
            };

            spritepositions.Add(name, list.ToArray());
        }

        private static void AddToPosBulk(string name, double left, double right, double top, double bottom, int lowerinclusive, int upperinclusive)
        {
            for (int i = lowerinclusive; i < upperinclusive + 1; i++)
            {
                AddToPos(name + i, left, right, top, bottom);
            }
        }

        public static void Initialise()
        {
            //SPOILER ALERT.
            //I could NOT figure out a better way to do it. Oh well.

            AddToPosBulk("IdleGV", -0.4219, 0.5312, 0.625, -1.4063,1,9);
            AddToPosBulk("SlashGV", -1.125, 0.46874985, 0.734375, -1.3125, 1, 15);
            AddToPosBulk("SlashAltGV", -0.8844, 1.056, 0.6094, -1.3906, 1, 15);
            AddToPosBulk("UpSlashGV", -0.5469, 1.25, 0.6094, -1.4375, 1, 15);
            AddToPosBulk("AirborneGV", -0.8281, 0.8281, 0.7812, -1.4375, 1, 12);
            AddToPosBulk("DashGV", -0.9219, 2.0781, 0.4063, -1.4219, 1, 11);
            AddToPosBulk("FallGV", -0.6406, 0.7812, 0.7812, -1.4219, 1, 11);
            AddToPosBulk("Fireball AnticGV", -0.8281, 0.5, 0.7812, -1.3438, 1, 3);
            AddToPosBulk("Fireball2 CastGV", -0.7031, 2.5938, 1.4219, -1.2969, 1, 6);
            AddToPosBulk("TurnGV", -0.6094, 0.4375, 0.5469, -1.4219, 1, 2);
            AddToPosBulk("RunGV", -0.7656, 0.5625, 0.625, -1.4063, 1, 13);
            AddToPosBulk("LandGV", -0.671875, 0.57812494, 0.48437506, -1.390625, 1, 3);
            AddToPosBulk("IntroJumpGV", -0.9219, 2.0781, 0.4063, -1.4219, 1, 1);
            AddToPosBulk("StunRiseGV", -1.7813, 0.6562, 0.8906, -1.4219, 1, 5);
            AddToPosBulk("StunGV", -1.7813, 0.6562, 0.8906, -1.4219, 1, 5);
            AddToPosBulk("DeathGV", -1.0313, 0.7187, 0.7031, -1.18,1,18);
            AddToPosBulk("Double JumpGV", -0.8281, 1.2969, 0.8125, -1.4531, 1, 8);
            AddToPosBulk("DownSlashGV", -0.8437, 1.3594, 0.5469, -1.2656, 1, 15);
            AddToPosBulk("FocusGV", -0.5937, 0.5625, 0.5781, -1.3906, 1, 7);
            AddToPosBulk("Focus EndGV", -0.5937, 0.5625, 0.5781, -1.3906, 1, 3);
            AddToPosBulk("Focus Get OnceGV", -0.5937, 0.5625, 0.5781, -1.3906, 1, 11);
            AddToPosBulk("Get OffGV", -0.6719, 0.7031, 0.7812, -1.4063, 1, 5);
            AddToPosBulk("Sitting AsleepGV", -0.6719, 0.7031, 0.7812, -1.4063, 1, 1);
            AddToPosBulk("Wake To SitGV", -0.6719, 0.7031, 0.7812, -1.4063, 1, 9);
            AddToPosBulk("Quake AnticGV", -0.5781, 0.8906, 0.8594, -1.25, 1, 5);
            AddToPosBulk("Scream 2GV", -2.4063, 2.3594, 0.8125, -1.4531, 1, 7);
            AddToPosBulk("Scream End 2GV", -0.6094, 1.0313, 0.6875, -1.3125, 1, 1);
            AddToPosBulk("Scream StartGV", -0.75, 0.4219, 0.5, -1.3125, 1, 2);
            AddToPosBulk("Shadow DashGV", -1.9688, 2.6094, 0.6875, -1.4375, 1, 11);




        }
    }
}
