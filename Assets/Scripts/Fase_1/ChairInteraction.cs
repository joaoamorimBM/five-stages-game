using System.Collections;
using UnityEngine;

public class ChairInteraction : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public Transform sitPosition;
    public Light chairGlow;
    public float interactionDistance = 2f;

    [Header("Configurações")]
    public bool isAvailable = false;

    private bool playerSat = false;
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (!isAvailable || playerSat) return;

        if (chairGlow != null)
            chairGlow.intensity = 1.5f + Mathf.Sin(Time.time * 3f) * 0.5f;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= interactionDistance && Input.GetKeyDown(KeyCode.E))
            SitDown();
    }

    public void Activate()
    {
        isAvailable = true;
    }

    void SitDown()
{
    playerSat = true;

    // Move para a posição da cadeira
    player.position = sitPosition.position;

    // Força a rotação ANTES de travar
    // sitPosition.eulerAngles.y = direção que o Noah vai olhar
    // 0f = câmera reta (nem pra cima nem pra baixo)
    playerMovement.ForceRotation(sitPosition.eulerAngles.y, 0f);

    // Agora trava tudo já na posição certa
    playerMovement.SetMovementLocked(true);

    if (chairGlow != null)
        chairGlow.gameObject.SetActive(false);

    StartCoroutine(WaitThenBlink());
}

    private IEnumerator WaitThenBlink()
    {
        yield return new WaitForSeconds(3f);

        BlinkTransition.Instance.DoBlink(() =>
        {
            // Por enquanto vazio — família virá depois
        });
    }
}