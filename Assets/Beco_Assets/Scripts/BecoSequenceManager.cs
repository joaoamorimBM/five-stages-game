using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BecoSequenceManager : MonoBehaviour
{
    [Header("Referências de Personagem e Câmera")]
    [SerializeField] private Transform noahTransform;
    [SerializeField] private Camera noahCamera; 
    [SerializeField] private Transform[] caminhoCaminhadaNoah;
    [SerializeField] private Transform[] caminhoAteFinalBeco;
    
    [Header("Referências do Gato e Lata")]
    [SerializeField] private GameObject gatoPlaceholder;
    [SerializeField] private CatBecoCutscene gatoScript;
    [SerializeField] private Rigidbody lataLixoRb;

    [Header("Dados de Diálogo")]
    [SerializeField] private DialogueData dialogoGato;          
    [SerializeField] private DialogueData noahReacaoDialogue;
    [SerializeField] private DialogueData dialogoAntesFlashback;
    [SerializeField] private DialogueData dialogoPosFlashback;   

    [Header("Configurações Físicas e Áudio")]
    [SerializeField] private float velocidadeCaminhadaNoah = 2f;
    [SerializeField] private float velocidadeChuteLata = 2.8f;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip somChuteLata;

    [Header("Física da Tampa")]
    [SerializeField] private Rigidbody tampaLixoRb;

    [Header("Scripts do Player para Desativar")]
    [SerializeField] private MonoBehaviour scriptMovimentacaoPlayer;
    [SerializeField] private MonoBehaviour scriptRotacaoCamera;

    [Header("Parte 2: Emily e Flashback")]
    [SerializeField] private GameObject emilyPrefab;
    [SerializeField] private Transform pontoOlharEmily;

    [Header("Configurações do Trigger do Gato")]
    [SerializeField] private float alcanceDialogoGato = 2f;

    [Header("Efeitos de Raiva")]
    [SerializeField] private AlleyIntroManager alleyIntroManager;

    [Header("Segurança Anti-Travamento")]
    [Tooltip("Tempo máximo (s) esperando o gato terminar a fuga antes de forçar a continuação.")]
    [SerializeField] private float tempoMaxFugaGato = 8f;

    private bool _dialogoEmAndamento = false;
    private Coroutine sequenciaInicial;
    private bool voltandoDoFlashback;
    private const float DistanciaChute = 1.05f;

    private void OnEnable() => DialogueManager.OnDialogueEnded += NotificarFimDialogo;
    private void OnDisable() => DialogueManager.OnDialogueEnded -= NotificarFimDialogo;

    private void Start()
    {
        if (!voltandoDoFlashback && (GameManager.Instance == null || !GameManager.Instance.returningFromFlashback))
            sequenciaInicial = StartCoroutine(SequenciaBecoRoutine());
    }

    private void NotificarFimDialogo()
    {
        _dialogoEmAndamento = false;
    }

    private IEnumerator SequenciaBecoRoutine()
    {
        PlayerMovement playerMovement = noahTransform.GetComponent<PlayerMovement>();
        CharacterController playerCC = noahTransform.GetComponent<CharacterController>();

        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);

        // Avisos claros se algo crítico não foi atribuído no Inspector.
        if (gatoPlaceholder == null)
            Debug.LogError("[BecoSequenceManager] 'gatoPlaceholder' NÃO está atribuído! " +
                           "Reatribua o objeto do gato no Inspector.");
        if (gatoScript == null)
            Debug.LogError("[BecoSequenceManager] 'gatoScript' (CatBecoCutscene) NÃO está atribuído! " +
                           "Arraste o componente CatBecoCutscene do gato para este campo.");

        // --- PARTE 1: ESPERA O PLAYER CHEGAR PERTO DO GATO ---
        yield return new WaitUntil(() =>
            DialogueManager.Instance != null && !DialogueManager.Instance.isDialogueActive &&
            (gatoPlaceholder == null || PertoDoGato())
        );

        if (playerMovement != null)
            playerMovement.SetMovementLocked(true);

        yield return new WaitForSeconds(0.5f);

        // --- PARTE 2: DIÁLOGO COM O GATO ---
        if (gatoScript != null)
            gatoScript.PlayMeow();
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(dialogoGato);
        yield return new WaitUntil(() => !_dialogoEmAndamento);

        // --- PARTE 3: GATO FUGINDO E CÂMERA SEGUINDO ---
        if (gatoScript != null)
            gatoScript.StartCatEscape();
        else
            Debug.LogWarning("[BecoSequenceManager] gatoScript ausente — pulando a fuga do gato.");

        // Inicia efeitos de raiva enquanto o gato foge
        if (alleyIntroManager != null)
            alleyIntroManager.StartAngerBuild();

        // Câmera segue o gato ENQUANTO ele estiver ativo, mas com TIMEOUT.
        // Se a fuga do gato travar (animação, root motion, waypoints, etc.),
        // a cena continua sozinha em vez de congelar para sempre.
        float tempoFuga = 0f;
        while (gatoPlaceholder != null && gatoPlaceholder.activeSelf && tempoFuga < tempoMaxFugaGato)
        {
            tempoFuga += Time.deltaTime;

            Vector3 direcaoOlharGato = (gatoPlaceholder.transform.position - noahCamera.transform.position).normalized;
            if (direcaoOlharGato != Vector3.zero)
                noahCamera.transform.rotation = Quaternion.LookRotation(direcaoOlharGato);
            yield return null;
        }

        // Rede de segurança: garante que o gato suma para a cena prosseguir.
        if (gatoPlaceholder != null && gatoPlaceholder.activeSelf)
        {
            Debug.LogWarning("[BecoSequenceManager] O gato não terminou a fuga a tempo. " +
                             "Forçando desativação para continuar a sequência. " +
                             "Confira os waypoints e o Animator do modelo do gato.");
            gatoPlaceholder.SetActive(false);
        }

        // Retorna câmera para frente
        float tempoManeio = 0f;
        while (tempoManeio < 1f)
        {
            tempoManeio += Time.deltaTime * 3f;
            noahCamera.transform.localRotation = Quaternion.Slerp(noahCamera.transform.localRotation, Quaternion.identity, tempoManeio);
            yield return null;
        }

        if (playerMovement != null)
        {
            float anguloXAtual = noahCamera.transform.localEulerAngles.x;
            if (anguloXAtual > 180) anguloXAtual -= 360;
            playerMovement.ForceRotation(noahTransform.eulerAngles.y, anguloXAtual);
        }

        // --- PARTE 4: AGUARDA 3 SEGUNDOS ---
        yield return new WaitForSeconds(3f);
        yield return AproximarDaLixeira(playerCC);

        // --- PARTE 5: ENCARAR A LATA ---
        float tempoOlharLata = 0f;
        while (lataLixoRb != null && tempoOlharLata < 0.8f)
        {
            tempoOlharLata += Time.deltaTime;

            Vector3 direcaoLataHorizontal = (lataLixoRb.transform.position - noahTransform.position).normalized;
            direcaoLataHorizontal.y = 0;
            if (direcaoLataHorizontal != Vector3.zero)
                noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation, Quaternion.LookRotation(direcaoLataHorizontal), 5f * Time.deltaTime);

            Vector3 direcaoCameraLata = (lataLixoRb.transform.position - noahCamera.transform.position).normalized;
            if (direcaoCameraLata != Vector3.zero)
                noahCamera.transform.rotation = Quaternion.Slerp(noahCamera.transform.rotation, Quaternion.LookRotation(direcaoCameraLata), 5f * Time.deltaTime);

            yield return null;
        }

        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(noahReacaoDialogue);
        yield return new WaitUntil(() => !_dialogoEmAndamento);

        // --- PARTE 6: CHUTE FÍSICO — para os efeitos de raiva ---
        if (alleyIntroManager != null)
            alleyIntroManager.StopAngerEffects();

        ChutarLixeira(playerCC);

        yield return new WaitForSeconds(1.5f);

        // --- PARTE 6.5: NOAH CAMINHA ATÉ O FINAL DO BECO ---
        for (int i = 0; i < caminhoAteFinalBeco.Length; i++)
        {
            Transform pontoAtual = caminhoAteFinalBeco[i];
            if (pontoAtual == null) continue;

            while (Vector3.Distance(noahTransform.position, pontoAtual.position) > 0.3f)
            {
                Vector3 direcaoPonto = (pontoAtual.position - noahTransform.position).normalized;

                if (playerCC != null)
                    playerCC.Move(direcaoPonto * velocidadeCaminhadaNoah * Time.deltaTime);
                else
                    noahTransform.position = Vector3.MoveTowards(noahTransform.position, pontoAtual.position, velocidadeCaminhadaNoah * Time.deltaTime);

                if (direcaoPonto != Vector3.zero)
                {
                    direcaoPonto.y = 0;
                    noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation, Quaternion.LookRotation(direcaoPonto), 6f * Time.deltaTime);
                    noahCamera.transform.rotation = Quaternion.Slerp(noahCamera.transform.rotation, Quaternion.LookRotation(direcaoPonto), 6f * Time.deltaTime);
                }

                yield return null;
            }
        }

        // --- PARTE 7: APARIÇÃO DA EMILY ---
        if (emilyPrefab != null)
            emilyPrefab.SetActive(true);

        yield return new WaitForSeconds(1.0f);

        // --- PARTE 8: NOAH SE VIRA PARA A EMILY ---
        float tempoViradaEmily = 0f;
        while (tempoViradaEmily < 1.2f)
        {
            tempoViradaEmily += Time.deltaTime;

            Vector3 direcaoEmilyHorizontal = (emilyPrefab.transform.position - noahTransform.position).normalized;
            direcaoEmilyHorizontal.y = 0;
            if (direcaoEmilyHorizontal != Vector3.zero)
                noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation, Quaternion.LookRotation(direcaoEmilyHorizontal), 4f * Time.deltaTime);

            if (pontoOlharEmily != null)
            {
                Vector3 direcaoCamEmily = (pontoOlharEmily.position - noahCamera.transform.position).normalized;
                noahCamera.transform.rotation = Quaternion.Slerp(noahCamera.transform.rotation, Quaternion.LookRotation(direcaoCamEmily), 4f * Time.deltaTime);
            }

            yield return null;
        }

        // --- PARTE 9: DIÁLOGO ANTES DO FLASHBACK ---
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(dialogoAntesFlashback);
        yield return new WaitUntil(() => !_dialogoEmAndamento);

        // Salva posições antes do flashback
        if (GameManager.Instance != null)
        {
            GameManager.Instance.noahPositionBeforeFlashback = noahTransform.position;
            GameManager.Instance.noahRotationBeforeFlashback = noahTransform.rotation;
            GameManager.Instance.emilyPositionBeforeFlashback = emilyPrefab.transform.position;
            GameManager.Instance.emilyRotationBeforeFlashback = emilyPrefab.transform.rotation;
        }

        // --- PARTE 10: TRANSIÇÃO PARA O FLASHBACK ---
        DispararTransiciaoFlashback();
    }

    private void ChutarLixeira(CharacterController playerCC)
    {
        if (lataLixoRb == null) return;
        if (DirecaoHorizontalAteLixeira().sqrMagnitude > DistanciaChute * DistanciaChute)
        {
            Debug.LogWarning("[BecoSequenceManager] Noah não conseguiu chegar perto da lixeira. Pulando o chute.");
            return;
        }

        if (sfxSource != null && somChuteLata != null)
            sfxSource.PlayOneShot(somChuteLata);

        Vector3 rota = noahTransform.forward;
        foreach (Transform ponto in caminhoAteFinalBeco)
        {
            if (ponto == null) continue;

            Vector3 trecho = ponto.position - noahTransform.position;
            trecho.y = 0f;
            if (trecho.sqrMagnitude < 0.09f) continue;

            rota = trecho;
            break;
        }

        // Empurra para o lado da rota onde a lixeira já está.
        Vector3 lado = Vector3.Cross(Vector3.up, rota).normalized;
        if (Vector3.Dot(lataLixoRb.position - noahTransform.position, lado) < 0f)
            lado = -lado;

        Vector3 eixoTombo = Vector3.Cross(Vector3.up, lado);
        if (tampaLixoRb != null)
        {
            tampaLixoRb.transform.SetParent(null);
            LancarPeca(tampaLixoRb, playerCC, lado * (velocidadeChuteLata * 0.7f) + Vector3.up * 2f, eixoTombo * 4f);
        }

        LancarPeca(lataLixoRb, playerCC, lado * velocidadeChuteLata + Vector3.up * 0.6f, eixoTombo * 3f);
    }

    private IEnumerator AproximarDaLixeira(CharacterController playerCC)
    {
        float tempoRestante = 4f;
        while (lataLixoRb != null && tempoRestante > 0f)
        {
            Vector3 direcao = DirecaoHorizontalAteLixeira();
            float distancia = direcao.magnitude;
            if (distancia <= DistanciaChute) yield break;

            noahTransform.rotation = Quaternion.Slerp(noahTransform.rotation,
                Quaternion.LookRotation(direcao), 6f * Time.deltaTime);
            float passo = Mathf.Min(velocidadeCaminhadaNoah * Time.deltaTime, distancia - (DistanciaChute - 0.05f));
            Vector3 movimento = direcao / distancia * passo;
            if (playerCC != null)
                playerCC.Move(movimento);
            else
                noahTransform.position += movimento;

            tempoRestante -= Time.deltaTime;
            yield return null;
        }
    }

    private Vector3 DirecaoHorizontalAteLixeira()
    {
        Vector3 direcao = lataLixoRb.position - noahTransform.position;
        direcao.y = 0f;
        return direcao;
    }

    private static void LancarPeca(Rigidbody peca, CharacterController playerCC, Vector3 velocidade, Vector3 giro)
    {
        // Mantém as colisões com o cenário, mas impede que a peça trave a caminhada do Noah.
        if (playerCC != null)
        {
            foreach (Collider collider in peca.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(playerCC, collider);
        }

        peca.isKinematic = false;
        peca.linearVelocity = velocidade;
        peca.angularVelocity = giro;
    }

    private void DispararTransiciaoFlashback()
    {
        StartCoroutine(TransicaoParaFlashback());
    }

    private IEnumerator TransicaoParaFlashback()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.accidentVisit = 2;

        BlinkTransition blink = FindObjectOfType<BlinkTransition>();
        if (blink != null)
            yield return StartCoroutine(blink.FadeToBlack());

        UnityEngine.SceneManagement.SceneManager.LoadScene("Acidente_Scene");
    }

    public void RetornarDoFlashback()
    {
        if (voltandoDoFlashback) return;
        voltandoDoFlashback = true;
        if (sequenciaInicial != null)
            StopCoroutine(sequenciaInicial);
        if (gatoPlaceholder != null)
            gatoPlaceholder.SetActive(false);

        StartCoroutine(SequenciaPosFlashbackRoutine());
    }

    private bool PertoDoGato()
    {
        Transform gato = gatoPlaceholder.transform;
        Vector3 distancia = gato.position - noahTransform.position;
        distancia.y = 0f;
        if (distancia.sqrMagnitude > alcanceDialogoGato * alcanceDialogoGato) return false;

        Vector3 alvo = gato.position + Vector3.up * 0.25f;
        return !Physics.Linecast(noahCamera.transform.position, alvo, out RaycastHit hit,
                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
               || hit.transform.IsChildOf(gato);
    }

    private IEnumerator SequenciaPosFlashbackRoutine()
    {
        PlayerMovement playerMovement = noahTransform.GetComponent<PlayerMovement>();

        if (GameManager.Instance != null)
        {
            CharacterController cc = noahTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            noahTransform.position = GameManager.Instance.noahPositionBeforeFlashback;
            noahTransform.rotation = GameManager.Instance.noahRotationBeforeFlashback;
            if (cc != null) cc.enabled = true;

            if (emilyPrefab != null)
            {
                emilyPrefab.SetActive(true);
                emilyPrefab.transform.position = GameManager.Instance.emilyPositionBeforeFlashback;
                emilyPrefab.transform.rotation = GameManager.Instance.emilyRotationBeforeFlashback;
            }

            if (pontoOlharEmily != null && playerMovement != null)
            {
                Vector3 direcao = (pontoOlharEmily.position - noahCamera.transform.position).normalized;
                direcao.y = 0;
                float yAngle = Quaternion.LookRotation(direcao).eulerAngles.y;
                playerMovement.ForceRotation(yAngle, 0f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(dialogoPosFlashback);
        yield return new WaitUntil(() => !_dialogoEmAndamento);

        if (playerMovement != null)
        {
            float anguloXAtual = noahCamera.transform.localEulerAngles.x;
            if (anguloXAtual > 180) anguloXAtual -= 360;
            playerMovement.ForceRotation(noahTransform.eulerAngles.y, anguloXAtual);
            playerMovement.SetMovementLocked(false);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.playFinalCutscene = true;

        BlinkTransition blink = FindObjectOfType<BlinkTransition>();
        if (blink != null)
            yield return StartCoroutine(blink.FadeToBlack());

        UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_House");
    }
}
