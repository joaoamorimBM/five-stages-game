using System.Collections;
using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("Referências")]
    public DialogueData dialogueData;
    public Transform player;
    public float interactionDistance = 2.5f;
    public ChairInteraction chairInteraction;

    private bool playerInRange = false;
    private bool alreadyTalked = false;

    void Update()
    {
        if (alreadyTalked) return;

        float dist = Vector3.Distance(transform.position, player.position);
        playerInRange = dist <= interactionDistance;

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!DialogueManager.Instance.isDialogueActive)
            {
                alreadyTalked = true;
                DialogueManager.Instance.StartDialogue(dialogueData);
                StartCoroutine(ActivateChairAfterDialogue());
            }
        }
    }

    private IEnumerator ActivateChairAfterDialogue()
    {
        yield return new WaitUntil(() => !DialogueManager.Instance.isDialogueActive);
        chairInteraction.Activate();
    }
}