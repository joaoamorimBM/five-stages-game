using UnityEngine;

public class BartenderBehaviour : MonoBehaviour
{
    [Header("Configurações")]
    public float detectionRange = 4f;
    public float leavingRange   = 6f;

    [Tooltip("Ajuste aqui se o modelo estiver olhando para o lado errado (Ex: 90, -90, 180)")]
    public float rotationOffset = 0f;

    [Header("Referências")]
    public Transform player;

    Animator anim;
    bool playerWasNear   = false;
    bool hasBeenGreeted  = false; // Garante que ela só se despede se você realmente chegou perto antes
    float originalY;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        originalY = transform.eulerAngles.y;

        if (player == null) Debug.LogError($"Player não atribuído em {gameObject.name}!");
        if (anim == null)   Debug.LogError($"Animator não encontrado nos filhos de {gameObject.name}!");
    }

    void Update()
    {
        // Proteção para evitar erros no console caso falte referências
        if (player == null || anim == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool playerNear = distance <= detectionRange;

        // 1. Fluxo de Entrada: Player acabou de entrar no raio de detecção
        if (playerNear && !playerWasNear)
        {
            anim.ResetTrigger("onLeaving");
            hasBeenGreeted = true;
            Debug.Log("Player entrou no alcance - Olhando");
        }

        // 2. Fluxo de Saída: Player acabou de sair do raio curto, mas ainda está no raio limite de tchau
        if (!playerNear && playerWasNear && hasBeenGreeted)
        {
        anim.SetTrigger("onLeaving");
        hasBeenGreeted = false; // Desliga para não disparar repetidamente
        Debug.Log("Player saiu do alcance curto - Disparando Talking no Animator");
        }
        
        // Se o player se afastar demais além do limite máximo, cancela o estado de tchau pendente
        if (distance > leavingRange)
        {
            hasBeenGreeted = false;
        }

        // Atualiza a flag do Animator (Controla transição de IDLE para Olhar de perto)
        anim.SetBool("playerNear", playerNear);

        // 3. Sistema de Rotação Suave
        if (playerNear)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f; // Mantém a espinha do personagem reta

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
            // Retorna suavemente para a rotação original do cenário
            Quaternion defaultRotation = Quaternion.Euler(0f, originalY, 0f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                defaultRotation,
                Time.deltaTime * 3f
            );
        }

        // Guarda o estado para o próximo frame
        playerWasNear = playerNear;
    }

    void OnDrawGizmosSelected()
    {
        // Linha verde: Área onde ela percebe o jogador e começa a olhar
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Linha amarela: Limite máximo onde o "Talking" (Tchau) ainda faz sentido acontecer
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, leavingRange);
    }
}