using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SimpleColony
{
    public sealed class ColonyView : MonoBehaviour
    {
        [SerializeField] Mechanics sessionMechanics = Mechanics.All;
        GameObject construction;
        readonly System.Collections.Generic.List<Button> buildButtons = new System.Collections.Generic.List<Button>();
        readonly System.Collections.Generic.List<Text> stockAmounts = new System.Collections.Generic.List<Text>();
        Button researchMenu, buildNodeButton;
        int directingPawn = -1;
        Action inspectSelection;
        Func<string> inspectSummary;
        string inspectedTitle;
        Color inspectedColor;
        Button moveOrder, defenderMode;
        Sprite buttonSkin;
        World world;
        [SerializeField] GameObject generatedInterface, generatedInput;
        [SerializeField] int sessionSeed = 4271;
        MapGraphic map;
        RectTransform safeArea;
        RectTransform mapViewport;
        RectTransform topPanel, selectionPanel, colonyControlsRect;
        CanvasScaler canvasScaler;
        bool portraitLayout, layoutReady;
        Text caption, description, details, diagnosticText, feedback, speedText;
        DescriptionPopup descriptionPopup;
        readonly RuntimeStatistics statistics = new RuntimeStatistics();
        readonly System.Collections.Generic.List<Button> researchButtons = new System.Collections.Generic.List<Button>();
        readonly System.Collections.Generic.List<Text> researchPrices = new System.Collections.Generic.List<Text>();
        UiIcon playIcon;
        Image portrait;
        GameObject help, diagnostics, pawnActions, nodeActions, coreActions, research, workforce;
        Button convert, accessButton;
        Font font;
        Sprite disc;
        Texture2D discTexture;
        readonly SimulationClock clock = new SimulationClock();
        float refresh;
        bool paused;
        // Zero selects unrestricted simulation within the per-frame CPU budget.
        static readonly int[] Speeds = { 1, 2, 5, 10, 25, 50, 100, 0 };
        int speedIndex;
        Profession chosenRole;
        public int SelectedNode { get; private set; }
        public int SelectedPawn { get; private set; } = -1;
        public bool MapInteractive => map != null && map.isActiveAndEnabled;
        static readonly string[] Names = { "Собиратель", "Защитник", "Разведчик", "Исследователь", "Строитель", "Снабженец", "Медик", "Стабилизатор", "Безработный" };
        static readonly string[] Descriptions = { "Доставляет питание", "Подавляет угрозы", "Открывает территории", "Получает знания", "Доставляет материю", "Доставляет заряд и эссенцию", "Доставляет лекарства", "Несёт эссенцию и укрепляет территории", "Ожидает назначения профессии" };
        static readonly string[] ResourceNames = { "Питание", "Знания", "Материя", "Заряд", "Лекарства", "Эссенция" };
        static readonly string[] States = { "Ожидает работу", "В пути", "Работает", "Восстанавливается", "Погиб", "На посту" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        { if (FindAnyObjectByType<ColonyView>() == null) new GameObject("Colony").AddComponent<ColonyView>(); }
        void Awake()
        { InitializeSession(); }
        void OnEnable()
        {
            if (Application.isPlaying && world == null) InitializeSession();
        }
        void InitializeSession()
        {
            // Runtime callbacks and the non-serialized model need rebuilding after a script reload.
            var previousInterface = generatedInterface != null ? generatedInterface : transform.Find("Interface")?.gameObject;
            var previousInput = generatedInput != null ? generatedInput : transform.Find("Input")?.gameObject;
            if (previousInterface != null) { previousInterface.SetActive(false); Destroy(previousInterface); }
            if (previousInput != null) { previousInput.SetActive(false); Destroy(previousInput); }
            buildButtons.Clear(); stockAmounts.Clear();
            researchButtons.Clear();
            researchPrices.Clear();
            if (disc != null) Destroy(disc);
            if (discTexture != null) Destroy(discTexture);
            layoutReady = false;
            Application.targetFrameRate = 60;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateDisc(); buttonSkin = Sprite.Create(discTexture, disc.rect, new Vector2(.5f,.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(24,24,24,24)); BuildInterface(); Restart(SessionSeed.Create());
        }
        void CreateDisc()
        {
            discTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                discTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(31.5f - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)))));
            discTexture.Apply(); disc = Sprite.Create(discTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
        }
        void BuildInterface()
        {
            var canvasObject = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            generatedInterface = canvasObject;
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler = scaler;
            scaler.referenceResolution = new Vector2(1000, 650); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                generatedInput = events;
                events.transform.SetParent(transform, false);
            }
            var background = Panel(canvasObject.transform, "Background", new Color(.035f, .055f, .075f)); Stretch(background);
            safeArea = Panel(background, "Safe area", Color.clear); Stretch(safeArea);
            var popupObject = new GameObject("Description popup", typeof(RectTransform), typeof(DescriptionPopup));
            popupObject.transform.SetParent(safeArea, false);
            descriptionPopup = popupObject.GetComponent<DescriptionPopup>(); descriptionPopup.Initialize(font);
            var viewport = new GameObject("Map viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(safeArea, false);
            mapViewport = (RectTransform)viewport.transform;
            Stretch(mapViewport); mapViewport.offsetMin = new Vector2(88, 140); mapViewport.offsetMax = new Vector2(0, -64);
            var mapObject = new GameObject("Map", typeof(RectTransform), typeof(CanvasRenderer), typeof(MapGraphic));
            mapObject.transform.SetParent(viewport.transform, false);
            map = mapObject.GetComponent<MapGraphic>(); map.Owner = this;
            Stretch((RectTransform)mapObject.transform);
            var top = Panel(safeArea, "Session", new Color(.065f, .095f, .13f)); Anchor(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -64), Vector2.zero);
            topPanel = top;
            var sessionBar = Panel(top, "Playback", Color.clear);
            sessionBar.anchorMin = sessionBar.anchorMax = sessionBar.pivot = new Vector2(.5f,1);
            sessionBar.sizeDelta = new Vector2(210,64); sessionBar.anchoredPosition = Vector2.zero;
            playIcon = Button(sessionBar, "▶", 12, -7, 54, 50, () => { paused = !paused; Refresh(); }).GetComponentInChildren<UiIcon>();
            speedText = Button(sessionBar, "×1", 74, -7, 64, 50, () => { speedIndex = (speedIndex + 1) % Speeds.Length; clock.Reset(); Refresh(); }, holdText: "Скорость симуляции. MAX — максимально возможная в пределах времени кадра.").GetComponentInChildren<Text>();
            Button(sessionBar, "↻", 146, -7, 54, 50, () => Restart(SessionSeed.Create()));
            string[] resourceIcons = { "food", "research", "matter", "charge", "medicine", "ring" };
            var stockRow = Panel(top, "Inventory", Color.clear);
            stockRow.anchorMin = new Vector2(0,1); stockRow.anchorMax = new Vector2(1,1); stockRow.pivot = new Vector2(0,1);
            stockRow.anchoredPosition = new Vector2(12,-64); stockRow.sizeDelta = new Vector2(-24,40);
            var layout = stockRow.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.spacing = 2;
            for (int i = 0; i < 6; i++)
            {
                Resource resource = (Resource)i;
                var chip = Button(stockRow, "Resource", 0, 0, 68, 36, () => InspectResource(resource), iconSymbol: resourceIcons[i]);
                chip.GetComponentInChildren<UiIcon>().color = Palette.Resources[i];
                Place(chip.GetComponentInChildren<UiIcon>().rectTransform, 4,-7,22,22);
                stockAmounts.Add(Label(chip.transform,"",29,-3,52,30,16));
            }
            Button(top, "?", -118, -7, 48, 50, () => TogglePanel(help), true);
            Button(top, "…", -60, -7, 48, 50, () => TogglePanel(diagnostics), true);
            var bottom = Panel(safeArea, "Selection", new Color(.065f, .095f, .13f)); Anchor(bottom, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 140));
            selectionPanel = bottom;
            portrait = Panel(bottom, "Portrait", Color.white).GetComponent<Image>(); portrait.sprite = disc; Place(portrait.rectTransform, 70, -14, 56, 56);
            caption = Label(bottom, "", 8, -82, 180, 42, 17); caption.alignment = TextAnchor.UpperCenter;
            description = Label(bottom, "", 210, -14, 600, 28, 19);
            details = Label(bottom, "", 210, -44, 700, 30, 16);
            pawnActions = new GameObject("Profession actions", typeof(RectTransform)); pawnActions.transform.SetParent(bottom, false); Stretch((RectTransform)pawnActions.transform);
            convert = Button(pawnActions.transform, "Работа →", 210, -76, 100, 32, CycleProfession);
            moveOrder = Button(pawnActions.transform,"Направить",320,-76,125,36,BeginMoveOrder);
            defenderMode = Button(pawnActions.transform,"Режим",460,-76,150,36,() =>
            {
                var pawn = world.Pawns[SelectedPawn];
                world.SetDefenderMode(SelectedPawn,(DefenderMode)(((int)pawn.DefenderMode+1)%3)); Refresh();
            });
            nodeActions = new GameObject("Territory actions", typeof(RectTransform)); nodeActions.transform.SetParent(bottom, false); Stretch((RectTransform)nodeActions.transform);
            accessButton = Button(nodeActions.transform, "Доступ", 210, -76, 180, 48, () => { world.ToggleAccess(SelectedNode); Refresh(); });
            Button(bottom, "?", -64,-14,44,40,ShowSelectedInfo,true);
            buildNodeButton = Button(nodeActions.transform, "Строить", 410,-76,136,42,() => TogglePanel(construction));
            feedback = Label(bottom, "", 410, -88, 500, 30, 16);
            coreActions = new GameObject("Colony controls", typeof(RectTransform)); coreActions.transform.SetParent(safeArea, false);
            var colonyRect = (RectTransform)coreActions.transform;
            colonyControlsRect = colonyRect;
            colonyRect.anchorMin = colonyRect.anchorMax = new Vector2(0, 1); colonyRect.pivot = new Vector2(0, 1);
            colonyRect.anchoredPosition = new Vector2(12, -80); colonyRect.sizeDelta = new Vector2(64, 208);
            Button(coreActions.transform, "Пешки", 0, 0, 64, 56, () => TogglePanel(workforce), iconSymbol: "pawn", holdText: "Создание пешек и управление работой.");
            researchMenu = Button(coreActions.transform, "Исследования", 0, -72, 64, 56, () => TogglePanel(research), iconSymbol: "research", holdText: "Исследования колонии. Покупаются за знания. Удерживайте значок улучшения, чтобы прочитать его эффект.");
            help = new GameObject("Help", typeof(RectTransform)); help.AddComponent<HelpPanel>().Initialize(safeArea, font, descriptionPopup); help.SetActive(false);
            diagnostics = Overlay("Diagnostics", ""); diagnosticText = diagnostics.transform.Find("Body").GetComponent<Text>();
            diagnosticText.fontSize = 14;
            diagnosticText.rectTransform.sizeDelta = new Vector2(370,210);
            Button(diagnostics.transform,"DEV +1000 каждого",20,-292,370,40,() => { world.GrantDebugResources(); Refresh(); });
            research = Overlay("Research", "");
            foreach (var definition in ResearchProgress.Definitions)
            {
                var technology = definition;
                int index = researchButtons.Count;
                float x = 20 + index % 3 * 128;
                float y = -64 - index / 3 * 140;
                var button = Button(research.transform, technology.Name, x, y, 104, 64,
                    () => { world.ResearchTechnology(technology.Type); Refresh(); }, iconSymbol: technology.Icon,
                    holdDescription: () => technology.Name + "\nУровень " + world.Research.Level(technology.Type) + "/" + technology.MaxLevel + "\n" + technology.Description + "\n" + (world.Research.IsCompleted(technology.Type) ? "Максимальный уровень." : world.Research.NextCost(technology.Type) + " знаний за следующий уровень."));
                Label(research.transform, technology.Name, x, y - 68, 120, 24, 16);
                var price = Label(research.transform, "", x, y - 96, 120, 24, 16); price.color = Palette.Resources[(int)Resource.Knowledge]; researchPrices.Add(price);
                researchButtons.Add(button);
            }
            workforce = Overlay("Workforce", "");
            workforce.AddComponent<WorkforcePanel>().Initialize(font, () => world, Names, profession => ShowInfo(Names[(int)profession]+"\n\n"+Descriptions[(int)profession]));
            construction = Overlay("Construction", "");
            for (int i = 0; i < BuildingDefinition.All.Count; i++)
            {
                var definition = BuildingDefinition.All[i];
                var build = Button(construction.transform, definition.Name,20,-60-i*85,306,48,() =>
                {
                    feedback.text = world.Construction.Build(SelectedNode,definition.Type) ? "Заказ ожидает строителя" : "Стройка недоступна";
                    construction.SetActive(false); Refresh();
                }, holdText: definition.Description);
                Button(construction.transform,"?",338,-60-i*85,42,42,() => ShowInfo(definition.Name+"\n\n"+definition.Description+"\nНужен строитель. Цена — "+definition.FoodCost+" еды и "+definition.MatterCost+" материи."));
                buildButtons.Add(build);
                Label(construction.transform,definition.FoodCost+" еды · "+definition.MatterCost+" материи",20,-108-i*85,360,26,15);
            }
        }
        GameObject Overlay(string name, string body)
        {
            var panel = Panel(safeArea, name, new Color(.08f, .115f, .15f));
            bool colonyPanel = name == "Research" || name == "Workforce";
            Vector2 anchor = colonyPanel ? new Vector2(0, 1) : new Vector2(1, 1);
            panel.anchorMin = panel.anchorMax = panel.pivot = anchor;
            panel.anchoredPosition = new Vector2(colonyPanel ? 88 : -12, -72); panel.sizeDelta = new Vector2(410, 390);
            var text = Label(panel, body, 20, -55, 370, 320, 17); text.name = "Body";
            Button(panel, "×", -56, -7, 48, 42, () => panel.gameObject.SetActive(false), true);
            panel.gameObject.SetActive(false); return panel.gameObject;
        }
        void TogglePanel(GameObject panel)
        {
            bool show = !panel.activeSelf;
            help.SetActive(false);
            diagnostics.SetActive(false);
            if (construction != null) construction.SetActive(false);
            research.SetActive(false);
            workforce.SetActive(false);
            panel.SetActive(show);
            Refresh();
        }
        public void SelectPawn(int id) { descriptionPopup.Close(); inspectSelection = null; SelectedPawn = id; chosenRole = world.Pawns[id].Role; Refresh(); }
        public void SelectNode(int id) { descriptionPopup.Close(); inspectSelection = null; SelectedPawn = -1; SelectedNode = id; Refresh(); }
        Profession NextProfession(Profession current, int direction)
        {
            do
            {
                current = (Profession)(((int)current + direction + Names.Length) % Names.Length);
            } while (!world.Supports(current));
            return current;
        }
        void CycleProfession()
        {
            var pawn = world.Pawns[SelectedPawn];
            chosenRole = NextProfession(pawn.PendingRole ?? pawn.Role,1);
            world.Convert(SelectedPawn,chosenRole);
            Refresh();
        }
        void Restart(int seed)
        {
            inspectSelection = null;
            directingPawn = -1;
            sessionSeed = seed;
            world = new World(seed, sessionMechanics);
            speedIndex = 0;
            map.World = world; clock.Reset(); paused = false;
            statistics.Reset();
            researchMenu.gameObject.SetActive((sessionMechanics & Mechanics.Research) != 0);
            map.Pan = Vector2.zero; map.Zoom = Screen.height > Screen.width ? 40 : 60; help.SetActive(false); diagnostics.SetActive(false);
            if (construction != null) construction.SetActive(false);
            workforce.SetActive(false);
            feedback.text = ""; SelectNode(World.Home);
        }
        void Update()
        {
            Rect area = Screen.safeArea;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            ApplyMapLayout();
            int executedTicks = clock.Advance(world, Time.unscaledDeltaTime, Speeds[speedIndex], paused);
            statistics.Record(Time.unscaledDeltaTime, executedTicks);
            refresh += Time.unscaledDeltaTime;
            if (refresh > .2f) { refresh = 0; Refresh(); }
        }
        void Refresh()
        {
            if (world == null) return;
            ApplyMapLayout();
            playIcon.Symbol = paused ? "▶" : "Ⅱ"; playIcon.SetVerticesDirty();
            speedText.text = Speeds[speedIndex] == 0 ? "MAX" : "×" + Speeds[speedIndex];
            for (int i = 0; i < researchButtons.Count; i++)
            {
                var technology = ResearchProgress.Definitions[i];
                bool completed = world.Research.IsCompleted(technology.Type);
                researchButtons[i].interactable = world.CanResearchTechnology(technology.Type);
                researchButtons[i].GetComponentInChildren<UiIcon>().color = completed ? Palette.Resources[2] : Color.white;
                researchPrices[i].text = world.Research.Level(technology.Type) + "/" + technology.MaxLevel + (completed ? " · MAX" : " · " + world.Research.NextCost(technology.Type) + " ◆");
            }
            for (int i = 0; i < stockAmounts.Count; i++) stockAmounts[i].text = world.Stores[(Resource)i].ToString("0");
            for (int i = 0; i < buildButtons.Count; i++) buildButtons[i].interactable = SelectedPawn < 0 && world.Construction.CanBuild(SelectedNode, BuildingDefinition.All[i].Type);
            bool pawnSelected = SelectedPawn >= 0;
            pawnActions.SetActive(pawnSelected); nodeActions.SetActive(!pawnSelected && SelectedNode != World.Home && world.Nodes[SelectedNode].Known);
            if (pawnSelected)
            {
                var pawn = world.Pawns[SelectedPawn];
                defenderMode.gameObject.SetActive(pawn.Role == Profession.Defender);
                defenderMode.GetComponentInChildren<Text>().text = pawn.DefenderMode == DefenderMode.Patrol ? "Патруль" : pawn.DefenderMode == DefenderMode.Guard ? "Пост" : "Поиск угроз";
                moveOrder.GetComponentInChildren<Text>().text = pawn.Role == Profession.Defender && pawn.DefenderMode == DefenderMode.Guard ? "Указать пост" : pawn.OrderedDestination >= 0 ? "Отмена приказа" : "Направить";
                portrait.color = Palette.Professions[(int)pawn.Role]; caption.text = Names[(int)pawn.Role]; description.text = Descriptions[(int)pawn.Role];
                details.text = "♥ " + pawn.Health.ToString("0") + "   Сытость " + pawn.Food.ToString("0") + "   Силы " + pawn.Energy.ToString("0") +
                    "\n" + States[(int)pawn.State] + (pawn.Target >= 0 ? " · цель " + pawn.Target : "") + (pawn.OrderedDestination >= 0 ? " · приказ " + pawn.OrderedDestination : "") + " · груз " + pawn.Cargo.ToString("0");
                feedback.text = pawn.PendingRole.HasValue ? "На ядре → " + Names[(int)pawn.PendingRole.Value] : pawn.Health <= 0 ? "Погиб" : "";
                convert.interactable = pawn.Health > 0 && !world.Lost;
            }
            else
            {
                var node = world.Nodes[SelectedNode]; portrait.color = node.Id == World.Home ? Color.white : Color.gray;
                caption.text = node.Id == World.Home ? "Ядро" : "Территория";
                description.text = node.Id == World.Home ? "Склад и отдых" : node.Threat > 0 ? "Очаг угрозы" : node.Known ? "Запасы территории" : "Не исследована";
                details.text = "";

                if (node.Known) foreach (var deposit in node.Deposits) details.text += ResourceNames[(int)deposit.Type] + " " + deposit.Amount.ToString("0") + "  ";
                if (node.Construction != null) details.text = node.Construction.Definition.Name + (node.Construction.Complete ? " · работает" : " · стройка " + (node.Construction.Progress / BuildingDefinition.WorkRequired * 100).ToString("0") + "%");
                if (node.Fortification > 0) details.text += " · защита " + node.Fortification.ToString("0") + "/30";
                accessButton.GetComponentInChildren<Text>().text = node.Blocked ? "Открыть доступ" : "Закрыть доступ";
                feedback.text = "";
            }
            if (inspectSelection != null)
            {
                pawnActions.SetActive(false); nodeActions.SetActive(false);
                caption.text = inspectedTitle; description.text = inspectedTitle; details.text = inspectSummary?.Invoke() ?? ""; feedback.text = ""; portrait.color = inspectedColor;
            }
            if (world.Lost) feedback.text = "Колония потеряна · ↻ начать заново";
            diagnosticText.text = "Seed " + world.Seed + "\nВремя " + world.Time.ToString("0") + " с\nТерритории " + world.Nodes.Count + "\nДороги " + world.Edges() + "\nПешки " + world.Pawns.Count + "\nТик " + clock.LastTickMilliseconds.ToString("0.00") + " мс\nПоиски пути " + world.Searches + "\nМасштаб " + map.Zoom.ToString("0") + "\n" + map.InputStatus;
            diagnosticText.text = "FPS " + statistics.FramesPerSecond.ToString("0") + "   TPS " + statistics.TicksPerSecond.ToString("0") + "\n" + diagnosticText.text;
            if (pawnSelected)
            {
                var pawn = world.Pawns[SelectedPawn];
                diagnosticText.text += "\n" + Names[(int)pawn.Role] + " · " + States[(int)pawn.State] + "\nПитание " + pawn.Food.ToString("0") + " · силы " + pawn.Energy.ToString("0") + "\nЗдоровье " + pawn.Health.ToString("0");
            }
        }
        void BeginMoveOrder()
        {
            if (directingPawn >= 0) { directingPawn = -1; feedback.text = "Выбор цели отменён"; return; }
            if (world.Pawns[SelectedPawn].OrderedDestination >= 0 && !(world.Pawns[SelectedPawn].Role == Profession.Defender && world.Pawns[SelectedPawn].DefenderMode == DefenderMode.Guard))
            {
                world.CancelOrder(SelectedPawn);
                Refresh();
                return;
            }
            directingPawn = SelectedPawn;
            feedback.text = "Выбери клетку назначения";
        }
        public bool IsDirecting => directingPawn >= 0;
        public void DirectToNode(int id)
        {
            bool success = world.DirectPawn(directingPawn,id);
            directingPawn = -1;
            Refresh();
            feedback.text = success ? "Приказ принят" : "Нет пути или запасов для возвращения";
        }
        void Inspect(string title, Color color, Action show, Func<string> summary = null)
        {
            descriptionPopup.Close();
            inspectedTitle = title; inspectedColor = color; inspectSelection = show; inspectSummary = summary;
            Refresh();
        }
        public void InspectResource(Resource resource,int nodeId = -1) => Inspect(ResourceNames[(int)resource], Palette.Resources[(int)resource], () => ShowResourceInfo(resource), () => nodeId < 0 ? "Склад · " + world.Stores[resource].ToString("0.0") : "Территория " + nodeId + " · " + world.Nodes[nodeId].Find(resource).Amount.ToString("0.0") + " / " + world.Nodes[nodeId].Find(resource).Capacity.ToString("0"));
        public void InspectFortification(int id) => Inspect("Защита " + world.Nodes[id].Fortification.ToString("0") + "/30",Palette.Resources[(int)Resource.Stability],() => ShowInfo("Укрепление территории\n\n" + world.Nodes[id].Fortification.ToString("0.0") + " из 30 защиты.\nВходящее заражение расходует защиту. После её исчерпания возникает очаг. Стабилизатор пополняет защиту эссенцией со склада."));
        public void InspectThreat(int id) => Inspect("Угроза · узел " + id, Palette.Professions[1], () => ShowThreatInfo(id), () => "Сила очага " + world.Nodes[id].Threat.ToString("0.0") + " · уничтожает ресурсы");
        public void InspectRoad(int a, int b) => Inspect("Дорога " + a + " — " + b, Color.gray, () => ShowRoadInfo(a,b), () => world.Travel.FoodCost(world.Nodes[a],world.Nodes[b]).ToString("0.0") + " еды · " + world.Travel.EnergyCost(world.Nodes[a],world.Nodes[b]).ToString("0.0") + " сил за переход");
        public void ShowInfo(string text)
        {
            descriptionPopup.Show(null,text,new Vector2(Screen.width*.5f,Screen.height*.5f));
        }
        public void ShowResourceInfo(Resource resource)
        {
            string[] explanations = {
                "Питание расходуется на еду пешек, походы, найм и строительство. Собиратели приносят его в ядро.",
                "Знания нужны для исследований. Исследователь получает их непосредственно на территории.",
                "Материя расходуется на заказанные постройки. Её доставляет строитель.",
                "Заряд со склада ускоряет восстановление истощённого пищевого источника. Его доставляет снабженец.",
                "Лекарства восстанавливают здоровье на ядре. Медик доставляет их со специальных территорий.",
                "Эссенцию собирает снабженец и доставляет в ядро. Стабилизатор несёт её на территорию. Одна эссенция создаёт 3 защиты, максимум 30. Заражение сначала расходует защиту."
            };
            ShowInfo(ResourceNames[(int)resource]+"\n\n"+explanations[(int)resource]);
        }
        public void ShowThreatInfo(int id)
        {
            ShowInfo("Очаг угрозы\n\nУзел "+id+" · сила "+world.Nodes[id].Threat.ToString("0.0")+"\nУничтожает запасы и распространяется по дорогам. Защитник подавляет очаг. Стабилизатор сдерживает распространение.");
        }
        public void ShowRoadInfo(int a,int b)
        {
            ShowInfo("Дорога "+a+" — "+b+"\n\nПереход требует "+world.Travel.FoodCost(world.Nodes[a],world.Nodes[b]).ToString("0.0")+" еды и "+world.Travel.EnergyCost(world.Nodes[a],world.Nodes[b]).ToString("0.0")+" сил.\nЗелёный цвет — дешевле, красный — дороже. Путевой пост снижает расход.");
        }
        void ShowSelectedInfo()
        {
            if (world == null) return;
            if (inspectSelection != null) { inspectSelection(); return; }
            if (SelectedPawn >= 0)
            {
                var p=world.Pawns[SelectedPawn];
                ShowInfo(Names[(int)p.Role]+" №"+(p.Id+1)+"\n"+Descriptions[(int)p.Role]+"\n\n"+States[(int)p.State]+" · узел "+p.At+(p.Next>=0?" → "+p.Next:"")+"\nЗдоровье "+p.Health.ToString("0")+" · еда "+p.Food.ToString("0")+" · силы "+p.Energy.ToString("0")+"\nГруз "+p.Cargo.ToString("0")+"\nСмена профессии выполняется на ядре.");
            }
            else
            {
                var n=world.Nodes[SelectedNode];
                string text=SelectedNode==World.Home?"Ядро\nПешки сдают груз, едят, отдыхают и меняют профессию.":"Территория "+SelectedNode+"\n"+(n.Known?"Можно заказать постройку и изменить доступ.":"Требуется разведчик.");
                if(n.Construction!=null)text+="\n\n"+n.Construction.Definition.Name+"\n"+n.Construction.Definition.Description;
                ShowInfo(text);
            }
        }
        void OnDestroy() { if (buttonSkin) Destroy(buttonSkin); if (disc) Destroy(disc); if (discTexture) Destroy(discTexture); }
        RectTransform Panel(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image)); obj.transform.SetParent(parent, false);
            obj.GetComponent<Image>().color = color; return (RectTransform)obj.transform;
        }
        Text Label(Transform parent, string value, float x, float y, float width, float height, int size)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = new Color(.87f, .92f, .96f); text.text = value;
            text.raycastTarget = false; text.supportRichText = true; Place(text.rectTransform, x, y, width, height); return text;
        }
        Button Button(Transform parent, string title, float x, float y, float width, float height, UnityEngine.Events.UnityAction action, bool right = false, string iconSymbol = null, string holdText = null, Func<string> holdDescription = null)
        {
            var panel = Panel(parent, title, new Color(.13f, .2f, .26f)); Place(panel, x, y, width, height);
            if (right) { panel.anchorMin = panel.anchorMax = Vector2.one; }
            var image = panel.GetComponent<Image>();
            image.sprite = buttonSkin; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 3;
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
            button.onClick.AddListener(action);
            if (title.Length == 1 || iconSymbol != null)
            {
                var obj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(UiIcon)); obj.transform.SetParent(panel, false);
                var icon = obj.GetComponent<UiIcon>(); icon.Symbol = iconSymbol ?? title; icon.raycastTarget = false;
                Place(icon.rectTransform, (width - 28) / 2, -(height - 28) / 2, 28, 28);
            }
            else
            {
                var text = Label(panel, title, 0, 0, width, height, 21); text.alignment = TextAnchor.MiddleCenter;
            }
            return button;
        }
        static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        static string IconDescription(string symbol, string fallback)
        {
            switch (symbol)
            {
                case "▶": return "Приостановить или продолжить симуляцию.";
                case "×": return "Закрыть окно.";
                case "↻": return "Начать заново на новой карте.";
                case "?": return "Обозначения и управление. Удерживайте значок для подробностей.";
                case "…": return "Статистика симуляции, TPS и FPS.";
                case "⌂": return "Вернуться к ядру колонии.";
                case "‹": return "Предыдущая профессия.";
                case "›": return "Следующая профессия.";
                default: return fallback;
            }
        }
        void ApplyMapLayout()
        {
            bool portraitMode = Screen.height > Screen.width;
            if (!layoutReady || portraitMode != portraitLayout)
            {
                portraitLayout = portraitMode;
                canvasScaler.referenceResolution = portraitMode ? new Vector2(480, 800) : new Vector2(1000, 650);
                map.Pan = Vector2.zero;
                map.Zoom = portraitMode ? 40 : 60;
                layoutReady = true;
            }
            float headerHeight = 112;
            float bottomHeight = portraitMode ? 180 : 140;
            topPanel.offsetMin = new Vector2(0, -headerHeight);
            selectionPanel.offsetMax = new Vector2(0, bottomHeight);
            colonyControlsRect.anchoredPosition = new Vector2(12, -headerHeight - 16);

            Place(description.rectTransform, 210, -14, portraitMode ? Mathf.Max(100, safeArea.rect.width - 290) : 600, portraitMode ? 44 : 28);
            Place(details.rectTransform, 210, portraitMode ? -58 : -40, portraitMode ? safeArea.rect.width - 226 : 700, portraitMode ? 52 : 38);
            Place(convert.GetComponent<RectTransform>(), 210, portraitMode ? -118 : -76, 100, 32);
            Place(defenderMode.GetComponent<RectTransform>(),portraitMode ? 320 : 460,portraitMode ? -72 : -76,150,36);
            Place(buildNodeButton.GetComponent<RectTransform>(), portraitMode ? 320 : 410, portraitMode ? -118 : -76, 136, 42);
            Place(accessButton.GetComponent<RectTransform>(), 210, portraitMode ? -118 : -76, 180, 48);
            Place(feedback.rectTransform, portraitMode ? 12 : 410, portraitMode ? -136 : -88, portraitMode ? 184 : 500, portraitMode ? 36 : 30);
            PositionMenu(construction, portraitMode, true, bottomHeight);
            PositionMenu(research, portraitMode, true, bottomHeight);
            PositionMenu(workforce, portraitMode, true, bottomHeight);
            PositionMenu(diagnostics, portraitMode, false, bottomHeight);
            if (portraitMode)
            {
                bool menuOpen = construction.activeSelf || research.activeSelf || workforce.activeSelf || diagnostics.activeSelf || help.activeSelf;
                mapViewport.offsetMin = new Vector2(88, bottomHeight + (menuOpen ? 374 : 0));
                mapViewport.offsetMax = new Vector2(0, -headerHeight);
                return;
            }
            float left = 88;
            if (construction.activeSelf || research.activeSelf || workforce.activeSelf) left = 510;
            if (help.activeSelf) left = 100 + ((RectTransform)help.transform).rect.width;
            float right = diagnostics.activeSelf ? 434 : 0;
            mapViewport.offsetMin = new Vector2(left, bottomHeight);
            mapViewport.offsetMax = new Vector2(-right, -headerHeight);
        }
        static void PositionMenu(GameObject menu, bool portraitMode, bool left, float bottomHeight)
        {
            var rect = (RectTransform)menu.transform;
            Vector2 anchor = portraitMode ? new Vector2(.5f, 0) : new Vector2(left ? 0 : 1, 1);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = portraitMode ? new Vector2(0, bottomHeight + 12) : new Vector2(left ? 88 : -12, -72);
            rect.sizeDelta = new Vector2(410, portraitMode ? 350 : 390);
        }
    }
}
