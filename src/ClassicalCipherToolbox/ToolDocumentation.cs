using System.Text;
using ClassicalCipherToolbox.Core;

namespace ClassicalCipherToolbox
{
    internal static class ToolDocumentation
    {
        internal static string GetSummary(string name)
        {
            switch (name)
            {
                case "通用破解": return "自动筛选适用破解器，以两个后台工作槽分阶段试解，并按经长度校准的语言分、结构匹配度和综合分排列候选。";
                case "密码识别器": return "统一分析字符签名、数字分组、文本结构、可选语言匹配方法、重合指数、周期和实际试解结果，按匹配分排列密码类型。";
                case "Crib 工具": return "把已知明文滑过密文，推导凯撒、维吉尼亚密钥片段与单表映射。";
                case "凯撒": return "固定字母位移；破解模式列出全部 26 种结果并评分。";
                case "ROT13": return "字母旋转 13 位，执行两次即可还原。";
                case "Atbash": return "将字母表首尾反向对应，无需密钥。";
                case "维吉尼亚": return "重复关键词多表位移；破解综合 IC、Kasiski 和分列频率恢复密钥。";
                case "仿射": return "使用 ax+b 模 26 替换；破解模式自动尝试所有可逆参数。";
                case "ROT-N": return "支持普通 ROT-N、ROT5/18/47；破解枚举并按语言评分。";
                case "栅栏": return "锯齿路线换位；破解自动尝试 2–30 栏。";
                case "列换位": return "按关键词字母顺序重排列；破解枚举 2–9 列的读取顺序并按语言模型评分。";
                case "Polybius": return "将字母映射为 5×5 方阵坐标，I/J 合并。";
                case "培根": return "将字母编码成五位 A/B 序列。";
                case "单表替换": return "支持拉丁或最多 26 个 Unicode 密文符号；语言 ZH 可恢复被单表替换的四位 Unicode 十六进制中文和无声调拼音流。";
                case "Playfair": return "5×5 方阵双字母加密；自动插入 X，I/J 合并。";
                case "Beaufort": return "密钥字母减明文字母；破解按列恢复重复密钥。";
                case "Autokey": return "初始密钥后接明文形成连续密钥流。";
                case "Hill 2×2": return "使用可逆 2×2 矩阵处理字母对；破解枚举模 26 下的全部可逆矩阵。";
                case "Bifid": return "结合 Polybius 坐标与周期分组进行分组换位。";
                case "ADFGX": return "5×5 方阵替换后执行关键词列换位。";
                case "ADFGVX": return "6×6 方阵版本，支持字母和数字。";
                case "频率": return "统计英文字母出现次数及百分比。";
                case "N-gram": return "统计连续 N 个字母组合的出现次数。";
                case "重合指数": return "估算文本更接近单表语言还是随机/多表文本。";
                case "Kasiski": return "寻找重复序列及间距因子，推测维吉尼亚密钥长度。";
                case "Porta": return "使用 13 组互易替换表的多表密码。";
                case "Gronsfeld": return "数字位移密钥；破解估计长度并逐列恢复数字。";
                case "Running Key": return "使用与消息等长的自然语言文本作为密钥。";
                case "Four-square": return "使用两个 keyed 5×5 方阵处理字母对。";
                case "Two-square": return "使用两个横向方阵进行双字母替换。";
                case "Nihilist": return "Polybius 坐标与重复密钥坐标相加的数字密码。";
                case "Bazeries": return "数字分组反转结合 keyed 方阵替换。";
                case "Myszkowski": return "允许关键词重复字母共享列序的换位密码。";
                case "双重列换位": return "按两个关键词连续执行两次列换位。";
                case "路线换位": return "顺时针螺旋读取；破解尝试所有可整除宽度。";
                case "Fractionated Morse": return "将摩尔斯符号流按三元组映射为字母。";
                case "同音替换": return "为每个字母分配多个两位数字，削弱单字母频率。";
                case "跨行棋盘": return "使用两个空位数字构造可变长度坐标。";
                case "VIC": return "完整历史流程：消息组、日期、个人编号、短语派生、链式加法、跨行棋盘、普通与扰乱换位。";
                case "分析工作台": return "以 Unicode 字符为单位显示频率、N-gram、IC、Shannon 熵、文字体系、语言推测和周期 IC。";
                case "Variant Beaufort": return "Beaufort 的变体 C=P−K；支持重复密钥自动破解。";
                case "Trithemius": return "每处理一个字母，位移自动增加一位。";
                case "渐进凯撒": return "从指定起点逐字增加位移；破解枚举全部起点。";
                case "Scytale": return "按固定列数绕棍书写并按列读取；破解枚举列数。";
                case "Caesar Box": return "把文本写入矩形并按列读取。";
                case "Redefence": return "带起点偏移的栅栏换位；破解枚举栏数与偏移。";
                case "AMSCO": return "以 1、2 字符交替单元执行不规则列换位。";
                case "Turning Grille": return "使用旋转四次且互不重叠的孔位填充方阵。";
                case "A1Z26": return "在 A=1 到 Z=26 的数字与字母之间转换。";
                case "Tap Code": return "使用 5×5 方阵的敲击坐标，I/J 合并。";
                case "Morse": return "英文字母和数字的国际摩尔斯码转换。";
                case "Morbit": return "把摩尔斯符号流按二元组映射为 1–9；破解搜索九种二元组的排列。";
                case "Pollux": return "用多组数字同音表示摩尔斯点、划和分隔；破解直接恢复数字组对应的摩尔斯流。";
                case "Trifid": return "使用 3×3×3 坐标和周期分组进行三维分数化。";
                case "Alberti": return "混合字母盘按周期旋转的多表密码。";
                case "Bellaso": return "以关键词在混合字母表中执行重复位移。";
                case "Ragbaby": return "按词号和词内位置执行渐进位移；原始版本省略 J 与 X。";
                case "Jefferson Wheel": return "使用确定性转轮组和行偏移模拟杰斐逊转轮。";
                case "Three-square": return "两个 keyed 方阵与标准方阵把明文二元组扩展为三元组。";
                case "Digrafid": return "使用两个 3×9 方阵把二元组坐标分数化并按周期重组。";
                case "Grandpré": return "使用 10×10 同音坐标表，让一个字母对应多个数字。";
                case "Nomenclator": return "用用户给定的数字码替换字母、词或名称。";
                case "Book Cipher": return "以书本密钥中的词序和字母位置表示明文。";
                case "Ubchi": return "以同一关键词执行两次列换位，并在两次之间加入空字母。";
                case "Quagmire I": case "Quagmire II": case "Quagmire III": case "Quagmire IV": return "使用 keyed 字母表与循环指示词构造周期替换表。";
                case "Gromark": return "混合字母表结合数字引子及链式数字密钥流。";
                case "Periodic Gromark": return "按指定周期重新开始 Gromark 数字密钥流。";
                case "Chaocipher": return "左右两个动态字母表在每个字符后重排。";
                case "Solitaire": return "使用一副含两张 Joker 的牌组生成逐字密钥流。";
                case "Phillips": return "使用周期变化的 5×5 keyed 方阵执行替换。";
                case "Swagman": return "按关键词生成的拉丁式行列次序重排方块。";
                case "Cadenus": return "以 25 行矩形、列位移和关键词列序执行换位。";
                case "Nicodemus": return "把循环多表替换与关键词列换位组合。";
                case "扰乱式换位": return "各行使用不同有效长度，再按关键词列序读取。";
                case "Enigma": return "支持 Enigma I、M3、M4 的转子、环位、初始位置、反射器和插线板，并可用 Crib 搜索。";
                case "自动解码": return "并行尝试常见文本与传输编码，并继续尝试第二层嵌套编码，按可读性排列结果。";
                case "Base64": case "Base64URL": case "Base32": case "Base58": case "ASCII85": case "十六进制": case "二进制": return "在 UTF-8 文本与所选二进制安全表示之间转换。";
                case "URL 编码": return "转换 URL 百分号编码，适合查询参数和路径片段。";
                case "Unicode 转义": return "在文本与 \\uXXXX 形式之间转换。";
                case "HTML 实体": return "编码或还原 HTML 特殊字符与字符引用。";
                case "Quoted-Printable": return "转换邮件正文常用的 Quoted-Printable 传输格式。";
                case "Punycode": return "在国际化域名与 ASCII 域名标签之间转换。";
                case "字符集字节": return "在 Unicode、国标简体、繁体与公开历史中文代码页之间转换文本和十六进制字节。";
                case "盲文（英语一级）": return "在英语一级盲文 Unicode 点阵与普通字母、数字之间转换。";
                case "博多码 ITA2": return "在文本与五单位 ITA2 电报码之间转换，自动插入字母/数字换挡码。";
                case "中文电报码": return "使用内嵌的 Unihan 大陆四位电报码表转换 7078 个汉字。";
                case "中文输入法码": return "批量查询拼音、注音、双拼、形码、音形码、方言码和检字码，并可用精确码或通配符反查候选字。";
                case "中文编码工作台": return "把每个汉字的输入法码、读音、检字信息、异体、IDS、Unicode 和常见字符集字节集中到同一页。";
                case "字符详情卡": return "针对单个 Unicode 字符汇总所有已收录的输入法码、语音编码、字符集字节、释义与拆字信息。";
                case "中文码表工作台": return "查询内置形码、音形码和方言码表，也可导入 Rime、CIN 或简单文本码表进行正反查与统计。";
                case "中文语音与罗马化": return "把汉字批量转换为拼音、注音、粤拼、方言拼音及多种普通话罗马化表示。";
                case "拼音格式转换": return "在无声调拼音、数字声调、声调符号和注音之间批量转换，并保留原有分隔符。";
                case "中文编码识别": return "检查十六进制字符集字节和输入法码命中情况，给出可继续使用的中文工具。";
                case "中文字符集对照": return "并排显示同一文本在 Unicode、国标和 Big5 系列字符集中的字节及不可表示项。";
                case "Unicode 兼容格式": return "转换 UTF-7、CESU-8、Modified UTF-8，并按 BOM 自动识别 Unicode 字节序。";
                case "中文传输格式": return "转换 MIME encoded-word、JSON、JavaScript、CSS、XML、URI 和 IRI 表示。";
                case "历史中文字符集": return "调用 Windows 可用的 CNS、EUC-TW、Big5-HKSCS、ISO-2022-CN-EXT 和 IBM EBCDIC 中文代码页。";
                case "北约音标字母": return "在拉丁字母与 Alfa、Bravo 等拼读词之间转换。";
                case "猪圈密码符号": return "用一组可复制的线框符号表示猪圈密码字母。";
                case "旗语": return "用成对方向箭头表示旗语字母位置。";
                case "条形码": return "生成并解析 Code 39 或 EAN-13 的标准位串，同时显示条纹预览。";
                case "QR Code": return "生成和解析离线 QR Version 1-L 字节模式矩阵，适合短文本。";
                case "颜色编码": return "把 UTF-8 字节按三个一组写成 RGB 十六进制颜色，并可无损还原。";
                case "取色器与调色盘": return "读取 HEX 或 RGB 颜色，显示 HSL、互补色、邻近色和三角色调色盘。";
                case "Keyword Cipher": return "用关键词生成混合字母表；破解复用单表替换的统计搜索。";
                case "Multiplicative": return "执行模 26 乘法替换；破解枚举全部 12 个可逆乘数。";
                case "Reverse": return "按 Unicode 文本元素反转顺序；破解可直接还原。";
                case "Vatsyayana": return "把混合字母表相邻字母配成 13 对并互换，是一种互易替换。";
                case "Hill 3×3": return "使用可逆 3×3 模 26 矩阵处理三字母块；可由对齐的已知明文恢复矩阵。";
                default: return string.Empty;
            }
        }

