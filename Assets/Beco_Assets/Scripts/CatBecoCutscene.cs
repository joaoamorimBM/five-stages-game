using UnityEngine;

public class CatBecoCutscene : MonoBehaviour
{
    [Header("Configurações de Movimento (Ordem de Passagem)")]
    [SerializeField] private Transform targetSaidaCaixa; // Ponto 1: Logo à frente da abertura da caixa
    [SerializeField] private Transform targetHole;        // Ponto 2: O buraco na parede de arame
    [SerializeField] private float walkSpeed = 2f;

    [Header("Animações (nomes dos estados no Animator Controller)")]
    [SerializeField] private string animSitting = "sitting";
    [SerializeField] private string animMiau = "miau";
    [SerializeField] private string animWalk = "walk";

    private bool shouldMove = false;

    // Controla qual ponto do caminho o gato está perseguindo no momento
    private int currentWaypointIndex = 0;
    private Transform[] pathWaypoints;
    private Animator anim;
    private float alturaDaFuga;

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
        {
            anim.applyRootMotion = false;
            RuntimeAnimatorController controllerDoBeco =
                Resources.Load<RuntimeAnimatorController>("CatBeco");
            if (controllerDoBeco != null)
                anim.runtimeAnimatorController = controllerDoBeco;
        }

        pathWaypoints = new Transform[2];
        pathWaypoints[0] = targetSaidaCaixa;
        pathWaypoints[1] = targetHole;

        // Pose inicial sentado (se o estado existir no Controller do modelo novo).
        PlaySafe(animSitting);
    }

    private void Update()
    {
        if (shouldMove)
            MoveAlongPath();
    }

    public void PlayMeow() => PlaySafe(animMiau);

    public void StartCatEscape()
    {
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
            alturaDaFuga = transform.position.y;
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

        Vector3 destino = pathWaypoints[currentWaypointIndex].position;
        // Os pontos marcam o trajeto no plano do piso do beco.
        destino.y = alturaDaFuga;

        // Move frame a frame em direção ao waypoint atual.
        Vector3 direcao = destino - transform.position;
        transform.position = Vector3.MoveTowards(
            transform.position, destino, walkSpeed * Time.deltaTime);

        // Faz o gato olhar suavemente para onde está andando.
        direcao.y = 0;
        if (direcao != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(direcao), 10f * Time.deltaTime);

        // Chegou perto o suficiente -> próximo ponto.
        if (Vector3.Distance(transform.position, destino) < 0.05f)
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

        int state = Animator.StringToHash(stateName);
        if (anim.HasState(0, state))
        {
            anim.Play(state, 0, 0f);
        }
        else
        {
            Debug.LogWarning($"[CatBecoCutscene] O Animator do gato não tem o estado " +
                             $"'{stateName}'. Verifique/renomeie os estados no Animator " +
                             $"Controller do modelo novo (ou ajuste os campos no Inspector).");
        }
    }

}
