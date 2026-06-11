using UnityEngine;
using System.Collections.Generic;

namespace Game
{
    public class CodeSimulator
    {
        public List<string> Outputs { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool Success { get; private set; }
        public int TotalSteps { get; private set; }

        public void Run(List<IOperation> codes, List<string> inputValues, string[] initialCarpet)
        {
            var labelIndex = new Dictionary<LabelOperation, int>();
            for (int i = 0; i < codes.Count; i++)
                if (codes[i] is LabelOperation label)
                    labelIndex[label] = i;

            string[] carpet = (string[])initialCarpet.Clone();
            Queue<string> inputs = new Queue<string>(inputValues);
            Outputs = new List<string>();
            string holding = null;
            int index = 0;

            while (index < codes.Count) 
            {
                IOperation op = codes[index];

                switch (op)
                {
                    case InBoxOperation:
                        if (holding != null) holding = null;
                        if (inputs.Count == 0) { Success=true ; return; }
                        holding = inputs.Dequeue();
                        index++;
                        break;

                    case OutBoxOperation:
                        if(holding == null) { ErrorMessage = "两手空空不能 OutBox!"; return; }
                        Outputs.Add(holding);
                        holding = null;
                        index++;
                        break;

                    case CopyToOperation c:
                        if (holding == null) { ErrorMessage = "两手空空不能 CopyTo!"; return; }
                        if (!TryResolveAddress(carpet, c.Param2, c.Param3, out int dest)) return;
                        carpet[dest] = holding;
                        index++;
                        break;

                    case CopyFromOperation c:
                        if (!TryResolveAddress(carpet, c.Param2, c.Param3, out int src)) return;
                        if (carpet[src] == null) { ErrorMessage = "不能 CopyFrom 空地毯!"; return; }
                        holding = carpet[src];
                        index++;
                        break;

                    case ArithmeticOperation a:
                        if (!Arithmetic.IsUnaryType(a.Param3) && holding == null) 
                        { ErrorMessage = "两手空空不能进行二元运算!"; return; }
                        if (!TryResolveAddress(carpet, a.Param2, a.Param4, out int opIdx)) return;
                        if (carpet[opIdx] == null) { ErrorMessage = "不能 Arithmetic 空地毯!"; return; }
                        string result = Arithmetic.Compute(holding ?? "0", carpet[opIdx], a.Param3);
                        if (Arithmetic.IsUnaryType(a.Param3)) carpet[opIdx] = result;
                        holding = result;
                        index++;
                        break;

                    case JumpOperation j:
                        bool shouldJump = j.Param2 switch
                        {
                            JumpType.Any => true,
                            JumpType.IfZero => Arithmetic.EvaluateZero(holding),
                            JumpType.IfNegative => Arithmetic.EvaluateNegative(holding),
                            _ => false
                        };
                        if(shouldJump)
                        {
                            if (!labelIndex.TryGetValue(j.Param1, out int targetIndex))
                            { ErrorMessage = "Jump 目标不存在!"; return; }
                            index = targetIndex;
                        }
                        index++;
                        break;

                    case LabelOperation:
                        index++;
                        break;
                }

                if (op is not LabelOperation) TotalSteps++;
            }

            Success = true;
        }

        private bool TryResolveAddress(string[] carpet, int index, bool isAddress, out int actual)
        {
            actual = index;
            if (!isAddress) return true;

            if (carpet[index] == null ||
                Arithmetic.GetTypeAndValue(carpet[index], out object parsed) == ValueType.None)
            {
                ErrorMessage = "地址无效！";
                return false;
            }
            actual = Arithmetic.ToInt(parsed);
            if (actual < 0 || actual >= carpet.Length)
            {
                ErrorMessage = $"地址 {actual} 超出地毯范围！";
                return false;
            }
            return true;
        }
    }
}