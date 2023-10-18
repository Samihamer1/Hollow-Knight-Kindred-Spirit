namespace Kindred_Spirit
{
    internal class DreamNailReject : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.tag == "Dream Attack")
            {
                PlayMakerFSM fsm = gameObject.LocateMyFSM("Dreamnail Reject");
                if (fsm != null)
                {
                    fsm.SendEvent("DREAM HIT");
                }
            }
        }
    }
}
