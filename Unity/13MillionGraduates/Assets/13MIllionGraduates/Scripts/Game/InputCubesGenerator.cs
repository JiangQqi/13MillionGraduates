using UnityEngine;
using System.Collections.Generic;
using System;

namespace Game
{
    public static class InputCubesGenerator
    {
        /// <summary>
        /// 通过关卡ID调用不同生成逻辑
        /// </summary>
        public static List<string> GenerateInputCubes(int id)
        {
            if (id < 0 || id >= s_Generators.Length) return null;
            return s_Generators[id]();
        }

        private static readonly Func<List<string>>[] s_Generators = new Func<List<string>>[]
        {
            Generate_Level00,
            Generate_Level01,
            Generate_Level02,
            Generate_Level03,
            Generate_Level04,
            Generate_Level05,
        };

        private static readonly char[] name = new char[] { '赵', '钱', '孙', '李', '周', '吴', '郑', '王', '冯', '陈', '朱', '吕', '董', '胡', '徐', '牛' };

        /// <summary>
        /// 测试关卡
        /// </summary>
        private static List<string> Generate_Level00()
        {
            List<string> values = new List<string>();

            int cnt = UnityEngine.Random.Range(1, 10);
            for (int i = 0; i < cnt; i++) 
            {
                values.Add(UnityEngine.Random.Range(0, 100).ToString());
            }

            return values;
        }

        private static List<string> Generate_Level01()
        {
            return null;
        }

        private static List<string> Generate_Level02()
        {
            List<string> values = new List<string>();

            var pool = new List<char>(name);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int cnt = UnityEngine.Random.Range(4, 17);
            for (int i = 0; i < cnt; i++) values.Add(pool[i].ToString());

            values.Add("0");
            return values;
        }

        private static List<string> Generate_Level03()
        {
            List<string> values = new List<string>();

            int cnt = UnityEngine.Random.Range(3, 11);
            for (int i = 0; i < cnt; i++)
            {
                values.Add(name[UnityEngine.Random.Range(0, 16)].ToString());
                values.Add(UnityEngine.Random.Range(0, 10).ToString());
            }

            return values;
        }

        private static List<string> Generate_Level04()
        {
            List<string> values = new List<string>();

            var pool = new List<char>(name);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int cnt = UnityEngine.Random.Range(4, 17);
            for (int i = 0; i < cnt; i++) values.Add(pool[i].ToString());

            values.Add("0");
            return values;
        }

        private static List<string> Generate_Level05()
        {
            List<string> values = new List<string>();

            var pool = new List<char>(name);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int cnt = UnityEngine.Random.Range(3, 10);
            for (int i = 0; i < cnt; i++)
            {
                values.Add(pool[i].ToString());
                values.Add(UnityEngine.Random.Range(0, 101).ToString());
            }

            values.Add("0");
            return values;
        }
    }
}