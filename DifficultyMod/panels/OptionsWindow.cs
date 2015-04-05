using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DifficultyMod
{
    using ColossalFramework;
    using ColossalFramework.Plugins;
    using ColossalFramework.UI;
    using System;
    using System.Collections.Generic;
    using System.Timers;
    using UnityEngine;
    public class OptionsWindow2 : UIPanel
    {
        UIButton okButton;
        UILabel title;
        UICustomCheckbox2 disastersCheck;
        UILabel disastersLabel;
        UIDropDown difficultySelect;
        UIButton selButton;
        UILabel difficultyLabel;
        UILabel infoLabel;

        public override void Awake()
        {
            title = AddUIComponent<UILabel>();
            disastersCheck = AddUIComponent<UICustomCheckbox2>();
            disastersLabel = AddUIComponent<UILabel>();            
            difficultySelect = AddUIComponent<UIDropDown>();
            selButton = AddUIComponent<UIButton>();
            difficultyLabel = AddUIComponent<UILabel>();
            infoLabel = AddUIComponent<UILabel>();
            okButton = AddUIComponent<UIButton>();
            width = 300;
            height = 200;
            base.Awake();

        }
        public override void Start()
        {
            base.Start();

            relativePosition = new Vector3(396, 58);
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            this.CenterToParent();
            SetupControls();
        }

        public void SetupControls()
        {
            title.text = "Proper Hardness";
            title.relativePosition = new Vector3(15, 15);
            title.textScale = 0.9f;
            title.size = new Vector2(200, 30);

            var x = 15;
            var y = 50;

            disastersCheck.IsChecked = true;
            disastersCheck.relativePosition = new Vector3(x + 100, y);
            disastersCheck.size = new Vector2(13, 13);
            disastersCheck.Show();
            disastersCheck.color = new Color32(185, 221, 254, 255);
            disastersCheck.enabled = true;
            disastersCheck.eventClick += (component, param) =>
            {
                disastersCheck.IsChecked = !disastersCheck.IsChecked;
            };
            disastersLabel.relativePosition = new Vector3(x, y);
            disastersLabel.text = "Disasters";
            disastersLabel.textScale = 0.8f;
            disastersLabel.size = new Vector3(280, 30);
            y += 30;
            difficultySelect.AddItem("Vanilla");
            difficultySelect.AddItem("Normal");
            difficultySelect.AddItem("Hard");
            difficultySelect.AddItem("DwarfFortress");
            difficultySelect.relativePosition = new Vector3(x + 100, y);
            difficultySelect.selectedIndex = 2;
            difficultySelect.size = new Vector2(width - 120, 20);
            difficultySelect.popupColor = new Color32(185, 221, 254, 255);
            difficultySelect.useGradient = true;
            difficultySelect.triggerButton = selButton;
            difficultySelect.eventSelectedIndexChanged += difficultySelect_eventSelectedIndexChanged;
            difficultySelect.listBackground = "ListItemHover";
            difficultySelect.normalBgSprite = "ListItemHover";
            difficultySelect.textScale = 0.8f;
            difficultySelect.itemPadding = new RectOffset(5,0, 3, 5);

            selButton.text = "";
            selButton.size = new Vector2(100, 30);
            selButton.relativePosition = new Vector3(x + 100, y);
            difficultyLabel.relativePosition = new Vector3(x, y);
            difficultyLabel.text = "Difficulty";
            difficultyLabel.textScale = 0.8f;
            difficultyLabel.size = new Vector3(280, 30);

            y += 30;
            infoLabel.relativePosition = new Vector3(x, y);
            infoLabel.text = "Description";
            infoLabel.textScale = 0.7f;            
            infoLabel.size = new Vector3(280,60);
            y += 60;

            okButton.text = "Update";
            okButton.normalBgSprite = "ButtonMenu";
            okButton.hoveredBgSprite = "ButtonMenuHovered";
            okButton.focusedBgSprite = "ButtonMenuFocused";
            okButton.pressedBgSprite = "ButtonMenuPressed";
            okButton.size = new Vector2(100, 30);
            okButton.relativePosition = new Vector3( (width - okButton.size.x) / 2, y);
            okButton.eventClick += okButton_eventClick;
            okButton.textScale = 0.8f;


            height = y + 40;
            UpdateText();
        }

        void difficultySelect_eventSelectedIndexChanged(UIComponent component, int value)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            var diff = (DifficultyLevel)difficultySelect.selectedIndex;
            switch (diff)
            {
                case DifficultyLevel.DwarfFortress:
                    infoLabel.text = "Very hard mode.";
                    break;
                case DifficultyLevel.Hard:
                    infoLabel.text = "Gameplay and cost changes.";
                    break;
                case DifficultyLevel.Normal:
                    infoLabel.text = "Gameplay changes only, no cost changes.";
                    break;

                case DifficultyLevel.Vanilla:
                    infoLabel.text = "No gameplay changes.";
                    break;
            }
        }

        private void okButton_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            var data = SaveData2.saveData;
            data.cityId = Singleton<SimulationManager>.instance.m_metaData.m_gameInstanceIdentifier;
            data.DifficultyLevel = (DifficultyLevel)difficultySelect.selectedIndex;
            data.disastersEnabled = disastersCheck.IsChecked;
            SaveData2.WriteData(data);
            this.Hide();
            LoadingExtension.LoadMod(data);
        }


        public void ShowError()
        {
            this.Show();
            infoLabel.text = "Not all AI classes replaced, please ensure you have no conflicting mods.";
        }
    }

    public class UICustomCheckbox2 : UISprite
    {
        public bool IsChecked { get; set; }

        public override void Start()
        {
            base.Start();
            IsChecked = true;
            spriteName = "AchievementCheckedTrue";
        }

        public override void Update()
        {
            base.Update();
            spriteName = IsChecked ? "AchievementCheckedTrue" : "AchievementCheckedFalse";
        }
    }
}
