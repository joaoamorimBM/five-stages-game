using UnityEngine;

public class DoorController : MonoBehaviour, IInteractable
{
    public float openAngle   = 90f;
    public float closedAngle = 0f;
    public float rotateSpeed = 5f;
    public bool  startsOpen  = true;

    bool  isOpen;
    bool  isMoving;
    float targetAngle;

    // Variáveis para guardar a rotação original
    float startX;
    float startZ;

    void Start()
    {
        // Salva a rotação inicial (para a porta não deitar)
        startX = transform.localEulerAngles.x;
        startZ = transform.localEulerAngles.z;

        isOpen      = startsOpen;
        targetAngle = isOpen ? openAngle : closedAngle;
        
        // Aplica o ângulo inicial mantendo X e Z originais
        transform.localEulerAngles = new Vector3(startX, targetAngle, startZ);
    }

    void Update()
    {
        if (!isMoving) return;

        float newAngle = Mathf.LerpAngle(
            transform.localEulerAngles.y,
            targetAngle,
            Time.deltaTime * rotateSpeed
        );
        
        // Gira apenas o Y, mantendo X e Z de pé
        transform.localEulerAngles = new Vector3(startX, newAngle, startZ);

        if (Mathf.Abs(Mathf.DeltaAngle(newAngle, targetAngle)) < 0.5f)
        {
            transform.localEulerAngles = new Vector3(startX, targetAngle, startZ);
            isMoving = false;
        }
    }

    public void Interact()
    {
        isOpen      = !isOpen;
        targetAngle = isOpen ? openAngle : closedAngle;
        isMoving    = true;
    }

    public string GetPromptText()
    {
        return isOpen ? "[E] Fechar porta" : "[E] Abrir porta";
    }
}