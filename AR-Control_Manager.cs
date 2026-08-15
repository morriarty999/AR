using UnityEngine;
using AC;

public class AutoKinematicPlayers : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // On s'abonne à l'événement de changement de player
        EventManager.OnSetPlayer += OnSetPlayer;
    }

    private void OnDestroy()
    {
        // Toujours se désabonner proprement
        EventManager.OnSetPlayer -= OnSetPlayer;
    }

    private void Start()
    {
        // Vérifie dès le départ si ce player est actif ou non
        if (KickStarter.player != null && KickStarter.player != GetComponent<Player>())
        {
            SetKinematic(true);
        }
        else
        {
            SetKinematic(false);
        }
    }

    private void OnSetPlayer(Player newPlayer)
    {
        // Si c'est ce player qui devient actif ? désactive Kinematic
        if (newPlayer == GetComponent<Player>())
        {
            SetKinematic(false);
        }
        else
        {
            // Si ce player devient inactif ? active Kinematic
            SetKinematic(true);
        }
    }

    private void SetKinematic(bool state)
    {
        if (rb != null)
        {
            rb.isKinematic = state;
        }
    }
}