        internal static string GetPrinciple(string name)
        {
            switch (name)
            {
                case "通用破解": return "先由识别器提取字符集、编码、语言、重合指数和周期特征，为每个适用破解器生成兼容度。快速、标准和深入三个强度决定纳入的搜索层级与预算；最多两个子任务并行，完成一个便把结构化候选送入统一面板。自然度按目标文字分别结合 N-gram、词或音节覆盖、汉字率、常用字和可打印字符比例，再与密码结构匹配度合成总排名。";
                case "密码识别器": return "识别器先生成字符集、数字 token、分隔符、文本边界、语言得分、整体 IC、周期分列 IC 和重复片段特征。16 符号且长度为四的倍数时，还会检查四位码的首位取值是否符合 Unicode 汉字码位结构。随后让明文、数字编码、单表替换、周期多表、换位、双字母方阵与分数化家族分别评分，最终按匹配分统一排序。";
                case "Crib 工具": return "把已知明文片段在密文的每个可能位置滑动。凯撒密码要求每个对应字符产生相同位移；维吉尼亚可由 C−P mod 26 推导密钥片段；单表替换则检查同一密文字母是否始终对应同一明文字母。";
                case "维吉尼亚": return "把 A–Z 映射为 0–25。加密公式为 C=(P+K) mod 26，解密为 P=(C−K) mod 26，关键词循环使用。高级破解先用整体 IC、周期分列 IC、Kasiski 和密钥长度约束选择周期，再对每一列做频率拟合，最后用多语言 N-gram 评分优化完整密钥。";
                case "凯撒": case "ROT13": case "ROT-N": case "渐进凯撒": case "Trithemius": return "属于移位替换。普通凯撒对每个字母使用固定偏移；渐进版本按字母位置增加偏移；ROT13 是偏移 13 的互易特例。所有计算都在所选字母表长度内取模。";
                case "仿射": return "将字母编号 x 变换为 y=(a×x+b) mod 26。只有 a 与 26 互质时才存在逆元；解密使用 x=a⁻¹×(y−b) mod 26。破解会枚举全部可逆 a 和所有 b。";
                case "Atbash": return "把字母表位置 i 映射到 n−1−i，因此同一操作既可加密也可解密。";
                case "单表替换": return "密文符号与明文符号之间是一一置换。拉丁密文直接处理 A–Z；连续的非拉丁密文按 Unicode 文本元素建立内部表，数学符号和图形标点也可成为密文符号。选择 ZH 后，16 符号、四位分组路径会把映射目标设为 0–F，按 Unicode 码位解码，并联合汉字率、常用字、中文标点和常见搭配搜索十六进制表；同时生成无声调拼音候选，以拼音音节覆盖和拼音连续片段评分。其他语言的无空格路径使用回退 N-gram、温度并行、词形束搜索和动态分词。";
                case "培根": return "每个字母变成五位 A/B 二进制式编码。解密按五位分组还原字符。";
                case "同音替换": case "Grandpré": return "一个明文字母可对应多个数字符号，以摊平单字符频率。解密表仍要求每个数字符号只对应一个明文字母。";
                case "A1Z26": return "直接使用字母序号表示字符，即 A=1、B=2、…、Z=26。它属于编码而非安全密码。";
                case "Morse": return "用点、划表示字母和数字；空格分隔字符，斜线分隔单词。";
                case "中文输入法码": return "从内嵌的 Unicode Unihan 读音和字形属性读取每个汉字的公开码值。正查逐字输出全部读音或码值；反查建立所选方案的索引，因此同一个输入码可以返回多个候选字。速成码取仓颉码的首尾字母，注音由汉语拼音读音转换。";
                case "中文编码工作台": case "字符详情卡": return "先按 Unicode 文本元素拆分输入，再关联 Unihan 属性、公开输入法码表、IDS 拆字和字符集编码器。补充平面字符以完整码点处理，不会拆成两个代理项。";
                case "中文码表工作台": return "内置码表建立“字→码”和“码→候选字”双向索引；反查支持 ? 单字符和 * 任意长度通配符。自定义码表会识别 Rime/CIN 常见的制表符或空格分列。";
                case "中文语音与罗马化": return "读音以 Unihan 和内置方言码表为底稿，再按目标方案转换声母、韵母或符号。多音字保留多个读音，不在缺少上下文时擅自选定。";
                case "拼音格式转换": return "识别拼音中的数字或 Unicode 声调符号，分离基本音节和声调，再按 a、e、ou 优先及末元音规则重新放置声调，或转换为注音。";
                case "中文编码识别": return "对十六进制输入尝试多种中文字符集严格解码；对字母数字码按每个内置输入法反向索引计算命中数。";
                case "中文字符集对照": return "使用严格编码回退逐项编码；无法表示的字符显示为“无法表示”，从而直接看出字符集覆盖差异。";
                case "Unicode 兼容格式": return "UTF-7 使用平台编码器；CESU-8 分别编码 UTF-16 代码单元；Modified UTF-8 在此基础上把空字符写成 C0 80；BOM 模式识别 UTF-8 与 UTF-16 大小端标记。";
                case "中文传输格式": return "按各格式的转义语法在 Unicode 文本与 ASCII 安全表示之间转换；MIME encoded-word 同时支持 B 与 Q 两种邮件头形式。";
                case "历史中文字符集": return "从所选名称提取 Windows 代码页编号，以严格回退方式在文本和十六进制字节间转换。代码页未安装时明确报告，而不替换为相近字符集。";
                case "Nomenclator": return "使用用户码表同时替换常见词、名称或单字母。加密优先匹配较长条目，避免短条目抢先替换。";
                case "Book Cipher": return "用共享书本中的词序号和词内字母序号表示字符。本实现坐标写作“词.字母”，两项均从 1 开始。";
                case "Beaufort": return "使用 C=(K−P) mod 26，变换具有互易性质。破解按候选周期拆列并拟合目标语言频率。";
                case "Variant Beaufort": return "使用 C=(P−K) mod 26；解密为 P=(C+K) mod 26。";
                case "Porta": return "把密钥字母两两分为 13 组，每组选择一张互易替换表，因此加密和解密执行同一变换。";
                case "Gronsfeld": return "与维吉尼亚相似，但密钥只允许数字 0–9，每个数字代表一列位移。";
                case "Autokey": return "密钥流由初始关键词后接明文组成，避免短关键词无限重复。解密过程中已恢复的明文会继续加入密钥流。";
                case "Running Key": return "使用与消息等长的自然语言文本作密钥流，逐字执行维吉尼亚加减。";
                case "Alberti": return "使用固定外盘和混合内盘；处理若干字符后旋转内盘，从而改变替换表。";
                case "Bellaso": return "在关键词生成的混合字母表上循环执行位移，是早期多表替换体系。";
                case "Ragbaby": return "每个词的初始位移等于词序号，词内每前进一个字母再增加给定步长。原始 24 字母版本把 J 合并到 I、X 合并到 W。";
                case "Jefferson Wheel": return "每个位置使用一个独立乱序字母转轮。明文行对齐后，读取相隔固定行数的另一行得到密文；解密反向移动相同行数。";
                case "栅栏": case "Redefence": return "按上下往返轨迹把字符写入多条栏，再逐栏读取。Redefence 允许轨迹从周期中的不同偏移开始。";
                case "Scytale": case "Caesar Box": return "将文本按固定宽度写入矩形，再按列读取。解密根据文本长度计算长短列并恢复行序。";
                case "列换位": return "按行写入关键词宽度的矩形，按关键词稳定排序后的列序读取。破解按列数枚举读取顺序，生成等价关键词并使用稀疏 N-gram 模型排列明文。";
                case "Myszkowski": return "关键词中的重复字母共享同一列等级；共享等级的列按行交错读取。";
                case "双重列换位": return "使用两个关键词先后执行两次列换位，解密时按相反顺序逆转。";
                case "路线换位": return "按行写入矩形，再沿顺时针螺旋路线读取；解密先沿路线填回，再逐行读取。";
                case "AMSCO": return "把明文切成长度 1、2 交替的单元，放入关键词矩形后按排序列读取，因此列长不规则。";
                case "Turning Grille": return "在偶数方阵模板上开四分之一数量的孔。每填一轮旋转模板 90°，四次旋转必须覆盖所有格且不能重叠。";
                case "Ubchi": return "用同一个关键词执行第一次列换位，加入少量空字母，再执行第二次列换位。解密先逆转第二次、删除约定数量的空字母，再逆转第一次。";
                case "Polybius": case "Tap Code": return "把字符放入 5×5 方阵，以行列坐标代替字母；标准方阵合并 I/J。";
                case "Playfair": return "把明文分成字母对。同一行向右移动、同一列向下移动，否则取矩形另两角；重复字母对之间插入 X。解密执行相反方向。";
                case "Hill 2×2": return "将两个字母组成向量，与 2×2 密钥矩阵相乘并模 26。破解遍历 26⁴ 个矩阵组合，保留可逆矩阵，先对样本筛选再对优胜矩阵全文评分。";
                case "Bifid": return "先把字母转换为 Polybius 行列坐标，在每个周期内先连接全部行坐标、再连接全部列坐标，重新成对后映射回字母。";
                case "Trifid": return "把 27 个符号放入 3×3×3 立方体，在周期内分离三维坐标、重排后重新组合。";
                case "Digrafid": return "两个 3×9 方阵分别提供字母对的坐标，再通过 3×3 中间坐标连接；坐标按周期分数化重排。";
                case "Four-square": case "Two-square": case "Three-square": return "使用多个 keyed 5×5 方阵处理字母对。矩形坐标在不同方阵间交叉读取；Three-square 还加入一个中间字符，使二元组扩展为三元组。";
                case "ADFGX": case "ADFGVX": return "先用 5×5 或 6×6 方阵把字符替换为 ADFGX/ADFGVX 坐标，再对坐标串执行关键词列换位。6×6 版本同时支持数字。";
                case "Nihilist": return "把明文和重复密钥都转换为 Polybius 数字坐标，再逐项相加。解密先减去密钥坐标。";
                case "Bazeries": return "按数字各位循环分组并反转字符顺序，同时在标准方阵与 keyed 方阵之间进行替换。";
                case "Fractionated Morse": return "先生成包含字符分隔符的摩尔斯流，再每三符号映射为 keyed 字母表中的一个字母。";
                case "Morbit": return "将摩尔斯流每两个符号映射为 1–9；关键词决定九种二元组的数字排列。破解枚举 9! 种映射，以摩尔斯合法性和语言 N-gram 共同评分。";
                case "Pollux": return "以多组数字分别表示点、划和分隔符，同一符号可随机选用多个数字。本实现的数字组固定，破解模式直接还原符号流并解码。";
                case "跨行棋盘": return "十个表头中两个位置为空，它们成为后两行的前缀；高频字母只用一位数字，其余字母使用两位数字。";
                case "VIC": return "由消息组减去日期前五位，结合记忆短语的两次顺序编号和无进位链式加法生成 50 位伪随机块。该块派生两段换位密钥与棋盘表头。正文依次经过消息切分、跨行棋盘、普通列换位和扰乱列换位，最后按日期第六位插入消息组。";
                case "Quagmire I": case "Quagmire II": case "Quagmire III": case "Quagmire IV": return "四个变体分别选择标准或 keyed 明文字母表、密文字母表；指示词的每个字母给出当前表的循环位移。解密在同一组表上执行反向位移。";
                case "Gromark": case "Periodic Gromark": return "先由关键词生成混合字母表。数字引子继续以相邻数字无进位相加生成密钥流，每个数字在混合字母表内给出位移；Periodic 版本按设置周期重置。";
                case "Chaocipher": return "输入字符在右字母表中的位置决定左字母表输出。随后两个字母表分别旋转并移动枢轴字符；解密以左表查找并从右表读取，同时执行相同重排。";
                case "Solitaire": return "牌组通过 Joker 移动、三切、计数切牌产生 1–26 密钥值。口令先对初始牌组执行额外计数切牌；同一口令可生成相同密钥流。";
                case "Phillips": return "把 I/J 合并到 keyed 5×5 方阵，在方阵坐标内替换，并按五字符分组轮换行布局；解密沿相反方向移动。";
                case "Swagman": return "关键词给出列等级，每个方块中的行号进一步循环移动列位置。解密用同一置换的逆映射还原原顺序。";
                case "Cadenus": return "文本按 25×关键词长度分块。各列按关键词字母决定纵向位移，再按关键词等级读取列；解密先恢复列序，再逆转纵向位移。";
                case "Nicodemus": return "先用关键词执行循环多表位移，再以同一关键词执行列换位。解密先撤销列换位，再撤销字母位移。";
                case "扰乱式换位": return "关键词等级决定每行实际填入的单元数，因此形成不规则矩形。密文仍按关键词列等级读取，解密先重建有效单元形状。";
                case "Enigma": return "按历史走线顺序执行插线板、从右到左的转子、反射器、从左到右的逆转子和插线板。移动转子在每个字母前步进，并实现中轮双步进；M4 的 Greek 转子保持静止。Crib 搜索枚举三枚移动转子的 26³ 个初始位置，可选枚举转子顺序。";
                case "分析工作台": return "先按 Unicode 文本元素切分并规范化字符，保留非拉丁文字、组合字符和代理对字符。工作台计算字符频率、N-gram、整体重合指数、Shannon 熵、文字体系分布和 1–20 周期的平均分列 IC；当文本含拉丁字母时，再按所选方法匹配内置语言模型。";
                case "自动解码": return "把输入分别交给 Base64、Base64URL、Base32、Base58、ASCII85、十六进制、二进制、URL、HTML、Unicode 转义和 Quoted-Printable 解码器；有效结果再展开一层，并按可打印字符、空白、文字比例和语言形状排序。";
                case "Base64": case "Base64URL": case "Base32": case "Base58": case "ASCII85": case "十六进制": case "二进制": return "先把文本编码为 UTF-8 字节，再把位组映射到相应的可打印字母表；解码按相反方向恢复字节和 UTF-8 文本。";
                case "URL 编码": case "HTML 实体": case "Unicode 转义": case "Quoted-Printable": case "Punycode": return "按照对应传输格式的转义、标签或域名规则在 Unicode 文本与 ASCII 表示之间转换。";
                case "字符集字节": return "使用所选字符编码把 Unicode 文本映射为字节，输出十六进制；反向操作把十六进制字节按同一字符集解释。GB2312/EUC-CN、GBK/CP936 与 GB18030 分别处理，不再互相替代。";
                case "盲文（英语一级）": return "字母映射到 Unicode U+2800–U+28FF 盲文点阵；大写和连续数字使用前置指示符。";
                case "博多码 ITA2": return "每个符号使用五位码；11111 切换到字母表，11011 切换到数字与标点表。";
                case "中文电报码": return "每个已收录汉字映射到固定四位十进制代码；运行时从压缩的 Unihan kMainlandTelegraph 资源建立双向索引。";
                case "条形码": return "Code 39 把每个字符写成九段窄/宽条空组合；EAN-13 使用左右奇偶模式、守护条和模 10 校验位。";
                case "QR Code": return "使用 QR Version 1-L 字节模式：写入模式、长度和 UTF-8 数据，补齐 19 个数据码字，生成 7 个 Reed–Solomon 纠错码字，再放置定位、计时、格式和掩码模块。";
                case "颜色编码": return "把 UTF-8 字节每三个组成 R、G、B；长度字段用于去除最后一个颜色中的补零。";
                case "取色器与调色盘": return "把 RGB 转为 HSL，在色相环上旋转 30°、120°、180° 和 240°生成邻近、三角与互补配色。";
                case "Keyword Cipher": return "把关键词去重后置于字母表开头，再接剩余字母，形成固定单表替换。";
                case "Multiplicative": return "对字母编号执行 C=aP mod 26；解密使用 a 的模逆元。";
                case "Reverse": return "按 Unicode 文本元素而非 UTF-16 代码单元反转，避免拆开组合字符或代理对。";
                case "Vatsyayana": return "把 keyed 字母表依次分成 13 对，每个字母替换为同组的另一个字母；同一操作可解密。";
                case "Hill 3×3": return "把三字母列向量乘以 3×3 矩阵并模 26。已知明文破解从三个对齐块构造明文矩阵和密文矩阵，计算 K=C·P⁻¹，并用其余片段验证。";
                case "频率": return "统计 A–Z 的次数和百分比，并按出现次数排序。";
                case "N-gram": return "使用滑动窗口统计连续 N 个字母组合；N 可在 1–8 之间。";
                case "重合指数": return "IC=Σfᵢ(fᵢ−1)/(N(N−1))。自然语言单表文本通常高于随机或多表文本。";
                case "Kasiski": return "寻找重复字符串，计算相邻出现位置的距离，再统计距离因数；高票因数可能是重复密钥长度。";
                default: return "该工具按照经典定义执行可逆转换。加密和解密必须使用相同的字母表、密钥、分组与文本规则。";
            }
        }

