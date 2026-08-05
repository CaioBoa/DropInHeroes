using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DropInHeroes.Utils;

namespace DropInHeroes.Sandbox
{

    /// <summary>
    /// Controller fino do overlay do Sandbox (UI autorada na cena — só referencia elementos e delega ao
    /// <see cref="SandboxController"/>). Cobre a manipulação da partida: iniciar/parar/reiniciar/limpar,
    /// pausar, velocidade de exibição, timer máximo e os stats do dummy.
    /// </summary>
    public class SandboxOverlayController : MonoBehaviour
    {
        [Header("Núcleo")]
        [SerializeField] private SandboxController sandbox;

        [Header("Controles de partida")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Toggle pauseToggle;

        [Header("Velocidade")]
        [Tooltip("min/max devem ser configurados no Inspector do Slider (ex.: 0.1 a 4).")]
        [SerializeField] private Slider speedSlider;
        [SerializeField] private TMP_Text speedLabel;

        [Header("Timer máximo (segundos; 0 = ilimitado)")]
        [SerializeField] private TMP_InputField timerInput;

        [Header("Dummy")]
        [SerializeField] private TMP_InputField defenseInput;
        [SerializeField] private TMP_InputField magicalDefenseInput;
        [SerializeField] private TMP_InputField healthInput;
        [SerializeField] private Toggle immortalToggle;
        [SerializeField] private Button applyDummyButton;

        private void Awake()
        {
            if (sandbox == null) sandbox = FindFirstObjectByType<SandboxController>();
        }

        private void OnEnable()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (stopButton != null) stopButton.onClick.AddListener(OnStop);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
            if (clearButton != null) clearButton.onClick.AddListener(OnClear);
            if (pauseToggle != null) pauseToggle.onValueChanged.AddListener(OnPauseChanged);
            if (speedSlider != null) speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            if (timerInput != null) timerInput.onEndEdit.AddListener(OnTimerChanged);
            if (applyDummyButton != null) applyDummyButton.onClick.AddListener(OnApplyDummy);

            if (speedSlider != null) OnSpeedChanged(speedSlider.value);
        }

        private void OnDisable()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStart);
            if (stopButton != null) stopButton.onClick.RemoveListener(OnStop);
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestart);
            if (clearButton != null) clearButton.onClick.RemoveListener(OnClear);
            if (pauseToggle != null) pauseToggle.onValueChanged.RemoveListener(OnPauseChanged);
            if (speedSlider != null) speedSlider.onValueChanged.RemoveListener(OnSpeedChanged);
            if (timerInput != null) timerInput.onEndEdit.RemoveListener(OnTimerChanged);
            if (applyDummyButton != null) applyDummyButton.onClick.RemoveListener(OnApplyDummy);
        }

        // === MATCH ===

        private void OnStart() => sandbox?.StartMatch();
        private void OnStop() => sandbox?.StopMatch();
        private void OnRestart() => sandbox?.RestartMatch();
        private void OnClear() => sandbox?.ClearField();
        private void OnPauseChanged(bool paused) => sandbox?.SetPaused(paused);

        private void OnSpeedChanged(float value)
        {
            sandbox?.SetSpeed(value);
            if (speedLabel != null) speedLabel.text = value.ToString("0.0", CultureInfo.InvariantCulture) + "x";
        }

        private void OnTimerChanged(string raw)
        {
            if (sandbox != null && TryParse(raw, out float seconds))
                sandbox.SetMaxMatchSeconds(Mathf.Max(0f, seconds));
        }

        // === DUMMY ===

        private void OnApplyDummy()
        {
            if (sandbox == null) return;
            TryParse(defenseInput != null ? defenseInput.text : null, out float def);
            TryParse(magicalDefenseInput != null ? magicalDefenseInput.text : null, out float mdef);
            TryParse(healthInput != null ? healthInput.text : null, out float hp);
            bool immortal = immortalToggle == null || immortalToggle.isOn;
            sandbox.ConfigureDummy(def, mdef, hp, immortal);
            DebugManager.Log($"Sandbox: dummy configurado (Def {def}, MDef {mdef}, HP {hp}, imortal {immortal}).", DebugCategory.Combat);
        }

        private static bool TryParse(string raw, out float value)
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                || float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }
    }
}
