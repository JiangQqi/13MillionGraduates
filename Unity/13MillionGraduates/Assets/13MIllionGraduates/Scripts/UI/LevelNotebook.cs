using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace Game.UI
{
    public class LevelNotebook : MonoBehaviour
    {
        [Header("References")]
        public Sprite[] StateSprites = new Sprite[3];
        public Sprite[] AdvanceSprites = new Sprite[4];

        [Header("Level")]
        public List<LevelConfig> LevelConfigs = new List<LevelConfig>();
        public List<LevelItem> LevelItems = new List<LevelItem>();

        private void Start()
        {
            foreach (var item in LevelItems) 
            {
                int id = item.LevelId;
                LevelConfig cfg = LevelConfigs.Find(c => c.LevelID == id);

                int state = 0;
                if (SaveManager.IsPassed(id)) state = 2;
                else state = 
                        (cfg.RequiredLevels == null || 
                        cfg.RequiredLevels.Count == 0 || 
                        cfg.RequiredLevels.All(requiredId => SaveManager.IsPassed(requiredId))) ? 
                        1 : 0;

                int advance = 0;
                if (SaveManager.GetBestLines(id) > 0 && SaveManager.GetBestLines(id) <= cfg.OptimalLines) advance += 1;
                if (SaveManager.GetBestSteps(id) > 0 && SaveManager.GetBestSteps(id) <= cfg.OptimalSteps) advance += 2;

                item.Init(cfg, state, StateSprites[state], AdvanceSprites[advance], cfg.LevelTitle);
            }
        }
    }
}