        internal static string GetUsage(ICryptoTool tool)
        {
            if (tool.Name == "通用破解")
                return "1. 粘贴或拖入密文。\r\n2. 不确定语言时保留 AUTO；中文原文或中文编码选择 ZH。\r\n3. 一般先用“标准”。识别器前三项命中的密码家族会加入搜索，即使它们原本属于更深档位。\r\n4. 有线索时点击“线索”：算法名称和已知明文分别填写，窗口会自动生成正确格式。明文填连续原文，不加引号。\r\n5. 候选会逐项出现；高匹配算法会保留更多内部候选，避免正确答案因单算法预排名较低而消失。单击查看全文，双击进入对应工具继续调整。";
            StringBuilder result = new StringBuilder();
            result.Append("1. 在主窗口选择“").Append(tool.Category).Append(" → ").Append(tool.Name).Append("”；也可先选标签缩小工具列表。\r\n");
            result.Append("2. 选择所需模式并填写该模式显示的参数；标为必填的参数不能为空。\r\n");
            if (tool.Name == "Book Cipher" || tool.Name == "Nomenclator" || tool.Name == "Running Key" || tool.Name == "VIC" || tool.Name == "中文码表工作台") result.Append("   长文本参数可双击输入框或点击 … 打开可缩放编辑器，并可读取文本文件。\r\n");
            result.Append("3. 输入或拖入文本。停止输入后会自动处理。\r\n");
            if (tool.Modes.Contains(ToolMode.Crack)) result.Append("4. 破解结果显示在候选面板；选择候选可查看对应明文、密钥和评分。带已知明文参数时点击“明文”使用可缩放编辑器；长搜索在窗口底部显示进度，点击 × 可取消。\r\n");
            else result.Append("4. 输出区实时显示结果；“互换”可把结果送回输入区。\r\n");
            result.Append("5. “识别”和“通用”保留当前输入并在识别器、通用破解和本工具之间切换；顶部 ? 直接打开当前工具的说明。\r\n");
            return result.ToString();
        }

