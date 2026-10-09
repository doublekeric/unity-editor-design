# 来源与许可

本 skill 是面向 Unity 编辑器工具的新写作，示例 C# 为原创实现；不是把网页 SKILL.md 全文或第三方组件库拼接起来。下列资料提供了通用设计方法、技术约束或启发。

## 设计 skill

- [Anthropic · frontend-design](https://github.com/anthropics/skills/blob/main/skills/frontend-design/SKILL.md)，[该目录许可](https://github.com/anthropics/skills/blob/main/skills/frontend-design/LICENSE.txt)：Apache-2.0。参考真实内容优先、视觉层级、克制的动效和自我批评方法，不引入网页 Hero 或品牌展示规范。
- [MengTo · no-ai-design-slop](https://github.com/MengTo/Skills/blob/main/agent-skills/ui/no-ai-design-slop/SKILL.md)，[仓库许可](https://github.com/MengTo/Skills/blob/main/LICENSE)：MIT。参考尊重已有视觉系统、用距离与层级分组、判断设计是否服务任务，而非机械禁止某种字体或颜色。
- [Google Labs · taste-design](https://github.com/google-labs-code/stitch-skills/blob/main/plugins/stitch-utilities/skills/taste-design/SKILL.md)，[仓库许可](https://github.com/google-labs-code/stitch-skills/blob/main/LICENSE)：Apache-2.0。仅参考主动定义密度、色彩角色和组件状态；不迁移其高不对称、Hero、CSS Grid、字体黑名单或持续微动效要求。

## 设计资料

- [Unity Foundations](https://www.foundations.unity.com/)：编辑器排版、内容组织和工具条规范。
- [Unity UI Toolkit 文档](https://docs.unity3d.com/2022.3/Documentation/Manual/UIElements.html)：目标版本 API 和 USS 支持范围。
- [Linear UI redesign](https://linear.app/now/how-we-redesigned-the-linear-ui)：桌面应用结构与表面分层。
- [Radix Colors 色阶角色](https://www.radix-ui.com/colors/docs/palette-composition/understanding-the-scale)：背景、交互状态、边界与文字的语义分配。本仓库色板为独立配制，不复制其 CSS 或声称具有其对比保证。

这些项目不与本仓库构成官方合作或背书。外部资料可能更新；重新使用其原文或代码时应核对当时的许可证。

## 本仓库与第三方素材

仓库和随附 skill 原创文件使用 [MIT](../LICENSE)。未附上述项目的原始 SKILL.md 或第三方代码。

Unity 内置类型图标由运行时 Editor API 获取，截图展示它们作为编辑器示例的一部分；本仓库不单独提取/分发图标素材。Microsoft YaHei UI 由本机系统加载，未附字体文件或可分发字库图集，其许可不受本仓库 MIT 覆盖。其他平台回退到 Editor 字体。

未来若加入上游原文、实质改写的代码或素材，应另行保留相关版权、完整许可和必要 NOTICE；仅放 GitHub 链接不能替代许可义务。
