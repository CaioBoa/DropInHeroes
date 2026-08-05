using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Combat;
using static DropInHeroes.EditorTools.StatTreeSymmetry;

namespace DropInHeroes.EditorTools
{

    /// <summary>
    /// Editor visual e livre da árvore de stats (Tools ▸ DropInHeroes ▸ Stat Tree Editor).
    /// Canvas com zoom (scroll) e pan (botão do meio / Alt+arrastar), enquadrar (F). Ferramentas: Mover/Selecionar,
    /// Conectar (arrastar de um nó a outro) e Adicionar. Inspector por nó define stat, rank, notable e pontos.
    /// Modo AUTO-ESPELHO: qualquer edição em um setor replica nos 5 (grupos de simetria). Modo LIVRE: edita nós soltos.
    /// "Semear base" gera uma topologia simétrica inicial; edições só tocam o asset ao Salvar (com Undo/Recarregar).
    /// </summary>
    public class StatTreeEditorWindow : EditorWindow
    {
        private enum Tool { Move, Connect, Add }
        private enum DragMode { None, Pan, Move, Connect, Marquee }

        private const float PPU = 220f;            // pixels por unidade normalizada com zoom 1
        private float inspectorW = 300f;
        private bool draggingSplitter;

        private StatTreeData target;
        private readonly List<TreeNode> nodes = new List<TreeNode>();
        private readonly List<TreeEdge> edges = new List<TreeEdge>();
        private int[] pointsPerTier = { 8, 10, 12 };

        // --- Modo Build padrão: autora o CharacterData.defaultBuild clicando nos nós (mesmas regras do jogo) ---
        private enum Mode { Topologia, Build }
        private Mode mode = Mode.Topologia;
        private static readonly string[] ModeNames = { "Topologia", "Build padrão" };
        private CharacterData buildTarget;
        private CharacterBuild workingBuild;   // cópia de trabalho; commit no asset só ao "Salvar build"
        private BuildAllocator buildAllocator;

        private Vector2 pan;
        private float zoom = 1f;
        private Tool tool = Tool.Move;
        private bool autoMirror = true;
        private bool snap;
        private int snapAngleDiv = 6;
        private bool showGuides = true;
        private bool frameQueued;

        private readonly HashSet<string> selected = new HashSet<string>();
        private string hoverId;
        private DragMode drag = DragMode.None;
        private Vector2 dragLastWorld;
        private bool dragMoved;
        private string edgeFrom;
        private Vector2 marqueeStart;
        private bool marqueeAdditive;

        private StatType newStat = StatType.Attack;
        private int newTier;
        private int nodePointsValue = 10;      // pontos concedidos por nó normal (global)
        private int notablePointsValue = 20;   // pontos concedidos por notable (global)
        private int nextGroup, freeCounter;
        private Vector2 inspectorScroll;

        private bool seedFold;
        private readonly StatTreeSeeder.SeedParams seed = new StatTreeSeeder.SeedParams();

        private readonly List<Snapshot> undoStack = new List<Snapshot>();
        private struct Snapshot { public TreeNode[] n; public TreeEdge[] e; public int[] p; }

        private static readonly Color[] RankColors =
        {
            new Color(0.32f, 0.72f, 1f), new Color(0.74f, 0.52f, 1f), new Color(1f, 0.62f, 0.30f)
        };
        private static readonly string[] ToolNames = { "Mover", "Conectar", "Adicionar" };
        private static readonly string[] RankLabels = { "1", "2", "3" };

        [MenuItem("Tools/DropInHeroes/Stat Tree Editor")]
        private static void Open()
        {
            var w = GetWindow<StatTreeEditorWindow>("Stat Tree");
            w.minSize = new Vector2(760, 520);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            if (target == null) target = AssetDatabase.LoadAssetAtPath<StatTreeData>("Assets/Data/SharedStatTree.asset");
            Reload();
            frameQueued = true;
        }

        // ---------------------------------------------------------------- OnGUI

        private void OnGUI()
        {
            DrawToolbar();
            float top = EditorStyles.toolbar.fixedHeight > 0f ? EditorStyles.toolbar.fixedHeight : 21f;

            Rect canvasRect = new Rect(0f, top, position.width - inspectorW, position.height - top);
            Rect splitterRect = new Rect(canvasRect.xMax - 3f, top, 6f, canvasRect.height);
            Rect inspectorRect = new Rect(canvasRect.xMax, top, inspectorW, canvasRect.height);

            if (frameQueued && canvasRect.width > 1f && Event.current.type == EventType.Repaint) { FrameAll(canvasRect); frameQueued = false; }

            HandleSplitter(splitterRect);
            if (!draggingSplitter) HandleEvents(canvasRect);
            DrawCanvas(canvasRect);
            DrawSplitter(splitterRect);
            DrawInspector(inspectorRect);
        }

        private void HandleSplitter(Rect r)
        {
            EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeHorizontal);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { draggingSplitter = true; e.Use(); }
            else if (e.type == EventType.MouseDrag && draggingSplitter)
            {
                inspectorW = Mathf.Clamp(position.width - e.mousePosition.x, 230f, Mathf.Max(230f, position.width - 320f));
                e.Use(); Repaint();
            }
            else if (e.type == EventType.MouseUp && draggingSplitter) { draggingSplitter = false; e.Use(); }
        }

