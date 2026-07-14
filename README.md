<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 8">
  <img src="https://img.shields.io/badge/WPF-Windows-blue?style=for-the-badge&logo=windows" alt="WPF">
  <img src="https://img.shields.io/badge/AI-DeepSeek-4A90D9?style=for-the-badge" alt="DeepSeek">
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT">
</p>

<h1 align="center">🐾 DesktopPet — 桌面宠物 AI 助手</h1>

<p align="center">
  <b>一只趴在桌面上的 Q 版机器人，能聊天、会说话、还能帮你操控电脑 🎮</b>
</p>

<p align="center">
  <sub>Built with C# + WPF + .NET 8 | Powered by DeepSeek AI</sub>
</p>

---

## ✨ 它是什么

**DesktopPet** 是一个 Windows 桌面伴侣程序——一个可爱的 Q 版机器人角色会常驻在你的桌面上。它接入了 DeepSeek 大语言模型，你可以通过**文字聊天**或**语音**和它对话，它还能听懂你的指令帮你操控电脑。

```
   ┌───◆───┐    ← 发光天线
   │ ╭───╮ │    ← 蓝色头部
   │ │◉ ◉│ │    ← 会眨眼的大眼睛
   │ │ ╰╯ │ │    ← 微笑嘴巴
   │ ╰───╯ │
  ┌┴───────┴┐   ← 身体
  │  ▐▌▐▌  │   ← 胸前状态灯
  │  ┃  ┃  │   ← 会摇摆的手臂
  └─────────┘
```

---

## 🎯 核心功能

### 🤖 桌面宠物
- **常驻桌面**：透明悬浮窗口，不遮挡正常操作（鼠标穿透）
- **生动动画**：呼吸缩放、随机眨眼、天线摇摆、手臂摆动、桌面闲逛
- **可拖拽**：鼠标按住即可拖到屏幕任意位置，自动记忆位置
- **系统托盘**：最小化到托盘，右键菜单快捷操作

### 🧠 AI 智能对话
- **DeepSeek 驱动**：接入 DeepSeek 大模型，流式输出回复（打字机效果）
- **对话气泡**：回复以气泡形式在机器人旁弹出，带有弹入弹出动画
- **聊天窗口**：完整的对话窗口，支持查看历史记录
- **角色人设**：可自定义机器人的性格、名字和说话风格
- **知识库**：告诉它关于你的信息，它会记住并在对话中提及

### 🎤 语音交互
- **语音输入**：说话直接转文字发送给 AI
- **语音播报**：AI 回复通过 TTS 朗读出来
- **一键唤醒**：`Ctrl+Shift+Q` 快捷键触发语音
- **状态指示**：机器人天线变色表示正在听/正在说

### 🖥️ 电脑操控
AI 可以帮你完成这些操作：

| 类型 | 指令示例 |
|------|---------|
| 🔈 音量 | "音量加大" "静音" |
| 📂 应用 | "打开浏览器" "打开记事本" |
| 🔍 搜索 | "搜索今天天气" |
| 🔒 系统 | "锁屏" "截图" |
| 📋 剪贴板 | "复制你好" "粘贴" |

---

## 🏗️ 项目架构

```
src/DesktopPet/
├── App.xaml/.cs                 # 入口：系统托盘 + 全局快捷键
├── MainWindow.xaml/.cs          # 主窗口：悬浮穿透 + 拖拽 + AI/语音联动
├── Models/
│   ├── AppConfig.cs             # 配置模型（JSON 持久化）
│   └── ChatMessage.cs           # 聊天消息模型
├── Controls/
│   ├── PetRobot.xaml/.cs        # Q版机器人：纯代码绘制 + 5种动画
│   └── ChatBubble.xaml/.cs      # 对话气泡：弹入弹出 + 打字动画
├── Windows/
│   ├── ChatWindow.xaml/.cs      # 完整聊天窗口（历史记录）
│   └── SettingsWindow.xaml/.cs  # 设置窗口（API Key / 人设）
└── Services/
    ├── AiService.cs             # DeepSeek API（OpenAI 兼容 / SSE 流式）
    ├── VoiceService.cs          # Windows 语音识别 + TTS
    ├── ControlService.cs        # 电脑操控（模拟按键 / 进程启动）
    └── ConfigService.cs         # 配置读写（JSON）
```

### 技术栈

