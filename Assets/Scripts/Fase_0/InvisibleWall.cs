using UnityEngine;

public class InvisibleWall : MonoBehaviour
{
    [Header("Referências")]
    public Transform           player;
    public PlayerMovement      playerMovement;

    [Header("Configurações")]
    public float blockDistance = 1.2f;  // distância que trava o player
    public float pushForce     = 3f;    // força que empurra de volta

    Vector3 blockedDirection;
    bool    isBlocked = false;

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= blockDistance)
        {
            isBlocked = true;

            // Empurra o player para trás (para longe da porta)
            Vector3 pushDirection = (player.position - transform.position).normalized;
            pushDirection.y = 0f;

            player.position += pushDirection * pushForce * Time.deltaTime;

            // Deixa o movimento mais pesado (como na Fase 3 do GDD)
            if (playerMovement != null)
                playerMovement.walkSpeed = Mathf.Lerp(
                    playerMovement.walkSpeed, 0.3f, Time.deltaTime * 3f
                );
        }
        else
        {
            isBlocked = false;

            // Restaura velocidade normal ao sair
            if (playerMovement != null)
                playerMovement.walkSpeed = Mathf.Lerp(
                    playerMovement.walkSpeed, 2.5f, Time.deltaTime * 3f
                );
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blockDistance);
    }
}