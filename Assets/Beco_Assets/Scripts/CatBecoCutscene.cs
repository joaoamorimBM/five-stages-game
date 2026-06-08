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
    private Animator anim;

    // private void OnEnable()
    // {
    //     DialogueManager.OnDialogueEnded += StartCatEscape;
    // }

    // private void OnDisable()
    // {
    //     DialogueManager.OnDialogueEnded -= StartCatEscape;
    // }

    private void Start()
    {
        anim = GetComponent<Animator>();

        pathWaypoints = new Transform[2];
        pathWaypoints[0] = targetSaidaCaixa;
        pathWaypoints[1] = targetHole;

        // FORÇA O GATO A COMEÇAR SENTADO DENTRO DA CAIXA
        if (anim != null)
        {
            anim.Play("sitting"); // Ele fica na pose estática de sentado até o Noah interagir 
        }
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

        if (anim != null)
        {
            anim.Play("miau");
        }

        if (DialogueManager.Instance != null && catDialogueData != null)
        {
            DialogueManager.Instance.StartDialogue(catDialogueData);
        }
    }

   public void StartCatEscape()
{
    // Forçamos dialogueStarted para true para garantir que o gato ande,
    // mesmo que o diálogo tenha sido disparado pelo SequenceManager
    dialogueStarted = true; 

        if (!shouldMove)
        {
            shouldMove = true;
            currentWaypointIndex = 0; // Começa indo para o Ponto 1 (Saída)

            // Troca a animação para caminhada na hora de fugir
            if (anim != null)
            {
                anim.Play("walk");
            }
        }
    }


    private void MoveAlongPath()
    {
        // Verifica se ainda temos pontos válidos para andar ou se o caminho acabou
        if (currentWaypointIndex >= pathWaypoints.Length || pathWaypoints[currentWaypointIndex] == null)
        {
            shouldMove = false;
            gameObject.SetActive(false); // Desativa o gato (O BecoSequenceManager vai notar isso automaticamente!)
            return;
        }

        Transform targetTarget = pathWaypoints[currentWaypointIndex];

        // Move frame a frame em direção ao waypoint atual
        transform.position = Vector3.MoveTowards(transform.position, targetTarget.position, walkSpeed * Time.deltaTime);

        // ROTAÇÃO: Faz o corpinho do gato olhar suavemente para o ponto para onde ele está correndo
        Vector3 direcaoCaminho = (targetTarget.position - transform.position).normalized;
        direcaoCaminho.y = 0; // Evita que ele incline o corpo para cima ou para baixo
        if (direcaoCaminho != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direcaoCaminho), 10f * Time.deltaTime);
        }

        // Se o gato chegou muito perto do waypoint atual, muda o foco para o próximo ponto
        if (Vector3.Distance(transform.position, targetTarget.position) < 0.05f)
        {
            currentWaypointIndex++;
            
            // Se após o incremento alcançamos o fim do array, desativa o gato imediatamente
            if (currentWaypointIndex >= pathWaypoints.Length)
            {
                shouldMove = false;
                gameObject.SetActive(false); 
            }
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