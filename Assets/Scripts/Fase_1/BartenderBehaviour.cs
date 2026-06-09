using UnityEngine;

public class BartenderBehaviour : MonoBehaviour
{
    [Header("Configurações")]
    public float detectionRange = 4f;

    [Tooltip("Ajuste aqui se o modelo estiver olhando para o lado errado (Ex: 90, -90, 180)")]
    public float rotationOffset = 0f;

    [Header("Referências")]
    public Transform player;

    Animator anim;
    bool playerWasNear = false;
    float originalY;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        originalY = transform.eulerAngles.y;

        if (player == null) Debug.LogError($"Player não atribuído em {gameObject.name}!");
        if (anim   == null) Debug.LogError($"Animator não encontrado nos filhos de {gameObject.name}!");
    }

    void Update()
    {
        if (player == null || anim == null) return;

        float distance  = Vector3.Distance(transform.position, player.position);
        bool  playerNear = distance <= detectionRange;

        // Atualiza o Animator
        anim.SetBool("playerNear", playerNear);

        // Rotação suave
        if (playerNear)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                targetRotation *= Quaternion.Euler(0f, rotationOffset, 0f);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.deltaTime * 5f
                );
            }
        }
        else
        {
            Quaternion defaultRotation = Quaternion.Euler(0f, originalY, 0f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                defaultRotation,
                Time.deltaTime * 3f
            );
        }

        playerWasNear = playerNear;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}