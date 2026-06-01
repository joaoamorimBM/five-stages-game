using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovimentoTeste : MonoBehaviour
{
    public float velocidade = 5f;

    void Update()
    {
        // Garante que o teclado está conectado
        if (Keyboard.current == null) return;

        float moveX = 0f;
        float moveZ = 0f;

        // Controles simples usando WASD ou Setas
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;

        // Aplica o movimento na cápsula
        Vector3 movimento = new Vector3(moveX, 0, moveZ).normalized;
        transform.Translate(movimento * velocidade * Time.deltaTime, Space.Self);
    }
}