| 技术 | 用途 |
|------|------|
| **.NET 8** | 运行时框架 |
| **WPF** | 桌面 UI（透明窗口、动画渲染） |
| **System.Speech** | Windows 内置语音识别和 TTS |
| **DeepSeek API** | AI 对话引擎（OpenAI 兼容） |
| **SQLite** | 可选的对话历史存储 |
| **P/Invoke** | 桌面穿透、全局热键、系统操控 |

---

## 🚀 快速开始

### 环境要求

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- DeepSeek API Key（[免费注册](https://platform.deepseek.com)）
- 语音功能需要安装中文语音包（Win10/11 设置 → 语音 → 添加中文语音）

### 下载运行

```bash
# 克隆项目
git clone https://github.com/你的用户名/DesktopPet.git
cd DesktopPet

# 编译运行
dotnet run --project src/DesktopPet
```

### 首次配置

1. 启动后桌面右下角出现 Q 版机器人
2. 右键托盘图标 → **设置**
3. 填入你的 **DeepSeek API Key**
4. （可选）设置你的名字、机器人名字、个人简介

### 使用方式

| 操作 | 方法 |
|------|------|
| 和机器人聊天 | 右键机器人 → 输入消息 |
| 语音对话 | 按 `Ctrl+Shift+Q` → 说话 |
| 移动机器人 | 鼠标按住拖拽 |
| 隐藏/显示 | 双击托盘图标 |

---

## 🎨 机器人动画

机器人完全使用 WPF 几何图形绘制，**无需任何图片资源**：

| 动画 | 效果 |
|------|------|
| 呼吸 | 整体 1.0x ↔ 1.03x 缓慢缩放 |
| 眨眼 | 随机 2-5 秒间隔快速闭合眼睛 |
| 天线 | ±4° 摇摆 + 顶端光球脉冲发光 |
| 手臂 | 左右手臂交替钟摆式微摆 |
| 游走 | 每 6 秒在当前位置小范围随机移动 |
| 交互 | 鼠标按住时弹性压缩，松开时弹回 |

---

## 📝 配置说明

配置文件位置：`%LocalAppData%\DesktopPet\config.json`

```json
{
  "ApiKey": "sk-xxxxxxxx",
  "ApiBaseUrl": "https://api.deepseek.com",
  "ModelName": "deepseek-chat",
  "RobotName": "小Q",
  "UserName": "主人",
  "UserProfile": "主人是一名程序员，喜欢喝咖啡",
  "Temperature": 0.7
}
```

---

## 📦 项目结构总览

```
DesktopPet/
├── DesktopPet.sln
├── README.md
└── src/
    └── DesktopPet/
        ├── App.xaml                # 应用入口定义
        ├── App.xaml.cs             # 托盘/快捷键/服务初始化
        ├── MainWindow.xaml         # 主窗口布局
        ├── MainWindow.xaml.cs      # 主窗口逻辑
        ├── GlobalUsings.cs         # 全局命名空间
        ├── DesktopPet.csproj       # 项目配置
        ├── Controls/
        │   ├── PetRobot.xaml       # 机器人视觉定义
        │   ├── PetRobot.xaml.cs    # 机器人动画逻辑
        │   ├── ChatBubble.xaml     # 对话气泡视觉
        │   └── ChatBubble.xaml.cs  # 对话气泡逻辑
        ├── Windows/
        │   ├── ChatWindow.xaml     # 聊天窗口布局
        │   ├── ChatWindow.xaml.cs  # 聊天窗口逻辑
        │   ├── SettingsWindow.xaml # 设置窗口布局
        │   └── SettingsWindow.xaml.cs
        ├── Models/
        │   ├── AppConfig.cs        # 应用配置模型
        │   └── ChatMessage.cs      # 聊天消息模型
        └── Services/
            ├── AiService.cs        # AI 对话服务
            ├── VoiceService.cs     # 语音服务
            ├── ControlService.cs   # 电脑操控服务
            └── ConfigService.cs    # 配置管理服务
```

---

## 🔮 未来计划

- [ ] 多角色支持（猫、狗、自定义形象）
- [ ] Azure Speech 唤醒词（"嘿小助手"）
- [ ] 本地 LLM 支持（Ollama）
- [ ] 定时提醒 / 番茄钟
- [ ] 插件系统
- [ ] 更多电脑操控指令

---

## 📄 License

MIT © 2026

---

<p align="center">
  <sub>Made with ❤️ and ☕ | 如果你喜欢这个项目，请给个 ⭐ Star！</sub>
</p>
