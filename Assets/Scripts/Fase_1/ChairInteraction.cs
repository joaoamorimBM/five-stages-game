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
    player.position = sitPosition.position;
    playerMovement.ForceRotation(sitPosition.eulerAngles.y, 0f);
    playerMovement.SetMovementLocked(true);

    if (chairGlow != null)
        chairGlow.gameObject.SetActive(false);

    // Chama a sequência da padaria
    FindObjectOfType<BakeryAnxiety>().StartSequence();
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