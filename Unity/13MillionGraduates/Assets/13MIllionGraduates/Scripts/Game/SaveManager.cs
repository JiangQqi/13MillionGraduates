using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game
{
    public static class SaveManager
    {
        private static string FilePath => System.IO.Path.Combine(Application.persistentDataPath, "save.json");

        [Serializable]
        private class LevelRecord
        {
            public int Id;
            public bool Passed;
            public int BestLines;
            public int BestSteps;
            public string[] CodePages;
            public int LastPageIndex;
            public bool HasReadMoreTalk;
        }

        [Serializable]
        private class Wrapper
        {
            public List<LevelRecord> Levels;
            public bool BgmMuted;
        }

        private static List<LevelRecord> s_Cache;
        private static bool s_BgmMuted;

        private static List<LevelRecord> Cache
        {
            get
            {
                if (s_Cache != null) return s_Cache;

                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var wrapper = JsonUtility.FromJson<Wrapper>(json);
                    s_Cache = wrapper?.Levels ?? new List<LevelRecord>();
                    s_BgmMuted = wrapper != null && wrapper.BgmMuted;
                }
                else
                {
                    s_Cache = new List<LevelRecord>();
                    s_BgmMuted = false;
                }
                return s_Cache;
            }
        }

        #region Code Page
        public static void SavePage(int levelId, int pageIdx, string code)
        {
            var record = Cache.Find(r => r.Id == levelId) ?? AddNew(levelId);
            if (record.CodePages == null || record.CodePages.Length == 0) record.CodePages = new string[3];
            record.CodePages[pageIdx] = code;
            WriteToDisk();
        }

        public static string LoadPage(int levelId, int pageIdx)
        {
            var record = Cache.Find(r => r.Id == levelId) ?? AddNew(levelId);
            if (record.CodePages == null || record.CodePages.Length == 0) record.CodePages = new string[3];
            return (pageIdx < record.CodePages.Length)
                ? record.CodePages[pageIdx] ?? ""
                : "";
        }

        public static void SaveLastPageIndex(int levelId, int pageIdx)
        {
            var record = Cache.Find(r => r.Id == levelId) ?? AddNew(levelId);
            if (record.LastPageIndex == pageIdx) return;

            record.LastPageIndex = pageIdx;
            WriteToDisk();
        }

        public static int LoadLastPageIndex(int levelId)
        {
            var record = Cache.Find(r => r.Id == levelId);
            return record?.LastPageIndex ?? 0;
        }
        #endregion

        #region Record
        public static void TrySaveBest(int levelId, int lines, int steps)
        {
            var record = Cache.Find(r => r.Id == levelId) ?? AddNew(levelId);
            bool improved = false;

            if (record.Passed == false) 
                { record.Passed = true; improved = true; };
            if (record.BestLines == 0 || lines < record.BestLines)
                { record.BestLines = lines; improved = true; }
            if (record.BestSteps == 0 || steps < record.BestSteps)
                { record.BestSteps = steps; improved = true; }

            if (improved) WriteToDisk();
        }

        public static bool IsPassed(int levelId)
        {
            var record = Cache.Find(r => r.Id == levelId);
            return record?.Passed ?? false;
        }

        public static int GetBestLines(int levelId)
        {
            var record = Cache.Find(r => r.Id == levelId);
            return record?.BestLines ?? 0;
        }

        public static int GetBestSteps(int levelId)
        {
            var record = Cache.Find(r => r.Id == levelId);
            return record?.BestSteps ?? 0;
        }
        #endregion

        #region Other
        public static void SaveHasReadMoreTalk(int levelId, bool hasRead)
        {
            var record = Cache.Find(r => r.Id == levelId) ?? AddNew(levelId);
            if (record.HasReadMoreTalk == hasRead) return;

            record.HasReadMoreTalk = hasRead;
            WriteToDisk();
        }

        public static bool HasReadMoreTalk(int levelId)
        {
            var record = Cache.Find(r => r.Id == levelId);
            return record?.HasReadMoreTalk ?? false;
        }
        #endregion

        #region BGM
        public static void SetBgmMuted(bool muted)
        {
            if (s_BgmMuted == muted) return;

            s_BgmMuted = muted;
            WriteToDisk();
        }

        public static bool IsBgmMuted()
        {
            if (!File.Exists(FilePath)) return false;
            _ = Cache; // 触发加载
            return s_BgmMuted;
        }
        #endregion

        public static void ClearAll()
        {
            s_Cache = new List<LevelRecord>();
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }

        #region Inner
        private static LevelRecord AddNew(int levelId)
        {
            var record = new LevelRecord { Id = levelId };
            Cache.Add(record);
            return record;
        }

        private static void WriteToDisk()
        {
            var json = JsonUtility.ToJson(new Wrapper { Levels = Cache, BgmMuted = s_BgmMuted }, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }
        #endregion

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Save/清空所有存档")]
        private static void ClearAllMenu()
        {
            ClearAll();
            UnityEditor.EditorUtility.DisplayDialog("SaveManager", "所有存档已清空", "确定");
        }
#endif
    }
}