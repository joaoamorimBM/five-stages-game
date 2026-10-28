using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// Registra as ações do jogador durante a sessão e grava um relatório (.json + .txt)
// em Application.persistentDataPath/Relatorios. Foi pensado para apoiar o psicólogo,
// não é um instrumento validado clinicamente.
public class ReportLogger : MonoBehaviour
{
    public static ReportLogger Instance;

    public const int FormatVersion = 3;
    public const string MenuSceneName = "Scene_MainMenu";
    public const string PatientCodeKey = "patientCode";

    [Serializable]
    public class ReportEvent
    {
        public float t;            // segundos desde o início da sessão
        public string scene;
        public string type;        // sceneChange | interact | dialogue | choice
        public string target;      // objeto, diálogo ou cena
        public float seconds;      // dialogue: duração | choice: tempo pra escolher
        public int lines;          // falas exibidas (só em dialogue)
        public int typingSkips;    // vezes que pulou a digitação (só em dialogue)
        public string choice;      // texto da opção escolhida (só em choice)
        public string stage;       // Negacao | Raiva | Barganha | Depressao | Aceitacao (só em choice)
    }

    [Serializable]
    public class SceneInteractables
    {
        public string scene;
        public List<string> objects = new List<string>();
    }

    [Serializable]
    public class Report
    {
        public int formatVersion = FormatVersion;
        public string patientCode;
        public string sessionId;
        public string startedAt;
        public string endedAt;
        public float durationSeconds;
        public bool partial = true;
        public List<ReportEvent> events = new List<ReportEvent>();
        public List<SceneInteractables> availableInteractables = new List<SceneInteractables>();
    }

    // Nomes amigáveis pro .txt
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

    private Report report;
    private float sessionStartTime;
    private string fileBaseName;

    public bool SessionActive => report != null;

