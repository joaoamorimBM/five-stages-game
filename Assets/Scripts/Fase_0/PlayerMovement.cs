using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidades")]
    public float walkSpeed = 2.5f;
    public float runSpeed  = 5f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;

    [Header("Referências — arraste no Inspector")]
    public Camera cam;
    public Animator anim;

    CharacterController cc;
    float rotX;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        // Proteção: avisa no Console se esqueceu de arrastar
        if (cam  == null) Debug.LogError("Cam não atribuída no PlayerMovement!");
        if (anim == null) Debug.LogError("Anim não atribuído no PlayerMovement!");
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
}