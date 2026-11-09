using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// Registra as ações do jogador durante a sessão e grava um relatório (.json + .txt)
// em ReportsFolder. Depois o ReportUploader envia o .json pro site do psicólogo.
// Foi pensado para apoiar o psicólogo, não é um instrumento validado clinicamente.
//
// Formato 4: os dados já vêm organizados como o site mostra (cenas, objetos
// observáveis com tema, escolhas com TODAS as opções mostradas, diálogos).
public class ReportLogger : MonoBehaviour
{
    public static ReportLogger Instance;

    public const int FormatVersion = 4;
    public const string MenuSceneName = "Scene_MainMenu";

    // ---------- Formato do relatório (v4) ----------

    [Serializable]
    public class SceneEntry
    {
        public string scene;
        public string label;
        public float t;        // segundos desde o início da sessão
        public float seconds;  // quanto tempo ficou na cena
    }

    [Serializable]
    public class ObservableEntry
    {
        public string scene;
        public string id;      // nome do diálogo (objetos com o mesmo diálogo contam como um)
        public string label;
        public string theme;
        public int order;      // 1, 2, 3... na ordem em que observou; 0 = não observou
        public float t;
        public float seconds;  // quanto tempo ficou no pensamento
    }

    [Serializable]
    public class ChoiceOption
    {
        public string text;
        public string stage;   // Negacao | Raiva | Barganha | Depressao | Aceitacao
    }

    [Serializable]
    public class ChoiceEntry
    {
        public float t;
        public string scene;
        public string dialogue;
        public string speaker;  // quem fez a pergunta
        public string prompt;   // a fala que veio antes das opções
        public List<ChoiceOption> options = new List<ChoiceOption>();
        public int chosen;
        public float seconds;   // tempo pra escolher
    }

    [Serializable]
    public class DialogueEntry
    {
        public float t;
        public string scene;
        public string label;
        public float seconds;
        public int lines;
        public int typingSkips;
    }

    [Serializable]
    public class Report
    {
        public int formatVersion = FormatVersion;
        public string patientCode;
        public string sessionId;
        public string gameVersion;
        public string startedAt;
        public string endedAt;
        public float durationSeconds;
        public bool partial = true;
        public List<SceneEntry> scenes = new List<SceneEntry>();
        public List<ObservableEntry> observables = new List<ObservableEntry>();
        public List<ChoiceEntry> choices = new List<ChoiceEntry>();
        public List<DialogueEntry> dialogues = new List<DialogueEntry>();
    }

    // Nomes amigáveis das cenas
    private static readonly Dictionary<string, string> SceneLabels = new Dictionary<string, string>
    {
        { "Scene_Intro", "Introdução" },
        { "Scene_Room", "Quarto" },
        { "Scene_House", "Casa" },
        { "Scene_Alley", "Beco" },
        { "padaria_scene", "Padaria" },
        { "Acidente_Scene", "Acidente" },
        { "Dedicatoria_Scene", "Dedicatória" },
    };

    private static readonly Dictionary<string, string> StageLabels = new Dictionary<string, string>
    {
        { "Negacao", "Negação" },
        { "Raiva", "Raiva" },
        { "Barganha", "Barganha" },
        { "Depressao", "Depressão" },
        { "Aceitacao", "Aceitação" },
    };

    // No editor: pasta Relatorios na raiz do projeto (ao lado de Assets, fora do git).
    // No jogo final: persistentDataPath, porque a pasta do jogo pode não aceitar escrita.
#if UNITY_EDITOR
    public static string ReportsFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Relatorios"));
#else
    public static string ReportsFolder => Path.Combine(Application.persistentDataPath, "Relatorios");
#endif

    // ---------- Código do paciente (definido pela tela de código) ----------
    // Fica só na memória: num computador de clínica, o próximo paciente não herda o código.

    public static string PatientCode { get; private set; } = "";
    private static bool codeDecided;   // a tela de código já foi respondida?
    private static bool recordingAllowed;

    // "Começar" com um código válido
    public static void ArmSession(string normalizedCode)
    {
        PatientCode = normalizedCode;
        codeDecided = true;
        recordingAllowed = true;
    }

    // "Jogar sem código": nada é gravado
    public static void DisarmSession()
    {
        PatientCode = "";
        codeDecided = true;
        recordingAllowed = false;
    }

    private Report report;
    private float sessionStartTime;
    private string fileBaseName;
    private int observedCount;
    private bool quitting;