        private void DrawSplitter(Rect r)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(new Rect(r.x + 2f, r.y, 2f, r.height), new Color(0f, 0f, 0f, 0.45f));
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            Mode newMode = (Mode)GUILayout.Toolbar((int)mode, ModeNames, EditorStyles.toolbarButton, GUILayout.Width(180f));
            if (newMode != mode) { mode = newMode; if (mode == Mode.Build) EnterBuildMode(); frameQueued = true; }
            GUILayout.Space(8f);
            if (mode == Mode.Topologia) DrawTopologyToolbar();
            else DrawBuildToolbar();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTopologyToolbar()
        {
            tool = (Tool)GUILayout.Toolbar((int)tool, ToolNames, EditorStyles.toolbarButton, GUILayout.Width(210f));
            GUILayout.Space(6f);
            autoMirror = GUILayout.Toggle(autoMirror, "Auto-espelho ×5", EditorStyles.toolbarButton);
            snap = GUILayout.Toggle(snap, "Snap", EditorStyles.toolbarButton);
            showGuides = GUILayout.Toggle(showGuides, "Guias", EditorStyles.toolbarButton);
            if (GUILayout.Button("Espelhar sel. ×5", EditorStyles.toolbarButton)) MirrorSelection();
            if (GUILayout.Button("Enquadrar (F)", EditorStyles.toolbarButton)) frameQueued = true;
            GUILayout.FlexibleSpace();
            GUILayout.Label(nodes.Count + " nós · " + edges.Count + " arestas", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(undoStack.Count == 0))
                if (GUILayout.Button("Desfazer", EditorStyles.toolbarButton)) Undo();
            using (new EditorGUI.DisabledScope(target == null))
            {
                if (GUILayout.Button("Salvar", EditorStyles.toolbarButton)) Save();
                if (GUILayout.Button("Recarregar", EditorStyles.toolbarButton)) Reload();
            }
        }

        private void DrawBuildToolbar()
        {
            if (GUILayout.Button("Enquadrar (F)", EditorStyles.toolbarButton)) frameQueued = true;
            GUILayout.Space(8f);
            GUILayout.Label(buildTarget != null ? "Build: " + buildTarget.name : "Escolha um personagem no painel →", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (buildTarget != null && workingBuild != null && buildAllocator != null)
                for (int t = 0; t < 3; t++)
                    GUILayout.Label($"R{t + 1} {buildAllocator.TierSpent(workingBuild, t)}/{buildAllocator.TierBudget(t)}", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(buildTarget == null))
                if (GUILayout.Button("Salvar build", EditorStyles.toolbarButton)) SaveBuild();
        }

        // ---------------------------------------------------------------- Canvas draw

        private void DrawCanvas(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, new Color(0.10f, 0.11f, 0.14f, 1f));
            GUI.BeginClip(rect);
            Vector2 size = rect.size;
            Handles.BeginGUI();

            if (showGuides) DrawGuides(size);
            DrawEdges(size);
            if (drag == DragMode.Connect && edgeFrom != null && TryGet(edgeFrom, out TreeNode fn))
            {
                Handles.color = new Color(1f, 0.9f, 0.4f, 0.9f);
                Handles.DrawAAPolyLine(2.5f, W2L(fn.normalizedPos, size), Event.current.mousePosition - rect.position);
            }
            DrawNodes(size);

            Handles.EndGUI();
            if (drag == DragMode.Marquee) DrawMarquee(rect);
            DrawHoverLabel(size);
            GUI.EndClip();
        }

        private const int GuideRingCount = 18;
        private const float GuideRingStep = 0.16f;   // espaçamento uniforme entre anéis
        private readonly Vector2[] guideVerts = new Vector2[Sectors];

        private void DrawGuides(Vector2 size)
        {
            Vector2 c = Center(size);
            float scale = PPU * zoom;
            float outer = GuideRingCount * GuideRingStep;   // raio total das guias
            float rMax = outer * scale;

            // Eixos centrais de cada setor (onde os nós de estrada se alinham) — mais visíveis.
            Handles.color = new Color(1f, 1f, 1f, 0.11f);
            for (int s = 0; s < Sectors; s++)
                DrawSpoke(c, 90f + SectorDeg * s, rMax);

            // Fronteiras entre setores — mais fracas.
            Handles.color = new Color(1f, 1f, 1f, 0.05f);
            for (int s = 0; s < Sectors; s++)
                DrawSpoke(c, 90f + 36f + SectorDeg * s, rMax);

            // Anéis concêntricos uniformemente espaçados.
            Handles.color = new Color(1f, 1f, 1f, 0.06f);
            for (int i = 1; i <= GuideRingCount; i++)
                Handles.DrawWireDisc(c, Vector3.forward, i * GuideRingStep * scale);

            // Pentágono nos 5 vértices + pentagrama (diagonais) atravessando toda a guia.
            for (int s = 0; s < Sectors; s++)
                guideVerts[s] = c + Dir(90f + SectorDeg * s) * rMax;
            Handles.color = new Color(1f, 1f, 1f, 0.10f);
            for (int s = 0; s < Sectors; s++)
                Handles.DrawLine(guideVerts[s], guideVerts[(s + 1) % Sectors]);
            Handles.color = new Color(1f, 1f, 1f, 0.07f);
            for (int s = 0; s < Sectors; s++)
                Handles.DrawLine(guideVerts[s], guideVerts[(s + 2) % Sectors]);
        }

