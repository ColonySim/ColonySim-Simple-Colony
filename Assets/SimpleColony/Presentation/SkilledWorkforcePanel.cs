using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleColony
{
    // Installed only by SkilledPopulationModule; simple workforce has a different presenter.
    public sealed class SkilledWorkforcePanel : MonoBehaviour
    {
        sealed class ProfessionRow
        {
            public Text Total, Minimum, Priority;
            public Button Inspect, Decrease, Increase, SetPriority;
        }

        Func<World> getWorld;
        readonly ProfessionRow[] rows = new ProfessionRow[8];
        Button recruit;
        float elapsed;

        public void Initialize(Font font, Func<World> world, string[] names, Action<Profession> inspect)
        {
            getWorld = world;
            recruit = MakeButton(transform, font, "+ пешка · 50", 204, -52, 182, 40,
                () => { getWorld().Recruit(Profession.Unemployed); Refresh(); });
            MakeText(transform, font, "Работает / минимум", 12, -52, 186, 40);

            var viewport = new GameObject("Work plan", typeof(RectTransform), typeof(Image),
                typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(transform, false);
            viewport.GetComponent<Image>().color = Color.clear;
            var rect = (RectTransform)viewport.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12, 12);
            rect.offsetMax = new Vector2(-12, -104);

            var content = new GameObject("Professions", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport.transform, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, rows.Length * 84);
            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = rect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            for (int i = 0; i < rows.Length; i++)
            {
                var role = (Profession)i;
                float y = -i * 84;
                var row = new ProfessionRow();
                rows[i] = row;
                row.Inspect = MakeButton(content, font, names[i], 0, y, 140, 76, () => inspect(role));
                row.Total = MakeText(content, font, "", 140, y, 44, 76);
                row.Minimum = MakeText(content, font, "", 232, y, 46, 36);
                row.Decrease = MakeButton(content, font, "−", 184, y, 44, 36, () => ChangeMinimum(role, -1));
                row.Increase = MakeButton(content, font, "+", 282, y, 44, 36, () => ChangeMinimum(role, 1));
                row.SetPriority = MakeButton(content, font, "", 184, y - 40, 142, 36, () => ChangePriority(role));
                row.Priority = row.SetPriority.GetComponentInChildren<Text>();
            }
            Refresh();
        }

        void ChangeMinimum(Profession role, int delta)
        {
            var world = getWorld();
            ((SkilledPawnModel)world.Population).WorkPlan.ChangeMinimum(world, role, delta);
            Refresh();
        }

        void ChangePriority(Profession role)
        {
            var world = getWorld();
            var plan = ((SkilledPawnModel)world.Population).WorkPlan;
            plan.SetPriority(world, role, (plan.Priority(role) + 1) % 6);
            Refresh();
        }

        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed <= .2f) return;
            elapsed = 0;
            Refresh();
        }

        void Refresh()
        {
            var world = getWorld();
            if (world == null) return;
            var plan = ((SkilledPawnModel)world.Population).WorkPlan;
            recruit.interactable = world.CanRecruit;
            int totalMinimum = 0, living = 0;
            for (int i = 0; i < rows.Length; i++) totalMinimum += plan.Minimum((Profession)i);
            foreach (var pawn in world.Pawns) if (pawn.Health > 0) living++;
            for (int i = 0; i < rows.Length; i++)
            {
                var role = (Profession)i;
                var row = rows[i];
                bool enabled = world.Supports(role) && !world.Lost;
                row.Total.text = world.ProfessionCount(role).ToString();
                row.Minimum.text = plan.Minimum(role).ToString();
                row.Priority.text = "Приоритет " + plan.Priority(role);
                row.Decrease.interactable = enabled && plan.Minimum(role) > 0;
                row.Increase.interactable = enabled && totalMinimum < living;
                row.SetPriority.interactable = enabled;
            }
        }

        static Text MakeText(Transform parent, Font font, string value, float x, float y, float w, float h)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            Place((RectTransform)obj.transform, x, y, w, h);
            var text = obj.GetComponent<Text>();
            text.font = font;
            text.fontSize = 16;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        static Button MakeButton(Transform parent, Font font, string value, float x, float y, float w,
            float h, UnityEngine.Events.UnityAction action)
        {
            var obj = new GameObject(value, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            Place((RectTransform)obj.transform, x, y, w, h);
            obj.GetComponent<Image>().color = new Color(.13f, .2f, .26f);
            var button = obj.GetComponent<Button>();
            button.onClick.AddListener(action);
            MakeText(obj.transform, font, value, 0, 0, w, h);
            return button;
        }

        static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }
    }
}