        internal static string GetExample(string name)
        {
            switch (name)
            {
                case "通用破解": return "一段被识别为 Fractionated Morse 的连续字母密文，无须切换到深入档：识别结果会把该破解器提升到当前批次。若已知类型，可填“算法:Fractionated Morse”；若只知道原文中含 ATTACK AT DAWN，则填“明文:ATTACK AT DAWN”。";
                case "密码识别器": return "输入 ADFGX 字符流会得到 ADFGX；输入 20-15 2-5 会得到 A1Z26；短拉丁文本可选 COSINE，较长文本可选 LLR 或 NGRAM。";
                case "Crib 工具": return "密文 KHOOR、已知明文 HELLO 会得到凯撒位移 3；对维吉尼亚密文则会输出对应位置的密钥片段。";
                case "凯撒": return "明文 ABC，密钥 3 → 密文 DEF；使用同一密钥解密 DEF → ABC。";
                case "维吉尼亚": return "明文 ATTACKATDAWN，密钥 LEMON → LXFOPVEFRNHR。破解时可填写已知长度 5 或部分密钥 LE?ON。";
                case "仿射": return "明文 AFFINECIPHER，A=5、B=8 → IHHWVCSWFRCP。";
                case "Playfair": return "使用关键词 PLAYFAIR EXAMPLE；明文会先去除非字母、合并 I/J，并按规则插入 X。";
                case "Hill 2×2": return "密钥 3,3,2,5；明文 HELP → HIAT。矩阵不可逆时工具会直接提示。";
                case "VIC": return "常用字母 ATONESIR，记忆短语 TWAS THE NIGHT BEFORE CHRISTMAS，日期 139195，个人编号 6，消息组 72401。解密时消息组可留空，由日期指定位置自动提取。";
                case "Enigma": return "M3、转子 I II III、环位 1 1 1、位置 AAA、反射器 B：AAAAA → BDZGO。破解模式提供原文片段作为 Crib，可恢复初始位置。";
                case "Quagmire I": case "Quagmire II": case "Quagmire III": case "Quagmire IV": return "主关键词 EXAMPLE、第二关键词 KEYWORD、指示词 FORT；四个变体使用相同操作，但标准与 keyed 字母表的位置不同。";
                case "Gromark": case "Periodic Gromark": return "关键词 KEYWORD、数字引子 31415；Periodic 版本还可把周期设为 10。";
                case "Chaocipher": return "分别输入左右两个 26 字母排列或关键词；每处理一个字母，两侧字母表都会改变。";
                case "Solitaire": return "口令 CRYPTONOMICON；使用同一口令再次解密可恢复原文。";
                case "A1Z26": return "HELLO → 8-5-12-12-15。";
                case "Morse": return "SOS → ... --- ...；单词之间使用 /。";
                case "单表替换": return "加解密时可直接粘贴 QWERTYUIOPASDFGHJKLZXCVBNM 这样的 26 字母替换表，也可点击 …，按 A 到 Z 的位置逐格填写。破解中文编码时把语言设为 ZH；结果会显示映射、汉字率或拼音分词。";
                case "Polybius": case "Tap Code": return "HELLO 在标准 5×5 方阵中转换为成对行列数字；J 会按 I 处理。";
                case "分析工作台": return "输入“天地玄黄宇宙洪荒天地玄黄”，N 选 2，可直接得到汉字频率、“天地”等二元组、熵和周期 IC；输入拉丁文本时还会显示语言匹配方法与推测。";
                case "自动解码": return "输入 U0dWc2JHOGdWMjl5YkdRPQ==，候选会显示 Base64 → Base64 的二层还原结果。";
                case "Base64": return "密码箱 → 5a+G56CB566x；解码恢复“密码箱”。";
                case "字符集字节": return "文本“中文”、字符集 GB18030 → D6D0CEC4；按同一字符集解码可恢复原文。可选 Unicode 大小端、GB2312/EUC-CN、GBK、GB18030、HZ、ISO-2022-CN、Big5，以及 Mac、CNS、TCA、ETen、IBM5550、TeleText、Wang 中文代码页。";
                case "中文电报码": return "一丁七 → 0001 0002 0003。";
                case "中文输入法码": return "选择汉语拼音，中国 → zhong guo；选择仓颉，中国 → L WMGI。切换到解码并输入 zhong guo，可分别查看两个输入码的候选汉字。";
                case "中文编码工作台": return "输入“汉字”，同页查看 han/zi、注音、粤拼、五笔、仓颉、四角号码、IDS、UTF-8、GB18030 与 Big5。";
                case "字符详情卡": return "输入“漢”，查看 U+6F22、读音、仓颉、五笔、繁简异体、IDS 与各字符集字节。只取第一个 Unicode 字符。";
                case "中文码表工作台": return "选择五笔86并输入“中文”可正查；输入 k 或 k* 可反查。导入自定义文本时，每行可写“中 khk”或“khk 中”。";
                case "中文语音与罗马化": return "输入“中国”，目标选注音、威妥玛、粤拼、吴语拼音或台罗，结果按字保留多读音。";
                case "拼音格式转换": return "输入 zhong1 guo2，目标选声调符号 → zhōng guó；目标选注音 → ㄓㄨㄥ ㄍㄨㄛˊ。";
                case "中文编码识别": return "输入 D6D0CEC4 可比较 GBK、GB18030 等解码；输入 khk lll 可查看哪些输入法码表命中。";
                case "中文字符集对照": return "输入“中文𠀀”，表格会显示各字符集字节数，并标出哪些旧字符集无法表示扩展汉字。";
                case "Unicode 兼容格式": return "选择 CESU-8：😀 → EDA0BDEDB880；解码同一字节可恢复字符。";
                case "中文传输格式": return "选择 MIME encoded-word Base64：中文 → =?UTF-8?B?5Lit5paH?=。";
                case "历史中文字符集": return "选择 Big5-HKSCS / CP951，把汉字编码为十六进制；若系统没有对应代码页会显示明确提示。";
                case "QR Code": return "输入 HELLO QR，编码后得到 21×21 矩阵和带静区的块状预览；把完整输出送入解码可恢复文本。";
                case "条形码": return "类型 CODE39、内容 CODE39 可生成条纹；类型 EAN13、输入 690123456789 会自动补校验位 2。";
                case "取色器与调色盘": return "点击“取色”，或输入 #3366CC；结果显示 RGB、HSL、互补色、邻近色和三角色。";
                case "Hill 3×3": return "密钥 6,24,1,13,16,10,20,17,15：ACT → POH。破解时填写从密文开头对齐、至少 9 个字母的已知明文。";
                case "Book Cipher": return "点击书本参数后的 …，输入或打开包含 ALPHA BRAVO CHARLIE … ZULU 的文本。明文 DEFEND 会生成“词.字母”坐标；解密时使用完全相同的书本文本。";
                case "Nomenclator": return "点击码表参数后的 …，按行或用分号填写 KING=42、ARMY=731。明文 KING ARMY → 42 731。";
                case "Running Key": return "明文 ATTACKATDAWN，密钥文本 THISISALONGKEYTEXT。密钥可以在可缩放编辑器中粘贴或从文件读取，并须至少覆盖明文字母数。";
                default: return "先用一小段容易辨认的文本加密，再使用完全相同的参数解密。解密结果应与该算法规范化后的输入一致。";
            }
        }