        private static Vector2 Dir(float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), -Mathf.Sin(a));
        }

        private static void DrawSpoke(Vector2 c, float deg, float len) => Handles.DrawLine(c, c + Dir(deg) * len);

        private void DrawEdges(Vector2 size)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (!TryGet(edges[i].fromNodeId, out TreeNode a) || !TryGet(edges[i].toNodeId, out TreeNode b)) continue;
                Color col = Rank(Mathf.Max(a.tier, b.tier)); col.a = 0.55f;
                Handles.color = col;
                Handles.DrawAAPolyLine(2f, W2L(a.normalizedPos, size), W2L(b.normalizedPos, size));
            }
        }

        private void DrawNodes(Vector2 size)
        {
            if (mode == Mode.Build) { DrawBuildNodes(size); return; }
            for (int i = 0; i < nodes.Count; i++)
            {
                TreeNode n = nodes[i];
                Vector2 p = W2L(n.normalizedPos, size);
                bool center = n.nodeId == CenterId;
                bool sel = selected.Contains(n.nodeId);
                float wr = center ? 0.05f : (n.isNotable ? 0.04f : 0.03f);
                float r = Mathf.Clamp(wr * PPU * zoom, 3f, 60f);

                if (n.nodeId == hoverId) { Handles.color = new Color(1f, 1f, 1f, 0.9f); Handles.DrawWireDisc(p, Vector3.forward, r + 3f); }
                if (sel) { Handles.color = new Color(1f, 0.85f, 0.3f, 1f); Handles.DrawWireDisc(p, Vector3.forward, r + 2f); }

                Handles.color = center ? new Color(1f, 0.85f, 0.45f) : Rank(n.tier);
                Handles.DrawSolidDisc(p, Vector3.forward, r);
                if (n.isNotable) { Handles.color = new Color(1f, 1f, 1f, 0.8f); Handles.DrawWireDisc(p, Vector3.forward, r); }
            }
        }

        private void DrawMarquee(Rect rect)
        {
            Vector2 a = marqueeStart - rect.position;
            Vector2 b = (Event.current.mousePosition) - rect.position;
            Rect m = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            EditorGUI.DrawRect(m, new Color(0.4f, 0.7f, 1f, 0.12f));
        }

        private void DrawHoverLabel(Vector2 size)
        {
            if (string.IsNullOrEmpty(hoverId) || !TryGet(hoverId, out TreeNode n) || n.nodeId == CenterId) return;
            Vector2 p = W2L(n.normalizedPos, size) + new Vector2(10f, -6f);
            string txt = n.stat + "  ·  R" + (n.tier + 1) + (n.isNotable ? "  ·  notable" : string.Empty);
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
            Vector2 sz = style.CalcSize(new GUIContent(txt));
            EditorGUI.DrawRect(new Rect(p.x - 3f, p.y - 1f, sz.x + 6f, sz.y + 2f), new Color(0f, 0f, 0f, 0.75f));
            GUI.Label(new Rect(p.x, p.y, sz.x + 4f, sz.y + 2f), txt, style);
        }

        // ---------------------------------------------------------------- Events

        private void HandleEvents(Rect rect)
        {
            if (mode == Mode.Build) { HandleBuildEvents(rect); return; }

            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            bool inside = rect.Contains(mouse);

            switch (e.type)
            {
                case EventType.ScrollWheel:
                    if (!inside) break;
                    ZoomAt(mouse - rect.position, rect.size, -e.delta.y);
                    e.Use();
                    break;

                case EventType.MouseMove:
                    if (inside) { string h = HitNode(mouse, rect); if (h != hoverId) { hoverId = h; Repaint(); } }
                    break;

                case EventType.MouseDown:
                    if (!inside) break;
                    OnMouseDown(e, rect);
                    break;

                case EventType.MouseDrag:
                    OnMouseDrag(e, rect);
                    break;

                case EventType.MouseUp:
                    OnMouseUp(e, rect);
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl != 0) break;
                    if (e.keyCode == KeyCode.F) { frameQueued = true; e.Use(); }
                    else if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace) { DeleteSelected(); e.Use(); }
                    else if (e.control && e.keyCode == KeyCode.Z) { Undo(); e.Use(); }
                    break;
            }
        }

        private void OnMouseDown(Event e, Rect rect)
        {
            Vector2 world = L2W(e.mousePosition - rect.position, rect.size);
            string hit = HitNode(e.mousePosition, rect);

            if (e.button == 2 || (e.button == 0 && e.alt)) { drag = DragMode.Pan; e.Use(); return; }

            if (e.button == 1)
            {
                if (hit != null) NodeContextMenu(hit); else EmptyContextMenu(world);
                e.Use();
                return;
            }

            if (e.button != 0) return;

            if (tool == Tool.Add)
            {
                AddNode(world);
                e.Use();
                return;
            }

            if (tool == Tool.Connect)
            {
                if (hit != null) { drag = DragMode.Connect; edgeFrom = hit; }
                e.Use();
                return;
            }

            // Mover / selecionar
            if (hit != null)
            {
                if (e.shift) { if (!selected.Add(hit)) selected.Remove(hit); }
                else if (!selected.Contains(hit)) { selected.Clear(); selected.Add(hit); }
                drag = DragMode.Move; dragLastWorld = world; dragMoved = false;
            }
            else
            {
                if (!e.shift) selected.Clear();
                drag = DragMode.Marquee; marqueeStart = e.mousePosition; marqueeAdditive = e.shift;
            }
            e.Use();
            Repaint();
        }

        private void OnMouseDrag(Event e, Rect rect)
        {
            switch (drag)
            {
                case DragMode.Pan:
                    pan += e.delta; e.Use(); Repaint(); break;
                case DragMode.Move:
                    Vector2 world = L2W(e.mousePosition - rect.position, rect.size);
                    Vector2 d = world - dragLastWorld; dragLastWorld = world;
                    if (!dragMoved) { PushUndo(); dragMoved = true; }
                    MoveSelected(d);
                    e.Use(); Repaint(); break;
                case DragMode.Connect:
                case DragMode.Marquee:
                    e.Use(); Repaint(); break;
            }
        }

        private void OnMouseUp(Event e, Rect rect)
        {
            if (drag == DragMode.Connect && edgeFrom != null)
            {
                string hit = HitNode(e.mousePosition, rect);
                if (hit != null && hit != edgeFrom) AddEdge(edgeFrom, hit);
                edgeFrom = null;
            }
            else if (drag == DragMode.Marquee)
            {
                ApplyMarquee(rect);
            }
            if (drag != DragMode.None) { drag = DragMode.None; e.Use(); Repaint(); }
        }

        // ---------------------------------------------------------------- Transform / hit

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
            zoom = Mathf.Clamp(zoom * (1f + delta * 0.05f), 0.15f, 6f);
            float s = PPU * zoom;
            pan.x = local.x - size.x * 0.5f - worldBefore.x * s;
            pan.y = local.y - size.y * 0.5f + worldBefore.y * s;
            Repaint();
        }

        private void FrameAll(Rect rect)
        {
            if (nodes.Count == 0) { pan = Vector2.zero; zoom = 1f; return; }
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (TreeNode n in nodes)
            {
                min = Vector2.Min(min, n.normalizedPos); max = Vector2.Max(max, n.normalizedPos);
            }
            Vector2 c = (min + max) * 0.5f;
            Vector2 half = (max - min) * 0.5f + Vector2.one * 0.12f;
            Vector2 size = rect.size;
            float sx = size.x * 0.5f * 0.9f / Mathf.Max(half.x, 0.05f);
            float sy = size.y * 0.5f * 0.9f / Mathf.Max(half.y, 0.05f);
            zoom = Mathf.Clamp(Mathf.Min(sx, sy) / PPU, 0.15f, 6f);
            float s = PPU * zoom;
            pan = new Vector2(-c.x * s, c.y * s);
            Repaint();
        }

        private string HitNode(Vector2 mouse, Rect rect)
        {
            Vector2 size = rect.size;
            string best = null; float bestD = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++)
            {
                TreeNode n = nodes[i];
                float wr = n.nodeId == CenterId ? 0.05f : (n.isNotable ? 0.04f : 0.03f);
                float r = Mathf.Max(Mathf.Clamp(wr * PPU * zoom, 3f, 60f), 8f);
                float d = Vector2.Distance(mouse - rect.position, W2L(n.normalizedPos, size));
                if (d <= r && d < bestD) { bestD = d; best = n.nodeId; }
            }
            return best;
        }

        private void ApplyMarquee(Rect rect)
        {
            Vector2 a = marqueeStart - rect.position, b = Event.current.mousePosition - rect.position;
            Rect m = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            if (!marqueeAdditive) selected.Clear();
            Vector2 size = rect.size;
            foreach (TreeNode n in nodes)
                if (m.Contains(W2L(n.normalizedPos, size))) selected.Add(n.nodeId);
        }

        // ---------------------------------------------------------------- Node/edge ops (com espelho)

        private bool TryGet(string id, out TreeNode node)
        {
            for (int i = 0; i < nodes.Count; i++) if (nodes[i].nodeId == id) { node = nodes[i]; return true; }
            node = default; return false;
        }
        private int IndexOf(string id)
        {
            for (int i = 0; i < nodes.Count; i++) if (nodes[i].nodeId == id) return i;
            return -1;
        }

        private void AddNode(Vector2 world)
        {
            PushUndo();
            if (world.sqrMagnitude < 1e-4f) world = new Vector2(0f, 0.15f);   // evita cair no centro
            if (snap) world = Snap(world);

            if (autoMirror)
            {
                int sector = SectorOf(world);
                Vector2 canon = Canonical(world, sector);
                int g = nextGroup++;
                selected.Clear();
                for (int s = 0; s < Sectors; s++)
                {
                    string id = GroupId(g, s);
                    nodes.Add(new TreeNode
                    {
                        nodeId = id, tier = newTier, normalizedPos = Rotate(canon, SectorDeg * s),
                        stat = newStat, maxPoints = 1, grantPoints = nodePointsValue, isNotable = false
                    });
                    if (s == sector) selected.Add(id);
                }
            }
            else
            {
                string id = FreePrefix + freeCounter++;
                nodes.Add(new TreeNode { nodeId = id, tier = newTier, normalizedPos = world, stat = newStat, maxPoints = 1, grantPoints = nodePointsValue, isNotable = false });
                selected.Clear(); selected.Add(id);
            }
            Repaint();
        }

        private void MoveSelected(Vector2 delta)
        {
            if (!autoMirror)
            {
                foreach (string id in selected)
                {
                    if (id == CenterId) continue;
                    int idx = IndexOf(id); if (idx < 0) continue;
                    Vector2 np = nodes[idx].normalizedPos + delta;
                    SetPos(idx, snap ? Snap(np) : np);
                }
                return;
            }

            // Um representante por grupo: arrasta a "flor" inteira mantendo a simetria (delta rotacionado por cópia).
            var reps = new Dictionary<int, KeyValuePair<int, int>>();   // grupo -> (idx, setor)
            var loose = new List<int>();
            foreach (string id in selected)
            {
                if (id == CenterId) continue;
                int idx = IndexOf(id); if (idx < 0) continue;
                if (TryParse(id, out int g, out int s)) { if (!reps.ContainsKey(g)) reps[g] = new KeyValuePair<int, int>(idx, s); }
                else loose.Add(idx);
            }
            foreach (var kv in reps)
            {
                Vector2 np = nodes[kv.Value.Key].normalizedPos + delta;
                if (snap) np = Snap(np);
                Vector2 canon = Canonical(np, kv.Value.Value);
                for (int k = 0; k < Sectors; k++)
                {
                    int mi = IndexOf(GroupId(kv.Key, k));
                    if (mi >= 0) SetPos(mi, Rotate(canon, SectorDeg * k));
                }
            }
            foreach (int idx in loose)
            {
                Vector2 np = nodes[idx].normalizedPos + delta;
                SetPos(idx, snap ? Snap(np) : np);
            }
        }

        private void AddEdge(string a, string b)
        {
            PushUndo();
            if (autoMirror && a != CenterId && b != CenterId && TryParse(a, out int ga, out int sa) && TryParse(b, out int gb, out int sb))
            {
                for (int r = 0; r < Sectors; r++)
                    TryAddEdge(GroupId(ga, (sa + r) % Sectors), GroupId(gb, (sb + r) % Sectors));
            }
            else if (autoMirror && a == CenterId && TryParse(b, out int gb2, out int sb2))
            {
                for (int r = 0; r < Sectors; r++) TryAddEdge(CenterId, GroupId(gb2, (sb2 + r) % Sectors));
            }
            else if (autoMirror && b == CenterId && TryParse(a, out int ga2, out int sa2))
            {
                for (int r = 0; r < Sectors; r++) TryAddEdge(CenterId, GroupId(ga2, (sa2 + r) % Sectors));
            }
            else TryAddEdge(a, b);
            Repaint();
        }

        private void TryAddEdge(string a, string b)
        {
            if (a == b) return;
            for (int i = 0; i < edges.Count; i++)
            {
                TreeEdge e = edges[i];
                if ((e.fromNodeId == a && e.toNodeId == b) || (e.fromNodeId == b && e.toNodeId == a)) return;
            }
            edges.Add(new TreeEdge { fromNodeId = a, toNodeId = b });
        }

        private void DeleteSelected()
        {
            if (selected.Count == 0) return;
            PushUndo();
            var kill = new HashSet<string>();
            foreach (string id in selected)
            {
                if (id == CenterId) continue;
                if (autoMirror && TryParse(id, out int g, out _))
                    for (int k = 0; k < Sectors; k++) kill.Add(GroupId(g, k));
                else kill.Add(id);
            }
            nodes.RemoveAll(n => kill.Contains(n.nodeId));
            edges.RemoveAll(e => kill.Contains(e.fromNodeId) || kill.Contains(e.toNodeId));
            selected.Clear();
            Repaint();
        }

        // Converte cada nó solto selecionado em um grupo simétrico (5 cópias); arestas entre selecionados são replicadas.
        private void MirrorSelection()
        {
            if (selected.Count == 0) return;
            PushUndo();
            var newSel = new HashSet<string>();
            var idMap = new Dictionary<string, int>();   // idAntigo -> grupo novo (setor de origem preservado)
            var sectorOfOld = new Dictionary<string, int>();

            foreach (string id in new List<string>(selected))
            {
                if (id == CenterId) continue;
                int idx = IndexOf(id); if (idx < 0) continue;
                TreeNode n = nodes[idx];
                int s = SectorOf(n.normalizedPos);
                int g = nextGroup++;
                Vector2 canon = Canonical(n.normalizedPos, s);
                for (int k = 0; k < Sectors; k++)
                {
                    string nid = GroupId(g, k);
                    var copy = n; copy.nodeId = nid; copy.normalizedPos = Rotate(canon, SectorDeg * k);
                    if (k == s) { nodes[idx] = copy; }   // reaproveita o nó original (mantém arestas via rename)
                    else nodes.Add(copy);
                }
                RenameInEdges(id, GroupId(g, s));
                idMap[id] = g; sectorOfOld[id] = s;
                newSel.Add(GroupId(g, s));
            }

            // Replica arestas cujas duas pontas foram agrupadas nesta seleção.
            var snapshot = new List<TreeEdge>(edges);
            foreach (TreeEdge e in snapshot)
            {
                string aOld = FindOld(idMap, sectorOfOld, e.fromNodeId);
                string bOld = FindOld(idMap, sectorOfOld, e.toNodeId);
                if (aOld == null || bOld == null) continue;
                int ga = idMap[aOld], sa = sectorOfOld[aOld], gb = idMap[bOld], sb = sectorOfOld[bOld];
                for (int r = 1; r < Sectors; r++)
                    TryAddEdge(GroupId(ga, (sa + r) % Sectors), GroupId(gb, (sb + r) % Sectors));
            }

            selected.Clear();
            foreach (string s in newSel) selected.Add(s);
            Repaint();
        }

        private static string FindOld(Dictionary<string, int> idMap, Dictionary<string, int> sectorOfOld, string newId)
        {
            foreach (var kv in idMap)
                if (GroupId(kv.Value, sectorOfOld[kv.Key]) == newId) return kv.Key;
            return null;
        }

        private void RenameInEdges(string oldId, string newId)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                TreeEdge e = edges[i];
                if (e.fromNodeId == oldId) e.fromNodeId = newId;
                if (e.toNodeId == oldId) e.toNodeId = newId;
                edges[i] = e;
            }
        }

        private void SetPos(int idx, Vector2 p) { var n = nodes[idx]; n.normalizedPos = p; nodes[idx] = n; }

        private Vector2 Snap(Vector2 w)
        {
            float r = w.magnitude;
            if (r < 1e-4f) return w;
            float a = Mathf.Atan2(w.y, w.x) * Mathf.Rad2Deg;
            r = Mathf.Round(r / 0.02f) * 0.02f;
            float step = SectorDeg / Mathf.Max(1, snapAngleDiv);
            a = Mathf.Round(a / step) * step;
            float rad = a * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r);
        }

        // Aplica uma mutação de campo à seleção; no auto-espelho, copia os campos (não a posição) para o grupo todo.
        private void ApplyToSelection(System.Func<TreeNode, TreeNode> mutate)
        {
            if (selected.Count == 0) return;
            PushUndo();
            var doneGroups = new HashSet<int>();
            foreach (string id in new List<string>(selected))
            {
                int idx = IndexOf(id); if (idx < 0) continue;
                TreeNode m = mutate(nodes[idx]); m.nodeId = nodes[idx].nodeId; m.normalizedPos = nodes[idx].normalizedPos;
                nodes[idx] = m;
                if (autoMirror && TryParse(id, out int g, out _) && doneGroups.Add(g))
                    for (int k = 0; k < Sectors; k++)
                    {
                        int mi = IndexOf(GroupId(g, k));
                        if (mi >= 0 && mi != idx) { var c = nodes[mi]; c.stat = m.stat; c.tier = m.tier; c.isNotable = m.isNotable; c.grantPoints = m.grantPoints; c.maxPoints = m.maxPoints; nodes[mi] = c; }
                    }
            }
        }

        // ---------------------------------------------------------------- Context menus

        private void NodeContextMenu(string id)
        {
            if (!selected.Contains(id)) { selected.Clear(); selected.Add(id); }
            var m = new GenericMenu();
            m.AddItem(new GUIContent("Alternar notable"), false, () => ApplyToSelection(n => { bool nb = !n.isNotable; n.isNotable = nb; n.grantPoints = nb ? notablePointsValue : nodePointsValue; return n; }));
            for (int t = 0; t < 3; t++) { int tt = t; m.AddItem(new GUIContent("Rank/" + (t + 1)), false, () => ApplyToSelection(n => { n.tier = tt; return n; })); }
            m.AddItem(new GUIContent("Excluir"), false, DeleteSelected);
            m.ShowAsContext();
        }

        private void EmptyContextMenu(Vector2 world)
        {
            var m = new GenericMenu();
            m.AddItem(new GUIContent("Adicionar nó aqui"), false, () => AddNode(world));
            m.AddItem(new GUIContent("Enquadrar tudo"), false, () => frameQueued = true);
            m.ShowAsContext();
        }

        // ---------------------------------------------------------------- Inspector

        private void DrawInspector(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.helpBox);
            float lw = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 84f;
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll, GUIStyle.none, GUI.skin.verticalScrollbar);

            EditorGUI.BeginChangeCheck();
            target = (StatTreeData)EditorGUILayout.ObjectField("Asset", target, typeof(StatTreeData), false);
            if (EditorGUI.EndChangeCheck()) { Reload(); if (mode == Mode.Build) RebuildBuildAllocator(); }
            EditorGUILayout.Space(4f);

            if (mode == Mode.Build)
            {
                DrawBuildInspectorBody();
                EditorGUILayout.EndScrollView();
                EditorGUIUtility.labelWidth = lw;
                GUILayout.EndArea();
                return;
            }

            EditorGUILayout.LabelField("Pontos por rank", EditorStyles.boldLabel);
            if (pointsPerTier == null || pointsPerTier.Length != 3) pointsPerTier = new[] { 8, 10, 12 };
            EditorGUIUtility.labelWidth = 24f;
            EditorGUILayout.BeginHorizontal();
            pointsPerTier[0] = EditorGUILayout.IntField("R1", pointsPerTier[0], GUILayout.Width(62f));
            pointsPerTier[1] = EditorGUILayout.IntField("R2", pointsPerTier[1], GUILayout.Width(62f));
            pointsPerTier[2] = EditorGUILayout.IntField("R3", pointsPerTier[2], GUILayout.Width(62f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 84f;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Pontos concedidos (global)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int nv = EditorGUILayout.IntField("Normal", nodePointsValue);
            int tv = EditorGUILayout.IntField("Notable", notablePointsValue);
            if (EditorGUI.EndChangeCheck())
            {
                PushUndo();
                nodePointsValue = Mathf.Max(0, nv);
                notablePointsValue = Mathf.Max(0, tv);
                ApplyGrantPointsAll();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Novo nó (ferramenta Adicionar)", EditorStyles.boldLabel);
            newStat = (StatType)EditorGUILayout.EnumPopup("Stat", newStat);
            newTier = EditorGUILayout.Popup("Rank", newTier, RankLabels);

            EditorGUILayout.Space(8f);
            DrawSelectionInspector();

            EditorGUILayout.Space(10f);
            seedFold = EditorGUILayout.Foldout(seedFold, "Semear base (topologia inicial)", true);
            if (seedFold) DrawSeed();

            EditorGUILayout.EndScrollView();
            EditorGUIUtility.labelWidth = lw;
            GUILayout.EndArea();
        }

        private void DrawSelectionInspector()
        {
            EditorGUILayout.LabelField("Seleção (" + selected.Count + ")", EditorStyles.boldLabel);
            if (selected.Count == 0) { EditorGUILayout.HelpBox("Clique em nós para editar. Shift soma; arraste para caixa de seleção.", MessageType.None); return; }

            bool multi = selected.Count > 1;
            TreeNode first = default; bool has = false;
            bool sameStat = true, sameTier = true, sameNot = true;
            foreach (string id in selected)
            {
                if (!TryGet(id, out TreeNode n)) continue;
                if (!has) { first = n; has = true; continue; }
                if (n.stat != first.stat) sameStat = false;
                if (n.tier != first.tier) sameTier = false;
                if (n.isNotable != first.isNotable) sameNot = false;
            }
            if (!has) return;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = multi && !sameStat;
            StatType stat = (StatType)EditorGUILayout.EnumPopup("Stat", first.stat);
            if (EditorGUI.EndChangeCheck()) ApplyToSelection(n => { n.stat = stat; return n; });

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = multi && !sameTier;
            int tier = EditorGUILayout.Popup("Rank", first.tier, new[] { "1", "2", "3" });
            if (EditorGUI.EndChangeCheck()) ApplyToSelection(n => { n.tier = tier; return n; });

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = multi && !sameNot;
            bool notable = EditorGUILayout.Toggle("Notable", first.isNotable);
            if (EditorGUI.EndChangeCheck()) ApplyToSelection(n => { n.isNotable = notable; n.grantPoints = notable ? notablePointsValue : nodePointsValue; return n; });
            EditorGUI.showMixedValue = false;

            if (!multi)
            {
                EditorGUILayout.LabelField("Concede", (first.isNotable ? notablePointsValue : nodePointsValue) + " pts", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("id", first.nodeId, EditorStyles.miniLabel);
            }
            if (GUILayout.Button("Excluir seleção")) DeleteSelected();
        }

        private void DrawSeed()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Raios interno/externo por rank");
            for (int t = 0; t < 3; t++)
            {
                EditorGUILayout.BeginHorizontal();
                seed.inner[t] = EditorGUILayout.FloatField("R" + (t + 1) + " in", seed.inner[t]);
                seed.outer[t] = EditorGUILayout.FloatField("out", seed.outer[t]);
                EditorGUILayout.EndHorizontal();
            }
            seed.wheels = EditorGUILayout.Toggle("Rodas (clusters)", seed.wheels);
            seed.wheelPerp = EditorGUILayout.Slider("Afastamento roda", seed.wheelPerp, 0.05f, 0.35f);
            seed.wheelRadius = EditorGUILayout.Slider("Raio roda", seed.wheelRadius, 0.03f, 0.15f);
            EditorGUILayout.LabelField("Pontos: use 'Pontos concedidos (global)' acima.", EditorStyles.miniLabel);
            EditorGUI.indentLevel--;

            if (GUILayout.Button("Semear (substitui a árvore)"))
            {
                if (EditorUtility.DisplayDialog("Semear base",
                    "Isto substitui TODOS os nós e arestas atuais por uma base simétrica. As mudanças só vão para o asset ao Salvar.",
                    "Semear", "Cancelar"))
                {
                    PushUndo();
                    seed.nodePoints = nodePointsValue;
                    seed.notablePoints = notablePointsValue;
                    StatTreeSeeder.Build(seed, out List<TreeNode> n, out List<TreeEdge> e, out int[] p);
                    nodes.Clear(); nodes.AddRange(n);
                    edges.Clear(); edges.AddRange(e);
                    pointsPerTier = p;
                    ApplyGrantPointsAll();
                    selected.Clear();
                    RecomputeCounters();
                    frameQueued = true;
                }
            }
        }

        // ---------------------------------------------------------------- Undo / persistência

        private void PushUndo()
        {
            undoStack.Add(new Snapshot { n = nodes.ToArray(), e = edges.ToArray(), p = (int[])pointsPerTier.Clone() });
            if (undoStack.Count > 40) undoStack.RemoveAt(0);
        }

        private void Undo()
        {
            if (undoStack.Count == 0) return;
            Snapshot s = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            nodes.Clear(); nodes.AddRange(s.n);
            edges.Clear(); edges.AddRange(s.e);
            pointsPerTier = s.p;
            selected.Clear();
            RecomputeCounters();
            InferPointValues();
            Repaint();
        }

        private void Reload()
        {
            nodes.Clear(); edges.Clear();
            if (target != null)
            {
                if (target.nodes != null) nodes.AddRange(target.nodes);
                if (target.edges != null) edges.AddRange(target.edges);
                pointsPerTier = target.pointsPerTier != null && target.pointsPerTier.Length == 3
                    ? (int[])target.pointsPerTier.Clone() : new[] { 8, 10, 12 };
            }
            selected.Clear();
            undoStack.Clear();
            RecomputeCounters();
            InferPointValues();
            frameQueued = true;
        }

        // Todos os notables concedem 'notablePointsValue'; todos os não-notables concedem 'nodePointsValue'.
        private void ApplyGrantPointsAll()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].nodeId == CenterId) continue;
                var n = nodes[i];
                n.grantPoints = n.isNotable ? notablePointsValue : nodePointsValue;
                nodes[i] = n;
            }
        }

        // Deduz os dois valores globais a partir dos nós já existentes no asset (fonte de verdade ao carregar).
        private void InferPointValues()
        {
            foreach (TreeNode n in nodes)
            {
                if (n.nodeId == CenterId || n.grantPoints <= 0) continue;
                if (n.isNotable) notablePointsValue = n.grantPoints;
                else nodePointsValue = n.grantPoints;
            }
        }

        private void Save()
        {
            if (target == null) return;
            UnityEditor.Undo.RecordObject(target, "Editar Stat Tree");
            target.nodes = nodes.ToArray();
            target.edges = edges.ToArray();
            target.pointsPerTier = (int[])pointsPerTier.Clone();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private void RecomputeCounters()
        {
            nextGroup = 0; freeCounter = 0;
            foreach (TreeNode n in nodes)
            {
                if (TryParse(n.nodeId, out int g, out _)) nextGroup = Mathf.Max(nextGroup, g + 1);
                else if (n.nodeId != null && n.nodeId.StartsWith(FreePrefix) && int.TryParse(n.nodeId.Substring(FreePrefix.Length), out int f))
                    freeCounter = Mathf.Max(freeCounter, f + 1);
            }
        }

        private static Color Rank(int tier) => tier >= 0 && tier < RankColors.Length ? RankColors[tier] : Color.gray;

        // ---------------------------------------------------------------- Modo Build padrão

        private void EnterBuildMode()
        {
            if (buildTarget != null && workingBuild == null) workingBuild = CloneBuild(buildTarget.defaultBuild);
            RebuildBuildAllocator();
        }

        // Aloca contra a topologia ATUAL da janela (WYSIWYG — inclui edições ainda não salvas).
        private void RebuildBuildAllocator() => buildAllocator = new BuildAllocator(nodes, edges, pointsPerTier);

        private void SetBuildTarget(CharacterData data)
        {
            buildTarget = data;
            workingBuild = data != null ? CloneBuild(data.defaultBuild) : null;
            RebuildBuildAllocator();
            Repaint();
        }

        private static CharacterBuild CloneBuild(CharacterBuild src)
        {
            var b = new CharacterBuild();
            if (src != null)
            {
                b.label = src.label; b.artifactId = src.artifactId;
                if (src.allocations != null) b.allocations = new List<TreeAllocation>(src.allocations);
            }
            return b;
        }

        private void SaveBuild()
        {
            if (buildTarget == null || workingBuild == null) return;
            // A ferramenta autora só a ÁRVORE; o artefato da build padrão é editado no CharacterData (SO).
            UnityEditor.Undo.RecordObject(buildTarget, "Editar Build Padrão");
            buildTarget.defaultBuild.allocations = new List<TreeAllocation>(workingBuild.allocations);
            EditorUtility.SetDirty(buildTarget);
            AssetDatabase.SaveAssets();
        }

        private void HandleBuildEvents(Rect rect)
        {
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            bool inside = rect.Contains(mouse);

            switch (e.type)
            {
                case EventType.ScrollWheel:
                    if (inside) { ZoomAt(mouse - rect.position, rect.size, -e.delta.y); e.Use(); }
                    break;
                case EventType.MouseMove:
                    if (inside) { string h = HitNode(mouse, rect); if (h != hoverId) { hoverId = h; Repaint(); } }
                    break;
                case EventType.MouseDown:
                    if (!inside) break;
                    if (e.button == 2 || (e.button == 0 && e.alt)) { drag = DragMode.Pan; e.Use(); break; }
                    if (e.button == 0)
                    {
                        string hit = HitNode(mouse, rect);
                        if (hit != null && buildAllocator != null && workingBuild != null && buildAllocator.Toggle(workingBuild, hit)) Repaint();
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (drag == DragMode.Pan) { pan += e.delta; e.Use(); Repaint(); }
                    break;
                case EventType.MouseUp:
                    if (drag != DragMode.None) { drag = DragMode.None; e.Use(); }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == 0 && e.keyCode == KeyCode.F) { frameQueued = true; e.Use(); }
                    break;
            }
        }

        private void DrawBuildNodes(Vector2 size)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                TreeNode n = nodes[i];
                Vector2 p = W2L(n.normalizedPos, size);
                bool center = n.nodeId == CenterId;
                float wr = center ? 0.05f : (n.isNotable ? 0.04f : 0.03f);
                float r = Mathf.Clamp(wr * PPU * zoom, 3f, 60f);

                bool alloc = !center && buildAllocator != null && buildAllocator.IsAllocated(workingBuild, n.nodeId);
                bool canOn = !center && buildAllocator != null && buildAllocator.CanAllocate(workingBuild, n.nodeId);

                if (n.nodeId == hoverId) { Handles.color = new Color(1f, 1f, 1f, 0.9f); Handles.DrawWireDisc(p, Vector3.forward, r + 3f); }
                if (alloc) { Handles.color = new Color(1f, 0.9f, 0.35f, 1f); Handles.DrawWireDisc(p, Vector3.forward, r + 2f); }

                Color col = center ? new Color(1f, 0.85f, 0.45f) : Rank(n.tier);
                if (!center && !alloc) col *= canOn ? 0.75f : 0.32f;   // alocável escurece pouco; bloqueado, muito
                Handles.color = col;
                Handles.DrawSolidDisc(p, Vector3.forward, r);
                if (n.isNotable) { Handles.color = new Color(1f, 1f, 1f, alloc ? 0.9f : 0.4f); Handles.DrawWireDisc(p, Vector3.forward, r); }
            }
        }

        private void DrawBuildInspectorBody()
        {
            EditorGUILayout.LabelField("Build padrão do personagem", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var picked = (CharacterData)EditorGUILayout.ObjectField("Personagem", buildTarget, typeof(CharacterData), false);
            if (EditorGUI.EndChangeCheck()) SetBuildTarget(picked);

            if (buildTarget == null)
            {
                EditorGUILayout.HelpBox("Escolha um personagem e clique nos nós para ligar/desligar — mesmas regras do jogo: precisa partir do centro e respeitar o orçamento por rank.", MessageType.Info);
                return;
            }
            if (workingBuild == null || buildAllocator == null) return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Orçamento por rank", EditorStyles.boldLabel);
            for (int t = 0; t < 3; t++)
                EditorGUILayout.LabelField("Rank " + (t + 1), buildAllocator.TierSpent(workingBuild, t) + " / " + buildAllocator.TierBudget(t));

            int allocCount = 0;
            if (workingBuild.allocations != null)
                for (int i = 0; i < workingBuild.allocations.Count; i++)
                    if (workingBuild.allocations[i].points > 0) allocCount++;
            EditorGUILayout.LabelField("Nós alocados", allocCount.ToString());

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Limpar alocação")) { buildAllocator.Clear(workingBuild); Repaint(); }
            if (GUILayout.Button("Salvar no personagem")) SaveBuild();

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("Clique = liga/desliga nó · Alt/botão-do-meio arrasta · scroll = zoom.\nAnel dourado = alocado · escuro = bloqueado (sem ligação ao centro ou sem orçamento).", MessageType.None);
        }
    }
}
