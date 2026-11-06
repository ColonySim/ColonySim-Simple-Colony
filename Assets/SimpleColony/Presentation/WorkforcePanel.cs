using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleColony
{
    public sealed class WorkforcePanel : MonoBehaviour
    {
        Func<World> getWorld;
        Text freeWorkers;
        Button recruit;
        readonly Text[] counts = new Text[8];
        readonly Button[] decrease = new Button[8], increase = new Button[8], increaseThree = new Button[8];
        float elapsed;

        public void Initialize(Font font, Func<World> world, string[] names, Action<Profession> inspect = null)
        {
            getWorld = world;
            freeWorkers = Label(transform, font, "", 20, -52, 180, 40);
            recruit = Counter(transform,font,"+1 · 25 еды",208,-52,() => { getWorld().Recruit(Profession.Unemployed); Refresh(); });
            Place(recruit.GetComponent<RectTransform>(),208,-52,150,40);
            Place(recruit.GetComponentInChildren<Text>().rectTransform,30,0,120,40);
            var preview = new GameObject("Unemployed preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(UiIcon));
            preview.transform.SetParent(recruit.transform,false);
            var icon = preview.GetComponent<UiIcon>(); icon.Symbol = "pawn"; icon.color = Palette.Professions[(int)Profession.Unemployed]; icon.raycastTarget = false;
            Place(icon.rectTransform,6,-9,22,22);
            var viewport = new GameObject("Workers viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(transform, false);
            viewport.GetComponent<Image>().color = Color.clear;
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(12, 12); viewportRect.offsetMax = new Vector2(-12, -104);
            var content = new GameObject("Workers", typeof(RectTransform)); content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = Vector2.one; contentRect.pivot = new Vector2(.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 8 * 54); contentRect.anchoredPosition = Vector2.zero;
            var scroll = viewport.GetComponent<ScrollRect>(); scroll.content = contentRect; scroll.viewport = viewportRect; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            for (int i = 0; i < 8; i++)
            {
                int index = i;
                float y = -i * 54;
                var name = Label(content.transform,font,names[i],4,y-12,134,28);
                var hit = new GameObject("Profession information",typeof(RectTransform),typeof(Image),typeof(Button));
                hit.transform.SetParent(content.transform,false);
                Place((RectTransform)hit.transform,4,y-12,134,28);
                var area = hit.GetComponent<Image>(); area.color = Color.clear;
                name.transform.SetParent(hit.transform,false); Place(name.rectTransform,0,0,134,28);
                var info = hit.GetComponent<Button>(); info.targetGraphic = area;
                info.onClick.AddListener(() => inspect?.Invoke((Profession)index));
                counts[i] = Label(content.transform, font, "", 140, y - 12, 30, 28);
                decrease[i] = Counter(content.transform, font, "−1", 176, y, () => Change(index, -1));
                increase[i] = Counter(content.transform, font, "+1", 230, y, () => Change(index, 1));
                increaseThree[i] = Counter(content.transform, font, "+3", 284, y, () => Change(index, 3));
            }
            Refresh();
        }
        void Change(int index, int amount) { getWorld().AdjustProfession((Profession)index, amount); Refresh(); }
        void OnEnable() { if (getWorld != null) Refresh(); }
        void Update() { elapsed += Time.unscaledDeltaTime; if (elapsed >= .2f) { elapsed = 0; Refresh(); } }
        void Refresh()
        {
            var world = getWorld(); if (world == null) return;
            int free = world.ProfessionCount(Profession.Unemployed);
            recruit.interactable = world.CanRecruit;
            freeWorkers.text = "Безработные " + free;
            for (int i = 0; i < 8; i++)
            {
                int count = world.ProfessionCount((Profession)i);
                counts[i].text = count.ToString();
                decrease[i].interactable = !world.Lost && count > 0;
                increase[i].interactable = world.Supports((Profession)i) && !world.Lost && free > 0;
                increaseThree[i].interactable = world.Supports((Profession)i) && !world.Lost && free >= 3;
            }
        }
        static Button Counter(Transform parent, Font font, string text, float x, float y, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform; Place(rect, x, y, 48, 46);
            obj.GetComponent<Image>().color = new Color(.13f, .2f, .26f);
            var label = Label(obj.transform, font, text, 0, 0, 48, 46); label.alignment = TextAnchor.MiddleCenter;
            var button = obj.GetComponent<Button>(); button.targetGraphic = obj.GetComponent<Image>(); button.onClick.AddListener(action); return button;
        }
        static Text Label(Transform parent, Font font, string value, float x, float y, float width, float height)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>(); text.font = font; text.fontSize = 16; text.color = Color.white; text.text = value; text.raycastTarget = false;
            Place(text.rectTransform, x, y, width, height); return text;
        }
        static void Place(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); }
    }
}
