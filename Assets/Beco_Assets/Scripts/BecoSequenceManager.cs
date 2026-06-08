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
    [SerializeField] private Transform pontoInteracaoGato; 
    
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
    [SerializeField] private float forcaChute = 18f;
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
    [SerializeField] private float distanciaAtivacaoGato = 4f;

    [Header("Efeitos de Raiva")]
    [SerializeField] private AlleyIntroManager alleyIntroManager;

    private bool _dialogoEmAndamento = false;

    private void OnEnable() => DialogueManager.OnDialogueEnded += NotificarFimDialogo;
    private void OnDisable() => DialogueManager.OnDialogueEnded -= NotificarFimDialogo;

    private void Start()
    {
        StartCoroutine(SequenciaBecoRoutine());
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

        // --- PARTE 1: ESPERA O PLAYER CHEGAR PERTO DO GATO ---
        yield return new WaitUntil(() =>
            gatoPlaceholder != null &&
            Vector3.Distance(noahTransform.position, gatoPlaceholder.transform.position) <= distanciaAtivacaoGato
        );

        yield return new WaitForSeconds(0.5f);

        if (playerMovement != null)
            playerMovement.SetMovementLocked(true);

        // --- PARTE 2: DIÁLOGO COM O GATO ---
        _dialogoEmAndamento = true;
        DialogueManager.Instance.StartDialogue(dialogoGato);
        yield return new WaitUntil(() => !_dialogoEmAndamento);

        // --- PARTE 3: GATO FUGINDO E CÂMERA SEGUINDO ---
        if (gatoScript != null)
            gatoScript.StartCatEscape();

        // Inicia efeitos de raiva enquanto o gato foge
        if (alleyIntroManager != null)
            alleyIntroManager.StartAngerBuild();

        while (gatoPlaceholder.activeSelf)
        {
            Vector3 direcaoOlharGato = (gatoPlaceholder.transform.position - noahCamera.transform.position).normalized;
            if (direcaoOlharGato != Vector3.zero)
                noahCamera.transform.rotation = Quaternion.LookRotation(direcaoOlharGato);
            yield return null;
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

        // --- PARTE 5: ENCARAR A LATA ---
        float tempoOlharLata = 0f;
        while (tempoOlharLata < 0.8f)
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

        if (lataLixoRb != null)
        {
            if (sfxSource != null && somChuteLata != null)
                sfxSource.PlayOneShot(somChuteLata);

            if (tampaLixoRb != null)
            {
                tampaLixoRb.isKinematic = false;
                tampaLixoRb.transform.SetParent(null);
                Vector3 direcaoSaltoTampa = (lataLixoRb.transform.position - noahTransform.position).normalized + (Vector3.up * 1.5f);
                tampaLixoRb.AddForce(direcaoSaltoTampa * (forcaChute * 0.7f), ForceMode.Impulse);
            }

            Vector3 direcaoChuteLata = (lataLixoRb.transform.position - noahTransform.position).normalized + (Vector3.up * 0.3f);
            lataLixoRb.AddForce(direcaoChuteLata * forcaChute, ForceMode.Impulse);
        }

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
        StartCoroutine(SequenciaPosFlashbackRoutine());
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