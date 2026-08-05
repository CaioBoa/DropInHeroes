using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DropInHeroes.Data;

namespace DropInHeroes.EditorTools
{

    /// <summary>
    /// Editor visual da configuração de UI/escala por personagem (Tools ▸ DropInHeroes ▸ Character Visual Config).
    /// Preview do personagem num grid medido, lista de TODAS as animações com play/scrub, e handles para
    /// posicionar a barra de vida/energia (overheadHeight) e o anel de seleção (offsetY/scale), além do
    /// visualScale global. As animações são sprite-swap, então os frames vêm por AnimationUtility e são
    /// desenhados direto no canvas — sem PreviewRenderUtility. Edições só tocam o asset ao Salvar.
    /// F2 (escala por animação) e F3 (hit-events) entram depois; esta é a fundação (F1).
    /// </summary>
    public class CharacterVisualConfigWindow : EditorWindow
    {
        private const float PPU = 150f;             // pixels por unidade de mundo com zoom 1
        private const float AnchorFeetDistance = 1f; // espelha VisualModule.AnchorFeetDistance
        private const float StatusStackMargin = 0.22f; // espelha VisualModule.StatusStackMargin

        private float inspectorW = 320f;
        private bool draggingSplitter;

        private CharacterData target;

        // Cópia de trabalho dos campos do asset (commit só ao Salvar).
        private float wVisualScale = 1f;
        private float wOverheadHeight = 1.14f;
        private float wRingScale = 0.9f;
        private float wRingOffsetY = -0.45f;
        private bool dirty;

        // Ajuste por-clipe (cópia de trabalho da lista + valores do clipe selecionado).
        private readonly List<AnimClipTuning> wTunings = new List<AnimClipTuning>();
        private float wScaleX = 1f, wScaleY = 1f, wOffsetX = 0f, wOffsetY = 0f;
        private AnimationCurve wScaleXCurve, wScaleYCurve, wOffsetXCurve, wOffsetYCurve;
        private bool wLockAspect = true;   // trava X = Y (escala uniforme)

        // Clipes enumerados do personagem (rótulo + clipe).
        private readonly List<string> clipLabels = new List<string>();
        private readonly List<AnimationClip> clips = new List<AnimationClip>();
        private int selectedClip = -1;

        // Frames sprite-swap do clipe selecionado.
        private readonly List<Sprite> frames = new List<Sprite>();
        private readonly List<float> frameTimes = new List<float>();
        private float clipLength;

        // Reprodução.
        private bool playing;
        private double playStart;
        private float playhead;
        private float wPlaySpeed = 0.5f;   // 1 = velocidade real do clipe; menor = mais lento p/ inspecionar

        // Canvas.
        private Vector2 pan;
        private float zoom = 1f;
        private bool frameQueued;

        // Arraste de handles verticais.
        private enum Handle { None, Bar, Ring }
        private Handle dragHandle = Handle.None;

        private Vector2 inspectorScroll;

        [MenuItem("Tools/DropInHeroes/Character Visual Config")]
        private static void Open()
        {
            var w = GetWindow<CharacterVisualConfigWindow>("Visual Config");
            w.minSize = new Vector2(820, 540);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            EditorApplication.update += OnEditorUpdate;
            frameQueued = true;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!playing || clipLength <= 0f) return;
            playhead = (float)(((EditorApplication.timeSinceStartup - playStart) * wPlaySpeed) % clipLength);
            Repaint();
        }

        // ---------------------------------------------------------------- Load

        private void LoadTarget(CharacterData data)
        {
            target = data;
            wTunings.Clear();
            if (target != null)
            {
                wVisualScale = target.visualScale;
                wOverheadHeight = target.overheadHeight;
                wRingScale = target.selectionRingScale;
                wRingOffsetY = target.selectionRingOffsetY;
                if (target.animClipTunings != null)
                    foreach (var e in target.animClipTunings)
                        wTunings.Add(new AnimClipTuning {
                            clip = e.clip, scaleX = Norm(e.scaleX), scaleY = Norm(e.scaleY), offsetX = e.offsetX, offsetY = e.offsetY,
                            scaleXCurve = CloneCurve(e.scaleXCurve), scaleYCurve = CloneCurve(e.scaleYCurve),
                            offsetXCurve = CloneCurve(e.offsetXCurve), offsetYCurve = CloneCurve(e.offsetYCurve)
                        });
            }
            dirty = false;
            RebuildClipList();
            frameQueued = true;
        }

