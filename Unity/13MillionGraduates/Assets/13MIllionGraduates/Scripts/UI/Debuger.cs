using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    public class Debuger : MonoBehaviour
    {
        public TextMeshProUGUI text;

        public string[] inputs = new[] { "1", "2", "3", "4", "5", "6", "7", "8" };
        public string[] carpet = new string[25];

        private void Update()
        {
            text.text = CodeExecutor.Ins.State;
        }

        [ContextMenu("CodeSimulatorTest")]
        public void CodeSimulatorTest()
        {
            List<IOperation> operations = NotePad.Ins.CodeManager.Instructions.ToList();

            CodeSimulator sim = new CodeSimulator();
            sim.Run(operations, new List<string>(inputs), carpet);
            Debug.Log($"Inputs: [{string.Join(",", inputs)}] → Outputs: [{string.Join(",", sim.Outputs)}]");
        }
    }
}