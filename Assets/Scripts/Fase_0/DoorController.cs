using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Configurações")]
    public float openAngle   = 90f;
    public float closedAngle = 0f;
    public float rotateSpeed = 5f;
    public bool  startsOpen  = false;  // ← Deve começar fechada

    [Header("Sons")]
    public AudioClip soundOpen;
    public AudioClip soundClose;

    AudioSource audioSource;
    bool  isOpen;
    bool  isMoving;
    float targetAngle;

    // Guardas de rotação base para o modelo
    float baseRotationY;
    float startX;
    float startZ;

    void Start()
    {
        // 1. Salva a rotação EXATA em que a porta foi colocada no cenário (Editor)
        startX = transform.localEulerAngles.x;
        startZ = transform.localEulerAngles.z;
        
        // Se ela começa FECHADA, a rotação atual do editor é o nosso "ponto zero" (closedAngle)
        // Se começa ABERTA, a rotação atual do editor já é o openAngle
        baseRotationY = transform.localEulerAngles.y - (startsOpen ? openAngle : closedAngle);

        // 2. Configura o AudioSource automaticamente
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;  // som 3D
        audioSource.volume       = 1f;
        audioSource.playOnAwake  = false;

        isOpen = startsOpen;
        
        // 3. Define para onde a porta deve ir quando for ativada
        targetAngle = isOpen ? openAngle : closedAngle;

        // 4. Força a porta a aplicar a rotação correta de fecho/abertura logo no frame 1
        float initialY = baseRotationY + (isOpen ? openAngle : closedAngle);
        transform.localEulerAngles = new Vector3(startX, initialY, startZ);
    }

    void Update()
    {
        if (!isMoving) return;

        // Interpolação suave do ângulo relativo (0 a 90)
        float currentRelativeY = Mathf.MoveTowardsAngle(
            GetCurrentRelativeY(),
            targetAngle,
            Time.deltaTime * rotateSpeed * 20f // Velocidade ajustada para ficar natural
        );

        // Aplica a rotação somando à rotação base da parede
        transform.localEulerAngles = new Vector3(startX, baseRotationY + currentRelativeY, startZ);

        if (Mathf.Abs(Mathf.DeltaAngle(currentRelativeY, targetAngle)) < 0.1f)
        {
            transform.localEulerAngles = new Vector3(startX, baseRotationY + targetAngle, startZ);
            isMoving = false;
        }
    }

    public void Interact()
    {
        isOpen   = !isOpen;
        targetAngle = isOpen ? openAngle : closedAngle;
        isMoving    = true;

        // Toca o som correto
        if (audioSource != null)
        {
            AudioClip clip = isOpen ? soundOpen : soundClose;
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }

    // Função auxiliar para descobrir o ângulo atual da porta em relação ao "zero" dela
    float GetCurrentRelativeY()
    {
        return Mathf.DeltaAngle(baseRotationY, transform.localEulerAngles.y);
    }

    public string GetPromptText()
    {
        return isOpen ?  "[E] Abrir porta" : "[E] Fechar porta";
    }
}