        private void RebuildClipList()
        {
            clipLabels.Clear();
            clips.Clear();
            selectedClip = -1;
            if (target == null) { SelectClip(-1); return; }

            AddClip("Idle", target.idleAnimation);
            AddClip("Run", target.runAnimation);
            AddClip("Drag", target.dragAnimation);
            AddClip("Attack", target.attackAnimation);
            AddClip("Supreme", target.supremeAnimation);
            AddClip("Death", target.deathAnimation);
            AddClip("DeathIdle", target.deathIdleAnimation);
            AddClip("Stun", target.stunAnimation);
            AddClip("Victory", target.victoryAnimation);

            if (target.extraAnimations != null)
                foreach (var e in target.extraAnimations)
                    AddClip("Extra: " + e.key, e.clip);

            if (target.animationProfiles != null)
                foreach (var p in target.animationProfiles)
                {
                    if (p == null) continue;
                    // Perfil = transformação: espelha o conjunto base completo. Clipe nulo cai no base
                    // (já listado acima), então só surfaceia os que a forma realmente sobrescreve.
                    string pre = "Perfil " + p.key + ": ";
                    AddClipIfPresent(pre + "Idle", p.idleAnimation);
                    AddClipIfPresent(pre + "Run", p.runAnimation);
                    AddClipIfPresent(pre + "Drag", p.dragAnimation);
                    AddClipIfPresent(pre + "Attack", p.attackAnimation);
                    AddClipIfPresent(pre + "Supreme", p.supremeAnimation);
                    AddClipIfPresent(pre + "Death", p.deathAnimation);
                    AddClipIfPresent(pre + "DeathIdle", p.deathIdleAnimation);
                    AddClipIfPresent(pre + "Stun", p.stunAnimation);
                    AddClipIfPresent(pre + "Victory", p.victoryAnimation);
                }

            if (clips.Count > 0) SelectClip(FirstNonNullClip());
            else SelectClip(-1);
        }

        private int FirstNonNullClip()
        {
            for (int i = 0; i < clips.Count; i++) if (clips[i] != null) return i;
            return 0;
        }

        private void AddClip(string label, AnimationClip clip)
        {
            clipLabels.Add(label);
            clips.Add(clip);
        }

        // Perfis/extras: só entram se atribuídos (nulo = herda o base, que já está na lista).
        private void AddClipIfPresent(string label, AnimationClip clip)
        {
            if (clip != null) AddClip(label, clip);
        }