    public bool SessionActive => report != null;
    public string ActiveFileName => fileBaseName == null ? null : fileBaseName + ".json";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("ReportLogger");
            go.AddComponent<ReportLogger>();
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += OnQuitting;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Relatórios que ficaram de sessões anteriores (sem internet, jogo fechado...)
        ReportUploader.UploadPending(this);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= OnQuitting;
    }

    // ---------- API usada pelos outros scripts ----------

    public static void LogObserveStart(string id)
    {
        if (Instance == null || !Instance.SessionActive) return;
        Instance.MarkObserved(id);
    }

    public static void LogObserveEnd(string id, float seconds)
    {
        if (Instance == null || !Instance.SessionActive) return;
        ObservableEntry o = Instance.report.observables.Find(x => x.id == id && x.order > 0);
        if (o != null) o.seconds = Round(o.seconds + seconds);
    }

    public static void LogDialogue(string label, float seconds, int lines, int typingSkips)
    {
        if (Instance == null || !Instance.SessionActive) return;
        Instance.report.dialogues.Add(new DialogueEntry
        {
            t = Instance.Now(seconds), // registrado no fim, mas marcado no início do diálogo
            scene = SceneManager.GetActiveScene().name,
            label = label,
            seconds = Round(seconds),
            lines = lines,
            typingSkips = typingSkips,
        });
    }

    public static void LogChoice(string dialogue, string speaker, string prompt,
                                 DialogueData.DialogueChoice[] options, int chosen, float seconds)
    {
        if (Instance == null || !Instance.SessionActive) return;

        ChoiceEntry c = new ChoiceEntry
        {
            t = Instance.Now(),
            scene = SceneManager.GetActiveScene().name,
            dialogue = dialogue,
            speaker = speaker,
            prompt = prompt,
            chosen = chosen,
            seconds = Round(seconds),
        };
        foreach (DialogueData.DialogueChoice o in options)
            c.options.Add(new ChoiceOption { text = o.text, stage = o.stage.ToString() });

        Instance.report.choices.Add(c);
    }

    // ---------- Sessão ----------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Voltar ao menu encerra a sessão (e a tela de código aparece de novo)
        if (scene.name == MenuSceneName)
        {
            if (SessionActive) EndSession();
            codeDecided = false;
            recordingAllowed = false;
            PatientCode = "";
            return;
        }

        if (!SessionActive)
        {
            // No editor, dar Play direto numa cena (sem passar pelo menu) grava sem código, pra testar
            bool devShortcut = Application.isEditor && !codeDecided;
            if (!recordingAllowed && !devShortcut) return;
            StartSession();
        }

        CloseCurrentScene();
        report.scenes.Add(new SceneEntry { scene = scene.name, label = SceneLabel(scene.name), t = Now() });
        RecordObservables(scene);
        Save(partial: true);
    }

    private void StartSession()
    {
        sessionStartTime = Time.realtimeSinceStartup;
        observedCount = 0;

        report = new Report
        {
            patientCode = PatientCode,
            sessionId = Guid.NewGuid().ToString(),
            gameVersion = Application.version,
            startedAt = DateTime.UtcNow.ToString("o"),
        };

        fileBaseName = "sessao_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + report.sessionId.Substring(0, 8);
    }

    private void EndSession()
    {
        CloseCurrentScene();
        Save(partial: false);
        report = null;
        fileBaseName = null;
    }

    private void OnQuitting()
    {
        quitting = true;
        // Fechou o jogo no meio: grava como incompleta (é enviada na próxima vez que abrir)
        if (!SessionActive) return;
        CloseCurrentScene();
        Save(partial: true);
    }

    private void CloseCurrentScene()
    {
        if (report.scenes.Count == 0) return;
        SceneEntry last = report.scenes[report.scenes.Count - 1];
        last.seconds = Round(Now() - last.t);
    }

    // Lista uma vez, ao carregar a cena, os objetos de "observar" (ThoughtInteractable).
    // Inclui inativos porque algumas sequências ativam objetos depois.
    private void RecordObservables(Scene scene)
    {
        ThoughtInteractable[] found = FindObjectsByType<ThoughtInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ThoughtInteractable ti in found)
        {
            if (ti.gameObject.scene != scene || !ti.HasDialogue) continue;
            string id = ti.ReportId;
            if (report.observables.Exists(o => o.id == id)) continue; // cena revisitada ou objeto repetido

            report.observables.Add(new ObservableEntry
            {
                scene = scene.name,
                id = id,
                label = ti.ReportLabel,
                theme = string.IsNullOrEmpty(ti.ReportTheme) ? "Outros" : ti.ReportTheme,
            });
        }
    }

    private void MarkObserved(string id)
    {
        ObservableEntry o = report.observables.Find(x => x.id == id);
        if (o == null || o.order > 0) return;
        o.order = ++observedCount;
        o.t = Now();
    }

    // ---------- Gravação ----------

    private void Save(bool partial)
    {
        if (!SessionActive) return;

        report.partial = partial;
        report.endedAt = DateTime.UtcNow.ToString("o");
        report.durationSeconds = Round(Now());

        try
        {
            string dir = ReportsFolder;
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, fileBaseName + ".json"), JsonUtility.ToJson(report, true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(dir, fileBaseName + ".txt"), BuildText(), Encoding.UTF8);
        }
        catch (Exception e)
        {
            // Nunca derrubar o jogo por causa do relatório
            Debug.LogWarning("[ReportLogger] Falha ao salvar relatório: " + e.Message);
            return;
        }

        // Envia a versão parcial também: o psicólogo vê a sessão mesmo se o jogo fechar no meio.
        // Fechando o jogo não dá tempo: fica pra próxima vez que abrir.
        if (!quitting) ReportUploader.UploadPending(this);
    }

    // ---------- Texto legível (cópia local; o site mostra isso organizado) ----------

    private string BuildText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Relatório Five Stages (formato v" + report.formatVersion + ")");
        sb.AppendLine("Material complementar pensado para apoiar a conversa com o psicólogo. Não é um instrumento diagnóstico.");
        sb.AppendLine();
        sb.AppendLine("Código do paciente: " + (string.IsNullOrEmpty(report.patientCode) ? "(sem código, teste no editor)" : report.patientCode));
        sb.AppendLine("Duração: " + FormatTime(report.durationSeconds) + " · " + (report.partial ? "incompleta" : "completa"));
        sb.AppendLine();

        sb.AppendLine("== Escolhas ==");
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (ChoiceEntry c in report.choices)
        {
            string stage = c.chosen >= 0 && c.chosen < c.options.Count ? c.options[c.chosen].stage : "";
            counts.TryGetValue(stage, out int n);
            counts[stage] = n + 1;
        }
        foreach (KeyValuePair<string, string> s in StageLabels)
        {
            counts.TryGetValue(s.Key, out int n);
            sb.AppendLine("  " + s.Value + ": " + n);
        }
        sb.AppendLine();

        sb.AppendLine("== Objetos observados ==");
        foreach (ObservableEntry o in report.observables)
        {
            sb.AppendLine("  " + (o.order > 0 ? o.order + "º " : "-  ") + o.label + " [" + o.theme + "]" +
                          (o.order > 0 ? " " + o.seconds.ToString("0.0") + "s" : " (não observou)"));
        }
        sb.AppendLine();

        sb.AppendLine("== Linha do tempo ==");
        List<KeyValuePair<float, string>> lines = new List<KeyValuePair<float, string>>();
        foreach (SceneEntry s in report.scenes) lines.Add(new KeyValuePair<float, string>(s.t, "Entrou em " + s.label));
        foreach (ChoiceEntry c in report.choices)
        {
            string text = c.chosen >= 0 && c.chosen < c.options.Count ? c.options[c.chosen].text : "?";
            string stage = c.chosen >= 0 && c.chosen < c.options.Count ? StageLabel(c.options[c.chosen].stage) : "?";
            lines.Add(new KeyValuePair<float, string>(c.t, "  " + c.dialogue + ": \"" + text + "\" → " + stage + " (" + c.seconds.ToString("0.0") + "s)"));
        }
        lines.Sort((a, b) => a.Key.CompareTo(b.Key));
        foreach (KeyValuePair<float, string> l in lines) sb.AppendLine("[" + FormatTime(l.Key) + "] " + l.Value);

        return sb.ToString();
    }

    // ---------- Utilitários ----------

    private float Now(float secondsAgo = 0f) => Round(Mathf.Max(0f, Time.realtimeSinceStartup - sessionStartTime - secondsAgo));

    private static string SceneLabel(string scene) => SceneLabels.TryGetValue(scene, out string label) ? label : scene;
    private static string StageLabel(string stage) => StageLabels.TryGetValue(stage, out string label) ? label : stage;

    private static float Round(float value) => Mathf.Round(value * 10f) / 10f;

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}