        internal static string GetInterpretation(ICryptoTool tool)
        {
            if (tool.Name == "通用破解")
                return "语言分比较候选与所选语言模型的接近程度，并按样本长度限制最高分；它不是正确率，也不会再取 100。匹配表示密文结构对密码家族的支持程度，综合用于最终排序。候选会在搜索过程中持续更新；双击候选可进入对应工具。";
            if (tool.Name == "密码识别器")
                return "每一项依次显示排名、类型、匹配分和判断依据。字符签名条目显示命中的字符集或分组规则；单表与换位条目显示实际试解参数；周期条目显示整体 IC、周期、分列 IC、Kasiski 计数、语言和候选密钥。选择候选后可直接查看完整依据。";
            if (tool.Category == ToolCategories.Encoding || tool.Category == ToolCategories.Chinese)
                return "编码只改变表示形式，不隐藏信息。解码结果应与原文本一致；字节类工具默认按 UTF-8 解释，字符集字节工具则按所选字符集解释。二维码和条形码同时给出机器位串与屏幕预览。";
            if (tool.Modes.Contains(ToolMode.Crack))
                return "候选按语言模型评分从高到低排列。先观察首位明文是否具有连续单词、合理字母频率和标点位置，再比较相邻候选；增加密文长度、约束密钥范围或提供已知明文可以扩大候选之间的评分差异。";
            if (IsAnalysisTool(tool.Name))
                return "统计结果用于缩小范围，不单独证明密码类型。自然语言、转写方式、样本长度和多层加密都会改变指标。建议把频率、重合指数、重复片段、周期峰值与实际试解结果联合判断。";
            return "加密模式的输出可复制、保存或与输入互换；解密模式应恢复算法规范化后的原文。部分古典算法会合并 I/J、删除非字母、补入 X 或改变大小写，因此应以本页的原理和注意事项说明判断是否正确，而不是只做逐字符比较。";
        }

