using UnityEngine;

public class PlayerMovimentTest : MonoBehaviour
{
    [Header("Configurações de Teste")]
    public float speed = 5f;

    void Update()
    {
        // Trava: Impede o movimento se o diálogo estiver acontecendo
        if (DialogueManager.Instance != null)
        {
            try 
            {
                if (DialogueManager.Instance.isDialogueActive) return;
            }
            catch (UnityEngine.MissingReferenceException) {}
        }

        // Captura o WASD / Setas
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Cria a direção do movimento
        Vector3 move = new Vector3(x, 0f, z);

        // Move o objeto ignorando a física (focado apenas em deslocamento)
        transform.Translate(move * speed * Time.deltaTime, Space.World);
    }
}