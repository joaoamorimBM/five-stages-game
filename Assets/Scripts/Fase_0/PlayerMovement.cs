using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed = 2.5f;
    public float runSpeed  = 5f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;

    [Header("Referências")]
    public Camera  cam;
    public Animator anim;

    [Header("Passos")]
    public AudioClip stepSound;
    public float stepInterval     = 0.45f;  // tempo entre passos andando
    public float stepIntervalRun  = 0.25f;  // tempo entre passos correndo

    CharacterController cc;
    AudioSource audioSource;
    float stepTimer = 0f;
    float rotX;

    void Start()
    {
        cc          = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        // Cria AudioSource automaticamente se não tiver
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 0f;  // som 2D (passos do próprio jogador)
        audioSource.playOnAwake  = false;

        Cursor.lockState = CursorLockMode.Locked;

        if (cam  == null) Debug.LogError("Cam não atribuída!");
        if (anim == null) Debug.LogError("Anim não atribuído!");
    }

    void Update()
    {
        Move();
        Look();
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool  isRunning    = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = (h != 0 || v != 0)
                             ? (isRunning ? runSpeed : walkSpeed)
                             : 0f;

        Vector3 move = transform.right * h + transform.forward * v;
        cc.Move(move * currentSpeed * Time.deltaTime);
        cc.Move(Vector3.down * 9.8f * Time.deltaTime);

        if (anim != null)
            anim.SetFloat("speed", currentSpeed, 0.1f, Time.deltaTime);

        // Sistema de passos
        bool isMoving = currentSpeed > 0f && cc.isGrounded;
        if (isMoving)
        {
            float interval = isRunning ? stepIntervalRun : stepInterval;
            stepTimer += Time.deltaTime;

            if (stepTimer >= interval)
            {
                PlayStep();
                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;  // reseta quando para
        }
    }

    void PlayStep()
    {
        if (stepSound != null && audioSource != null)
            audioSource.PlayOneShot(stepSound, 0.6f);
    }

    void Look()
    {
        if (cam == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotX -= mouseY;
        rotX  = Mathf.Clamp(rotX, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
public void SetMovementLocked(bool locked)
{
    enabled = !locked;

    // Para a animação quando travado
    Animator anim = GetComponentInChildren<Animator>();
    if (anim != null)
    {
        if (locked)
        {
            // Força idle — velocidade zero
            anim.SetFloat("speed", 0f);
            anim.speed = 0f;  // pausa a animação completamente
        }
        else
        {
            anim.speed = 1f;  // retoma a animação
        }
    }

    if (!locked)
        Cursor.lockState = CursorLockMode.Locked;
}

public void ForceRotation(float playerYAngle, float cameraXAngle)
{
    // Rotaciona o corpo do player no eixo Y
    transform.rotation = Quaternion.Euler(0f, playerYAngle, 0f);
    
    // Força o pitch da câmera
    rotX = cameraXAngle;
    if (cam != null)
        cam.transform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
}
}