# 开发与维护

项目保持 Windows Forms、.NET Framework 4 和单文件发布方式。使用 Windows 自带的 C# 编译器，不需要安装 NuGet 包。脚本需要 Windows PowerShell 5.1 或 PowerShell 7；C# 代码保持系统编译器支持的语法，不使用新版本语言特性。

## 编译和验证

在项目根目录运行：

```powershell
.\test.ps1
.\build.ps1
.\benchmark-crackers.ps1 -Group fast
```

也可以从其他目录通过脚本的完整路径运行。测试会临时切换到仓库根目录读取样本，并恢复调用者的目录。测试输出 `RUN` 分组、最终 `PASS` 计数；失败时输出异常和非零退出码。

开发时可把程序写到临时目录，避免改动仓库中的发布文件：

```powershell
.\build.ps1 -OutputDirectory .\work\build
```

`scripts/Compile.ps1` 是编译配置的唯一入口：递归收集 `src/ClassicalCipherToolbox` 下的 C# 文件，统一引用、资源和编译选项，通过入口类选择程序、测试或基准。新增源文件不需要再修改多个脚本。新增内嵌数据只需在 `scripts/Resources.psd1` 登记资源名称与路径；名称必须与读取数据时使用的资源名一致。

`work/` 只存放可重新生成的文件。需要长期使用的测试、基准源码和样本必须放到版本管理目录。

## 代码位置

| 位置 | 职责 |
| --- | --- |
| `Core/ICryptoTool.cs`、`ToolRequest.cs`、`ToolParameter.cs` | 工具接口、执行请求和参数元数据 |
| `Core/ToolRegistry.cs` | 汇总工具、复用注册辅助方法 |
| `Core/ToolRegistry.Classical.cs` | 古典密码的参数和执行绑定 |
| `Core/ToolRegistry.Encoding.cs`、`.Chinese.cs`、`.Analysis.cs` | 编码、中文、通用及分析工具的注册 |
| `Core/ToolRegistry.Conversions.cs` | 格雷码、进制换算与碱基翻译的注册 |
| `Ciphers/ExactNumber.cs`、`RadixConversion.cs`、`MachineDataEncoding.cs` | 精确有理数、2–36 进制小数与计算机数值位模式 |
| `Core/ToolRegistry.Parameters.cs`、`.Ordering.cs` | 公共参数定义、分类与常用程度排序 |
| `Ciphers/` | 加解密、编码和中文工具的实现 |
| `Analysis/` | 识别、评分、搜索及通用破解调度 |
| `CipherForm.cs` | 主窗口构造、工具选择和基础交互 |
| `CipherForm.Parameters.cs` | 模式选择、参数控件及编辑器入口 |
| `CipherForm.Execution.cs` | 后台执行、实时调度、取消和进度 |
| `CipherForm.Candidates.cs` | 候选解析、显示和工具导航 |
| `CipherForm.Files.cs` | 文件打开、拖放和批量输入 |
| `CipherForm.Controls.cs` | 控件创建与布局辅助方法 |
| `CipherForm.Dialogs.cs` | 字母表、长文本、已知明文对话框 |
| `CipherForm.Semaphore.cs`、`SemaphorePreview.cs` | 旗语图形接入和绘制，映射复用 `Ciphers/SemaphoreCode.cs` |
| `ToolDocumentation.cs`、`Core/ToolTags.cs` | 工具帮助和标签 |
| `tests/`、`benchmarks/` | 回归测试及破解效果基准 |

以上源码路径均相对于 `src/ClassicalCipherToolbox`，测试和基准目录除外。窗口与注册表使用 `partial` 按职责分文件，仍然是同一个类型；不是独立插件，也不意味着状态已隔离。新增方法放到对应职责文件，避免把所有内容重新加回入口文件。

## 添加或修改工具

1. 在 `Ciphers/` 或 `Analysis/` 实现逻辑；通过 `ToolRequest` 读取参数、报告进度及响应取消，算法层不访问窗口控件。
2. 在对应的 `ToolRegistry.*.cs` 注册名称、分类、模式、参数与执行函数。复用公共语言、评分方法和搜索参数定义。
3. 更新工具说明和标签；如需调整默认位置，修改 `ToolRegistry.Ordering.cs`。
4. 如果需要接入自动识别和通用破解，检查 `CipherIdentifier`、`UniversalCracker` 的支持列表和路由规则。需要显示破解进度时，检查 `CipherForm.Execution.cs` 的 `SupportsProgress`。
5. 在 `tests/` 添加已知输入输出、参数错误及相关交互的回归验证。修改工具数量时，同步更新注册表数量断言和 README。

当前工具名称同时被导航、识别和测试用作标识。改名时应搜索全部引用。候选输出仍是约定格式的文本，修改 `#`、`明文：` 等格式时，需要同时检查窗口、通用破解器与基准解析，不能只改显示文字。

## 后台执行约定

窗口使用 `executionVersion` 标识当前请求。输入或参数变化、取消时使旧版本失效；回调必须检查版本，旧结果不能覆盖新输入。`workRunning` 与 `rerunPending` 控制旧任务退出后重启。修改这部分时保留取消检查以及通过 UI 线程更新控件的行为。

## 破解基准的含义

基准源码保存在 `benchmarks/CrackerBenchmark.cs`，编译产物写入 `work/benchmarks/`。支持 `fast`、`medium`、`deep`、`diag`、`tune`、`period`、`homo`、`quick` 分组。部分分组计算量很大，日常先运行 `fast`。

输出包含首候选是否完全匹配、所有返回候选中是否命中、字符位置相似度、命中排名及耗时。它沿用固定英文样本和固定密钥，主要用于比较代码变更前后表现，不能代表真实题目的总体成功率，也不覆盖中文破解质量。

基准发生执行异常或没有选中任何用例时返回失败；有候选但未恢复明文时记录未命中，不将其伪装成程序异常。回归测试中的候选冒烟检查也不等于破解成功率验收。

## 重构边界

优先保持工具参数、排序、输出格式和加解密结果不变。算法或评分改进单独进行，并附带效果对比。重构验证通过前，不覆盖已发布二进制、不更改版本号；发布时再按 README 的流程测试、编译和更新版本。
