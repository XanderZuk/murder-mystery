using Mirror;

namespace Scripts.Player
{
    public class DeathEventArgs : NetworkBehaviour
    {
        public NetworkConnection ConnectionToClient;
    }
}