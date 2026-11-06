using UnityEngine;
using UnityEngine.UI;

namespace SimpleColony
{
    public sealed class DescriptionPopup : MonoBehaviour
    {
        RectTransform panel;
        Text label;
        HoldDescription owner;
        Button wikiLink;
        UnityEngine.Events.UnityAction openArticle;
        public void Initialize(Font font)
        {
            panel = (RectTransform)transform;
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            var background = gameObject.AddComponent<Image>(); background.color = new Color(.12f, .18f, .23f); background.raycastTarget = true;
            var obj = new GameObject("Description", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(transform, false);
            label = obj.GetComponent<Text>(); label.font = font; label.fontSize = 18; label.color = Color.white; label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(12, 12); label.rectTransform.offsetMax = new Vector2(-12, -12);
            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            close.transform.SetParent(transform, false);
            var rect = (RectTransform)close.transform; rect.anchorMin = rect.anchorMax = new Vector2(1,1); rect.pivot = new Vector2(1,1); rect.sizeDelta = new Vector2(40,40);
            close.GetComponent<Image>().color = new Color(.2f,.3f,.36f);
            close.GetComponent<Button>().onClick.AddListener(() => gameObject.SetActive(false));
            var glyph = new GameObject("×", typeof(RectTransform), typeof(Text)); glyph.transform.SetParent(close.transform,false);
            var text = glyph.GetComponent<Text>(); text.font = font; text.text = "×"; text.fontSize = 26; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            var g = text.rectTransform; g.anchorMin = Vector2.zero; g.anchorMax = Vector2.one; g.offsetMin = g.offsetMax = Vector2.zero;
            label.rectTransform.offsetMax = new Vector2(-16,-46);
            var link = new GameObject("Wiki link",typeof(RectTransform),typeof(Image),typeof(Button));
            link.transform.SetParent(transform,false);
            var lr=(RectTransform)link.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=new Vector2(1,0);lr.pivot=new Vector2(.5f,0);lr.offsetMin=new Vector2(12,8);lr.offsetMax=new Vector2(-12,48);
            link.GetComponent<Image>().color=new Color(.17f,.29f,.35f);
            wikiLink=link.GetComponent<Button>();wikiLink.onClick.AddListener(()=>openArticle?.Invoke());
            var linkText=new GameObject("Label",typeof(RectTransform),typeof(Text));linkText.transform.SetParent(link.transform,false);
            var lt=linkText.GetComponent<Text>();lt.font=font;lt.fontSize=16;lt.text="Открыть в справочнике →";lt.alignment=TextAnchor.MiddleCenter;lt.raycastTarget=false;
            lt.rectTransform.anchorMin=Vector2.zero;lt.rectTransform.anchorMax=Vector2.one;lt.rectTransform.offsetMin=lt.rectTransform.offsetMax=Vector2.zero;
            link.SetActive(false);
            gameObject.SetActive(false);
        }
        public void Show(HoldDescription source, string text, Vector2 screenPosition)
        {
            owner = source;
            wikiLink.gameObject.SetActive(false);
            label.rectTransform.offsetMin=new Vector2(12,12);
            var parent = (RectTransform)transform.parent;
            float width = Mathf.Min(360, parent.rect.width - 24);
            panel.sizeDelta = new Vector2(width, 100);
            label.text = text;
            float height = Mathf.Min(parent.rect.height - 24, Mathf.Max(64, label.preferredHeight + 60));
            panel.sizeDelta = new Vector2(width, height);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out Vector2 point);
            point.y += height * .5f + 32;
            panel.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x, parent.rect.xMin + width * .5f + 12, parent.rect.xMax - width * .5f - 12),
                Mathf.Clamp(point.y, parent.rect.yMin + height * .5f + 12, parent.rect.yMax - height * .5f - 12));
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }
        public void ShowArticle(string id,string liveDetails,System.Action<string> navigate)
        {
            var article=WikiCatalog.Get(id);
            Show(null,article.Title+"\n\n"+(string.IsNullOrEmpty(liveDetails)?"":liveDetails+"\n\n")+article.Body,new Vector2(Screen.width*.5f,Screen.height*.5f));
            openArticle=()=>{Close();navigate(id);};
            wikiLink.gameObject.SetActive(true);
            label.rectTransform.offsetMin=new Vector2(12,60);
            var parent=(RectTransform)transform.parent;
            panel.sizeDelta=new Vector2(panel.sizeDelta.x,Mathf.Min(parent.rect.height-24,panel.sizeDelta.y+48));
            var point=panel.anchoredPosition;
            point.y=Mathf.Clamp(point.y,parent.rect.yMin+panel.sizeDelta.y*.5f+12,parent.rect.yMax-panel.sizeDelta.y*.5f-12);
            panel.anchoredPosition=point;
        }
        public void Close() { gameObject.SetActive(false); owner = null; }
        public void Hide(HoldDescription source)
        {
            if (owner != source) return;
            gameObject.SetActive(false);
            owner = null;
        }
    }
}
