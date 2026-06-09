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

    [Header("Animações (nomes dos estados no Animator Controller)")]
    [SerializeField] private string animSitting = "sitting";
    [SerializeField] private string animMiau = "miau";
    [SerializeField] private string animWalk = "walk";

    private bool canInteract = false;
    private bool dialogueStarted = false;
    private bool shouldMove = false;

    // Controla qual ponto do caminho o gato está perseguindo no momento
    private int currentWaypointIndex = 0;
    private Transform[] pathWaypoints;
    private Animator anim;

    private void Start()
    {
        // Pega o Animator no próprio objeto OU em algum filho (modelos importados
        // quase sempre têm o Animator num objeto filho, não na raiz).
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        // MUITO IMPORTANTE para o modelo real: desliga o Root Motion para a
        // animação NÃO brigar com o transform.position do MoveAlongPath.
        if (anim != null)
            anim.applyRootMotion = false;

        pathWaypoints = new Transform[2];
        pathWaypoints[0] = targetSaidaCaixa;
        pathWaypoints[1] = targetHole;

        // Pose inicial sentado (se o estado existir no Controller do modelo novo).
        PlaySafe(animSitting);
    }

    private void Update()
    {
        if (canInteract && !dialogueStarted)
        {
            bool eKeyPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (eKeyPressed)
                TriggerCatDialogue();
        }

        if (shouldMove)
            MoveAlongPath();
    }

    private void TriggerCatDialogue()
    {
        dialogueStarted = true;
        canInteract = false;

        PlaySafe(animMiau);

        if (DialogueManager.Instance != null && catDialogueData != null)
            DialogueManager.Instance.StartDialogue(catDialogueData);
    }

    public void StartCatEscape()
    {
        // Garante que o gato vai andar mesmo que o diálogo tenha sido disparado
        // pelo SequenceManager.
        dialogueStarted = true;

        // Se nenhum waypoint foi atribuído, não dá pra fazer a fuga.
        // Em vez de travar a cena inteira, avisa e se desativa para o
        // BecoSequenceManager seguir em frente.
        if (targetSaidaCaixa == null && targetHole == null)
        {
            Debug.LogError($"[CatBecoCutscene] Waypoints (targetSaidaCaixa / targetHole) " +
                           $"não atribuídos em '{name}'. Reatribua-os no Inspector. " +
                           $"Desativando o gato para não travar a cutscene.");
            gameObject.SetActive(false);
            return;
        }

        if (!shouldMove)
        {
            shouldMove = true;
            currentWaypointIndex = 0; // Começa indo para o Ponto 1 (Saída)
            PlaySafe(animWalk);       // Troca para caminhada na hora de fugir
        }
    }

    private void MoveAlongPath()
    {
        // Pula automaticamente waypoints nulos em vez de desistir tudo de uma vez.
        while (currentWaypointIndex < pathWaypoints.Length &&
               pathWaypoints[currentWaypointIndex] == null)
        {
            currentWaypointIndex++;
        }

        // Caminho acabou -> desativa o gato (o BecoSequenceManager percebe isso).
        if (currentWaypointIndex >= pathWaypoints.Length)
        {
            shouldMove = false;
            gameObject.SetActive(false);
            return;
        }

        Transform alvo = pathWaypoints[currentWaypointIndex];

        // Move frame a frame em direção ao waypoint atual.
        transform.position = Vector3.MoveTowards(
            transform.position, alvo.position, walkSpeed * Time.deltaTime);

        // Faz o gato olhar suavemente para onde está correndo.
        Vector3 direcao = (alvo.position - transform.position).normalized;
        direcao.y = 0;
        if (direcao != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(direcao), 10f * Time.deltaTime);

        // Chegou perto o suficiente -> próximo ponto.
        if (Vector3.Distance(transform.position, alvo.position) < 0.05f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= pathWaypoints.Length)
            {
                shouldMove = false;
                gameObject.SetActive(false);
            }
        }
    }

    // Toca um estado de animação só se ele realmente existir no Controller.
    // Assim, se o modelo novo do gato não tiver "sitting"/"walk"/"miau",
    // a lógica da cutscene NÃO quebra — só ignora a animação e segue.
    private void PlaySafe(string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName)) return;

        if (anim.HasState(0, Animator.StringToHash(stateName)))
        {
            anim.Play(stateName);
        }
        else
        {
            Debug.LogWarning($"[CatBecoCutscene] O Animator do gato não tem o estado " +
                             $"'{stateName}'. Verifique/renomeie os estados no Animator " +
                             $"Controller do modelo novo (ou ajuste os campos no Inspector).");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            canInteract = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            canInteract = false;
    }
}