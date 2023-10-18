namespace Kindred_Spirit
{
    internal class RangeCheck : MonoBehaviour
    {
        private void OnTriggerEnter2D (Collider2D collider)
        {
            if (collider.gameObject.name == "Knight")
            {
                FsmBool Bool = Boss.Vessel.LocateMyFSM("Control").FsmVariables.FindFsmBool(gameObject.name);
                if (Bool != null)
                {
                    Bool.Value = true;
                }
            }
        }

        private void OnTriggerExit2D (Collider2D collider)
        {
            if (collider.gameObject.name == "Knight")
            {
                FsmBool Bool = Boss.Vessel.LocateMyFSM("Control").FsmVariables.FindFsmBool(gameObject.name);
                if (Bool != null)
                {
                    Bool.Value = false;
                }
            }
        }
    }
}
