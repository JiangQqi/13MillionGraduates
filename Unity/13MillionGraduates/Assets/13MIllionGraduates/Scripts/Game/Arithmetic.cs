using System;

namespace Game
{
    public enum ArithmeticType
    {
        Add,
        Subtract,
        Multiply,
        Divide,

        Inc,
        Dec,

        Equal,
        NotEqual,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual,
    }

    public enum ValueType
    {
        Int,
        Bool,
        Char,
        None,
    }

    public static class Arithmetic
    {
        private static readonly char[] s_ASCII = new char[]
        {
            '?',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
            'u', 'v', 'w', 'x', 'y', 'z',
            '赵','钱','孙','李','周','吴','郑','王','冯','陈','朱','吕','董','胡','徐','牛'
        };

        public static ValueType GetType(string value) => value switch
        {
            null or "" => ValueType.None,
            "true" or "false" => ValueType.Bool,
            _ when int.TryParse(value, out _) => ValueType.Int,
            { Length: 1 } when Array.IndexOf(s_ASCII, value[0]) >= 0 => ValueType.Char,
            _ => ValueType.None
        };

        public static ValueType GetTypeAndValue(string value, out object parsedValue)
        {
            var type = GetType(value);
            parsedValue = type switch
            {
                ValueType.Bool => value.Equals("true"),
                ValueType.Int => int.Parse(value),
                ValueType.Char => value[0],
                _ => value
            };
            return type;
        }

        public static ValueType GetResultType(string s1, string s2, ArithmeticType op)
        {
            ValueType t1 = GetType(s1);
            ValueType t2 = GetType(s2);

            return GetResultType(t1, t2, op);
        }

        private static ValueType GetResultType(ValueType t1, ValueType t2, ArithmeticType op)
        {
            if (IsComparisionOpeartor(op)) return ValueType.Bool;
            if (IsUnaryType(op)) return t2;
            if (t1 == ValueType.Char || t2 == ValueType.Char) return ValueType.Char;
            return ValueType.Int;
        }

        public static string Compute(string s1, string s2, ArithmeticType op)
        {
            ValueType t1 = GetTypeAndValue(s1, out object v1);
            ValueType t2 = GetTypeAndValue(s2, out object v2);

            object result = Compute(v1, v2, t1, t2, op);
            return ToString(result);
        }

        private static object Compute(object p1, object p2, ValueType t1, ValueType t2, ArithmeticType op)
        {
            ValueType resultType = GetResultType(t1, t2, op);

            int v1 = ToInt(p1);
            int v2 = ToInt(p2);

            int result = op switch
            {
                ArithmeticType.Add => v1 + v2,
                ArithmeticType.Subtract => v1 - v2,
                ArithmeticType.Multiply => v1 * v2,
                ArithmeticType.Divide => v2 != 0 ? v1 / v2 : DivideZeroError(),
                ArithmeticType.Inc => v2 + 1,
                ArithmeticType.Dec => v2 - 1,
                ArithmeticType.Equal => v1 == v2 ? 1 : 0,
                ArithmeticType.NotEqual => v1 != v2 ? 1 : 0,
                ArithmeticType.Greater => v1 > v2 ? 1 : 0,
                ArithmeticType.Less => v1 < v2 ? 1 : 0,
                ArithmeticType.GreaterOrEqual => v1 >= v2 ? 1 : 0,
                ArithmeticType.LessOrEqual => v1 <= v2 ? 1 : 0,
                _ => throw new ArgumentException($"[Arithmetic] 未知操作类型：{op}"),
            };

            return ToDestinationType(result, resultType);
        }

        private static object ToDestinationType(int value, ValueType type)
        {
            return type switch
            {
                ValueType.Int => value,
                ValueType.Bool => ToBool(value),
                ValueType.Char => ToChar(value),
                _ => value,
            };
        }

        public static int ToInt(object p)
        {
            return p switch
            {
                int i => i,
                bool b => b ? 1 : 0,
                char c => Array.IndexOf(s_ASCII, c),
                _ => throw new ArgumentException($"[Arithmetic] 不支持类型：{p?.GetType()}")
            };
        }

        private static char ToChar(int value) => s_ASCII[(value % s_ASCII.Length + s_ASCII.Length) % s_ASCII.Length];

        private static bool ToBool(int value) => value != 0;

        private static string ToString(object value)
        {
            if (value == null) return "null";

            return value switch
            {
                bool b => b ? "true" : "false",
                _ => value.ToString()
            };
        }

        private static bool IsComparisionOpeartor(ArithmeticType op)
        {
            return op switch
            {
                ArithmeticType.Equal => true,
                ArithmeticType.NotEqual => true,
                ArithmeticType.Greater => true,
                ArithmeticType.Less => true,
                ArithmeticType.GreaterOrEqual => true,
                ArithmeticType.LessOrEqual => true,
                _ => false
            };
        }

        private static int DivideZeroError()
        {
            CodeExecutor.Ins.HandleExecutionError("除以0是极其危险的行为！");
            return 0;
        }

        public static bool IsUnaryType(ArithmeticType op)
        {
            return op == ArithmeticType.Inc || op == ArithmeticType.Dec;
        }

        public static bool EvaluateZero(string x)
        {
            var type = GetType(x);
            return type switch
            {
                ValueType.None => true,
                ValueType.Int => int.Parse(x) == 0,
                ValueType.Bool => x == "false",
                ValueType.Char => Array.IndexOf(s_ASCII, x[0]) == 0,
                _ => false
            };
        }

        public static bool EvaluateNegative(string x)
        {
            return GetType(x) == ValueType.Int && int.Parse(x) < 0;
        }
    }
}