        private void SelectClip(int index)
        {
            selectedClip = index;
            frames.Clear();
            frameTimes.Clear();
            clipLength = 0f;
            playhead = 0f;
            playing = false;
            LoadClipTuning();

            if (index < 0 || index >= clips.Count || clips[index] == null) return;
            AnimationClip clip = clips[index];
            clipLength = clip.length;

            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var b in bindings)
            {
                if (b.propertyName != "m_Sprite") continue;
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, b);
                foreach (var k in keys)
                {
                    frames.Add(k.value as Sprite);
                    frameTimes.Add(k.time);
                }
                break; // um único curve de sprite por clipe
            }
            frameQueued = true;
        }

        private Sprite CurrentFrame()
        {
            if (frames.Count == 0) return target != null ? target.defaultSprite : null;
            Sprite s = frames[0];
            for (int i = 0; i < frameTimes.Count; i++)
            {
                if (frameTimes[i] <= playhead + 1e-4f) s = frames[i];
                else break;
            }
            return s;
        }

        private AnimationClip SelectedClip()
            => selectedClip >= 0 && selectedClip < clips.Count ? clips[selectedClip] : null;

        private void LoadClipTuning()
        {
            wScaleX = 1f; wScaleY = 1f; wOffsetX = 0f; wOffsetY = 0f;
            wScaleXCurve = null; wScaleYCurve = null; wOffsetXCurve = null; wOffsetYCurve = null;
            wLockAspect = true;
            var clip = SelectedClip();
            if (clip == null) return;
            for (int i = 0; i < wTunings.Count; i++)
                if (wTunings[i].clip == clip)
                {
                    var e = wTunings[i];
                    wScaleX = e.scaleX <= 0f ? 1f : e.scaleX;
                    wScaleY = e.scaleY <= 0f ? 1f : e.scaleY;
                    wOffsetX = e.offsetX; wOffsetY = e.offsetY;
                    wScaleXCurve = CloneCurve(e.scaleXCurve);
                    wScaleYCurve = CloneCurve(e.scaleYCurve);
                    wOffsetXCurve = CloneCurve(e.offsetXCurve);
                    wOffsetYCurve = CloneCurve(e.offsetYCurve);
                    wLockAspect = Mathf.Abs(wScaleX - wScaleY) < 1e-3f && (wScaleXCurve == null) == (wScaleYCurve == null);
                    return;
                }
        }

        private void UpsertClipTuning()
        {
            var clip = SelectedClip();
            if (clip == null) return;
            var entry = new AnimClipTuning
            {
                clip = clip, scaleX = wScaleX, scaleY = wScaleY, offsetX = wOffsetX, offsetY = wOffsetY,
                scaleXCurve = CurveOrNull(wScaleXCurve), scaleYCurve = CurveOrNull(wScaleYCurve),
                offsetXCurve = CurveOrNull(wOffsetXCurve), offsetYCurve = CurveOrNull(wOffsetYCurve)
            };
            for (int i = 0; i < wTunings.Count; i++)
                if (wTunings[i].clip == clip) { wTunings[i] = entry; return; }
            wTunings.Add(entry);
        }

        private static AnimationCurve CloneCurve(AnimationCurve c)
            => (c != null && c.length > 0) ? new AnimationCurve(c.keys) : null;
        private static AnimationCurve CurveOrNull(AnimationCurve c) => (c != null && c.length >= 2) ? c : null;
        private static bool CurveActive(AnimationCurve c) => c != null && c.length >= 2;
        private static float Norm(float v) => v <= 0f ? 1f : v;

        // Insere/atualiza uma chave no tempo t (0..1) com valor v; cria a curva se necessário.
        private static AnimationCurve SetCurveKey(AnimationCurve c, float t, float v)
        {
            if (c == null) c = new AnimationCurve();
            for (int i = 0; i < c.length; i++)
                if (Mathf.Abs(c.keys[i].time - t) < 0.001f) { c.MoveKey(i, new Keyframe(t, v)); return c; }
            c.AddKey(t, v);
            return c;
        }

        private float NormalizedPlayhead() => clipLength > 0f ? Mathf.Clamp01(playhead / clipLength) : 0f;

        // Linha de um eixo: slider (valor constante / no frame) + campo de curva + Fixar + limpar. Retorna se mudou.
        private bool DrawAxisRow(string label, ref float constVal, ref AnimationCurve curve, float min, float max, float nt)
        {
            bool changed = false;
            bool on = CurveActive(curve);
            EditorGUI.BeginChangeCheck();
            constVal = EditorGUILayout.Slider(on ? label + " (frame)" : label, constVal, min, max);
            if (EditorGUI.EndChangeCheck()) changed = true;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14f);
            EditorGUI.BeginChangeCheck();
            var c = EditorGUILayout.CurveField(curve ?? new AnimationCurve());
            if (EditorGUI.EndChangeCheck()) { curve = (c != null && c.length > 0) ? c : null; changed = true; }
            if (GUILayout.Button("Fixar", EditorStyles.miniButton, GUILayout.Width(44f))) { curve = SetCurveKey(curve, nt, constVal); changed = true; }
            if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20f))) { curve = null; changed = true; }
            EditorGUILayout.EndHorizontal();
            return changed;
        }

        // ---------------------------------------------------------------- OnGUI

        private void OnGUI()
        {
            DrawToolbar();
            float top = EditorStyles.toolbar.fixedHeight > 0f ? EditorStyles.toolbar.fixedHeight : 21f;

            Rect canvasRect = new Rect(0f, top, position.width - inspectorW, position.height - top);
            Rect splitterRect = new Rect(canvasRect.xMax - 3f, top, 6f, canvasRect.height);
            Rect inspectorRect = new Rect(canvasRect.xMax, top, inspectorW, canvasRect.height);

            if (frameQueued && canvasRect.width > 1f && Event.current.type == EventType.Repaint) { FrameCharacter(canvasRect); frameQueued = false; }

            HandleSplitter(splitterRect);
            if (!draggingSplitter) HandleCanvasEvents(canvasRect);
            DrawCanvas(canvasRect);
            DrawSplitter(splitterRect);
            DrawInspector(inspectorRect);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            var picked = (CharacterData)EditorGUILayout.ObjectField(target, typeof(CharacterData), false, GUILayout.Width(220f));
            if (EditorGUI.EndChangeCheck()) LoadTarget(picked);

            GUILayout.Space(8f);
            using (new EditorGUI.DisabledScope(frames.Count == 0))
            {
                if (GUILayout.Button(playing ? "❚❚ Pausar" : "▶ Play", EditorStyles.toolbarButton, GUILayout.Width(76f)))
                    TogglePlay();
            }
            if (GUILayout.Button("Enquadrar (F)", EditorStyles.toolbarButton)) frameQueued = true;

            GUILayout.FlexibleSpace();
            if (target != null)
                GUILayout.Label(dirty ? "● não salvo" : "salvo", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(target == null || !dirty))
                if (GUILayout.Button("Salvar", EditorStyles.toolbarButton)) Save();
            using (new EditorGUI.DisabledScope(target == null))
                if (GUILayout.Button("Recarregar", EditorStyles.toolbarButton)) LoadTarget(target);
            EditorGUILayout.EndHorizontal();
        }

        private void TogglePlay()
        {
            playing = !playing;
            if (playing) playStart = EditorApplication.timeSinceStartup - (wPlaySpeed > 0f ? playhead / wPlaySpeed : 0.0);
        }

        // ---------------------------------------------------------------- Canvas

        private void DrawCanvas(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, new Color(0.13f, 0.14f, 0.17f, 1f));
            GUI.BeginClip(rect);
            Vector2 size = rect.size;
            Handles.BeginGUI();

            DrawGrid(size);
            DrawCharacter(size);
            DrawWorldAnchors(size);

            Handles.EndGUI();
            GUI.EndClip();

            if (target == null)
                DrawCenteredHint(rect, "Escolha um CharacterData na barra de ferramentas ↑");
            else if (frames.Count == 0 && (target.defaultSprite == null))
                DrawCenteredHint(rect, "Este clipe não tem frames de sprite.");
        }

        private void DrawCenteredHint(Rect rect, string msg)
        {
            var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1, 1, 1, 0.5f) } };
            GUI.Label(rect, msg, style);
        }

        // Grid cartesiano com linhas a cada 0.5 unidade e rótulos.
        private void DrawGrid(Vector2 size)
        {
            float s = PPU * zoom;
            Vector2 c = Center(size);

            // Extensão visível em unidades de mundo.
            float halfW = size.x / (2f * s);
            float halfH = size.y / (2f * s);
            int minX = Mathf.FloorToInt(-halfW - c.x / s + (c.x - size.x * 0.5f) / s);

            // Linhas verticais/horizontais a cada 0.5, com destaque nos inteiros.
            const float step = 0.5f;
            float worldLeft = L2W(new Vector2(0, 0), size).x;
            float worldRight = L2W(new Vector2(size.x, 0), size).x;
            float worldTop = L2W(new Vector2(0, 0), size).y;
            float worldBottom = L2W(new Vector2(0, size.y), size).y;

            var minor = new Color(1, 1, 1, 0.05f);
            var major = new Color(1, 1, 1, 0.12f);
            var axis = new Color(0.4f, 0.8f, 1f, 0.35f);

            for (float x = Mathf.Ceil(worldLeft / step) * step; x <= worldRight; x += step)
            {
                bool whole = Mathf.Abs(x - Mathf.Round(x)) < 1e-3f;
                Handles.color = Mathf.Abs(x) < 1e-3f ? axis : (whole ? major : minor);
                Vector2 a = W2L(new Vector2(x, worldBottom), size);
                Vector2 b = W2L(new Vector2(x, worldTop), size);
                Handles.DrawLine(a, b);
            }
            for (float y = Mathf.Ceil(worldBottom / step) * step; y <= worldTop; y += step)
            {
                bool whole = Mathf.Abs(y - Mathf.Round(y)) < 1e-3f;
                Handles.color = Mathf.Abs(y) < 1e-3f ? axis : (whole ? major : minor);
                Vector2 a = W2L(new Vector2(worldLeft, y), size);
                Vector2 b = W2L(new Vector2(worldRight, y), size);
                Handles.DrawLine(a, b);
                if (whole && Mathf.Abs(y) > 1e-3f)
                {
                    var st = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1, 1, 1, 0.35f) } };
                    GUI.Label(new Rect(a.x + 3f, a.y - 8f, 40f, 16f), y.ToString("0.#") + "m", st);
                }
            }
        }

        // Desenha o frame atual espelhando ApplyVisualScale: origem (pivot) em y=(scale-1)*feet, escala visualScale.
        private void DrawCharacter(Vector2 size)
        {
            Sprite sp = CurrentFrame();
            if (sp == null) return;
            Texture2D tex = sp.texture;
            if (tex == null) return;

            float nt = NormalizedPlayhead();
            float baseVis = wVisualScale <= 0f ? 1f : wVisualScale;
            float mulX = CurveActive(wScaleXCurve) ? wScaleXCurve.Evaluate(nt) : Norm(wScaleX);
            float mulY = CurveActive(wScaleYCurve) ? wScaleYCurve.Evaluate(nt) : Norm(wScaleY);
            float offX = CurveActive(wOffsetXCurve) ? wOffsetXCurve.Evaluate(nt) : wOffsetX;
            float offY = CurveActive(wOffsetYCurve) ? wOffsetYCurve.Evaluate(nt) : wOffsetY;
            float sVisX = baseVis * (mulX <= 0f ? 1f : mulX);
            float sVisY = baseVis * (mulY <= 0f ? 1f : mulY);
            Vector2 worldSize = new Vector2(sp.rect.width / sp.pixelsPerUnit * sVisX, sp.rect.height / sp.pixelsPerUnit * sVisY);
            Vector2 pivotNorm = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
            Vector2 origin = new Vector2(offX, (sVisY - 1f) * AnchorFeetDistance + offY);
            Vector2 worldBL = origin - new Vector2(pivotNorm.x * worldSize.x, pivotNorm.y * worldSize.y);
            Vector2 worldTR = worldBL + worldSize;

            Vector2 slBL = W2L(new Vector2(worldBL.x, worldBL.y), size);
            Vector2 slTR = W2L(new Vector2(worldTR.x, worldTR.y), size);
            Rect screen = Rect.MinMaxRect(Mathf.Min(slBL.x, slTR.x), Mathf.Min(slBL.y, slTR.y), Mathf.Max(slBL.x, slTR.x), Mathf.Max(slBL.y, slTR.y));

            // Single-mode: o jogo renderiza a textura CHEIA; o sp.textureRect fica DEFASADO/trimado e,
            // se usado no UV com o quad dimensionado por sp.rect, estica a imagem. Usar sp.rect no UV
            // mantém quad e UV consistentes = fiel ao que o SpriteRenderer desenha.
            Rect uv = new Rect(sp.rect.x / tex.width, sp.rect.y / tex.height,
                               sp.rect.width / tex.width, sp.rect.height / tex.height);
            GUI.DrawTextureWithTexCoords(screen, tex, uv, true);
        }

        // Barra (overheadHeight), status (overheadHeight+margin) e anel (offsetY, raio=ringScale).
        private void DrawWorldAnchors(Vector2 size)
        {
            float worldLeft = L2W(new Vector2(0, 0), size).x;
            float worldRight = L2W(new Vector2(size.x, 0), size).x;

            // Linha da barra.
            DrawHorizontalMarker(size, wOverheadHeight, new Color(0.3f, 0.85f, 0.4f, 0.95f), "Barra (overhead)", worldLeft, worldRight);
            // Linha do status.
            DrawHorizontalMarker(size, wOverheadHeight + StatusStackMargin, new Color(0.5f, 0.6f, 0.9f, 0.6f), "Status", worldLeft, worldRight);
            // Linha + elipse do anel.
            DrawHorizontalMarker(size, wRingOffsetY, new Color(1f, 0.85f, 0.35f, 0.95f), "Anel", worldLeft, worldRight);
            DrawRingEllipse(size, wRingOffsetY, wRingScale);
            // Pés (y=0).
            Handles.color = new Color(1f, 1f, 1f, 0.25f);
            Vector2 fa = W2L(new Vector2(worldLeft, 0f), size);
            Vector2 fb = W2L(new Vector2(worldRight, 0f), size);
            Handles.DrawLine(fa, fb);
        }

        private void DrawHorizontalMarker(Vector2 size, float worldY, Color col, string label, float worldLeft, float worldRight)
        {
            Handles.color = col;
            Vector2 a = W2L(new Vector2(worldLeft, worldY), size);
            Vector2 b = W2L(new Vector2(worldRight, worldY), size);
            Handles.DrawAAPolyLine(2f, a, b);
            var st = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = col } };
            GUI.Label(new Rect(a.x + 4f, a.y - 15f, 160f, 14f), label + "  y=" + worldY.ToString("0.00"), st);
        }

        private void DrawRingEllipse(Vector2 size, float worldY, float ringScale)
        {
            // Aproxima o anel do prefab (1.92 x 1.12 na escala do sprite) escalado por ringScale.
            float rx = 0.96f * ringScale;
            float ry = 0.56f * ringScale;
            Handles.color = new Color(1f, 0.85f, 0.35f, 0.5f);
            const int seg = 40;
            Vector2 prev = default;
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg * Mathf.PI * 2f;
                Vector2 w = new Vector2(Mathf.Cos(t) * rx, worldY + Mathf.Sin(t) * ry);
                Vector2 sp = W2L(w, size);
                if (i > 0) Handles.DrawLine(prev, sp);
                prev = sp;
            }
        }

        // ---------------------------------------------------------------- Canvas events

        private void HandleCanvasEvents(Rect rect)
        {
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            bool inside = rect.Contains(mouse);
            Vector2 size = rect.size;

            switch (e.type)
            {
                case EventType.ScrollWheel:
                    if (!inside) break;
                    ZoomAt(mouse - rect.position, size, -e.delta.y);
                    e.Use();
                    break;

                case EventType.MouseDown:
                    if (!inside || target == null) break;
                    if (e.button == 2 || (e.button == 0 && e.alt)) { dragHandle = Handle.None; e.Use(); break; }
                    if (e.button == 0)
                    {
                        Vector2 local = mouse - rect.position;
                        float barY = W2L(new Vector2(0, wOverheadHeight), size).y;
                        float ringY = W2L(new Vector2(0, wRingOffsetY), size).y;
                        if (Mathf.Abs(local.y - barY) < 8f) dragHandle = Handle.Bar;
                        else if (Mathf.Abs(local.y - ringY) < 8f) dragHandle = Handle.Ring;
                        else dragHandle = Handle.None;
                        if (dragHandle != Handle.None) e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (dragHandle != Handle.None)
                    {
                        float worldY = L2W(e.mousePosition - rect.position, size).y;
                        if (dragHandle == Handle.Bar) wOverheadHeight = Mathf.Round(worldY * 100f) / 100f;
                        else wRingOffsetY = Mathf.Round(worldY * 100f) / 100f;
                        dirty = true; e.Use(); Repaint();
                    }
                    else if (e.button == 2 || (e.button == 0 && e.alt))
                    {
                        pan += e.delta; e.Use(); Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (dragHandle != Handle.None) { dragHandle = Handle.None; e.Use(); }
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl != 0) break;
                    if (e.keyCode == KeyCode.F) { frameQueued = true; e.Use(); }
                    break;
            }
        }

        // ---------------------------------------------------------------- Inspector

        private void DrawInspector(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll, GUIStyle.none, GUI.skin.verticalScrollbar);
            float lw = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 120f;

            if (target == null)
            {
                EditorGUILayout.HelpBox("Escolha um personagem para começar.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUIUtility.labelWidth = lw;
                GUILayout.EndArea();
                return;
            }

            EditorGUILayout.LabelField(target.displayName + "  (" + target.name + ")", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Escala & UI", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            wVisualScale = EditorGUILayout.Slider("Visual scale", wVisualScale, 0.2f, 3f);
            wOverheadHeight = EditorGUILayout.Slider("Barra (altura Y)", wOverheadHeight, 0f, 3f);
            wRingOffsetY = EditorGUILayout.Slider("Anel (offset Y)", wRingOffsetY, -1.5f, 0.5f);
            wRingScale = EditorGUILayout.Slider("Anel (escala)", wRingScale, 0.2f, 2f);
            if (EditorGUI.EndChangeCheck()) dirty = true;

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("Arraste as linhas verde (barra) e amarela (anel) direto no preview para posicionar verticalmente.", MessageType.None);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Animações", EditorStyles.boldLabel);
            for (int i = 0; i < clips.Count; i++)
            {
                bool has = clips[i] != null;
                using (new EditorGUI.DisabledScope(!has))
                {
                    bool sel = i == selectedClip;
                    var style = sel ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                    string suffix = has ? "" : "  (vazio)";
                    if (GUILayout.Toggle(sel, clipLabels[i] + suffix, style) && !sel && has)
                        SelectClip(i);
                }
            }

            if (frames.Count > 0)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Frames", frames.Count + "  ·  " + clipLength.ToString("0.00") + "s", EditorStyles.miniLabel);

                EditorGUI.BeginChangeCheck();
                float spd = EditorGUILayout.Slider("Velocidade", wPlaySpeed, 0.05f, 2f);
                if (EditorGUI.EndChangeCheck())
                {
                    wPlaySpeed = spd;
                    if (playing) playStart = EditorApplication.timeSinceStartup - (wPlaySpeed > 0f ? playhead / wPlaySpeed : 0.0);
                }

                EditorGUI.BeginChangeCheck();
                float ph = EditorGUILayout.Slider("Tempo", playhead, 0f, Mathf.Max(0.0001f, clipLength));
                if (EditorGUI.EndChangeCheck()) { playing = false; playhead = ph; Repaint(); }
            }

            var selClip = SelectedClip();
            if (selClip != null)
            {
                EditorGUILayout.Space(8f);
                float nt = NormalizedPlayhead();
                EditorGUILayout.LabelField("Ajuste deste clipe — " + clipLabels[selectedClip], EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Frame atual t=" + (nt * 100f).ToString("0") + "%   ('Fixar' crava a chave neste frame)", EditorStyles.miniLabel);

                bool ch = false;
                EditorGUI.BeginChangeCheck();
                wLockAspect = EditorGUILayout.ToggleLeft("Travar proporção (X = Y, escala uniforme)", wLockAspect);
                if (EditorGUI.EndChangeCheck() && wLockAspect) { wScaleY = wScaleX; wScaleYCurve = CloneCurve(wScaleXCurve); ch = true; }

                if (wLockAspect)
                {
                    if (DrawAxisRow("Escala", ref wScaleX, ref wScaleXCurve, 0.2f, 3f, nt)) { wScaleY = wScaleX; wScaleYCurve = CloneCurve(wScaleXCurve); ch = true; }
                }
                else
                {
                    if (DrawAxisRow("Escala X", ref wScaleX, ref wScaleXCurve, 0.2f, 3f, nt)) ch = true;
                    if (DrawAxisRow("Escala Y", ref wScaleY, ref wScaleYCurve, 0.2f, 3f, nt)) ch = true;
                }
                if (DrawAxisRow("Offset X", ref wOffsetX, ref wOffsetXCurve, -1.5f, 1.5f, nt)) ch = true;
                if (DrawAxisRow("Offset Y", ref wOffsetY, ref wOffsetYCurve, -1.5f, 1.5f, nt)) ch = true;
                if (ch) { UpsertClipTuning(); dirty = true; Repaint(); }

                float bv = wVisualScale <= 0f ? 1f : wVisualScale;
                float emX = CurveActive(wScaleXCurve) ? wScaleXCurve.Evaluate(nt) : Norm(wScaleX);
                float emY = CurveActive(wScaleYCurve) ? wScaleYCurve.Evaluate(nt) : Norm(wScaleY);
                EditorGUILayout.LabelField("Escala efetiva agora", "X " + (bv * emX).ToString("0.00") + "×   Y " + (bv * emY).ToString("0.00") + "×", EditorStyles.miniLabel);

                if (GUILayout.Button("Resetar este clipe"))
                {
                    wScaleX = 1f; wScaleY = 1f; wOffsetX = 0f; wOffsetY = 0f;
                    wScaleXCurve = null; wScaleYCurve = null; wOffsetXCurve = null; wOffsetYCurve = null;
                    wLockAspect = true; UpsertClipTuning(); dirty = true; Repaint();
                }
                EditorGUILayout.HelpBox("Ludo espremido/alongado → destrave a proporção e ajuste X e Y separados. Escrube até o frame errado, ajuste o eixo e 'Fixar' crava aquele frame; 2+ frames interpolam. '×' limpa a curva do eixo.", MessageType.None);
            }

            EditorGUILayout.EndScrollView();
            EditorGUIUtility.labelWidth = lw;
            GUILayout.EndArea();
        }

        // ---------------------------------------------------------------- Save

        private void Save()
        {
            if (target == null) return;
            UpsertClipTuning();
            var pruned = new List<AnimClipTuning>();
            foreach (var e in wTunings)
            {
                bool hasCurve = CurveActive(e.scaleXCurve) || CurveActive(e.scaleYCurve) || CurveActive(e.offsetXCurve) || CurveActive(e.offsetYCurve);
                bool nonDefault = Mathf.Abs(Norm(e.scaleX) - 1f) > 1e-3f || Mathf.Abs(Norm(e.scaleY) - 1f) > 1e-3f
                    || Mathf.Abs(e.offsetX) > 1e-3f || Mathf.Abs(e.offsetY) > 1e-3f || hasCurve;
                if (e.clip != null && nonDefault)
                    pruned.Add(e);
            }

            Undo.RecordObject(target, "Editar Visual Config");
            target.visualScale = wVisualScale;
            target.overheadHeight = wOverheadHeight;
            target.selectionRingScale = wRingScale;
            target.selectionRingOffsetY = wRingOffsetY;
            target.animClipTunings = pruned;
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            dirty = false;
        }

        // ---------------------------------------------------------------- Transform / zoom

        private Vector2 Center(Vector2 size) => size * 0.5f + pan;

        private Vector2 W2L(Vector2 w, Vector2 size)
        {
            float s = PPU * zoom; Vector2 c = Center(size);
            return new Vector2(c.x + w.x * s, c.y - w.y * s);
        }

        private Vector2 L2W(Vector2 l, Vector2 size)
        {
            float s = PPU * zoom; Vector2 c = Center(size);
            return new Vector2((l.x - c.x) / s, -(l.y - c.y) / s);
        }

        private void ZoomAt(Vector2 local, Vector2 size, float delta)
        {
            Vector2 worldBefore = L2W(local, size);
            zoom = Mathf.Clamp(zoom * (1f + delta * 0.05f), 0.2f, 8f);
            float s = PPU * zoom;
            pan.x = local.x - size.x * 0.5f - worldBefore.x * s;
            pan.y = local.y - size.y * 0.5f + worldBefore.y * s;
            Repaint();
        }

        // Enquadra o personagem (pés em y=0, ~2.5 unidades de altura) centralizado com margem.
        private void FrameCharacter(Rect rect)
        {
            Vector2 size = rect.size;
            float contentHeight = 3.2f; // pés a topo com folga p/ barra
            zoom = Mathf.Clamp(size.y * 0.85f / (contentHeight * PPU), 0.2f, 8f);
            float s = PPU * zoom;
            // Centraliza verticalmente em ~y=1.2 (meio do personagem+barra).
            pan = new Vector2(0f, 1.2f * s);
            Repaint();
        }

        private void HandleSplitter(Rect r)
        {
            EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeHorizontal);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { draggingSplitter = true; e.Use(); }
            else if (e.type == EventType.MouseDrag && draggingSplitter)
            {
                inspectorW = Mathf.Clamp(position.width - e.mousePosition.x, 260f, Mathf.Max(260f, position.width - 360f));
                e.Use(); Repaint();
            }
            else if (e.type == EventType.MouseUp && draggingSplitter) { draggingSplitter = false; e.Use(); }
        }

        private void DrawSplitter(Rect r)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(new Rect(r.x + 2f, r.y, 2f, r.height), new Color(0f, 0f, 0f, 0.45f));
        }
    }
}
