# 密码箱

版本 1.1.8。离线 Windows 古典密码工具，交付为单个 `密码箱.exe`。

共 107 个工具，其中 58 个可破解。分类：通用、编码、替换、多表、换位、方阵；统计分析已并入通用。

## 使用

1. 默认在「通用 → 通用破解」。粘贴密文，或先选分类、标签，再选工具。`不限标签` 恢复完整列表。
2. 选择加密 / 解密 / 编码 / 解码 / 破解 / 分析，填写当前显示的参数。
3. 停止输入后自动处理。长搜索在底部显示进度，点 `×` 取消。
4. 候选出现后，单击查看全文，双击进入对应工具。`识别`、`通用`会保留输入并在相关功能间切换。
5. `?` 直接打开当前工具的说明。`Ω` 设置 26 字符字母表，以及大小写、空白、标点、I/J 合并、变音符号。

「打开」可多选文本文件，也可把文件拖入窗口。多文件按文件名分段处理。Book Cipher、Nomenclator、Running Key 和 VIC 的长文本参数可双击或点击 `…`，在可缩放窗口中编辑、粘贴或按所选字符集打开文件。

**通用破解**

- 语言不确定时用 `AUTO`；中文原文或中文编码用 `ZH`。
- 强度：`快速` / `标准` / `深入`。一般先用标准。
- 线索可留空。点击 `线索` 后，算法和已知明文分开填写；程序自动组成通用破解需要的格式。
- 识别器排名前三的密码家族会自动加入当前搜索；高匹配家族会保留更多内部候选。
- 首个候选显示本轮识别器的首位判断；双击候选可继续进入对应专用工具。

**维吉尼亚破解**可填最短/最长密钥长度、已知长度、部分密钥（如 `LE?ON`）和已知明文。带 Crib 的工具可点击 `明文`，在可缩放窗口内编辑连续明文片段。  
**单表替换**加解密时可直接粘贴完整 26 字母替换表，也可点击 `…` 按 A–Z 的位置逐格填写。破解时可锁定映射，如 `X=E` 或 `Ж=E`；语言 `ZH` 可恢复被单表替换的四位 Unicode 十六进制中文，以及无声调拼音。
**Enigma 破解**需要已知明文片段，可搜索初始位置，可选搜索转子顺序。

搜索型破解器可选择 `自动`、`模拟退火`、`爬山`、`延迟接受`、`再加热退火`。模拟退火适合从未知密钥开始；爬山速度较快；延迟接受有利于越过评分平台；再加热退火适合较长、局部最优较多的搜索。

语言：`AUTO`、`ZH`、`EN`、`FR`、`DE`、`ES`、`IT`、`PT`、`NL`、`SV`、`PL`、`TR`。  
识别器和分析工作台还可选匹配方法：`AUTO`、`COSINE`、`LLR`、`CHI`、`NGRAM`。

## 工具

**通用**  
通用破解、密码识别器、分析工作台、频率、重合指数、N-gram、Kasiski、Crib 工具

**编码**  
自动解码、Base64、Base64URL、Base32、Base58、ASCII85、十六进制、二进制、URL 编码、Unicode 转义、HTML 实体、Quoted-Printable、Punycode、字符集字节（UTF-8 / UTF-16 / GB18030 / Big5 / Shift_JIS）、Morse、A1Z26、Tap Code、盲文（英语一级）、博多码 ITA2、中文电报码、北约音标字母、猪圈密码符号、旗语、条形码（Code 39 / EAN-13）、QR Code、颜色编码、取色器与调色盘

**替换**  
凯撒、ROT13、ROT-N、Atbash、仿射、培根、单表替换、同音替换、Keyword Cipher、Multiplicative、Vatsyayana、Grandpré、Nomenclator、Book Cipher

**多表**  
维吉尼亚、Beaufort、Variant Beaufort、Autokey、Porta、Gronsfeld、Running Key、Trithemius、渐进凯撒、Alberti、Bellaso、Ragbaby、Jefferson Wheel、Quagmire I–IV、Gromark、Periodic Gromark、Chaocipher、Solitaire、Nicodemus、Enigma（I / M3 / M4）

**换位**  
栅栏、Redefence、Scytale、Caesar Box、列换位、Myszkowski、双重列换位、路线换位、AMSCO、Turning Grille、Ubchi、Swagman、Cadenus、扰乱式换位、Reverse

**方阵**  
Polybius、Playfair、Hill 2×2、Hill 3×3、Bifid、Trifid、Digrafid、ADFGX、ADFGVX、Four-square、Two-square、Three-square、Nihilist、Bazeries、Fractionated Morse、Morbit、Pollux、跨行棋盘、Phillips、VIC

通用中的分析工具按字符统计，支持中文及其他非拉丁文字。工作台给出频率、N-gram、重合指数、熵、文字体系和周期 IC。

## 版本记录

- 1.1：加入标签筛选，并保留分类内的常用程度排序。
- 1.1.4：打通识别器、通用破解和专用工具；扩展二维码、条形码、博多码、中文电报码、传输编码、颜色与符号格式识别。
- 1.1.5：搜索型破解器加入多种可选启发式策略。
- 1.1.6：加入已知明文/线索编辑窗口，算法提示与明文分栏填写。
- 1.1.7：加入可读取文件的长文本参数编辑器；常用选项改为选择框；通用破解按识别前三项调度并扩大高匹配家族的候选保留；重型实时任务改为串行取消与重启。
- 1.1.8：验收并合并 1.1.7 的交互修复；扩大参数控件；单表替换支持整表粘贴和 26 位逐格编辑；每个工具的 `?` 直达当前文档。

## 编译

需要 Windows 自带的 .NET Framework 4 C# 编译器。

```powershell
.\test.ps1
.\build.ps1
```

输出：`outputs\密码箱.exe`
