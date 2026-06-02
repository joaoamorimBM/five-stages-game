using UnityEngine;

public class InvisibleWall : MonoBehaviour
{
    [Header("Referências")]
    public CharacterController playerCC;
    public PlayerMovement      playerMovement;

    [Header("Configurações")]
    public float blockDistance = 1.5f;

    bool  isBlocking = false;
    float savedWalkSpeed;
    float savedRunSpeed;

    void Start()
    {
        // Cria o collider físico sólido automaticamente
        BoxCollider col = gameObject.AddComponent<BoxCollider>();
        col.isTrigger = false;
        col.size      = new Vector3(5f, 3f, 0.5f);

        if (playerCC       == null) Debug.LogError("CharacterController não atribuído!");
        if (playerMovement == null) Debug.LogError("PlayerMovement não atribuído!");

        if (playerMovement != null)
        {
            savedWalkSpeed = playerMovement.walkSpeed;
            savedRunSpeed  = playerMovement.runSpeed;
        }
    }

    void Update()
    {
        if (playerCC == null) return;

        float distance = Vector3.Distance(
            transform.position,
            playerCC.transform.position
        );

        if (distance <= blockDistance && !isBlocking)
        {
            isBlocking = true;
            if (playerMovement != null)
            {
                playerMovement.walkSpeed = 0f;
                playerMovement.runSpeed  = 0f;
            }
        }
        else if (distance > blockDistance && isBlocking)
        {
            isBlocking = false;
            if (playerMovement != null)
            {
                playerMovement.walkSpeed = savedWalkSpeed;
                playerMovement.runSpeed  = savedRunSpeed;
            }
        }

        // Enquanto bloqueado, empurra para trás continuamente
        if (isBlocking)
        {
            Vector3 pushDir = (playerCC.transform.position - transform.position).normalized;
            pushDir.y = 0f;
            playerCC.Move(pushDir * 2f * Time.deltaTime);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(5f, 3f, 0.5f));
    }
}