        internal static string GetTroubleshooting(ICryptoTool tool)
        {
            if (tool.Name == "通用破解")
                return "识别正确但候选缺失：在算法线索中填写识别器显示的名称，并确认密文长度满足该破解器要求。候选不自然：核对语言；中文原文或中文编码用 ZH。已知片段未生效：使用“明文:”前缀，按预计明文顺序填写连续片段，不要填写解释、引号或通配符。搜索过早结束时选择“深入”，并保持输入不变直到进度完成。";
            if (tool.Category == ToolCategories.Encoding || tool.Category == ToolCategories.Chinese)
                return "解码失败时，先去掉说明文字，只保留编码主体；再检查填充符、字符集和格式选项。字符集字节出现乱码通常表示 UTF-8、GB18030、Big5 或 Shift_JIS 选择错误。QR Code 解码需要粘贴本工具给出的 21×21 矩阵；图形预览本身不作为输入。";
            StringBuilder result = new StringBuilder();
            result.Append("没有输出时，检查输入和当前模式的必填参数。结果不正确时，核对模式、密钥、字母表、分组和填充约定。");
            if (tool.Modes.Contains(ToolMode.Crack))
                result.Append(" 破解结果不稳定时，可增加样本、切换语言、缩小已知密钥范围或加入已知明文片段。");
            else
                result.Append(" 往返结果不同时，确认解密参数一致，并查看该算法是否会规范化字符。");
            return result.ToString();
        }

