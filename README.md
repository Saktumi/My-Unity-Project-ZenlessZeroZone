# My Unity Project - Zenless Zero Zone Demo

基于 Unity 6 的第三人称动作战斗 demo：包含移动 / 连击 / 闪避 / 特殊技 / 终结技演出、
敌人 AI、失衡破防与 QTE 连携、队伍切换，以及大厅侧的登录 / 聊天 / 抽卡图鉴。

> 本仓库为代码优先版本，未收录大体积角色模型 / 动画 / 第三方特效素材。
> 完整可运行工程可联系作者获取。

## 内容

- `Assets/Script/`：全部游戏逻辑代码（战斗、AI、UI、网络），详见 [Assets/Script/README.md](Assets/Script/README.md)
- `Assets/Scenes/`：Boot（登录）、Lobby（大厅）、SampleScene（战斗）
- `Assets/Settings/`、`Assets/ScriptableOB/`：渲染与数值配置

## 运行

1. 使用 Unity 6000.3.x 打开工程。
2. 打开 `Assets/Scenes/Boot.unity` 或 `SampleScene.unity` 点击 Play。
3. 测试账号：`test / 123456`。

由于模型 / 动画素材未包含在仓库中，克隆后场景中的角色资源引用会缺失，
战斗系统的代码逻辑完整可读；如需可运行的完整工程请联系作者。
