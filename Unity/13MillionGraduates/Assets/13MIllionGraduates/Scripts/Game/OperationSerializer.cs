using System;
using System.Collections.Generic;

namespace Game
{
    /// <summary>
    /// 将 文本 解析为 IOperation 列表。
    /// </summary>
    public static class OperationSerializer
    {
        private const string HEADER = "--THIRTEEN MILLIONS GRADUATES--";

        private struct ParsedLine
        {
            public OperationType Type;
            public int Index;
            public bool IsAddress;
            public ArithmeticType ArithType;
            public JumpType JumpType;
            public string LabelTarget;
        }

        public static List<IOperation> Parse(string text)
        {
            List<ParsedLine> lines = ParseLines(text);
            if (lines == null || lines.Count == 0) return null;

            List<IOperation>    operations = new();
            List<LabelOperation> labelOps  = new();
            List<(int jumpIndex, string labelName)> jumpLinks = new();

            for (int i = 0; i < lines.Count; i++)
            {
                ParsedLine line = lines[i];
                IOperation op = CreateOperationFromParsedLine(line);
                if (op == null) continue;

                operations.Add(op);

                if (op is LabelOperation labelOp)
                {
                    labelOps.Add(labelOp);
                }
                else if (op is JumpOperation)
                {
                    jumpLinks.Add((operations.Count - 1, line.LabelTarget));
                }
            }

            // 配对 Jump → Label
            foreach (var (jumpIndex, labelName) in jumpLinks)
            {
                int labelIdx = LabelNameToIndex(labelName);
                if (labelIdx >= 0 && labelIdx < labelOps.Count)
                {
                    ((JumpOperation)operations[jumpIndex]).Param1 = labelOps[labelIdx];
                }
            }

            // 校验：存在未解析的 Jump 目标 → 无效程序
            foreach (var op in operations)
            {
                if (op is JumpOperation jump && jump.Param1 == null)
                    return null;
            }

            return operations;
        }

        private static List<ParsedLine> ParseLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            string[] rawLines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            int startIdx = -1;
            for (int i = 0; i < rawLines.Length; i++)
            {
                if (rawLines[i].Trim() == HEADER) { startIdx = i + 1; break; }
            }
            if (startIdx < 0) return null;

            List<ParsedLine> result = new();
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

            if (line.EndsWith(":"))
            {
                result.Type        = OperationType.Label;
                result.LabelTarget = line.TrimEnd(':');
                return result;
            }

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return result;

            switch (parts[0].ToUpperInvariant())
            {
                case "INBOX":
                    result.Type = OperationType.InBox;
                    break;
                case "OUTBOX":
                    result.Type = OperationType.OutBox;
                    break;
                case "COPYTO":
                    result.Type = OperationType.CopyTo;
                    ParseIndexArg(parts, 1, out result.Index, out result.IsAddress);
                    break;
                case "COPYFROM":
                    result.Type = OperationType.CopyFrom;
                    ParseIndexArg(parts, 1, out result.Index, out result.IsAddress);
                    break;
                case "ARITHMETIC":
                    result.Type      = OperationType.Arithmetic;
                    result.ArithType = parts.Length > 1 ? ParseArithmeticType(parts[1]) : ArithmeticType.Add;
                    ParseIndexArg(parts, 2, out result.Index, out result.IsAddress);
                    break;
                case "JUMP":
                    result.Type = OperationType.Jump;
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

        private static IOperation CreateOperationFromParsedLine(ParsedLine line)
        {
            return line.Type switch
            {
                OperationType.InBox      => new InBoxOperation(),
                OperationType.OutBox     => new OutBoxOperation(),
                OperationType.CopyTo     => new CopyToOperation     { Param1 = 0, Param2 = line.Index, Param3 = line.IsAddress },
                OperationType.CopyFrom   => new CopyFromOperation   { Param1 = 0, Param2 = line.Index, Param3 = line.IsAddress },
                OperationType.Arithmetic => new ArithmeticOperation { Param1 = 0, Param2 = line.Index, Param3 = line.ArithType, Param4 = line.IsAddress },
                OperationType.Jump       => new JumpOperation       { Param2 = line.JumpType },
                OperationType.Label      => new LabelOperation(),
                _ => null,
            };
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

        private static int LabelNameToIndex(string name)
        {
            int result = 0;
            for (int i = 0; i < name.Length; i++)
                result = result * 26 + (name[i] - 'a') + 1;
            return result - 1;
        }
    }
}