        internal static string GetNotes(string name)
        {
            switch (name)
            {
                case "通用破解": return "快速覆盖编码和秒级破解器；标准加入常用的多表、换位与方阵破解；深入使用更大的搜索预算。识别器前三项的家族和“算法:”指定的家族会跨档加入当前搜索；高匹配家族保留更多内部候选。AUTO 用于未知拉丁语言；中文原文或中文编码选择 ZH。“明文:”内容作为连续明文约束和排序依据，“算法:”内容控制密码家族。";
                case "自动解码": return "最多展开两层编码，并拒绝控制字符过多或与输入相同的结果。评分表示文本可读性，不代表编码类型的密码学置信度。";
                case "QR Code": return "当前实现固定为 QR Version 1-L、UTF-8 字节模式，容量为 17 字节。矩阵包含真实纠错码和格式信息；解码入口读取本工具输出的 21×21 0/1 矩阵。";
                case "条形码": return "Code 39 支持数字、大写字母和 - . 空格 $ / + %；EAN-13 可输入 12 位让工具计算校验位，或输入含正确校验位的 13 位。";
                case "中文电报码": return "大陆与台湾电报码存在差异。本工具使用 Unicode Unihan 的 kMainlandTelegraph 映射；未收录字符在编码时原样保留，未知数字在解码时用方括号标出。";
                case "中文输入法码": return "读音、部首、笔画、仓颉和四角号码来自 Unicode Unihan；五笔、郑码、二笔、表形码、行列、大易、嘸蝦米、笔顺、音形和方言方案来自随程序发布的公开码表。编码时未收录字符用方括号保留；反查支持 ? 与 *，同码候选按常用程度优先。";
                case "中文编码工作台": case "字符详情卡": return "工作台最多展开前 64 个 Unicode 字符，详情卡只处理第一个字符。码表没有的项目不会显示；“—”表示所选字符集不能无损表示该字符。";
                case "中文码表工作台": return "内置方案来自公开码表，方案版本和候选顺序可能与特定输入法发行版不同。自定义码表只在当前处理过程中使用，不写入程序或磁盘。";
                case "中文语音与罗马化": return "没有上下文分词时，多音字以斜线保留全部候选。威妥玛、国语罗马字、通用拼音、耶鲁与 IPA 为规则转换结果；需要词级读音时应结合上下文人工选读。";
                case "拼音格式转换": return "转换器处理已分词的拼音音节，不做汉字转拼音。轻声可用 5；无声调音节保持无标记。ü 既可写作 ü，也可写作 v。";
                case "中文编码识别": return "命中表示输入码存在于某方案，不等于唯一识别；短码常同时属于多种输入法。十六进制解码应结合预期地区、年代和可读文本判断。";
                case "中文字符集对照": return "对照使用严格编码，不用问号替换不可表示字符。GB2312 仍额外检查双字节范围，避免把 CP936 扩展误算为 GB2312。";
                case "Unicode 兼容格式": return "CESU-8 和 Modified UTF-8 不是标准 UTF-8；只应在明确需要兼容旧系统或 Java 修改版 UTF-8 时使用。编码输出和解码输入均为十六进制字节。";
                case "中文传输格式": return "JSON/JavaScript/CSS/XML 转义不会自动添加引号或文档结构。MIME encoded-word 用于邮件头字段，正文传输仍应使用合适的 Content-Type 与传输编码。";
                case "历史中文字符集": return "历史代码页是否可用取决于 Windows 的代码页组件。工具不以相近编码代替缺失代码页；同名标准的不同修订也可能存在字符映射差异。";
                case "盲文（英语一级）": return "使用 Unicode 六点盲文、英语一级字母和数字符号；不展开英语二级盲文缩写。";
                case "字符集字节": return "编码输出十六进制字节，解码输入可包含空格。UTF-16LE/BE 与 UTF-32LE/BE 已分成独立选项。无法表示某字符或遇到非法字节时会停止并提示；长文本文件编辑器使用同一套字符集选择。";
                case "密码识别器": return "匹配分由字符格式、文本结构、统计特征和试解语言得分共同生成。语言方法的适用条件：COSINE 适合约 10–60 个拉丁字母的稀疏短样本；LLR 适合约 40–240 字母并能更好处理稀有字母；CHI 适合约 200 字母以上、各期望频数较充分的样本；NGRAM 适合约 100 字母以上并用于区分频率相近的语言。AUTO 在少于 60 字母时选 COSINE，60–239 选 LLR，240 以上选 NGRAM。";
                case "维吉尼亚": return "自动破解通常至少需要 30 个字母；几十字母只能作为线索，数百字母更可靠。部分密钥使用 ? 表示未知字符。自定义字母表会改变所有模运算。";
                case "单表替换": return "锁定格式为 X=E,Q=T；非拉丁符号使用 Ж=E、Ω=T 等同样格式。语言 ZH 会自动比较四位 Unicode 十六进制与拼音载体，并优先排列汉字率和中文语言分较高的结果。中文模式的总搜索预算约六成用于 Unicode 十六进制映射，其余用于拼音；确认出现可读汉字后会缩短拼音支路。其他语言下，总预算在语言假设、字符模型搜索和联合分词搜索之间分配。";
                case "列换位": case "AMSCO": case "ADFGX": case "ADFGVX": return "最短与最长宽度限定排列空间。已知宽度时把两项设为相同值，可以更快得到稳定候选；ADFGX 与 ADFGVX 还可填写已知方阵关键词。";
                case "Myszkowski": case "双重列换位": case "Ubchi": return "搜索次数控制排列优化深度；缩小宽度范围可以把时间集中到更可能的密钥。Ubchi 的空字母上限同时限定插入数量搜索。";
                case "Autokey": case "Nihilist": return "按初始密钥或加法密钥长度逐项恢复，再用完整 N-gram 得分反复优化。已知长度时可把最短与最长长度设为同一个数。";
                case "Playfair": case "Two-square": case "Four-square": case "Fractionated Morse": case "Polybius": case "同音替换": return "搜索次数决定每次密钥搜索的长度，随机重启次数决定独立起点数量。模拟退火适合一般未知密钥；爬山适合好初值；延迟接受和阈值接受适合评分平台；再加热退火与自适应退火适合局部最优较多的长搜索；大洪水会逐步收紧可接受下限；记录到记录围绕本轮最佳解搜索。自动采用模拟退火。";
                case "Bifid": case "Trifid": return "破解会同时搜索 keyed 字母表和周期；最短、最长周期用于裁剪周期范围，搜索次数与随机重启控制每个周期的密钥优化。搜索策略的适用条件与 Playfair 相同。";
                case "跨行棋盘": return "破解枚举两个空位数字，并为每一组空位优化棋盘字母顺序。搜索次数控制每组棋盘的优化深度；搜索策略用于决定是否接受当前候选。";
                case "VIC": return "加解密双方必须完全一致地使用常用字母、短语、日期和个人编号。消息切分位置是可选历史扰动；启用时解密也应填写非空切分参数以恢复原顺序。";
                case "Enigma": return "加解密使用同一操作。型号决定转子数量和默认反射器：I/M3 使用三枚移动转子，M4 使用一枚静止 Greek 转子加三枚移动转子。环位从 1 开始；初始位置用 3 或 4 个字母。Crib 搜索固定环位、反射器和插线板，枚举移动转子位置；启用转子顺序搜索会把工作量扩大六倍。";
                case "Running Key": return "完整已知明文可直接推导等长密钥；从文本开头对齐的片段会锁定对应密钥前缀，其余位置用明文与密钥双语言评分搜索。增加搜索次数和重启次数会扩大探索范围；搜索策略与 Playfair 的适用条件相同。";
                case "Bazeries": case "Ragbaby": case "Alberti": case "Bellaso": case "Jefferson Wheel": return "破解模式使用明确的数字、周期或种子范围，并结合内嵌关键词表与语言评分。缩小已知范围可显著减少耗时；候选关键词数量控制字典搜索宽度。";
                case "Three-square": case "Digrafid": return "破解先分别筛选两个 keyed 方阵，再组合高分方阵；同时直接组合一组常见关键词。Digrafid 还枚举指定周期范围。";
                case "Turning Grille": return "孔位从 1 开始编号。四次旋转后必须恰好覆盖整个方阵；重叠或遗漏会被拒绝。";
                case "Book Cipher": return "双方的书本文本、分词和标点处理必须完全相同；密钥文本缺少某个字母时无法加密该字母。";
                case "Nomenclator": return "格式示例 KING=42;ARMY=731。不同明文条目不能复用同一代码，否则解密存在歧义。";
                case "重合指数": case "Kasiski": case "频率": case "N-gram": case "分析工作台": return "所有统计按 Unicode 文本元素执行，支持拉丁扩展、汉字、假名、韩文、西里尔、希腊、阿拉伯、希伯来、天城文及其他字符。IC 的英语经验阈值不能直接套用到字符集规模不同的文字；应在同一文字体系和规范化规则下比较样本。语言方法的适用区间与密码识别器一致。";
                default: return "传统方阵算法可能移除空格、标点并统一为大写；普通替换和换位通常保留原字符。若解密不能还原，请先检查密钥、字母表、分组、填充和文本规则是否一致。";
            }
        }

        private static bool IsAnalysisTool(string name) { return name == "分析工作台" || name == "频率" || name == "N-gram" || name == "重合指数" || name == "Kasiski"; }
    }
}
