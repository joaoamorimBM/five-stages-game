using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CatBecoCutscene : MonoBehaviour
{
    [Header("Dados do Diálogo")]
    [SerializeField] private DialogueData catDialogueData;

    [Header("Configurações de Movimento (Ordem de Passagem)")]
    [SerializeField] private Transform targetSaidaCaixa; // Ponto 1: Logo à frente da abertura da caixa
    [SerializeField] private Transform targetHole;        // Ponto 2: O buraco na parede de arame
    [SerializeField] private float walkSpeed = 2f;

    private bool canInteract = false;
    private bool dialogueStarted = false;
    private bool shouldMove = false;
    
    // Controla qual ponto do caminho o gato está perseguindo no momento
    private int currentWaypointIndex = 0;
    private Transform[] pathWaypoints;

    private void OnEnable()
    {
        DialogueManager.OnDialogueEnded += StartCatEscape;
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueEnded -= StartCatEscape;
    }

    private void Start()
    {
        // Inicializa a nossa lista ordenada de caminho
        pathWaypoints = new Transform[2];
        pathWaypoints[0] = targetSaidaCaixa;
        pathWaypoints[1] = targetHole;
    }

    private void Update()
    {
        if (canInteract && !dialogueStarted)
        {
            bool eKeyPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

            if (eKeyPressed)
            {
                TriggerCatDialogue();
            }
        }

        if (shouldMove)
        {
            MoveAlongPath();
        }
    }

    private void TriggerCatDialogue()
    {
        dialogueStarted = true;
        canInteract = false;

        if (DialogueManager.Instance != null && catDialogueData != null)
        {
            DialogueManager.Instance.StartDialogue(catDialogueData);
        }
    }

    private void StartCatEscape()
    {
        if (dialogueStarted && !shouldMove)
        {
            shouldMove = true;
            currentWaypointIndex = 0; // Começa indo para o Ponto 1 (Saída)
        }
    }

    private void MoveAlongPath()
    {
        // Verifica se ainda temos pontos válidos para andar
        if (currentWaypointIndex >= pathWaypoints.Length || pathWaypoints[currentWaypointIndex] == null)
        {
            shouldMove = false;
            gameObject.SetActive(false); // Desativa o gato quando completa todo o percurso
            return;
        }

        Transform targetTarget = pathWaypoints[currentWaypointIndex];

        // Move frame a frame em direção ao waypoint atual
        transform.position = Vector3.MoveTowards(transform.position, targetTarget.position, walkSpeed * Time.deltaTime);

        // Se o gato chegou muito perto do waypoint atual, ele muda o foco para o próximo ponto
        if (Vector3.Distance(transform.position, targetTarget.position) < 0.05f)
        {
            currentWaypointIndex++;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
        }
    }
}