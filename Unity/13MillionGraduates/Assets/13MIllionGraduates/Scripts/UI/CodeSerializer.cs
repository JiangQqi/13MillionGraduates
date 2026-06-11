using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 导入失败原因
    /// </summary>
    public enum ImportError
    {
        None = 0,
        SyntaxError,          // 格式无效 / 无法识别的指令 / 跳转目标丢失
        InstructionLocked,    // 当前关卡未开放该指令
    }

    /// <summary>
    /// 代码序列化工具类：CodeItem ↔ Text
    /// </summary>
    public static class CodeSerializer
    {
        private const string HEADER = "--THIRTEEN MILLIONS GRADUATES--";

        /// <summary>
        /// CodeItem → Text
        /// </summary>
        public static string Export(List<CodeItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine(HEADER);
            sb.AppendLine();

            List<LabelItem> labels = new List<LabelItem>();
            foreach (var item in items)
            {
                if (item is LabelItem label) labels.Add(label);
            }

            foreach (var item in items)
            {
                switch (item)
                {
                    case InBoxItem:
                        sb.AppendLine("    INBOX");
                        break;

                    case OutBoxItem:
                        sb.AppendLine("    OUTBOX");
                        break;

                    case CopyToItem copyTo:
                        string idx = copyTo.IsAddress ? $"[{copyTo.Index}]" : copyTo.Index.ToString();
                        sb.AppendLine($"    COPYTO {idx}");
                        break;

                    case CopyFromItem copyFrom:
                        string idx2 = copyFrom.IsAddress ? $"[{copyFrom.Index}]" : copyFrom.Index.ToString();
                        sb.AppendLine($"    COPYFROM {idx2}");
                        break;

                    case ArithmeticItem arith:
                        string arithType = arith.ArithmeticType switch
                        {
                            ArithmeticType.Add            => "ADD",
                            ArithmeticType.Subtract       => "SUB",
                            ArithmeticType.Multiply       => "MUL",
                            ArithmeticType.Divide         => "DIV",
                            ArithmeticType.Inc            => "INC",
                            ArithmeticType.Dec            => "DEC",
                            ArithmeticType.Equal          => "EQL",
                            ArithmeticType.NotEqual       => "NEQ",
                            ArithmeticType.Greater        => "GTR",
                            ArithmeticType.Less           => "LES",
                            ArithmeticType.GreaterOrEqual => "GEQ",
                            ArithmeticType.LessOrEqual    => "LEQ",
                            _ => "???"
                        };
                        string aridx = arith.IsAddress ? $"[{arith.Index}]" : arith.Index.ToString();
                        sb.AppendLine($"    ARITHMETIC {arithType} {aridx}");
                        break;

                    case JumpItem jump:
                        string jType = jump.JumpType switch
                        {
                            JumpType.Any        => "JUMP ANY",
                            JumpType.IfZero     => "JUMP ZRO",
                            JumpType.IfNegative => "JUMP NEG",
                            _ => "?"
                        };
                        sb.AppendLine($"    {jType} {IndexToLabelName(labels.IndexOf(jump.Label))}");
                        break;

                    case LabelItem label:
                        sb.AppendLine($"{IndexToLabelName(labels.IndexOf(label))}:");
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Text → CodeItem。失败返回 null，out 参数区分语法错误 / 指令未解锁。
        /// </summary>
        public static List<CodeItem> Import(string text, out ImportError error)
        {
            error = ImportError.None;

            // 1. 格式检查
            List<ParsedLine> lines = ParseLines(text);
            if (lines == null || lines.Count == 0)
            {
                error = ImportError.SyntaxError;
                return null;
            }

            // 2. 是否出现无法识别的指令行
            foreach (var line in lines)
            {
                if (!line.IsValid) { error = ImportError.SyntaxError; return null; }
            }

            // 3. 当前关卡权限校验
            if (!ValidateAgainstCurrentLevel(lines))
            {
                error = ImportError.InstructionLocked;
                return null;
            }

            List<CodeItem> items = new List<CodeItem>();
            List<LabelItem> labels = new List<LabelItem>();
            List<(JumpItem jump, int labelIdx)> pendingJumps = new List<(JumpItem, int)>();

            var prefabs = NotePad.Ins.CodeItemPrefabs;

            foreach (var line in lines)
            {
                CodeItem item = InstantiateFromParsedLine(line, prefabs);
                if (item == null) continue;

                items.Add(item);

                // 处理 Jump ↔ Label 配对
                if (line.Type == OperationType.Label)
                {
                    labels.Add((LabelItem)item);
                }
                else if (line.Type == OperationType.Jump)
                {
                    JumpItem jump = (JumpItem)item;
                    int labelIdx = LabelNameToIndex(line.LabelTarget);

                    if (labelIdx < labels.Count)
                    {
                        jump.Label = labels[labelIdx];
                        labels[labelIdx].JumpItem = jump;
                    }
                    else
                    {
                        pendingJumps.Add((jump, labelIdx));
                    }
                }
            }

            // 补配对：解析时 Label 尚未创建的 Jump
            foreach (var (jump, labelIdx) in pendingJumps)
            {
                if (labelIdx < labels.Count)
                {
                    jump.Label = labels[labelIdx];
                    labels[labelIdx].JumpItem = jump;
                }
            }

            // 校验：存在未解析的 Jump 目标 → 无效程序
            foreach (var (jump, _) in pendingJumps)
            {
                if (jump.Label == null)
                {
                    foreach (var item in items)
                        UnityEngine.Object.Destroy(item.gameObject);
                    error = ImportError.SyntaxError;
                    return null;
                }
            }

            return items;
        }

        #region 文本解析 
        private struct ParsedLine
        {
            public OperationType Type;
            public int Index;
            public bool IsAddress;
            public ArithmeticType ArithType;
            public JumpType JumpType;
            public string LabelTarget; // Jump 目标名 / Label 自身名
            public bool IsValid;       // 该行是否成功匹配到已知指令
        }

        private static List<ParsedLine> ParseLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            string[] rawLines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // 验证表头
            int startIdx = -1;
            for (int i = 0; i < rawLines.Length; i++)
            {
                if (rawLines[i].Trim() == HEADER) { startIdx = i + 1; break; }
            }
            if (startIdx < 0) return null;

            List<ParsedLine> result = new List<ParsedLine>();
            for (int i = startIdx; i < rawLines.Length; i++)
            {
                string line = rawLines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                result.Add(ParseSingleLine(line));
            }

            return result;
        }

        private static ParsedLine ParseSingleLine(string line)
        {
            var result = new ParsedLine();

            // Label: "a:", "bc:"
            if (line.EndsWith(":"))
            {
                result.Type        = OperationType.Label;
                result.LabelTarget = line.TrimEnd(':');
                result.IsValid     = true;
                return result;
            }

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return result;

            switch (parts[0].ToUpperInvariant())
            {
                case "INBOX":
                    result.Type    = OperationType.InBox;
                    result.IsValid = true;
                    break;

                case "OUTBOX":
                    result.Type    = OperationType.OutBox;
                    result.IsValid = true;
                    break;

                case "COPYTO":
                    result.Type    = OperationType.CopyTo;
                    result.IsValid = true;
                    ParseIndexArg(parts, 1, out result.Index, out result.IsAddress);
                    break;

                case "COPYFROM":
                    result.Type    = OperationType.CopyFrom;
                    result.IsValid = true;
                    ParseIndexArg(parts, 1, out result.Index, out result.IsAddress);
                    break;

                case "ARITHMETIC":
                    result.Type      = OperationType.Arithmetic;
                    result.ArithType = parts.Length > 1 ? ParseArithmeticType(parts[1]) : ArithmeticType.Add;
                    result.IsValid   = true;
                    ParseIndexArg(parts, 2, out result.Index, out result.IsAddress);
                    break;

                case "JUMP":
                    result.Type    = OperationType.Jump;
                    result.IsValid = true;
                    if (parts.Length > 2) result.LabelTarget = parts[2];
                    if (parts.Length > 1)
                    {
                        result.JumpType = parts[1].ToUpperInvariant() switch
                        {
                            "ANY" => JumpType.Any,
                            "ZRO" => JumpType.IfZero,
                            "NEG" => JumpType.IfNegative,
                            _ => JumpType.Any,
                        };
                    }
                    break;
            }

            return result;
        }

        private static void ParseIndexArg(string[] parts, int idx, out int value, out bool isAddress)
        {
            value = 0;
            isAddress = false;
            if (parts.Length <= idx) return;

            string s = parts[idx];
            isAddress = s.StartsWith("[") && s.EndsWith("]");
            if (isAddress) s = s.Substring(1, s.Length - 2);

            int.TryParse(s, out value);
        }

        /// <summary>
        /// 校验每行指令是否合法，是否在当前关卡允许使用。
        /// </summary>
        private static bool ValidateAgainstCurrentLevel(List<ParsedLine> lines)
        {
            foreach (var line in lines)
            {
                OperationConfig cfg = LevelManager.Ins.GetOperationConfig(line.Type);
                if (cfg == null || !cfg.Enabled) return false;

                switch (line.Type)
                {
                    case OperationType.Jump:
                        bool jumpOk = line.JumpType switch
                        {
                            JumpType.Any        => cfg.Jump_Always,
                            JumpType.IfZero     => cfg.Jump_IfZero,
                            JumpType.IfNegative => cfg.Jump_IfNegative,
                            _ => false,
                        };
                        if (!jumpOk) return false;
                        break;

                    case OperationType.Arithmetic:
                        bool arithOk = line.ArithType switch
                        {
                            ArithmeticType.Add or ArithmeticType.Subtract
                                or ArithmeticType.Multiply or ArithmeticType.Divide
                                => cfg.Arithmetic_Basic,
                            ArithmeticType.Inc or ArithmeticType.Dec
                                => cfg.Arithmetic_Unary,
                            ArithmeticType.Equal or ArithmeticType.NotEqual
                                or ArithmeticType.Greater or ArithmeticType.Less
                                or ArithmeticType.GreaterOrEqual or ArithmeticType.LessOrEqual
                                => cfg.Arithmetic_Compare,
                            _ => false,
                        };
                        if (!arithOk) return false;
                        if (line.IsAddress && !cfg.Addressabled) return false;
                        break;

                    case OperationType.CopyTo:
                    case OperationType.CopyFrom:
                        if (line.IsAddress && !cfg.Addressabled) return false;
                        break;
                }
            }

            return true;
        }
        #endregion

        #region 实例化Operation
        private static CodeItem InstantiateFromParsedLine(ParsedLine line, List<CodeItem> prefabs)
        {
            CodeItem item = UnityEngine.Object.Instantiate(prefabs[(int)line.Type]);

            switch (item)
            {
                case CopyToItem copyTo:
                    copyTo.SetIndex(line.Index);
                    copyTo.SetIsAddress(line.IsAddress);
                    break;

                case CopyFromItem copyFrom:
                    copyFrom.SetIndex(line.Index);
                    copyFrom.SetIsAddress(line.IsAddress);
                    break;

                case ArithmeticItem arith:
                    arith.SetArithmeticType(line.ArithType);
                    arith.SetIndex(line.Index);
                    arith.SetIsAddress(line.IsAddress);
                    break;

                case JumpItem jump:
                    jump.SetJumpType(line.JumpType);
                    break;
            }

            item.InitSilently();
            return item;
        }

        private static ArithmeticType ParseArithmeticType(string s)
        {
            return s.ToUpperInvariant() switch
            {
                "ADD" => ArithmeticType.Add,
                "SUB" => ArithmeticType.Subtract,
                "MUL" => ArithmeticType.Multiply,
                "DIV" => ArithmeticType.Divide,
                "INC" => ArithmeticType.Inc,
                "DEC" => ArithmeticType.Dec,
                "EQL" => ArithmeticType.Equal,
                "NEQ" => ArithmeticType.NotEqual,
                "GTR" => ArithmeticType.Greater,
                "LES" => ArithmeticType.Less,
                "GEQ" => ArithmeticType.GreaterOrEqual,
                "LEQ" => ArithmeticType.LessOrEqual,
                _    => ArithmeticType.Add,
            };
        }
        #endregion

        #region Label 名 ↔ 索引
        private static string IndexToLabelName(int index)
        {
            string result = "";
            while (index >= 0)
            {
                result = (char)('a' + index % 26) + result;
                index = index / 26 - 1;
            }
            return result;
        }

        private static int LabelNameToIndex(string name)
        {
            int result = 0;
            for (int i = 0; i < name.Length; i++)
            {
                result = result * 26 + (name[i] - 'a') + 1;
            }
            return result - 1;
        }
        #endregion
    }
}
