# unity-editor-design

面向 **Unity 编辑器工具界面**的 Agent Skill：不只是生成能用的 EditorWindow，而是把字体、信息层级、空间、配色和交互状态一起设计、渲染、审查。

适用于 UI Toolkit / USS、自定义 Inspector、资源列表和批处理工具，也支持对现有 IMGUI 做有限视觉改善。不是游戏运行时 UI 生成器，不会要求项目先重构技术栈。

> 实验性设计辅助，不保证“用了就更好看”。小样本有/无 skill 对照尚未证明稳定审美增益；真实渲染和修订仍是必要步骤。详见 [验证范围](skills/unity-editor-design/references/validation.md)。

## 实机示例

同一批24条假数据，三栏资源审查台、混合多选、问题详情和批处理冲突。截图来自 Unity 2022.3.62f2，不是网页仿图。

### 纸白

![纸白资源审查台](skills/unity-editor-design/examples/complex/screenshots/review-light.png)

### 石墨

![石墨资源审查台](skills/unity-editor-design/examples/complex/screenshots/review-dark.png)

[全部示例与运行说明](skills/unity-editor-design/examples/complex/README.md) · [批处理冲突截图](skills/unity-editor-design/examples/complex/screenshots/batch-conflicts.png)

## 安装到用户级

### 方式一：Skills CLI

```bash
npx skills add doublekeric/unity-editor-design --skill unity-editor-design -g
```

`-g` 表示用户级，不写入当前项目；按 CLI 提示选择使用的 Agent。各客户端的安装位置与自动触发行为以其当前版本为准。

### 方式二：克隆后链接（适合维护自己的仓库）

Windows PowerShell：

```powershell
git clone https://github.com/doublekeric/unity-editor-design.git
cd unity-editor-design
powershell -NoProfile -File .\scripts\install-user.ps1
```

默认建立目录联接：

```text
~/.agents/skills/unity-editor-design
    → <克隆目录>/skills/unity-editor-design
```

这是用户级安装，供所有项目使用；`git pull` 更新仓库后即可得到最新 skill。不要移动或删除源仓库，否则链接会失效。脚本遇到现有目标会停止，不会覆盖未知文件。

如果希望安装独立副本：

```powershell
powershell -NoProfile -File .\scripts\install-user.ps1 -Mode Copy
```

复制安装不会随仓库自动更新。更新前先检查并备份旧目录，再复制新版；脚本不会自动删除它。

macOS / Linux 可手动链接：

```bash
mkdir -p ~/.agents/skills
ln -s "$PWD/skills/unity-editor-design" ~/.agents/skills/unity-editor-design
```

上述命令从克隆仓库根目录执行；目标已存在时先检查，不使用强制覆盖。

### 已有项目级版本怎么办？

不要长期维护两个同名、不同内容的版本。核对客户端的发现顺序，确认用户级可读后，把旧项目级 skill 移到技能扫描目录之外备份；不要只在原目录内改个文件夹名，因为 `name` 仍可能冲突。自动同步出来的副本应通过项目原有同步机制处理，不直接手改生成文件。

ZCode 可在 Skills 设置里检查加载来源；安装后开一个新会话再调用。其它客户端需要支持 `~/.agents/skills` 或使用其相应安装方式。

## 使用

显式调用（客户端支持 slash skills 时）：

```text
/unity-editor-design 优化这个资源工具的字体、布局和状态，不改业务逻辑。
```

也可以描述任务：

- “这个 EditorWindow 太像默认控件拼起来的了，先给设计建议。”
- “做一个中英混排的资源批处理预览，深浅配色都要可读。”
- “改善现有 IMGUI Inspector，但保留序列化、Undo 和导入逻辑。”

Skill 会区分建议/审查与实现任务。只要建议时不擅自改代码；实现时保留业务行为，并要求看实际界面再评审。

## 包含什么

```text
skills/unity-editor-design/
├── SKILL.md
├── LICENSE
├── references/
│   ├── visual-direction.md
│   ├── type-and-color-recipes.md
│   ├── typography-and-states.md
│   ├── unity-adaptation.md
│   ├── validation.md
│   └── sources.md
└── examples/complex/
    ├── AssetReviewWindow.cs
    ├── ReviewFont.cs
    ├── README.md
    └── screenshots/
```

Skill 本体不依赖 Unity MCP、Pipeline、浏览器插件或本仓库以外的脚本。示例用两个 C# 文件即可在 Unity 编辑器里运行；测试过的版本是 2022.3.62f2，其它版本需复核 API。

## 设计重点

- 原生交互能力不等于必须使用默认外观。
- 字体需检查真实中英混排、字重、字面与缩放，不只修改字号。
- 中性背景、有限表面层次、小面积强调；颜色表达状态，不是装饰。
- 深浅配色、窄窗口、长文本、多选、空态和冲突都要检查。
- 独立审美 review 与编译/功能检查分开；“无遮挡”不等于“好看”。

## 许可与来源

[MIT License](LICENSE)。通用设计方法参考 Anthropic、MengTo、Unity Foundations、Radix 等，完整说明见 [sources.md](skills/unity-editor-design/references/sources.md)。

不分发系统字体。示例在可用时从本机加载 Microsoft YaHei UI；没有该字体时继承编辑器字体。Unity 内置图标由 Editor API 获取，它们及字体不因本仓库 MIT 许可而重新授权。
