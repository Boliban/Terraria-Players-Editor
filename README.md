# Terraria Players Editor

> 一个现代化的 Terraria 玩家存档 (.plr) 编辑器，基于 .NET 10 WinForms 构建。

## 功能

- **玩家信息**：编辑名称、难度、游戏时间、文件版本、当前负载
- **属性**：生命/魔力（当前与最大值）、死亡计数、税收、渔夫任务、高尔夫分数
- **外观**：发型/发色、肤色、7 种颜色选择器、可见性开关
- **物品**：网格物品栏（背包/装备/存储），支持搜索/筛选/图标动画
- **增益**：44 栏位增益编辑，类型 ID 与持续时间
- **加成与杂项**：永久升级、信息配件显示、冷却时间
- **重生点**：世界 ID/名称与坐标管理
- **内存修改**：直接读写游戏内存，查找游戏进程，通过指针链定位实时 Player 对象并可视化编辑物品栏/装备/银行（偏移表来自 `DemoFile/Terraria-Player.CSX`，支持自定义）
- **暗色模式**：亮色/暗色主题一键切换
- **中英文双语**：运行时语言切换

## 技术栈

- **.NET 10.0** (WinForms)
- 纯 GDI+ 自定义渲染（无第三方 UI 库）
- 嵌入式 JSON 资源（物品/增益图标、本地化文本）
- 自定义动画引擎（60fps 缓动过渡）
- 集中式主题系统（亮色/暗色）

## 快速开始

```bash
# 克隆仓库
git clone https://github.com/Boliban/Terraria-Players-Editor.git

# 构建
cd Terraria-Players-Editor
dotnet build

# 运行
dotnet run
```

## 使用说明

1. **文件 → 打开** 选择一个 `.plr` 文件（通常在 `Documents/My Games/Terraria/Players/`）
2. 在各个标签页中编辑玩家数据
3. **文件 → 保存** 或 **另存为** 保存修改

## 项目结构

```
Terraria Players Editor/
├── Program.cs                  # 应用入口
├── Forms/
│   └── MainForm.cs             # 主窗体（8个标签页）
├── Controls/
│   ├── SlotPanel.cs            # 物品格子控件
│   ├── SlotGrid.cs             # 格子网格容器
│   ├── ItemBrowser.cs          # 物品搜索浏览器
│   ├── ItemModifier.cs         # 物品属性修改器
│   └── FlatGroupBox.cs         # 扁平化分组容器
├── Models/                     # 数据模型
│   ├── PlayerData.cs
│   ├── PlayerStats.cs
│   ├── PlayerAppearance.cs
│   ├── ItemData.cs
│   └── ...
├── Services/                   # 业务逻辑
│   ├── PlrFileReader.cs        # .plr 文件读取
│   ├── PlrFileWriter.cs        # .plr 文件写入
│   ├── PlrCrypto.cs            # 加密/解密
│   ├── IconService.cs          # 图标加载与缓存
│   ├── ItemDatabase.cs         # 物品数据库
│   ├── ThemeManager.cs         # 主题系统
│   ├── AnimationEngine.cs      # 动画引擎
│   └── ...
├── Data/                       # 嵌入式资源
│   ├── items.json              # 物品数据
│   ├── buffs.json              # 增益数据
│   └── Locale/                 # 本地化 (EN/ZH)
└── .gitignore
```

## 许可证

MIT License