    // No editor: pasta Relatorios na raiz do projeto (ao lado de Assets, fora do git).
    // No jogo final: persistentDataPath, porque a pasta do jogo pode não aceitar escrita.
#if UNITY_EDITOR
    public static string ReportsFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Relatorios"));
#else
    public static string ReportsFolder => Path.Combine(Application.persistentDataPath, "Relatorios");
#endif

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

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= OnQuitting;
    }

    // ---------- API usada pelos outros scripts ----------

    // Fase 4: o menu chama isso com o código digitado pelo paciente.
    public static void SetPatientCode(string code)
    {
        PlayerPrefs.SetString(PatientCodeKey, code == null ? "" : code.Trim().ToUpperInvariant());
        PlayerPrefs.Save();
    }

    // Chamado pelo ThoughtInteractable (objetos de "observar")
    public static void LogInteract(string target)
    {
        if (Instance == null) return;
        Instance.AddEvent(new ReportEvent { type = "interact", target = target });
    }

    public static void LogDialogue(string target, float seconds, int lines, int typingSkips)
    {
        if (Instance == null) return;
        // Registrado quando o diálogo termina, mas entra na linha do tempo no momento em que começou
        Instance.AddEvent(new ReportEvent
        {
            type = "dialogue",
            target = target,
            seconds = Round(seconds),
            lines = lines,
            typingSkips = typingSkips,
        }, secondsAgo: seconds);
    }

    public static void LogChoice(string dialogue, string choiceText, DialogueData.GriefStage stage, float seconds)
    {
        if (Instance == null) return;
        Instance.AddEvent(new ReportEvent
        {
            type = "choice",
            target = dialogue,
            seconds = Round(seconds),
            choice = choiceText,
            stage = stage.ToString(),
        });
    }

    // ---------- Sessão ----------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Voltar ao menu encerra a sessão
        if (scene.name == MenuSceneName)
        {
            if (SessionActive) EndSession();
            return;
        }

        if (!SessionActive) StartSession();

        AddEvent(new ReportEvent { type = "sceneChange", target = scene.name });
        RecordAvailableInteractables(scene);
        Save(partial: true);
    }

    private void StartSession()
    {
        sessionStartTime = Time.realtimeSinceStartup;

        report = new Report
        {
            patientCode = PlayerPrefs.GetString(PatientCodeKey, ""),
            sessionId = Guid.NewGuid().ToString(),
            startedAt = DateTime.UtcNow.ToString("o"),
        };

        fileBaseName = "sessao_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + report.sessionId.Substring(0, 8);
    }

    private void EndSession()
    {
        Save(partial: false);
        report = null;
        fileBaseName = null;
    }

    private void OnQuitting()
    {
        // Fechou o jogo no meio: grava como incompleta
        if (SessionActive) Save(partial: true);
    }

    private void AddEvent(ReportEvent e, float secondsAgo = 0f)
    {
        if (!SessionActive) return;

        e.t = Round(Mathf.Max(0f, Time.realtimeSinceStartup - sessionStartTime - secondsAgo));
        e.scene = SceneManager.GetActiveScene().name;

        // Mantém a lista em ordem de tempo
        int index = report.events.FindIndex(x => x.t > e.t);
        if (index < 0) report.events.Add(e);
        else report.events.Insert(index, e);
    }

    // Lista uma vez, ao carregar a cena, os objetos de "observar" (ThoughtInteractable).
    // Portas, interruptores e transições ficam de fora: são mecânica, não observação.
    // Inclui inativos porque algumas sequências ativam objetos depois.
    private void RecordAvailableInteractables(Scene scene)
    {
        SceneInteractables entry = report.availableInteractables.Find(s => s.scene == scene.name);
        if (entry == null)
        {
            entry = new SceneInteractables { scene = scene.name };
            report.availableInteractables.Add(entry);
        }

        ThoughtInteractable[] found = FindObjectsByType<ThoughtInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ThoughtInteractable ti in found)
        {
            if (ti.gameObject.scene == scene && !entry.objects.Contains(ti.gameObject.name))
                entry.objects.Add(ti.gameObject.name);
        }

        entry.objects.Sort(StringComparer.Ordinal);
    }

    // ---------- Gravação ----------

    private void Save(bool partial)
    {
        if (!SessionActive) return;

        report.partial = partial;
        report.endedAt = DateTime.UtcNow.ToString("o");
        report.durationSeconds = Round(Time.realtimeSinceStartup - sessionStartTime);

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
        }
    }

    // ---------- Texto legível ----------

    private string BuildText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Relatório Five Stages (formato v" + report.formatVersion + ")");
        sb.AppendLine("Material complementar pensado para apoiar a conversa com o psicólogo. Não é um instrumento diagnóstico.");
        sb.AppendLine();
        sb.AppendLine("Código do paciente: " + (string.IsNullOrEmpty(report.patientCode) ? "(não informado)" : report.patientCode));
        sb.AppendLine("Sessão: " + report.sessionId);
        sb.AppendLine("Início (UTC): " + report.startedAt);
        sb.AppendLine("Fim (UTC): " + report.endedAt);
        sb.AppendLine("Duração: " + FormatTime(report.durationSeconds));
        sb.AppendLine("Situação: " + (report.partial ? "incompleta" : "completa"));
        sb.AppendLine();

        AppendChoicesSummary(sb);
        AppendExplorationSummary(sb);
        AppendReadingSummary(sb);
        AppendTimeline(sb);

        return sb.ToString();
    }

    private void AppendChoicesSummary(StringBuilder sb)
    {
        sb.AppendLine("== Escolhas nos diálogos ==");

        List<ReportEvent> choices = report.events.FindAll(e => e.type == "choice");
        if (choices.Count == 0)
        {
            sb.AppendLine("Nenhuma escolha registrada nesta sessão.");
            sb.AppendLine();
            return;
        }

        Dictionary<string, int> counts = new Dictionary<string, int>();
        float totalTime = 0f;
        foreach (ReportEvent c in choices)
        {
            counts.TryGetValue(c.stage, out int n);
            counts[c.stage] = n + 1;
            totalTime += c.seconds;
        }

        sb.AppendLine("Total de escolhas: " + choices.Count);
        int max = 0;
        foreach (KeyValuePair<string, string> s in StageLabels)
        {
            counts.TryGetValue(s.Key, out int n);
            max = Mathf.Max(max, n);
            sb.AppendLine("  " + s.Value + ": " + n + " (" + Mathf.RoundToInt(100f * n / choices.Count) + "%)");
        }

        List<string> top = new List<string>();
        foreach (KeyValuePair<string, string> s in StageLabels)
            if (counts.TryGetValue(s.Key, out int n) && n == max) top.Add(s.Value);

        sb.AppendLine(top.Count == 1
            ? "Tendência mais frequente: " + top[0] + " (" + max + " de " + choices.Count + ")"
            : "Sem tendência única: empate entre " + string.Join(", ", top) + " (" + max + " cada)");
        sb.AppendLine("Tempo médio para escolher: " + (totalTime / choices.Count).ToString("0.0") + "s");
        sb.AppendLine();
    }

    private void AppendExplorationSummary(StringBuilder sb)
    {
        sb.AppendLine("== Exploração dos cenários ==");

        bool any = false;
        foreach (SceneInteractables s in report.availableInteractables)
        {
            if (s.objects.Count == 0) continue;
            any = true;

            int observed = 0;
            foreach (string obj in s.objects)
                if (report.events.Exists(e => e.type == "interact" && e.scene == s.scene && e.target == obj))
                    observed++;

            float ratio = (float)observed / s.objects.Count;
            string level = ratio >= 0.7f ? "explorou bastante"
                         : ratio >= 0.35f ? "explorou parte do cenário"
                         : "explorou pouco";

            sb.AppendLine(SceneLabel(s.scene) + ": observou " + observed + " de " + s.objects.Count +
                          " objetos (" + Mathf.RoundToInt(ratio * 100f) + "%), " + level);
        }

        if (!any) sb.AppendLine("Nenhum cenário com objetos para observar.");
        sb.AppendLine();
    }

    private void AppendReadingSummary(StringBuilder sb)
    {
        sb.AppendLine("== Leitura dos diálogos ==");

        int dialogues = 0, lines = 0, skips = 0;
        float seconds = 0f;
        foreach (ReportEvent e in report.events)
        {
            if (e.type != "dialogue") continue;
            dialogues++;
            lines += e.lines;
            skips += e.typingSkips;
            seconds += e.seconds;
        }

        if (dialogues == 0)
        {
            sb.AppendLine("Nenhum diálogo registrado.");
            sb.AppendLine();
            return;
        }

        float skipRatio = lines > 0 ? (float)skips / lines : 0f;
        string pace = skipRatio >= 0.7f ? "pulou a digitação na maioria das falas"
                    : skipRatio >= 0.3f ? "pulou a digitação em parte das falas"
                    : "deixou o texto aparecer por completo na maioria das falas";

        sb.AppendLine(dialogues + " diálogos, " + lines + " falas, " + FormatTime(seconds) + " em diálogos no total.");
        sb.AppendLine("Ritmo: " + pace + " (" + Mathf.RoundToInt(skipRatio * 100f) + "%).");
        sb.AppendLine();
    }

    private void AppendTimeline(StringBuilder sb)
    {
        sb.AppendLine("== Linha do tempo ==");

        foreach (ReportEvent e in report.events)
        {
            string time = "[" + FormatTime(e.t) + "] ";
            switch (e.type)
            {
                case "sceneChange":
                    sb.AppendLine(time + "Entrou em: " + SceneLabel(e.target));
                    break;
                case "dialogue":
                    sb.AppendLine(time + "Diálogo: " + e.target + " (" + e.seconds.ToString("0.0") + "s, " + e.lines + " falas)");
                    break;
                case "choice":
                    sb.AppendLine(time + "  Escolheu: \"" + e.choice + "\" → " + StageLabel(e.stage) +
                                  " (pensou " + e.seconds.ToString("0.0") + "s)");
                    break;
                // interact não aparece: o diálogo do objeto já entra logo em seguida
            }
        }
    }

    private static string SceneLabel(string scene) => SceneLabels.TryGetValue(scene, out string label) ? label : scene;
    private static string StageLabel(string stage) => StageLabels.TryGetValue(stage, out string label) ? label : stage;

    private static float Round(float value) => Mathf.Round(value * 10f) / 10f;

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
}
