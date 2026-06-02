using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações")]
    public float interactRange = 2.5f;
    public LayerMask interactLayer;

    [Header("Referências — arraste no Inspector")]
    public Camera    cam;        // ← arraste a Main Camera aqui
    public GameObject promptUI;
    public TMP_Text   promptText;

    IInteractable currentInteractable;

    void Start()
    {
        if (cam        == null) Debug.LogError("Cam não atribuída!");
        if (promptUI   == null) Debug.LogError("PromptUI não atribuído!");
        if (promptText == null) Debug.LogError("PromptText não atribuído!");

        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        CheckInteractable();

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
            currentInteractable.Interact();
    }

    void CheckInteractable()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(cam.transform.position, cam.transform.forward * interactRange, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            Debug.Log("Bateu em: " + hit.collider.gameObject.name);

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                if (promptUI   != null) promptUI.SetActive(true);
                if (promptText != null) promptText.text = interactable.GetPromptText();
                return;
            }
        }

        currentInteractable = null;
        if (promptUI != null) promptUI.SetActive(false);
    }
}