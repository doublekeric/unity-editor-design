using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityDesignExamples
{
    public sealed class AssetReviewWindow : EditorWindow
    {
        private const int BodySize = 13;
        private const int CaptionSize = 12;
        private const int ResourceRowHeight = 30;
        private static readonly string[] Groups = { "全部资源", "界面纹理", "角色", "场景", "音频" };
        private readonly List<Asset> _assets = new();
        private readonly List<Asset> _visible = new();
        private readonly List<Asset> _selection = new();
        private readonly HashSet<int> _selectedIds = new();
        private Theme _theme;
        private ListView _table, _preview;
        private VisualElement _resourcesPage, _batchPage, _resourceEmpty, _batchEmpty;
        private ScrollView _details;
        private TextField _search;
        private Label _count, _selectionCount, _hint, _problemCount;
        private Button _sizeSortButton, _primary, _clearSelection;
        private string _searchText = "", _lastResult;
        private int _group, _sizeSort, _lastApplied;
        private bool _light, _problemsOnly, _batch, _syncing;
        private bool _detailsVisible = true;

        [MenuItem("Tools/设计示例/资源审查台")]
        public static void Open()
        {
            var window = GetWindow<AssetReviewWindow>();
            window.position = new Rect(window.position.x, window.position.y, 1260, 800);
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("资源审查台 · 设计示例");
            minSize = new Vector2(1040, 720);
        }

        private void OnDisable() => ReviewFont.Release();

        public void CreateGUI()
        {
            if (_assets.Count == 0)
            {
                Seed();
                _selectedIds.Add(0);
            }
            _theme = new Theme(_light);
            var root = rootVisualElement;
            root.Clear();
            root.style.backgroundColor = _theme.surface;
            root.style.color = _theme.text;
            root.style.fontSize = BodySize;
            root.style.unityFontStyleAndWeight = FontStyle.Normal;

            var heading = Box(true, 20);
            heading.style.height = 64;
            heading.style.flexShrink = 0;
            heading.style.backgroundColor = _theme.navigation;
            heading.Add(Text("资源审查台", 19));
            var subtitle = Text("设计示例 · 仅内存模拟", CaptionSize, muted: true);
            subtitle.style.marginLeft = 16;
            subtitle.style.flexGrow = 1;
            heading.Add(subtitle);
            heading.Add(Command("石墨", () => SetTheme(false), ButtonRole.Quiet, () => !_light));
            heading.Add(Command("纸白", () => SetTheme(true), ButtonRole.Quiet, () => _light));
            root.Add(heading);

            var body = Box(true);
            body.style.alignItems = Align.Stretch;
            body.style.flexGrow = 1;
            root.Add(body);
            BuildNavigation(body);
            BuildWorkspace(body);
            _details = new ScrollView(ScrollViewMode.Vertical);
            _details.style.width = 286;
            _details.style.flexShrink = 0;
            _details.style.minHeight = 0;
            _details.style.backgroundColor = _theme.navigation;
            _details.contentContainer.style.paddingLeft = _details.contentContainer.style.paddingRight = 18;
            _details.contentContainer.style.paddingTop = _details.contentContainer.style.paddingBottom = 20;
            body.Add(_details);
            Refresh();
            ReviewFont.Apply(root);
        }

        private void BuildNavigation(VisualElement parent)
        {
            var navigation = Box(padding: 12);
            navigation.style.width = 156;
            navigation.style.flexShrink = 0;
            navigation.style.backgroundColor = _theme.navigation;
            navigation.Add(Text("资源分组", CaptionSize, muted: true));
            for (int i = 0; i < Groups.Length; i++)
            {
                int group = i;
                var button = Command("", () => SetGroup(Groups[group]), ButtonRole.Quiet, () => _group == group);
                button.style.height = 34;
                button.style.marginTop = 4;
                button.style.flexDirection = FlexDirection.Row;
                var name = Text(Groups[group]);
                name.style.flexGrow = 1;
                button.Add(name);
                button.Add(Text((group == 0 ? 24 : _assets.Count(a => a.group == group)).ToString(), CaptionSize, muted: true));
                navigation.Add(button);
            }
            var scope = Text("审查范围", CaptionSize, muted: true);
            scope.style.marginTop = 28;
            scope.style.marginBottom = 8;
            navigation.Add(scope);
            var filters = Box(true);
            var all = Command("全部", () => SetProblemsOnly(false), ButtonRole.Quiet, () => !_problemsOnly);
            var problems = Command("有问题", () => SetProblemsOnly(true), ButtonRole.Quiet, () => _problemsOnly);
            all.style.flexGrow = problems.style.flexGrow = 1;
            filters.Add(all);
            filters.Add(problems);
            navigation.Add(filters);
            _problemCount = Text("", CaptionSize, muted: true);
            _problemCount.style.marginTop = 10;
            navigation.Add(_problemCount);
            var space = Box();
            space.style.flexGrow = 1;
            navigation.Add(space);
            navigation.Add(Text("Ctrl / ⌘ 多选\nShift 连续选择", CaptionSize, muted: true, wrap: true));
            var note = Text("筛选后，隐藏的资源会退出选择。", CaptionSize, muted: true, wrap: true);
            note.style.marginTop = 12;
            navigation.Add(note);
            parent.Add(navigation);
        }

        private void BuildWorkspace(VisualElement parent)
        {
            var workspace = Box();
            workspace.style.flexGrow = 1;
            workspace.style.flexBasis = 0;
            parent.Add(workspace);
            var tools = Box(true, 12);
            tools.style.height = 52;
            tools.style.flexShrink = 0;
            _search = new TextField { name = "asset-search", tooltip = "搜索文件名或完整路径，不区分大小写" };
            _search.SetValueWithoutNotify(_searchText);
            _search.style.flexGrow = 1;
            _search.style.minWidth = 0;
            StyleField(_search);
            _search.RegisterValueChangedCallback(e => { _searchText = e.newValue; _lastResult = null; Refresh(); });
            tools.Add(Text("搜索", CaptionSize, muted: true));
            tools.Add(_search);
            tools.Add(Command("清除", () => SetSearch(""), ButtonRole.Quiet));
            var detailsToggle = Command("详情", () => SetDetailsVisible(!_detailsVisible), ButtonRole.Quiet, () => _detailsVisible);
            detailsToggle.tooltip = "展开或收起资源详情，窄窗口可收起以完整查看列表";
            tools.Add(detailsToggle);
            workspace.Add(tools);

            var tabs = Box(true, 12);
            tabs.style.height = 42;
            tabs.style.flexShrink = 0;
            tabs.style.borderBottomWidth = 1;
            tabs.style.borderBottomColor = _theme.edge;
            var resourcesTab = Command("资源", () => { _batch = false; Refresh(false); }, ButtonRole.Tab, () => !_batch);
            resourcesTab.style.width = 72;
            var batchTab = Command("批处理预览", ShowBatch, ButtonRole.Tab, () => _batch);
            batchTab.style.width = 112;
            batchTab.style.marginLeft = 8;
            tabs.Add(resourcesTab);
            tabs.Add(batchTab);
            _count = Text("", CaptionSize, muted: true);
            _count.style.flexGrow = 1;
            _count.style.unityTextAlign = TextAnchor.MiddleRight;
            tabs.Add(_count);
            workspace.Add(tabs);

            _resourcesPage = Box();
            _resourcesPage.style.flexGrow = 1;
            var columns = Box(true, 14);
            columns.style.height = 32;
            columns.style.flexShrink = 0;
            var nameHeader = Text("名称", CaptionSize, muted: true);
            nameHeader.style.flexGrow = 1;
            nameHeader.style.marginLeft = 28;
            columns.Add(nameHeader);
            columns.Add(Cell("规格 / 类型", 104, muted: true));
            _sizeSortButton = Command("大小 ↕", () => { _sizeSort = _sizeSort == -1 ? 1 : -1; Refresh(); }, ButtonRole.Quiet);
            _sizeSortButton.style.width = 72;
            _sizeSortButton.style.paddingLeft = _sizeSortButton.style.paddingRight = 0;
            _sizeSortButton.style.fontSize = CaptionSize;
            columns.Add(_sizeSortButton);
            columns.Add(Cell("状态", 100, muted: true));
            _resourcesPage.Add(columns);
            _table = MakeList(_visible, false);
            _table.selectionChanged += items =>
            {
                if (_syncing) return;
                _selectedIds.Clear();
                _selectedIds.UnionWith(items.Cast<Asset>().Select(a => a.id));
                _lastResult = null;
                Refresh(false);
            };
            _resourcesPage.Add(_table);
            _resourceEmpty = Empty("没有匹配的资源", "试试文件名、路径片段，或清除组合筛选。", "清除全部筛选", ResetFilters);
            _resourcesPage.Add(_resourceEmpty);
            workspace.Add(_resourcesPage);

            _batchPage = Box();
            _batchPage.style.flexGrow = 1;
            var explanation = Text("原状态 → 目标状态 · 仅处理所选中的可自动项", CaptionSize, muted: true);
            explanation.style.marginLeft = explanation.style.marginRight = 16;
            explanation.style.height = 32;
            _batchPage.Add(explanation);
            _preview = MakeList(_selection, true);
            _batchPage.Add(_preview);
            _batchEmpty = Empty("尚未选择资源", "返回资源列表选择对象；筛选隐藏的项目不会被应用。", "返回资源", () => { _batch = false; Refresh(false); });
            _batchPage.Add(_batchEmpty);
            workspace.Add(_batchPage);

            var footer = Box(true, 12);
            footer.style.height = 72;
            footer.style.flexShrink = 0;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = _theme.edge;
            var summary = Box();
            summary.style.flexGrow = 1;
            _selectionCount = Text("");
            _hint = Text("", CaptionSize, muted: true, wrap: true);
            _hint.style.marginTop = 4;
            summary.Add(_selectionCount);
            summary.Add(_hint);
            footer.Add(summary);
            footer.Add(Command("全选当前", () => SelectAssets(_visible.Select(a => a.id).ToArray()), ButtonRole.Quiet));
            _clearSelection = Command("清空", () => SelectAssets(Array.Empty<int>()), ButtonRole.Quiet);
            footer.Add(_clearSelection);
            _primary = Command("", () => { if (_batch) ApplyBatch(); else ShowBatch(); }, ButtonRole.Primary);
            _primary.style.minWidth = 116;
            _primary.style.marginLeft = 8;
            footer.Add(_primary);
            workspace.Add(footer);
        }

        private ListView MakeList(List<Asset> source, bool batch)
        {
            var list = new ListView
            {
                itemsSource = source, fixedItemHeight = batch ? 62 : ResourceRowHeight,
                selectionType = batch ? SelectionType.None : SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None
            };
            list.style.flexGrow = 1;
            list.style.minHeight = 0;
            list.makeItem = () => MakeRow(batch);
            list.bindItem = (element, index) => BindRow(element, source[index], batch);
            list.style.borderTopWidth = list.style.borderBottomWidth = 1;
            list.RegisterCallback<FocusInEvent>(_ => SetListFocus(list, true));
            list.RegisterCallback<FocusOutEvent>(_ => SetListFocus(list, false));
            SetListFocus(list, false);
            return list;
        }

        private void SetListFocus(ListView list, bool focused)
        {
            list.style.borderTopColor = list.style.borderBottomColor = focused ? _theme.accent : Color.clear;
        }

        private VisualElement MakeRow(bool batch)
        {
            var row = Box(true, 12);
            row.style.height = batch ? 62 : ResourceRowHeight;
            row.style.flexShrink = 0;
            row.style.borderLeftWidth = 2;
            var icon = Cell("", 28, muted: true);
            icon.name = "icon";
            row.Add(icon);
            var description = Box();
            description.style.flexGrow = 1;
            description.style.flexBasis = 0;
            if (!batch) description.style.marginRight = 12;
            var name = Text("");
            name.name = "filename";
            description.Add(name);
            row.Add(description);
            if (batch)
            {
                var transition = Text("", CaptionSize, muted: true);
                transition.name = "transition";
                transition.style.marginTop = 5;
                description.Add(transition);
                var decision = Box();
                decision.style.width = 142;
                decision.style.flexShrink = 0;
                decision.style.marginLeft = 12;
                var verdict = Text("", CaptionSize);
                verdict.name = "state";
                var reason = Text("", CaptionSize, muted: true);
                reason.name = "reason";
                reason.style.marginTop = 5;
                decision.Add(verdict);
                decision.Add(reason);
                row.Add(decision);
            }
            else
            {
                var spec = Cell("", 104, muted: true);
                spec.name = "spec";
                var size = Cell("", 72);
                size.name = "size";
                size.style.unityTextAlign = TextAnchor.MiddleRight;
                size.style.paddingRight = 14;
                var state = Cell("", 100);
                state.name = "state";
                row.Add(spec);
                row.Add(size);
                row.Add(state);
            }
            bool hover = false;
            void Paint()
            {
                bool selected = !batch && row.userData is Asset asset && _selectedIds.Contains(asset.id);
                row.style.backgroundColor = selected ? (hover ? _theme.selectionHover : _theme.selection) : (hover ? _theme.hover : _theme.surface);
                row.style.borderLeftColor = selected ? _theme.accent : Color.clear;
            }
            row.RegisterCallback<MouseEnterEvent>(_ => { hover = true; Paint(); });
            row.RegisterCallback<MouseLeaveEvent>(_ => { hover = false; Paint(); });
            row.RegisterCallback<GeometryChangedEvent>(_ => Paint());
            row.Q<Label>("filename").userData = (Action)Paint;
            return row;
        }

        private void BindRow(VisualElement row, Asset asset, bool batch)
        {
            row.userData = asset;
            row.tooltip = asset.path;
            row.Q<Label>("icon").text = asset.glyph;
            var name = row.Q<Label>("filename");
            name.text = asset.name;
            var state = row.Q<Label>("state");
            state.text = batch ? asset.Verdict : asset.status;
            state.style.color = StateColor(asset);
            if (batch)
            {
                row.Q<Label>("transition").text = $"{asset.before ?? asset.status} → {asset.Destination}";
                row.Q<Label>("reason").text = asset.PlanReason;
                row.tooltip += $"\n{asset.before ?? asset.status} → {asset.Destination}\n{asset.PlanReason}";
            }
            else
            {
                row.Q<Label>("spec").text = asset.spec;
                row.Q<Label>("size").text = Size(asset.sizeKB);
            }
            ((Action)name.userData)();
        }

        private void Refresh(bool remap = true)
        {
            if (remap)
            {
                _syncing = true;
                _table.ClearSelection();
                var query = _assets.Where(a => (_group == 0 || a.group == _group) && (!_problemsOnly || a.state != Issue.Normal)
                    && (a.name.IndexOf(_searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 || a.path.IndexOf(_searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0));
                if (_sizeSort != 0) query = _sizeSort < 0 ? query.OrderByDescending(a => a.sizeKB).ThenBy(a => a.id) : query.OrderBy(a => a.sizeKB).ThenBy(a => a.id);
                _visible.Clear();
                _visible.AddRange(query);
                _selectedIds.IntersectWith(_visible.Select(a => a.id));
            }
            _selection.Clear();
            _selection.AddRange(_visible.Where(a => _selectedIds.Contains(a.id)));
            _table.RefreshItems();
            if (remap) _table.SetSelectionWithoutNotify(_visible.Select((a, i) => _selectedIds.Contains(a.id) ? i : -1).Where(i => i >= 0));
            _syncing = false;
            _preview.RefreshItems();
            int ready = _selection.Count(a => a.state == Issue.Automatic);
            _resourcesPage.style.display = _batch ? DisplayStyle.None : DisplayStyle.Flex;
            _batchPage.style.display = _batch ? DisplayStyle.Flex : DisplayStyle.None;
            _details.style.display = _detailsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _table.style.display = _visible.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _resourceEmpty.style.display = _visible.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _preview.style.display = _selection.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            _batchEmpty.style.display = _selection.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _count.text = $"显示 {_visible.Count} / {_assets.Count} 项";
            _problemCount.text = $"共 {_assets.Count(a => a.state != Issue.Normal)} 项有问题";
            _sizeSortButton.text = "大小 " + (_sizeSort == 0 ? "↕" : _sizeSort < 0 ? "↓" : "↑");
            _selectionCount.text = $"已选 {_selection.Count} 项 · 可处理 {ready} 项";
            _hint.text = _lastResult ?? (_selection.Count == 0 ? "先选择资源；支持组合筛选。" : ready == 0 ? "没有可自动处理项；可预览跳过原因。" : $"其余 {_selection.Count - ready} 项跳过或阻止 · 不改磁盘");
            _primary.text = _batch ? $"模拟应用 {ready} 项" : $"预览所选 {_selection.Count} 项";
            _primary.SetEnabled(_batch ? ready > 0 : _selection.Count > 0);
            _clearSelection.SetEnabled(_selection.Count > 0);
            BuildDetails();
            rootVisualElement.Query<Button>().ForEach(button => { if (button.userData is Action paint) paint(); });
        }

        private void BuildDetails()
        {
            _details.Clear();
            _details.Add(Text(_selection.Count > 1 ? "本次选择" : "资源详情", CaptionSize, muted: true));
            if (_selection.Count == 0)
            {
                Section(_details, "未选择资源", "选择列表中的资源，查看规格、完整路径与处理说明。");
                return;
            }
            if (_selection.Count > 1)
            {
                int ready = _selection.Count(a => a.state == Issue.Automatic);
                int manual = _selection.Count(a => a.state == Issue.Manual);
                int conflicts = _selection.Count(a => a.state == Issue.Conflict);
                int unchanged = _selection.Count - ready - manual - conflicts;
                Section(_details, $"已选 {_selection.Count} 项资源", $"可自动处理  {ready} 项\n需人工确认  {manual} 项\n冲突阻止  {conflicts} 项\n正常或已应用  {unchanged} 项");
                Section(_details, "处理范围", "只模拟可自动处理的项目。人工确认、设置冲突和正常项保持不变。");
                if (_batch)
                {
                    foreach (var conflict in _selection.Where(a => a.state == Issue.Conflict))
                    {
                        Section(_details, "冲突详情", conflict.reason, _theme.danger);
                        var conflictName = Filename(conflict.name);
                        conflictName.style.marginTop = 10;
                        _details.Add(conflictName);
                    }
                    if (conflicts == 0)
                        Section(_details, "没有配置冲突", "逐项结果见左侧预览。人工项仍需手动确认，不进入本次模拟。");
                    return;
                }
                foreach (var asset in _selection)
                {
                    var name = Filename(asset.name);
                    name.style.marginTop = 16;
                    name.tooltip = asset.path;
                    _details.Add(name);
                    var state = Text($"{asset.Verdict} · {asset.PlanReason}", CaptionSize);
                    state.style.marginTop = 4;
                    state.style.color = StateColor(asset);
                    _details.Add(state);
                }
                return;
            }
            var current = _selection[0];
            var filename = Filename(current.name);
            filename.style.marginTop = 12;
            _details.Add(filename);
            Section(_details, current.status, current.reason, StateColor(current));
            Section(_details, "模拟处理", $"{current.before ?? current.status} → {current.Destination}\n{current.PlanReason}");
            var preview = Box(true, 12);
            preview.style.height = 80;
            preview.style.marginTop = 20;
            preview.style.backgroundColor = _theme.surface;
            var icon = new Image
            {
                image = EditorGUIUtility.IconContent(current.type + " Icon").image,
                scaleMode = ScaleMode.ScaleToFit,
                tooltip = current.type + " 类型图标，并非资源缩略图"
            };
            icon.style.width = icon.style.height = 36;
            icon.style.flexShrink = 0;
            icon.style.marginRight = 12;
            preview.Add(icon);
            var type = Box();
            type.style.flexGrow = 1;
            type.style.flexBasis = 0;
            type.Add(Text(current.type));
            type.Add(Text("类型示意 · 非资源缩略图", CaptionSize, muted: true, wrap: true));
            preview.Add(type);
            _details.Add(preview);
            Section(_details, "资源规格", $"{Groups[current.group]} / {current.type}\n{current.spec}   ·   {Size(current.sizeKB)}");
            var pathHeader = Box(true);
            pathHeader.style.marginTop = 18;
            var pathLabel = Text("完整路径", CaptionSize, muted: true);
            pathLabel.style.flexGrow = 1;
            pathHeader.Add(pathLabel);
            pathHeader.Add(Command("复制", () => EditorGUIUtility.systemCopyBuffer = current.path, ButtonRole.Quiet));
            _details.Add(pathHeader);
            var path = new TextField { value = current.path, isReadOnly = true, multiline = true, tooltip = current.path };
            StyleField(path);
            path.style.height = 82;
            path.style.marginLeft = 0;
            path.Q<VisualElement>("unity-text-input").style.whiteSpace = WhiteSpace.Normal;
            _details.Add(path);
        }

        public void SetTheme(bool light) { _light = light; CreateGUI(); }
        public void SetDetailsVisible(bool visible) { _detailsVisible = visible; Refresh(false); }
        public void SetSearch(string text) => _search.value = text ?? "";
        public void SetGroup(string name)
        {
            int group = Array.IndexOf(Groups, name);
            if (group < 0) throw new ArgumentException("未知资源分组", nameof(name));
            _group = group;
            _lastResult = null;
            Refresh();
        }
        public void SetProblemsOnly(bool value) { _problemsOnly = value; _lastResult = null; Refresh(); }
        public void SelectAssets(int[] ids)
        {
            var requested = new HashSet<int>(ids ?? Array.Empty<int>());
            _table.SetSelection(_visible.Select((a, i) => requested.Contains(a.id) ? i : -1).Where(i => i >= 0));
        }
        public void ShowBatch() { _batch = true; Refresh(false); }
        public void ApplyBatch()
        {
            if (!_batch) return;
            var ready = _selection.Where(a => a.state == Issue.Automatic).ToArray();
            if (ready.Length == 0) return;
            int conflicts = _selection.Count(a => a.state == Issue.Conflict);
            int skipped = _selection.Count - ready.Length - conflicts;
            foreach (var asset in ready)
            {
                asset.before = asset.status;
                asset.status = "已模拟修复";
                asset.state = Issue.Normal;
                asset.sizeKB = asset.targetKB;
                asset.spec = asset.targetSpec ?? asset.spec;
                asset.reason = $"已在内存中模拟：{asset.target}。未检查或修改任何磁盘资源。";
            }
            _lastApplied = ready.Length;
            _lastResult = $"已模拟 {_lastApplied} 项 · 跳过 {skipped} · 阻止 {conflicts}";
            if (_problemsOnly) _lastResult += "；已处理项退出筛选。";
            Refresh();
        }
        public object Diagnostics() => new
        {
            total = _assets.Count, visible = _visible.Count, selected = _selection.Count,
            autoFixable = _selection.Count(a => a.state == Issue.Automatic),
            blocked = _selection.Count(a => a.state == Issue.Manual || a.state == Issue.Conflict),
            skipped = _selection.Count(a => a.state == Issue.Normal),
            conflicts = _selection.Count(a => a.state == Issue.Conflict),
            problems = _assets.Count(a => a.state != Issue.Normal), light = _light,
            page = _batch ? "batch" : "resources", detailsVisible = _detailsVisible,
            updated = _assets.Count(a => a.before != null),
            lastApplied = _lastApplied, selectedIds = _selection.Select(a => a.id).ToArray(),
            visibleIds = _visible.Select(a => a.id).ToArray()
        };

        private void ResetFilters()
        {
            _group = 0;
            _problemsOnly = false;
            _searchText = "";
            _search.SetValueWithoutNotify("");
            _lastResult = null;
            Refresh();
        }
        private Color StateColor(Asset asset) => asset.state switch
        {
            Issue.Automatic => _theme.accent,
            Issue.Manual => _theme.warning,
            Issue.Conflict => _theme.danger,
            _ => _theme.muted
        };
        private static string Size(int kb) => kb < 1024 ? $"{kb} KB" : $"{kb / 1024f:0.0} MB";
        private VisualElement Box(bool horizontal = false, int padding = 0)
        {
            var box = new VisualElement();
            box.style.flexDirection = horizontal ? FlexDirection.Row : FlexDirection.Column;
            if (horizontal) box.style.alignItems = Align.Center;
            box.style.minWidth = box.style.minHeight = 0;
            box.style.paddingLeft = box.style.paddingRight = padding;
            return box;
        }
        private Label Text(string text, int size = BodySize, bool strong = false, bool muted = false, bool wrap = false)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.unityFontStyleAndWeight = strong ? FontStyle.Bold : FontStyle.Normal;
            label.style.color = muted ? _theme.muted : _theme.text;
            label.style.minWidth = 0;
            label.style.flexShrink = 0;
            label.style.whiteSpace = wrap ? WhiteSpace.Normal : WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = wrap ? TextOverflow.Clip : TextOverflow.Ellipsis;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }
        private VisualElement Filename(string value)
        {
            var container = Box();
            container.tooltip = value;
            var first = Text(value);
            var second = Text("");
            first.style.height = second.style.height = 18;
            second.style.display = DisplayStyle.None;
            container.Add(first);
            container.Add(second);
            container.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                float width = container.contentRect.width;
                if (width <= 0) return;
                float Measure(string text) => first.MeasureTextSize(text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).x;
                if (Measure(value) <= width)
                {
                    first.text = value;
                    second.style.display = DisplayStyle.None;
                    return;
                }
                int split = -1;
                float balance = float.MaxValue;
                for (int i = 1; i < value.Length - 1; i++)
                {
                    if (value[i] != '_' && value[i] != '-') continue;
                    float candidate = Mathf.Max(Measure(value.Substring(0, i + 1)), Measure(value.Substring(i + 1)));
                    if (candidate >= balance) continue;
                    balance = candidate;
                    split = i + 1;
                }
                first.text = split < 0 ? value : value.Substring(0, split);
                second.text = split < 0 ? "" : value.Substring(split);
                second.style.display = split < 0 ? DisplayStyle.None : DisplayStyle.Flex;
            });
            return container;
        }
        private Label Cell(string text, int width, bool muted = false)
        {
            var cell = Text(text, CaptionSize, muted: muted);
            cell.style.width = width;
            return cell;
        }
        private void Section(VisualElement parent, string title, string description, Color? color = null)
        {
            var heading = Text(title, strong: true);
            heading.style.marginTop = 22;
            if (color.HasValue) heading.style.color = color.Value;
            parent.Add(heading);
            var body = Text(description, wrap: true);
            body.style.marginTop = 8;
            parent.Add(body);
        }
        private VisualElement Empty(string title, string description, string action, Action clicked)
        {
            var empty = Box(padding: 28);
            empty.style.flexGrow = 1;
            empty.style.justifyContent = Justify.Center;
            empty.style.alignItems = Align.FlexStart;
            Section(empty, title, description);
            var button = Command(action, clicked);
            button.style.marginTop = 16;
            empty.Add(button);
            return empty;
        }
        private void StyleField(TextField field)
        {
            field.style.height = 30;
            field.style.marginLeft = 8;
            field.style.marginRight = 4;
            field.style.marginTop = field.style.marginBottom = 0;
            var input = field.Q<VisualElement>("unity-text-input");
            input.style.backgroundImage = StyleKeyword.None;
            input.style.backgroundColor = _theme.control;
            input.style.color = _theme.text;
            input.style.fontSize = BodySize;
            input.style.paddingLeft = input.style.paddingRight = 8;
            input.style.paddingTop = input.style.paddingBottom = 4;
            Outline(input, _theme.edge);
            field.RegisterCallback<FocusInEvent>(_ => Outline(input, _theme.accent));
            field.RegisterCallback<FocusOutEvent>(_ => Outline(input, _theme.edge));
        }
        private static void Outline(VisualElement element, Color color)
        {
            element.style.borderLeftWidth = element.style.borderRightWidth = element.style.borderTopWidth = element.style.borderBottomWidth = 1;
            element.style.borderLeftColor = element.style.borderRightColor = element.style.borderTopColor = element.style.borderBottomColor = color;
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = 4;
        }
        private Button Command(string title, Action clicked, ButtonRole role = ButtonRole.Normal, Func<bool> active = null)
        {
            var button = new Button(clicked) { text = title };
            button.style.height = role == ButtonRole.Tab ? 34 : 30;
            button.style.flexShrink = 0;
            button.style.marginLeft = button.style.marginRight = button.style.marginTop = button.style.marginBottom = 0;
            button.style.paddingLeft = button.style.paddingRight = role == ButtonRole.Quiet ? 8 : 12;
            button.style.fontSize = BodySize;
            button.style.unityFontStyleAndWeight = FontStyle.Normal;
            button.style.backgroundImage = StyleKeyword.None;
            bool hover = false, pressed = false, focus = false;
            VisualElement underline = null;
            if (role == ButtonRole.Tab)
            {
                underline = new VisualElement { pickingMode = PickingMode.Ignore };
                underline.style.position = Position.Absolute;
                underline.style.left = underline.style.right = 12;
                underline.style.bottom = 0;
                underline.style.height = 2;
                button.Add(underline);
            }
            void Paint()
            {
                bool enabled = button.enabledInHierarchy;
                bool selected = active?.Invoke() == true;
                Color background = role == ButtonRole.Normal ? _theme.control : Color.clear;
                if (selected && role != ButtonRole.Tab) background = _theme.selection;
                if (hover) background = selected ? _theme.selectionHover : _theme.hover;
                if (pressed) background = _theme.pressed;
                if (role == ButtonRole.Primary) background = pressed ? _theme.accentPressed : hover ? _theme.accentHover : _theme.accent;
                button.style.backgroundColor = enabled ? background : _theme.control;
                button.style.color = !enabled ? _theme.disabled : role == ButtonRole.Primary ? _theme.onAccent : selected ? _theme.accent : _theme.text;
                button.style.opacity = 1;
                Outline(button, focus && enabled ? _theme.accent : role == ButtonRole.Normal ? _theme.edge : Color.clear);
                if (underline != null) underline.style.backgroundColor = selected ? _theme.accent : Color.clear;
            }
            button.userData = (Action)Paint;
            button.RegisterCallback<MouseEnterEvent>(_ => { hover = true; Paint(); });
            button.RegisterCallback<MouseLeaveEvent>(_ => { hover = pressed = false; Paint(); });
            button.RegisterCallback<PointerDownEvent>(_ => { pressed = true; Paint(); });
            button.RegisterCallback<PointerUpEvent>(_ => { pressed = false; Paint(); });
            button.RegisterCallback<FocusInEvent>(_ => { focus = true; Paint(); });
            button.RegisterCallback<FocusOutEvent>(_ => { focus = pressed = false; Paint(); });
            button.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.Return) { pressed = true; Paint(); } });
            button.RegisterCallback<KeyUpEvent>(_ => { pressed = false; Paint(); });
            Paint();
            return button;
        }

        private void Seed()
        {
            _assets.AddRange(new[]
            {
                new Asset(0, 1, "ui_character_portrait_summer_limited_2026.png", "2048×3072", 6144, Issue.Manual, "尺寸需确认", "非方形立绘需确认投放端尺寸与透明留白。不自动缩放，避免改变构图。", "人工确认尺寸"),
                new Asset(1, 1, "ui_common_button_primary.png", "256×64", 32),
                new Asset(2, 1, "ui_inventory_slot_legendary.png", "256×256", 128),
                new Asset(3, 1, "ui_shop_background_summer_event.png", "4096×2048", 16384, Issue.Automatic, "未压缩", "示例导入配置为 RGBA32。可模拟改为 ASTC 6×6；图像尺寸保持不变。", "ASTC 6×6", 3641),
                new Asset(4, 1, "ui_localization_banner_zh_CN.png", "1024×256", 512),
                new Asset(5, 1, "ui_localization_banner_en_US.png", "1024×256", 512),
                new Asset(6, 1, "ui_common_panel_shadow.png", "1024×1024", 4096, Issue.Automatic, "未压缩", "透明阴影当前采用未压缩配置，可模拟 ASTC 6×6。", "ASTC 6×6", 455),
                new Asset(7, 1, "ui_reward_icon_sparkle.png", "512×512", 1024, Issue.Manual, "引用缺失", "示例奖励配置中的 Sprite 引用缺失，需要人工重新关联。不会搜索真实资源。", "人工补全引用"),
                new Asset(8, 2, "pet002_idle.anim", "64 帧 / 30 fps", 84),
                new Asset(9, 2, "pet002_run.anim", "128 帧 / 60 fps", 216, Issue.Automatic, "关键帧偏多", "包含可合并的重复采样点，模拟精简后保留动作时长与 60 fps 采样率。", "合并重复关键帧", 148, "96 帧 / 60 fps"),
                new Asset(10, 2, "pet002_jump.anim", "48 帧 / 30 fps", 76),
                new Asset(11, 2, "pet002_body.fbx", "18.4k tris", 2480),
                new Asset(12, 2, "pet002_face.fbx", "4.2k tris", 896, Issue.Manual, "材质引用缺失", "Face_Mat 引用为空，需要美术确认应使用的材质。", "人工指定材质"),
                new Asset(13, 2, "pet002_fur_albedo.png", "2048×2048", 4096),
                new Asset(14, 3, "scene_lobby_floor.png", "2048×2048", 4096),
                new Asset(15, 3, "scene_lobby_wall.png", "4096×4096", 16384, Issue.Manual, "大贴图警告", "贴图尺寸超出此示例的 2048 建议值。需确认近景精度，不自动降低分辨率。", "人工确认精度"),
                new Asset(16, 3, "scene_garden_tree_a.fbx", "8.6k tris", 1536),
                new Asset(17, 3, "scene_garden_tree_b.fbx", "7.9k tris", 1408),
                new Asset(18, 3, "scene_garden_lightmap_01.exr", "2048×2048", 32768, Issue.Automatic, "未压缩", "示例采用可压缩的 HDR 光照贴图配置，模拟改为 BC6H。", "BC6H / HDR", 4096),
                new Asset(19, 3, "scene_garden_lightmap_02.exr", "2048×2048", 32768, Issue.Conflict, "压缩设置冲突", "此项示例目标格式为 BC1，但源配置要求保留 HDR。两项策略不兼容，阻止自动应用。", "BC1 不支持 HDR"),
                new Asset(20, 4, "bgm_lobby.ogg", "02:18 / Vorbis", 3480, Issue.Automatic, "导入策略提示", "长音乐示例使用 DecompressOnLoad，建议模拟调整为 Streaming；磁盘大小不变。", "Streaming", 3480),
                new Asset(21, 4, "bgm_garden.ogg", "03:42 / Streaming", 5620),
                new Asset(22, 4, "ui_confirm.wav", "0.24 s / PCM", 42),
                new Asset(23, 4, "ui_reward.wav", "1.80 s / PCM", 318)
            });
        }

        private enum Issue { Normal, Automatic, Manual, Conflict }
        private enum ButtonRole { Normal, Quiet, Primary, Tab }
        private sealed class Asset
        {
            public readonly int id, group, targetKB;
            public readonly string name, path, type, glyph, target, targetSpec;
            public int sizeKB;
            public string spec, status, reason, before;
            public Issue state;
            public string Destination => before != null || state == Issue.Automatic ? target : "保持不变";
            public string Verdict => before != null ? "已应用" : state == Issue.Automatic ? "待模拟" : state == Issue.Conflict ? "阻止" : "跳过";
            public string PlanReason => before != null ? "仅更新内存" : state == Issue.Automatic ? "可自动处理" : state == Issue.Normal ? "无需处理" : target;
            public Asset(int id, int group, string name, string spec, int sizeKB, Issue state = Issue.Normal,
                string status = "正常", string reason = "示例规格符合预设要求；不是实际工程扫描结果。", string target = "", int targetKB = 0, string targetSpec = null)
            {
                this.id = id; this.group = group; this.name = name; this.spec = spec; this.sizeKB = sizeKB;
                this.state = state; this.status = status; this.reason = reason; this.target = target;
                this.targetKB = targetKB; this.targetSpec = targetSpec;
                string[] folders = { "Art/UI/Release/2026/SummerLimited", "Art/Characters/Pet002/Release", "Art/Scenes/Release/GardenAndLobby", "Audio/Release" };
                path = $"Assets/{folders[group - 1]}/{name}";
                type = name.EndsWith(".anim") ? "AnimationClip" : name.EndsWith(".fbx") ? "Model" : group == 4 ? "AudioClip" : "Texture2D";
                glyph = type == "AnimationClip" ? "动" : type == "Model" ? "模" : type == "AudioClip" ? "音" : "图";
            }
        }
        private sealed class Theme
        {
            public readonly Color navigation, surface, control, hover, pressed, edge, text, muted, disabled;
            public readonly Color selection, selectionHover, accent, accentHover, accentPressed, onAccent, warning, danger;
            public Theme(bool light)
            {
                Color Pick(string dark, string paper)
                {
                    ColorUtility.TryParseHtmlString("#" + (light ? paper : dark), out var color);
                    return color;
                }
                navigation = Pick("1C1E1F", "F0F0EE"); surface = Pick("242627", "FCFCFA");
                control = Pick("2D3031", "FFFFFF"); hover = Pick("343738", "E9EEEA");
                pressed = Pick("3E4241", "DCE4DD"); edge = Pick("424646", "D6DCD7");
                text = Pick("E8EBE9", "27312C"); muted = Pick("B5BDB8", "626F66"); disabled = Pick("768079", "8D9890");
                selection = Pick("293F37", "E1EEE6"); selectionHover = Pick("344F44", "D5E6DA");
                accent = Pick("A0D5BB", "326C50"); accentHover = Pick("B3E2C9", "3B7A5B");
                accentPressed = Pick("88BDA3", "285B42"); onAccent = Pick("19372A", "FFFFFF");
                warning = Pick("E2C185", "885813"); danger = Pick("F0A59C", "AD4037");
            }
        }
    }
}
